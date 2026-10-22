using System.Text;
using KitobdaGimen.Domain.Entities;

namespace KitobdaGimen.Application.Common;

/// <summary>
/// O'zbek tili tutuq belgilari va apostroflarini normallashtirish uchun umumiy utilita.
/// Loyihadagi qidiruv, takroriy kitob tekshiruvi va Telegram bot uchun yagona mantiq.
/// </summary>
public static class TextNormalizer
{
    public const char StandardApostrophe = '\'';

    /// <summary>
    /// Foydalanuvchi kiritishi mumkin bo'lgan barcha apostrof va tutuq belgilari:
    /// ' (U+0027 - Oddiy apostrof)
    /// ‘ (U+2018 - Chap tomonga ochiladigan qo'shtirnoq)
    /// ’ (U+2019 - O'ng tomonga qaragan apostrof)
    /// ʻ (U+02BB - Modifier letter turned comma)
    /// ʼ (U+02BC - Modifier letter apostrophe)
    /// ` (U+0060 - Grave accent)
    /// ´ (U+00B4 - Acute accent)
    /// ʹ (U+02B9 - Prime belgisi)
    /// </summary>
    public static string NormalizeApostrophes(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        return text
            .Replace('‘', StandardApostrophe)
            .Replace('’', StandardApostrophe)
            .Replace('ʻ', StandardApostrophe)
            .Replace('ʼ', StandardApostrophe)
            .Replace('`', StandardApostrophe)
            .Replace('´', StandardApostrophe)
            .Replace('ʹ', StandardApostrophe);
    }

    /// <summary>
    /// Ikkita matn (kitob nomi yoki muallif) apostroflar farqiga qaramasdan bir xilligini tekshiradi.
    /// </summary>
    public static bool AreEquivalent(string? a, string? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return string.Equals(
            NormalizeApostrophes(a).Trim(),
            NormalizeApostrophes(b).Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// EF Core LINQ query uchun: kitob nomi yoki muallifini apostroflardan qat'i nazar qidirish.
    /// Ham InMemory, ham PostgreSQL (Npgsql string.Replace -> SQL replace()) bilan to'liq ishlaydi.
    /// So'zlarga ajratilgan holda bir necha so'z kiritilganda ham aniq topadi.
    /// </summary>
    public static IQueryable<Book> WhereMatchesBookSearch(this IQueryable<Book> query, string? rawSearch)
    {
        if (string.IsNullOrWhiteSpace(rawSearch)) return query;

        var normalized = NormalizeApostrophes(rawSearch).Trim().ToLower();
        var words = normalized
            .Split(new[] { ' ', ',', '-', '—', '.' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length >= 2)
            .ToArray();

        if (words.Length == 0)
        {
            words = new[] { normalized };
        }

        foreach (var word in words)
        {
            var w = word;
            query = query.Where(b =>
                b.Title.ToLower()
                    .Replace("‘", "'")
                    .Replace("’", "'")
                    .Replace("ʻ", "'")
                    .Replace("ʼ", "'")
                    .Replace("`", "'")
                    .Replace("´", "'")
                    .Replace("ʹ", "'")
                    .Contains(w)
                ||
                b.Author.ToLower()
                    .Replace("‘", "'")
                    .Replace("’", "'")
                    .Replace("ʻ", "'")
                    .Replace("ʼ", "'")
                    .Replace("`", "'")
                    .Replace("´", "'")
                    .Replace("ʹ", "'")
                    .Contains(w));
        }

        return query;
    }
}

