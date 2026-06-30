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
}
