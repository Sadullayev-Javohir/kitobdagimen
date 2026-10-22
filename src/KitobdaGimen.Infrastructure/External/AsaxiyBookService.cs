using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace KitobdaGimen.Infrastructure.External;

/// <summary>
/// asaxiy.uz kitoblar katalogini o'qiydi. asaxiy ochiq API bermaydi, lekin har bir
/// ro'yxat va kitob sahifasiga schema.org JSON-LD (ItemList / Product) joylangan —
/// shuni regex bilan ajratib olamiz.
///
/// UMRBOD ISHLASH KAFOLATI: asaxiy.uz Cloudflare ortida turadi va xorijiy data-markaz
/// IP'larini (mas. Hetzner) 403 bilan bloklaydi. Shuning uchun bitta yo'lga tayanmaymiz —
/// bir nechta transportni AVTOMATIK navbat bilan sinaymiz va oxirgi ishlaganini eslab
/// qolamiz (sticky). Transportlar (ustunlik tartibida):
///   1) Worker  — bepul Cloudflare Worker reverse-proxy (Asaxiy:WorkerUrl bo'lsa).
///   2) Proxy   — O'zbekiston IP'sidagi SOCKS/HTTP proksi (Asaxiy:ProxyUrl bo'lsa).
///   3) Direct  — to'g'ridan-to'g'ri (lokal ish yoki bloklanmagan server uchun).
///   4) Jina    — r.jina.ai o'qigich: Cloudflare bloki OSTIDA ham ishlaydi, sozlamasiz,
///                bepul. Bu — har doim mavjud "zaxira" yo'l (safety net).
/// Biror transport ishlamasa, keyingisiga o'tadi. Hech qachon butunlay o'chmaydi.
///
/// Bundan tashqari muvaffaqiyatli qidiruv natijalari keshlab qo'yiladi (Redis, best-effort),
/// shuning uchun qisqa uzilishlar foydalanuvchiga bilinmaydi.
/// </summary>
public class AsaxiyBookService : IAsaxiyBookService
{
    // MUHIM: kanonik `/product/knigi` ishlatamiz, `/uz/product/knigi` EMAS. asaxiy
    // `/uz/...` ni 301 bilan `/product/...` ga yo'naltiradi.
    private const string SearchUrl = "https://asaxiy.uz/product/knigi?key=";

