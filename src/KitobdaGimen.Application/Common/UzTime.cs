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
    /// Format: "17:43  02.11.26" (HH:mm  dd.MM.yy).
    /// </summary>
    public static string FormatPublishedAt(DateTime utc)
    {
        var local = FromUtc(utc);
        return local.ToString("HH:mm  dd.MM.yy", System.Globalization.CultureInfo.InvariantCulture);
    }
}
