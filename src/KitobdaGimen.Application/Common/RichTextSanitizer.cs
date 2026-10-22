using System.Net;
using System.Text.RegularExpressions;

namespace KitobdaGimen.Application.Common;

/// <summary>
/// Sanitizes user-authored post text into a tiny, XSS-safe HTML subset.
/// Foydalanuvchi kiritgan matnda FAQAT quyidagi inline teglar saqlanadi:
/// <c>&lt;b&gt; &lt;i&gt; &lt;u&gt; &lt;mark&gt;</c> (qalin, kursiv, tagchiziq, marker).
/// Barcha blok va konteyner teglari (p, div, span, h1-h6 va hk) xavfsiz tarzda tozalanadi,
/// yangi qatorlar esa \n sifatida saqlanadi. Natijani <c>Html.Raw</c> bilan chiqarish xavfsiz.
/// </summary>
public static class RichTextSanitizer
{
    // Ruxsat etilgan teglar — atributsiz ochuvchi/yopuvchi juftliklar.
    private static readonly string[] AllowedTags = { "b", "i", "u", "mark" };

    // Editor <strong>/<em> chiqarsa ham normallashtiramiz.
    private static readonly (string From, string To)[] Aliases =
    {
        ("strong", "b"),
        ("em", "i"),
    };

    /// <summary>
    /// Returns a sanitized string safe to render with Html.Raw. Newlines are kept
    /// as plain <c>\n</c> (ko'rinishda `white-space: pre-wrap` ularni qatorga ajratadi).
    /// </summary>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Avval mavjud HTML-entity'larni dekodlaymiz — bu funksiyani IDEMPOTENT qiladi.
        var text = WebUtility.HtmlDecode(input);
        if (text.Contains("&lt;") || text.Contains("&gt;") || text.Contains("&amp;"))
        {
            text = WebUtility.HtmlDecode(text);
        }

        // 1. Skript va style bloklarini butunlay o'chiramiz
        text = Regex.Replace(text, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<style\b[^<]*(?:(?!<\/style>)<[^<]*)*<\/style>", "", RegexOptions.IgnoreCase);

        // 2. Blok chegaralarini yangi qatorlarga aylantiramiz
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</(p|div|h[1-6]|li|blockquote|tr)>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<(p|div|h[1-6]|li|blockquote|tr)(\s[^>]*)?>", "\n", RegexOptions.IgnoreCase);

        // 3. Aliaslarni normallashtiramiz (strong -> b, em -> i)
        foreach (var (from, to) in Aliases)
        {
            text = Regex.Replace(text, $"<{from}(\\s[^>]*)?>", $"<{to}>", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, $"</{from}>", $"</{to}>", RegexOptions.IgnoreCase);
        }

        // 4. Ruxsat etilgan inline teglarni vaqtincha xavfsiz tokenlarga almashtiramiz
        foreach (var tag in AllowedTags)
        {
            text = Regex.Replace(text, $"<{tag}(\\s[^>]*)?>", $"\uE000{tag}\uE001", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, $"</{tag}>", $"\uE000/{tag}\uE001", RegexOptions.IgnoreCase);
        }

        // 5. Qolgan barcha HTML teglarni (p, div, span, a, font va hk) butunlay olib tashlaymiz
        text = Regex.Replace(text, @"<[a-zA-Z\/][^>]*>", "");

        // 6. Oddiy matnni HTML-encode qilamiz (5 < 10, <3, & xavfsiz bo'ladi)
        var encoded = WebUtility.HtmlEncode(text);

        // 7. Ruxsat etilgan teglarni xavfsiz tiklaymiz
        foreach (var tag in AllowedTags)
        {
            encoded = encoded.Replace($"\uE000{tag}\uE001", $"<{tag}>");
            encoded = encoded.Replace($"\uE000/{tag}\uE001", $"</{tag}>");
        }

        // 8. Bo'shliqlar va qatorlarni tartibga keltiramiz
        encoded = Regex.Replace(encoded, @"[^\S\r\n]+", " ");
        encoded = Regex.Replace(encoded, @" *\n *", "\n");
        encoded = Regex.Replace(encoded, @"\n{3,}", "\n\n");

        return encoded.Trim();
    }

    /// <summary>
    /// Strips all HTML tags and returns clean plain text with preserved newlines.
    /// Iqtiboslar va Telegram xabarlari uchun toza matn qaytaradi.
    /// </summary>
    public static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var text = WebUtility.HtmlDecode(html);
        if (text.Contains("&lt;") || text.Contains("&gt;") || text.Contains("&amp;"))
        {
            text = WebUtility.HtmlDecode(text);
        }

        text = Regex.Replace(text, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<style\b[^<]*(?:(?!<\/style>)<[^<]*)*<\/style>", "", RegexOptions.IgnoreCase);

        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</(p|div|h[1-6]|li|blockquote|tr)>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<(p|div|h[1-6]|li|blockquote|tr)(\s[^>]*)?>", "\n", RegexOptions.IgnoreCase);

        // Barcha qolgan teglarni olib tashlaymiz
        text = Regex.Replace(text, @"<[^>]+>", "");

        // Entity'larni to'liq dekodlaymiz
        text = WebUtility.HtmlDecode(text);

        // Bo'shliqlarni tartibga keltiramiz
        text = Regex.Replace(text, @"[^\S\r\n]+", " ");
        text = Regex.Replace(text, @" *\n *", "\n");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");

        return text.Trim();
    }
}