    // HTML ichidagi barcha JSON-LD script bloklarini ajratib olish.
    private static readonly Regex JsonLdRegex = new(
        @"<script[^>]*type=[""']application/ld\+json[""'][^>]*>(?<json>[\s\S]*?)</script>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Zaxira regex: agar JSON-LD script'i parse bo'lmasa, escaped qo'shtirnoqlar bilan ham ishlaydi.
    private static readonly Regex ItemRegex = new(
        @"""position"":\s*\d+,\s*""url"":\s*""(?<url>[^""]+)"",\s*""name"":\s*""(?<name>(?:\\.|[^""\\])*)"",\s*""image"":\s*""(?<image>[^""]+)""",
        RegexOptions.Compiled);

    // Product sahifasidagi zaxira JSON-LD regex.
    private static readonly Regex ProductRegex = new(
        @"""@type"":\s*""Product"".*?""name"":\s*""(?<name>(?:\\.|[^""\\])*)"".*?""image"":\s*""(?<image>[^""]+)""",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // Xususiyatlar jadvalidagi "Betlar soni" qatori (ixtiyoriy <br> va probellarni qo'llab-quvvatlaydi).
    private static readonly Regex PagesRegex = new(
        @"Betlar soni\s*</td>\s*<td[^>]*>\s*(?<pages>\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Kesh: muvaffaqiyatli (bo'sh bo'lmagan) qidiruv natijalari 24 soat saqlanadi.
    private static readonly TimeSpan SearchCacheTtl = TimeSpan.FromHours(24);
    private const string CacheKeyPrefix = "asaxiy:search:";

    private readonly IHttpClientFactory _httpFactory;
    private readonly AsaxiyTransportState _state;
    private readonly ICacheService _cache;
    private readonly ILogger<AsaxiyBookService> _logger;
    private readonly IReadOnlyList<Transport> _transports;

    public AsaxiyBookService(
        IHttpClientFactory httpFactory,
        AsaxiyTransportState state,
        AsaxiyOptions options,
        ICacheService cache,
        ILogger<AsaxiyBookService> logger)
    {
        _httpFactory = httpFactory;
        _state = state;
        _cache = cache;
        _logger = logger;
        _transports = BuildTransports(options);
    }

    // Oddiy transportlar uchun tez-uziladigan urinish timeout'i (soniya). Osilib qolgan
    // uy proksisi/worker butun failover zanjirini bloklab qo'ymasligi uchun qisqa.
    private const int FastTimeoutSeconds = 8;
    // Ochiq o'qigichlar (Jina/AllOrigins) — sekinroq, shuning uchun uzunroq timeout.
    private const int ReaderTimeoutSeconds = 25;
    // Butun failover zanjiri uchun umumiy vaqt byudjeti — bir nechta transport osilib
    // qolsa ham so'rov bundan oshmaydi.
    private const int OverallDeadlineSeconds = 35;

    /// <summary>Berilgan sozlamalar asosida transportlar ro'yxatini (ustunlik tartibida) tuzadi.</summary>
    private static IReadOnlyList<Transport> BuildTransports(AsaxiyOptions options)
    {
        var list = new List<Transport>();

        if (options.HasWorker)
        {
            var worker = options.WorkerUrl!.TrimEnd('/');
            var secret = options.WorkerSecret;
            list.Add(new Transport(
                Name: "worker",
                ClientName: AsaxiyClients.Direct,
                BuildUrl: url => $"{worker}?url={Uri.EscapeDataString(url)}",
                Configure: req =>
                {
                    if (!string.IsNullOrEmpty(secret))
                    {
                        req.Headers.TryAddWithoutValidation("X-Proxy-Secret", secret);
                    }
                },
                SupportsBinary: true,
                TimeoutSeconds: FastTimeoutSeconds));
        }

        if (options.HasProxy)
        {
            list.Add(new Transport("proxy", AsaxiyClients.Proxy, url => url, null,
                SupportsBinary: true, TimeoutSeconds: FastTimeoutSeconds));
        }

        list.Add(new Transport("direct", AsaxiyClients.Direct, url => url, null,
            SupportsBinary: true, TimeoutSeconds: FastTimeoutSeconds));

        // Jina Reader — Cloudflare bloki ostida ham ishlaydigan, sozlamasiz zaxira yo'l.
        // r.jina.ai maqsad URL'ni o'z infratuzilmasidan (bloklanmagan IP) o'qib, HTML
        // qaytaradi. Rasm (binary) qaytara olmaydi — shuning uchun muqova yuklashda ishlatilmaydi.
        list.Add(new Transport(
            Name: "jina",
            ClientName: AsaxiyClients.Jina,
            BuildUrl: url => "https://r.jina.ai/" + url,
            Configure: req => req.Headers.TryAddWithoutValidation("X-Return-Format", "html"),
            SupportsBinary: false,
            TimeoutSeconds: ReaderTimeoutSeconds));

        // AllOrigins — ikkinchi mustaqil ochiq proksi (zaxiraning zaxirasi). Jina o'chib qolsa
        // yoki rate-limit qilsa ham qidiruv shu orqali ishlab turadi. `/raw` maqsad javobini
        // o'zgartirmasdan qaytaradi, shuning uchun ichidagi JSON-LD saqlanadi. Bloklanmagan IP'dan
        // so'raydi. Binary (muqova) ishonchsiz bo'lgani uchun faqat HTML uchun ishlatamiz.
        list.Add(new Transport(
            Name: "allorigins",
            ClientName: AsaxiyClients.Jina,
            BuildUrl: url => "https://api.allorigins.win/raw?url=" + Uri.EscapeDataString(url),
            Configure: null,
            SupportsBinary: false,
            // AllOrigins ba'zan 522 bilan sekin uziladi — qisqaroq timeout beramiz, u zanjirning
            // qolgan byudjetini yeb qo'ymasin. Asosiy zaxira baribir Jina.
            TimeoutSeconds: 12));

        return list;
    }

    public async Task<IReadOnlyList<AsaxiyBookResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        query = TextNormalizer.NormalizeApostrophes(query ?? string.Empty).Trim();
        if (query.Length < 2)
        {
            return Array.Empty<AsaxiyBookResult>();
        }

        var cacheKey = CacheKeyPrefix + query.ToLowerInvariant();

        // 1) Kesh — tez va uzilishga chidamli. Faqat bo'sh bo'lmagan natijalar keshlanadi.
        var cached = await _cache.GetAsync<List<AsaxiyBookResult>>(cacheKey, ct);
        if (cached is { Count: > 0 })
        {
            return cached;
        }

        // 2) Jonli qidiruv (transport failover bilan).
        var results = await SearchLiveAsync(query, ct);

        if (results.Count > 0)
        {
            await _cache.SetAsync(cacheKey, results.ToList(), SearchCacheTtl, ct);
        }

        return results;
    }

    /// <summary>Keshdan foydalanmasdan, jonli qidiruv (transportlarni navbat bilan sinaydi).</summary>
    private async Task<IReadOnlyList<AsaxiyBookResult>> SearchLiveAsync(string query, CancellationToken ct)
    {
        var html = await FetchHtmlAsync(SearchUrl + Uri.EscapeDataString(query), IsValidAsaxiyHtml, ct);
        if (html is null)
        {
            _logger.LogWarning("asaxiy qidiruvi barcha transportlarda muvaffaqiyatsiz: {Query}", query);
            return Array.Empty<AsaxiyBookResult>();
        }

        return ParseSearch(html);
    }

    private static IReadOnlyList<AsaxiyBookResult> ParseSearch(string html)
    {
        var results = new List<AsaxiyBookResult>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1) Asosiy yo'l: JSON-LD script bloklaridan "ItemList" ni JsonDocument bilan xavfsiz o'qish
        foreach (Match match in JsonLdRegex.Matches(html))
        {
            var json = match.Groups["json"].Value;
            if (!json.Contains("ItemList", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("itemListElement", out var items) || items.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var item in items.EnumerateArray())
                {
                    var rawUrl = item.TryGetProperty("url", out var u) ? u.GetString() : null;
                    var rawName = item.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var rawImage = item.TryGetProperty("image", out var img) ? img.GetString() : null;

                    if (string.IsNullOrWhiteSpace(rawUrl) || string.IsNullOrWhiteSpace(rawName))
                    {
                        continue;
                    }

                    var name = Decode(rawName);
                    var (author, title) = SplitName(name);
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        continue;
                    }

                    var url = NormalizeProductUrl(Decode(rawUrl));
                    if (!seen.Add(url))
                    {
                        continue;
                    }

                    results.Add(new AsaxiyBookResult
                    {
                        Title = Truncate(title, 100),
                        Author = Truncate(author, 100),
                        CoverUrl = !string.IsNullOrWhiteSpace(rawImage) ? Decode(rawImage) : string.Empty,
                        Url = url
                    });
                }
            }
            catch
            {
                // Boshqa JSON-LD bloklarni tekshirishda davom etamiz
            }
        }

        // 2) Zaxira yo'l: agar birorta ham kitob topilmagan bo'lsa, regex orqali urinib ko'ramiz
        if (results.Count == 0)
        {
            foreach (Match m in ItemRegex.Matches(html))
            {
                var name = Decode(m.Groups["name"].Value);
                var (author, title) = SplitName(name);

                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                var url = NormalizeProductUrl(Decode(m.Groups["url"].Value));
                if (!seen.Add(url))
                {
                    continue;
                }

                results.Add(new AsaxiyBookResult
                {
                    Title = Truncate(title, 100),
                    Author = Truncate(author, 100),
                    CoverUrl = Decode(m.Groups["image"].Value),
                    Url = url
                });
            }
        }

        return results;
    }

