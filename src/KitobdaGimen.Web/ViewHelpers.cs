using KitobdaGimen.Application.Common;
using Microsoft.AspNetCore.Html;

namespace KitobdaGimen.Web;

/// <summary>Small view-side formatting helpers (Uzbek UI strings).</summary>
public static class ViewHelpers
{
    /// <summary>
    /// The single founder account. Only this username carries the "Asoschi" badge,
    /// shown to every visitor wherever the name appears (profil/feed/chat). Mijoz tomoni
    /// (chat qidiruvi) uchun ayni shu qiymat <c>site.js</c> dagi <c>FOUNDER_USERNAME</c> bilan mos.
    /// </summary>
    public const string FounderUsername = "javohirsadullayev";

    /// <summary>True when the username belongs to the platform founder (regdan mustaqil, katta-kichik harfsiz).</summary>
    public static bool IsFounder(string? username)
        => !string.IsNullOrWhiteSpace(username)
           && string.Equals(username.Trim(), FounderUsername, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Renders the gold "Asoschi" badge if the username is the founder; otherwise nothing.
    /// `@Html.Raw(ViewHelpers.FounderBadge(username))` shaklida ism yonida ishlatiladi.
    /// </summary>
    public static IHtmlContent FounderBadge(string? username)
        => IsFounder(username)
            ? new HtmlString("<span class=\"badge founder-badge\" title=\"kitobdagimen.uz asoschisi\"><span class=\"material-symbols-outlined\">verified</span>Asoschi</span>")
            : HtmlString.Empty;

    /// <summary>Compact icon-only founder badge for tight spots (mas. chat suhbatlar ro'yxati).</summary>
    public static IHtmlContent FounderBadgeMini(string? username)
        => IsFounder(username)
            ? new HtmlString("<span class=\"founder-badge-mini material-symbols-outlined\" title=\"kitobdagimen.uz asoschisi\">verified</span>")
            : HtmlString.Empty;

    /// <summary>
    /// Kitob tashqi manbadan (mas. asaxiy.uz) olingan bo'lsa, kichik "Manba: ..." kreditini
    /// chiqaradi (asaxiy.uz uchun havola bilan); aks holda hech nima. Manba nomi attribution
    /// uchun ko'rsatiladi. `@Html.Raw(ViewHelpers.SourceCredit(Model.Book.Source))` shaklida.
    /// </summary>
    public static IHtmlContent SourceCredit(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return HtmlString.Empty;
        }

        var name = System.Net.WebUtility.HtmlEncode(source.Trim());
        var inner = string.Equals(source.Trim(), "asaxiy.uz", StringComparison.OrdinalIgnoreCase)
            ? $"<a href=\"https://asaxiy.uz\" target=\"_blank\" rel=\"noopener nofollow\">{name}</a>"
            : name;

        return new HtmlString($"<span class=\"book-source\">Manba: {inner}</span>");
    }

    /// <summary>
    /// Renders post text as a safe HTML subset (qalin/kursiv/tagchiziq/marker).
    /// `Html.Raw(RichText(...))` shaklida ishlatiladi. Sanitizer idempotent va eski
    /// (sanitize qilinmagan) postlarni ham xavfsiz encode qiladi.
    /// </summary>
    public static IHtmlContent RichText(string? text)
        => new HtmlString(RichTextSanitizer.Sanitize(text));

