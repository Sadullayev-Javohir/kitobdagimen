using System.Net;
using System.Text.RegularExpressions;

namespace KitobdaGimen.Application.Common;

/// <summary>
/// Sanitizes user-authored post text into a tiny, XSS-safe HTML subset.
/// Foydalanuvchi kiritgan matnda FAQAT quyidagi inline teglar saqlanadi:
/// <c>&lt;b&gt; &lt;i&gt; &lt;u&gt; &lt;mark&gt;</c> (qalin, kursiv, tagchiziq, marker).
/// Boshqa hamma narsa (skript, atributlar, boshqa teglar) HTML-encode qilinadi,
/// shuning uchun natijani <c>Html.Raw</c> bilan chiqarish xavfsiz.
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

        // Avval mavjud HTML-entity'larni dekodlaymiz — bu funksiyani IDEMPOTENT
        // qiladi: allaqachon sanitize qilingan matn (`&amp;` kabi) qayta encode
        // bo'lib ikki marta kodlanib qolmaydi. Shu sabab uni ham yozishda, ham
        // render paytida (eski, sanitize qilinmagan postlar uchun) chaqirish xavfsiz.
        var text = WebUtility.HtmlDecode(input);

        // <br> va </p>/</div> kabi blok chegaralarini matn satriga aylantiramiz,
        // qolgan hamma narsa keyingi bosqichda encode qilinadi.
        text = Regex.Replace(text, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "</(p|div)>", "\n", RegexOptions.IgnoreCase);

        // Aliaslarni belgilangan teglarga moslab "marker"ga aylantiramiz, keyin
        // hamma narsani encode qilamiz va faqat ruxsat etilgan teglarni qaytaramiz.
        foreach (var (from, to) in Aliases)
        {
            text = Regex.Replace(text, $"<{from}(\\s[^>]*)?>", $"<{to}>", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, $"</{from}>", $"</{to}>", RegexOptions.IgnoreCase);
        }

        // 1) Hamma narsani encode qilamiz (XSS chegarasi shu yerda).
        var encoded = WebUtility.HtmlEncode(text);

        // 2) Faqat ruxsat etilgan teglarni qayta tiklaymiz (atributsiz).
        foreach (var tag in AllowedTags)
        {
            encoded = encoded.Replace($"&lt;{tag}&gt;", $"<{tag}>");
            encoded = encoded.Replace($"&lt;/{tag}&gt;", $"</{tag}>");
        }

        // Ortiqcha bo'sh qatorlarni jilovlash (uch va undan ko'pini ikkitaga).
        encoded = Regex.Replace(encoded, "\n{3,}", "\n\n");

        return encoded.Trim();
    }
}