    public async Task<AsaxiyBookDetails?> GetDetailsAsync(string productUrl, CancellationToken ct = default)
    {
        if (!IsAllowedAsaxiyUrl(productUrl))
        {
            return null;
        }

        // 301 redirectni oldini olish uchun kanonik `/product/...` URL'ni so'raymiz.
        productUrl = NormalizeProductUrl(productUrl);

        var html = await FetchHtmlAsync(productUrl, IsValidAsaxiyHtml, ct);
        if (html is null)
        {
            _logger.LogWarning("asaxiy kitob sahifasi barcha transportlarda olinmadi: {Url}", productUrl);
            return null;
        }

        string? rawName = null;
        string? rawImage = null;

        // 1) JSON-LD orqali Product ma'lumotlarini olish
        foreach (Match match in JsonLdRegex.Matches(html))
        {
            var json = match.Groups["json"].Value;
            if (!json.Contains("Product", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("@type", out var typeEl) &&
                    string.Equals(typeEl.GetString(), "Product", StringComparison.OrdinalIgnoreCase))
                {
                    if (root.TryGetProperty("name", out var n)) rawName = n.GetString();
                    if (root.TryGetProperty("image", out var img)) rawImage = img.GetString();
                    if (!string.IsNullOrWhiteSpace(rawName)) break;
                }
            }
            catch
            {
                // Continue
            }
        }

        // 2) Zaxira regex
        if (string.IsNullOrWhiteSpace(rawName))
        {
            var product = ProductRegex.Match(html);
            if (product.Success)
            {
                rawName = product.Groups["name"].Value;
                rawImage = product.Groups["image"].Value;
            }
        }

        if (string.IsNullOrWhiteSpace(rawName))
        {
            return null;
        }

        var name = Decode(rawName);
        var (author, title) = SplitName(name);
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var pagesMatch = PagesRegex.Match(html);
        var totalPages = pagesMatch.Success
            ? int.TryParse(pagesMatch.Groups["pages"].Value.Replace(" ", ""), out var p) ? p : 0
            : 0;

        return new AsaxiyBookDetails
        {
            Title = Truncate(title, 100),
            Author = Truncate(author, 100),
            TotalPages = totalPages,
            CoverUrl = !string.IsNullOrWhiteSpace(rawImage) ? Decode(rawImage) : string.Empty
        };
    }

