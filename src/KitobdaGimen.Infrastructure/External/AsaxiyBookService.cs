using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using KitobdaGimen.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace KitobdaGimen.Infrastructure.External;

/// <summary>
/// asaxiy.uz kitoblar katalogini o'qiydi. asaxiy ochiq API bermaydi, lekin har bir
/// ro'yxat va kitob sahifasiga schema.org JSON-LD (ItemList / Product) joylangan —
/// shuni regex bilan ajratib olamiz. HTML strukturasi o'zgarsa, faqat shu fayl yangilanadi.
/// </summary>
public class AsaxiyBookService : IAsaxiyBookService
{
    private const string SearchUrl = "https://asaxiy.uz/uz/product/knigi?key=";

    // JSON-LD ItemList elementi: url + name ("Muallif: Sarlavha") + image (muqova).
    private static readonly Regex ItemRegex = new(
        "\"position\":\\s*\\d+,\\s*\"url\":\\s*\"(?<url>[^\"]+)\",\\s*\"name\":\\s*\"(?<name>[^\"]+)\",\\s*\"image\":\\s*\"(?<image>[^\"]+)\"",
        RegexOptions.Compiled);

    // Product sahifasidagi JSON-LD: name + image.
    private static readonly Regex ProductRegex = new(
        "\"@type\":\\s*\"Product\".*?\"name\":\\s*\"(?<name>[^\"]+)\".*?\"image\":\\s*\"(?<image>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // Xususiyatlar jadvalidagi "Betlar soni" qatori.
    private static readonly Regex PagesRegex = new(
        "Betlar soni\\s*</td>\\s*<td[^>]*>\\s*(?<pages>[\\d ]+)",
        RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly ILogger<AsaxiyBookService> _logger;

    public AsaxiyBookService(HttpClient http, ILogger<AsaxiyBookService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AsaxiyBookResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        query = (query ?? string.Empty).Trim();
        if (query.Length < 2)
        {
            return Array.Empty<AsaxiyBookResult>();
        }

        string html;
        try
        {
            html = await _http.GetStringAsync(SearchUrl + Uri.EscapeDataString(query), ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "asaxiy qidiruvi muvaffaqiyatsiz: {Query}", query);
            return Array.Empty<AsaxiyBookResult>();
        }

        var results = new List<AsaxiyBookResult>();
        var seen = new HashSet<string>();

        foreach (Match m in ItemRegex.Matches(html))
        {
            var name = Decode(m.Groups["name"].Value);
            var (author, title) = SplitName(name);

            // Muallifsiz yozuvlar — odatda aksiya/to'plamlar; kitob katalogiga to'g'ri kelmaydi.
            if (string.IsNullOrEmpty(author) || string.IsNullOrEmpty(title))
            {
                continue;
            }

            var url = Decode(m.Groups["url"].Value);
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

        return results;
    }

    public async Task<AsaxiyBookDetails?> GetDetailsAsync(string productUrl, CancellationToken ct = default)
    {
        if (!IsAllowedAsaxiyUrl(productUrl))
        {
            return null;
        }

        string html;
        try
        {
            html = await _http.GetStringAsync(productUrl, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "asaxiy kitob sahifasi olinmadi: {Url}", productUrl);
            return null;
        }

        var product = ProductRegex.Match(html);
        if (!product.Success)
        {
            return null;
        }

        var name = Decode(product.Groups["name"].Value);
        var (author, title) = SplitName(name);
        if (string.IsNullOrEmpty(author) || string.IsNullOrEmpty(title))
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
            CoverUrl = Decode(product.Groups["image"].Value)
        };
    }

    public async Task<byte[]?> DownloadCoverAsync(string coverUrl, CancellationToken ct = default)
    {
        if (!IsAllowedAsaxiyUrl(coverUrl))
        {
            return null;
        }

        try
        {
            return await _http.GetByteArrayAsync(coverUrl, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "asaxiy muqova rasmi yuklanmadi: {Url}", coverUrl);
            return null;
        }
    }

    /// <summary>"Muallif: Sarlavha" ko'rinishidagi nomni muallif va sarlavhaga ajratadi.</summary>
    private static (string Author, string Title) SplitName(string name)
    {
        var idx = name.IndexOf(':');
        if (idx <= 0 || idx >= name.Length - 1)
        {
            return (string.Empty, string.Empty);
        }

        var author = name[..idx].Trim();
        var title = name[(idx + 1)..].Trim();
        return (author, title);
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
}