    /// <summary>Returns a short relative time in Uzbek, e.g. "5 daqiqa oldin".</summary>
    public static string RelativeTime(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;

        if (span.TotalSeconds < 60) return "hozirgina";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} daqiqa oldin";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} soat oldin";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} kun oldin";
        if (span.TotalDays < 30) return $"{(int)(span.TotalDays / 7)} hafta oldin";

        return utc.ToLocalTime().ToString("dd.MM.yyyy");
    }

    /// <summary>First letter of a name, upper-cased, for avatar fallbacks.</summary>
    public static string Initial(string? name)
        => string.IsNullOrWhiteSpace(name) ? "?" : name.Trim()[..1].ToUpper();

    /// <summary>Exact local date+time (O'zbekiston, UTC+5) with seconds, e.g. "29.06.2026 18:05:23".
    /// Returns "—" for null (never seen).</summary>
    public static string ExactLocal(DateTime? utc)
        => utc is null ? "—" : utc.Value.AddHours(5).ToString("dd.MM.yyyy HH:mm:ss");

    /// <summary>Presence text for a chat header/card: "online" or "oxirgi marta ...".</summary>
    public static string Presence(bool isOnline, DateTime? lastSeenAt)
    {
        if (isOnline) return "online";
        if (lastSeenAt is null) return "oflayn";
        return $"oxirgi marta {LastSeen(lastSeenAt.Value)}";
    }

    /// <summary>
    /// Aniq oxirgi faollik vaqti: bugun bo'lsa soatni ("09:10:12 da"), kecha bo'lsa
    /// "kecha 09:10:12 da", undan oldin bo'lsa sanani ("18.06.26") ko'rsatadi.
    /// O'zbekiston vaqti (UTC+5, DSTsiz) bo'yicha hisoblanadi.
    /// </summary>
    private static string LastSeen(DateTime utc)
    {
        var local = utc.AddHours(5);
        var nowLocal = DateTime.UtcNow.AddHours(5);
        var time = local.ToString("HH:mm:ss");

        if (local.Date == nowLocal.Date) return $"{time} da";
        if (local.Date == nowLocal.Date.AddDays(-1)) return $"kecha {time} da";
        return local.ToString("dd.MM.yy");
    }

    /// <summary>Canonical, shareable post URL: /post/{username}/{slug}. Falls back to the user id if a username is missing.</summary>
    public static string PostUrl(string? authorUsername, int authorId, string slug)
        => $"/post/{(string.IsNullOrWhiteSpace(authorUsername) ? authorId.ToString() : authorUsername)}/{slug}";

    /// <summary>Profile URL: /profile/{username}, falling back to /profile/{id} when no username is set.</summary>
    public static string ProfileUrl(string? username, int id)
        => $"/profile/{(string.IsNullOrWhiteSpace(username) ? id.ToString() : username)}";

    /// <summary>
    /// Canonical, shareable and Google-indexable quote URL: <c>/iqtibos/{id}</c>. Kept separate
    /// from the private <c>/quotes</c> list pages (which stay behind auth / robots-disallowed) so
    /// only the public quote detail page is crawled and indexed.
    /// </summary>
    public static string QuoteUrl(int id) => $"/iqtibos/{id}";

    /// <summary>
    /// Kanonik, Google indekslaydigan kitob sahifasi: <c>/kitob/{id}-{nom-slug}</c>.
    /// Id yetakchi bo'lgani uchun kitob nomi keyin o'zgarsa ham eski havolalar ishlayveradi
    /// (controller noto'g'ri slug'ni kanonik manzilga 301 qiladi). Kitob nomi URL'da
    /// bo'lishi qidiruvda muhim signal.
    /// </summary>
    public static string BookUrl(int id, string title)
    {
        var slug = Slugify(title);
        return slug.Length == 0 ? $"/kitob/{id}" : $"/kitob/{id}-{slug}";
    }

    /// <summary>
    /// Matnni URL uchun slug'ga aylantiradi: kichik harf, o'zbek kirillini lotinga
    /// o'girish, tutuq belgilarini tushirish (oʻ→o, gʻ→g), qolgan hamma narsa "-".
    /// </summary>
    public static string Slugify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var raw in text.Trim().ToLowerInvariant())
        {
            var ch = raw;
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(ch);
                continue;
            }
            // Tutuq/apostrof belgilari: oʻ, gʻ, so'z ichidagi ' — shunchaki tushiriladi.
            if (ch is '\'' or 'ʻ' or 'ʼ' or '`' or '‘' or '’') continue;

            var mapped = ch switch
            {
                'а' => "a", 'б' => "b", 'в' => "v", 'г' => "g", 'д' => "d",
                'е' => "e", 'ё' => "yo", 'ж' => "j", 'з' => "z", 'и' => "i",
                'й' => "y", 'к' => "k", 'л' => "l", 'м' => "m", 'н' => "n",
                'о' => "o", 'п' => "p", 'р' => "r", 'с' => "s", 'т' => "t",
                'у' => "u", 'ф' => "f", 'х' => "x", 'ц' => "ts", 'ч' => "ch",
                'ш' => "sh", 'щ' => "sh", 'э' => "e", 'ю' => "yu", 'я' => "ya",
                'ў' => "o", 'қ' => "q", 'ғ' => "g", 'ҳ' => "h",
                'ъ' => "", 'ь' => "",
                _ => "-"
            };
            sb.Append(mapped);
        }

        // Ketma-ket "-" larni bittaga tushirish, chetlarini kesish, uzunlikni cheklash.
        var slug = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-{2,}", "-").Trim('-');
        return slug.Length > 80 ? slug[..80].TrimEnd('-') : slug;
    }
}