    public async Task<byte[]?> DownloadCoverAsync(string coverUrl, CancellationToken ct = default)
    {
        if (!IsAllowedAsaxiyUrl(coverUrl))
        {
            return null;
        }

        return await FetchBytesAsync(coverUrl, ct);
    }

    public async Task<AsaxiyHealthResult> RefreshAsync(CancellationToken ct = default)
    {
        // Sticky holatni tiklaymiz — transportlar boshidan (Worker → ... → Jina) sinaladi.
        _state.Reset();

        // Jonli, keshsiz sinov qidiruvi. "roman" — asaxiy katalogida ko'p natija beradigan so'z.
        const string probe = "roman";
        var html = await FetchHtmlAsync(SearchUrl + Uri.EscapeDataString(probe), IsValidAsaxiyHtml, ct);

        if (html is null)
        {
            return new AsaxiyHealthResult
            {
                Healthy = false,
                Transport = string.Empty,
                Count = 0,
                Message = "Kitob qidiruvini hozircha tiklab bo'lmadi. Biroz vaqtdan keyin yana urinib ko'ring."
            };
        }

        var results = ParseSearch(html);
        var transport = _transports[Math.Min(_state.PreferredIndex, _transports.Count - 1)].Name;

        // Tekshiruv natijasini ham keshga yozamiz — bekorga ketmasin.
        if (results.Count > 0)
        {
            await _cache.SetAsync(CacheKeyPrefix + probe, results.ToList(), SearchCacheTtl, ct);
        }

        return new AsaxiyHealthResult
        {
            Healthy = results.Count > 0,
            Transport = transport,
            Count = results.Count,
            Message = results.Count > 0
                ? "Kitob qidiruvi ishlayapti ✓"
                : "Ulanish bor, lekin natija topilmadi. Yana urinib ko'ring."
        };
    }

