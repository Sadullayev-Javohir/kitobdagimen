namespace KitobdaGimen.Application.Common;

/// <summary>
/// Yillik hisobot ("Yillik Kitob Yakuni" — Year in Review) qachon ko'rsatilishini
/// belgilaydigan yagona yordamchi. Kartochka har yili yil oxirida — 20-dekabrdan
/// 1-yanvargacha (yil chegarasidan o'tib) — foydalanuvchi saytga kirganda ko'rsatiladi.
/// "Bugun" tushunchasi O'zbekiston vaqtiga (<see cref="UzTime"/>) bog'langan.
///
/// Hisobot yili — yakunlanayotgan yil: 20–31 dekabrda o'sha yilning o'zi, 1-yanvarda esa
/// endigina tugagan (oldingi) yil ko'rsatiladi.
/// </summary>
public static class YearReviewCalendar
{
    /// <summary>Oyna ochiladigan oy (dekabr) va kun (20-dekabr).</summary>
    public const int StartMonth = 12;
    public const int StartDay = 20;

    /// <summary>Oyna yopiladigan oy (yanvar) va kun (1-yanvar, shu kun ham kiradi).</summary>
    public const int EndMonth = 1;
    public const int EndDay = 1;

    /// <summary>Oyna necha kun ochiq turadi: 20–31 dekabr (12 kun) + 1-yanvar = 13 kun.</summary>
    public const int WindowDays = 13;

    /// <summary>
    /// Berilgan sana hisobot oynasi ichidami — 20-dekabrdan 1-yanvargacha (ikkala kun ham
    /// kiradi, yil chegarasidan o'tadi).
    /// </summary>
    public static bool IsWindowOpen(DateOnly today)
        => (today.Month == StartMonth && today.Day >= StartDay)
           || (today.Month == EndMonth && today.Day <= EndDay);

    /// <summary>Hozir (O'zbekiston vaqti) hisobot oynasi ochiqmi.</summary>
    public static bool IsWindowOpenNow() => IsWindowOpen(UzTime.Today);

    /// <summary>
    /// Berilgan sana uchun hisobot qilinadigan yil — yakunlanayotgan yil. Dekabrda o'sha
    /// yilning o'zi; yanvarda (1-yanvar) endigina tugagan oldingi yil.
    /// </summary>
    public static int ReportYear(DateOnly today)
        => today.Month == EndMonth ? today.Year - 1 : today.Year;

    /// <summary>Hozirgi (O'zbekiston vaqti) hisobot yili.</summary>
    public static int CurrentReportYear() => ReportYear(UzTime.Today);

    /// <summary>Yil chegaralari: [1-yanvar .. 31-dekabr] o'sha yil uchun.</summary>
    public static (DateOnly From, DateOnly To) YearRange(int year)
        => (new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));
}
