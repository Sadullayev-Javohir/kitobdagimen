namespace KitobdaGimen.Application.Common;

/// <summary>
/// O'zbekiston vaqti (UTC+5) bilan ishlash uchun yagona yordamchi. Server/Hangfire UTC'da
/// ishlaydi, lekin "bugun" tushunchasi foydalanuvchi uchun O'zbekiston sanasiga ko'ra
/// bo'lishi kerak. O'qish progressi, kunlik eslatma jobi va kitobxonlar reytingi —
/// hammasi "bugun"ni shu yerdan oladi, shunda ular bir-biriga to'liq mos keladi.
/// </summary>
public static class UzTime
{
    /// <summary>O'zbekiston UTC'dan 5 soat oldinda (yil bo'yi o'zgarmas, yozgi vaqt yo'q).</summary>
    public const int OffsetHours = 5;

    /// <summary>Hozirgi O'zbekiston vaqti.</summary>
    public static DateTime Now => DateTime.UtcNow.AddHours(OffsetHours);

    /// <summary>O'zbekiston bo'yicha bugungi sana.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(Now);

    /// <summary>UTC vaqtni O'zbekiston mahalliy vaqtiga aylantiradi.</summary>
    public static DateTime FromUtc(DateTime utc)
    {
        if (utc.Kind == DateTimeKind.Local)
            utc = utc.ToUniversalTime();
        return (utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc)).AddHours(OffsetHours);
    }

    /// <summary>
    /// Iqtibos va taqriz kartalari uchun chop etilgan vaqt va sana (O'zbekiston, UTC+5).
    /// 1 kun ichida (24 soatgacha): "hozirgina", "5 daqiqa oldin", "4 soat oldin", "23 soat oldin".
    /// 24 soatdan oshganda: "02.10.26" (dd.MM.yy formati).
    /// </summary>
    public static string FormatPublishedAt(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalSeconds < 0) span = TimeSpan.Zero;

        if (span.TotalSeconds < 60) return "hozirgina";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} daqiqa oldin";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} soat oldin";

        var local = FromUtc(utc);
        return local.ToString("dd.MM.yy", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Aniq soat va sana formati: masalan "17:43  02.11.26" (tooltip/title uchun).
    /// </summary>
    public static string FormatExactPublishedAt(DateTime utc)
    {
        var local = FromUtc(utc);
        return local.ToString("HH:mm  dd.MM.yy", System.Globalization.CultureInfo.InvariantCulture);
    }
}