    /// <summary>
    /// Berilgan asaxiy URL'ni transportlar bo'yicha navbat bilan (oxirgi ishlaganidan boshlab)
    /// o'qiydi. <paramref name="isValid"/> — javob HTML'i kutilgan JSON-LD'ni o'z ichiga
    /// olishini tekshiradi (Cloudflare blok sahifasi kabi yaroqsiz javobni rad etadi).
    /// Hech biri ishlamasa null qaytaradi.
    /// </summary>
    private async Task<string?> FetchHtmlAsync(string asaxiyUrl, Func<string, bool> isValid, CancellationToken ct)
    {
        var count = _transports.Count;
        var start = Math.Min(_state.PreferredIndex, count - 1);

        // Butun zanjir uchun umumiy vaqt byudjeti — bir nechta transport osilib qolsa ham
        // so'rov cheksiz kutib qolmaydi (foydalanuvchi/gateway timeout'iga tushmaydi).
        using var overall = CancellationTokenSource.CreateLinkedTokenSource(ct);
        overall.CancelAfter(TimeSpan.FromSeconds(OverallDeadlineSeconds));

        for (var i = 0; i < count; i++)
        {
            if (overall.IsCancellationRequested)
            {
                break;
            }

            var idx = (start + i) % count;
            var transport = _transports[idx];

            // Har bir urinish uchun alohida, tez-uziladigan timeout. Shu tufayli osilib qolgan
            // transport (mas. o'chgan uy proksisi) keyingisiga o'tishni bloklab qo'ymaydi.
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(overall.Token);
            attempt.CancelAfter(TimeSpan.FromSeconds(transport.TimeoutSeconds));
            try
            {
                var client = _httpFactory.CreateClient(transport.ClientName);
                using var req = new HttpRequestMessage(HttpMethod.Get, transport.BuildUrl(asaxiyUrl));
                transport.Configure?.Invoke(req);

                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead, attempt.Token);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogDebug("asaxiy transport {Transport}: {Status}", transport.Name, (int)resp.StatusCode);
                    continue;
                }

                var html = await resp.Content.ReadAsStringAsync(attempt.Token);
                if (!isValid(html))
                {
                    _logger.LogDebug("asaxiy transport {Transport}: yaroqsiz javob (blok?)", transport.Name);
                    continue;
                }

                _state.Remember(idx);
                return html;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Faqat CHAQIRUVCHI bekor qilgan bo'lsa uzatamiz. Bizning per-urinish yoki
                // umumiy timeout bo'lsa — keyingi transportga o'tamiz.
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "asaxiy transport {Transport} xato berdi", transport.Name);
            }
        }

        return null;
    }

    /// <summary>Muqova rasmini transportlar bo'yicha (binary'ni qo'llab-quvvatlaydiganlar) yuklaydi.</summary>
    private async Task<byte[]?> FetchBytesAsync(string asaxiyUrl, CancellationToken ct)
    {
        var count = _transports.Count;
        var start = Math.Min(_state.PreferredIndex, count - 1);

        using var overall = CancellationTokenSource.CreateLinkedTokenSource(ct);
        overall.CancelAfter(TimeSpan.FromSeconds(OverallDeadlineSeconds));

        for (var i = 0; i < count; i++)
        {
            if (overall.IsCancellationRequested)
            {
                break;
            }

            var idx = (start + i) % count;
            var transport = _transports[idx];
            if (!transport.SupportsBinary)
            {
                continue;
            }

            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(overall.Token);
            attempt.CancelAfter(TimeSpan.FromSeconds(transport.TimeoutSeconds));
            try
            {
                var client = _httpFactory.CreateClient(transport.ClientName);
                using var req = new HttpRequestMessage(HttpMethod.Get, transport.BuildUrl(asaxiyUrl));
                transport.Configure?.Invoke(req);

                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead, attempt.Token);
                if (!resp.IsSuccessStatusCode)
                {
                    continue;
                }

                var bytes = await resp.Content.ReadAsByteArrayAsync(attempt.Token);
                if (LooksLikeImage(bytes))
                {
                    return bytes;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "asaxiy muqova transport {Transport} xato berdi", transport.Name);
            }
        }

        _logger.LogWarning("asaxiy muqova rasmi barcha transportlarda yuklanmadi: {Url}", asaxiyUrl);
        return null;
    }

    /// <summary>Baytlar rasm ekanini tekshiradi (HTML blok/xato sahifasini rad etish uchun).</summary>
    private static bool LooksLikeImage(byte[] bytes)
    {
        if (bytes.Length < 64)
        {
            return false;
        }

        // JPEG (FF D8), PNG (89 50 4E 47), GIF (47 49 46), WEBP (RIFF....WEBP), BMP (42 4D).
        if (bytes[0] == 0xFF && bytes[1] == 0xD8) return true;
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return true;
        if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return true;
        if (bytes[0] == 0x42 && bytes[1] == 0x4D) return true;
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
            && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) return true;

        return false;
    }

    /// <summary>
    /// asaxiy URL'ni kanonik ko'rinishga keltiradi: `/uz/product/...` -> `/product/...`.
    /// </summary>
    private static string NormalizeProductUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        return Regex.Replace(url, @"(https?://[^/]+)/(uz|ru|en)/product/", "$1/product/",
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// HTML javobi haqiqiy asaxiy sahifasi ekanini (Cloudflare challenge/blok sahifasi emasligini) tekshiradi.
    /// </summary>
    private static bool IsValidAsaxiyHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html) || html.Length < 30)
        {
            return false;
        }

        // Cloudflare blok/challenge sahifasini aniqlash
        if (html.Contains("<title>Just a moment...</title>", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("cf-mitigated", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("Enable JavaScript and cookies to continue", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("Attention Required! | Cloudflare", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("challenge-platform", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    /// <summary>"Muallif: Sarlavha" ko'rinishidagi nomni muallif va sarlavhaga ajratadi.</summary>
    private static (string Author, string Title) SplitName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (string.Empty, string.Empty);
        }

        name = name.Trim();

        var idx = name.IndexOf(':');
        if (idx > 0 && idx < name.Length - 1)
        {
            var author = UnwrapQuotes(name[..idx]);
            var title = UnwrapQuotes(name[(idx + 1)..]);
            if (!string.IsNullOrEmpty(author) && !string.IsNullOrEmpty(title))
            {
                return (author, title);
            }
        }

        // Chiziqcha orqali ajratilgan bo'lsa (masalan: "Muallif - Sarlavha")
        var dashIdx = name.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIdx > 0 && dashIdx < name.Length - 3)
        {
            var p1 = UnwrapQuotes(name[..dashIdx]);
            var p2 = UnwrapQuotes(name[(dashIdx + 3)..]);
            if (!string.IsNullOrEmpty(p1) && !string.IsNullOrEmpty(p2))
            {
                return (p1, p2);
            }
        }

        // Agar ikki nuqta yoki chiziqcha bo'lmasa, butun nomni sarlavha deb olamiz
        return ("Noma'lum muallif", UnwrapQuotes(name));
    }

    /// <summary>Matn butunligicha qo'shtirnoqqa olingan bo'lsa, ularni olib tashlaydi.</summary>
    private static string UnwrapQuotes(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 &&
            ((s[0] == '"' && s[^1] == '"') ||
             (s[0] == '«' && s[^1] == '»') ||
             (s[0] == '“' && s[^1] == '”')))
        {
            return s[1..^1].Trim();
        }
        return s;
    }

    /// <summary>JSON ichidagi \/ va HTML entity'larni (&rsquo;, &amp; ...) tozalaydi.</summary>
    private static string Decode(string value) =>
        HttpUtility.HtmlDecode(value.Replace("\\/", "/")).Trim();

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max].Trim();

    /// <summary>SSRF himoyasi: faqat asaxiy.uz domenlariga so'rov yuborishga ruxsat.</summary>
    private static bool IsAllowedAsaxiyUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        return host == "asaxiy.uz" || host.EndsWith(".asaxiy.uz");
    }

    /// <summary>Bitta transport yo'li: HTTP klient nomi + URL o'zgartirgich + so'rov sozlagichi.</summary>
    /// <param name="TimeoutSeconds">
    /// Shu transport uchun urinish timeout'i. Failover TEZ bo'lishi uchun oddiy transportlar
    /// qisqa (osilib qolgan uy proksisi butun zanjirni bloklamasin), o'qigichlar (Jina/AllOrigins)
    /// sekinroq bo'lgani uchun uzunroq.
    /// </param>
    private sealed record Transport(
        string Name,
        string ClientName,
        Func<string, string> BuildUrl,
        Action<HttpRequestMessage>? Configure,
        bool SupportsBinary,
        int TimeoutSeconds);
}

/// <summary>Named HTTP client nomlari (DI'da ro'yxatga olinadi).</summary>
public static class AsaxiyClients
{
    public const string Direct = "asaxiy-direct";
    public const string Proxy = "asaxiy-proxy";
    public const string Jina = "asaxiy-jina";
}
