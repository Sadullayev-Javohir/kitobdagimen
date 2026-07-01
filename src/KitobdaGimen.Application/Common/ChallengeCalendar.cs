namespace KitobdaGimen.Application.Common;

/// <summary>
/// Challenge davri bilan ishlash uchun yagona yordamchi. Har bir davr — 1 oy: oyning
/// 1-kunidan o'sha oyning oxirgi kunigacha (masalan Iyul 1 – Iyul 31). G'oliblar davrning
/// oxirgi kunida aniqlanadi. Davrlar takrorlanmaydigan oylik bloklar sifatida belgilanadi
/// va (Year, Month) bo'yicha saqlanadi. "Hozir" tushunchasi O'zbekiston vaqtiga
/// (<see cref="UzTime"/>) bog'langan.
/// </summary>
public static class ChallengeCalendar
{
    /// <summary>Davr uzunligi — oylarda.</summary>
    public const int PeriodMonths = 1;

    /// <summary>
    /// Challenge rasman boshlanadigan birinchi davr (yil, oy). Bundan oldingi oylar uchun
    /// g'olib aniqlanmaydi, e'lon qilinmaydi va reyting ko'rsatilmaydi. Challenge Iyul 2026 dan
    /// boshlanadi.
    /// </summary>
    public static readonly (int Year, int Month) StartPeriod = (2026, 7);

    /// <summary>Berilgan davr challenge boshlanishidan oldinmi (unda g'olib bo'lmaydi).</summary>
    public static bool IsBeforeStart(int year, int month)
        => year < StartPeriod.Year || (year == StartPeriod.Year && month < StartPeriod.Month);

    /// <summary>Berilgan oy uchun davr boshlanish oyi. Oylik davrda — o'sha oyning o'zi.</summary>
    private static int StartMonthOf(int month) => month;

    /// <summary>Hozirgi (davom etayotgan) challenge davri — boshlanish (yil, oy).</summary>
    public static (int Year, int Month) CurrentPeriod()
    {
        var today = UzTime.Today;
        return (today.Year, today.Month);
    }

    /// <summary>Berilgan davrdan bir davr (1 oy) oldingi davr.</summary>
    public static (int Year, int Month) PreviousPeriod(int year, int month)
    {
        var d = new DateOnly(year, StartMonthOf(month), 1).AddMonths(-PeriodMonths);
        return (d.Year, d.Month);
    }

    /// <summary>Berilgan davrdan bir davr (1 oy) keyingi davr.</summary>
    public static (int Year, int Month) NextPeriod(int year, int month)
    {
        var d = new DateOnly(year, StartMonthOf(month), 1).AddMonths(PeriodMonths);
        return (d.Year, d.Month);
    }

    /// <summary>Oxirgi yakunlangan (o'tgan) challenge davri — joriy davrdan bir oldingi.</summary>
    public static (int Year, int Month) LastCompletedPeriod()
    {
        var (y, m) = CurrentPeriod();
        return PreviousPeriod(y, m);
    }

    /// <summary>Davrning sana chegaralari: [birinchi kun .. oxirgi kun] (1 oylik).</summary>
    public static (DateOnly From, DateOnly To) Range(int year, int month)
    {
        var start = StartMonthOf(month);
        var from = new DateOnly(year, start, 1);
        var to = from.AddMonths(PeriodMonths).AddDays(-1);
        return (from, to);
    }

    /// <summary>
    /// Davrning "o'rtacha" hisobida ishlatiladigan kunlar soni. Yakunlangan davr uchun — davrdagi
    /// barcha kunlar; joriy davr uchun — bugungacha o'tgan kunlar; kelajakdagi davr uchun 0.
    /// </summary>
    public static int ElapsedDays(int year, int month)
    {
        var (from, to) = Range(year, month);
        var today = UzTime.Today;

        if (today < from)
        {
            return 0;
        }

        var end = today >= to ? to : today;
        return end.DayNumber - from.DayNumber + 1;
    }

    /// <summary>Davr yakunlanganmi (oxirgi kun o'tganmi).</summary>
    public static bool IsCompleted(int year, int month)
    {
        var (_, to) = Range(year, month);
        return UzTime.Today > to;
    }

    /// <summary>Oy nomi (o'zbekcha).</summary>
    public static string MonthName(int month) => month switch
    {
        1 => "Yanvar",
        2 => "Fevral",
        3 => "Mart",
        4 => "Aprel",
        5 => "May",
        6 => "Iyun",
        7 => "Iyul",
        8 => "Avgust",
        9 => "Sentabr",
        10 => "Oktabr",
        11 => "Noyabr",
        12 => "Dekabr",
        _ => month.ToString()
    };

    /// <summary>"Iyul 2026" ko'rinishidagi davr sarlavhasi (1 oy).</summary>
    public static string PeriodLabel(int year, int month)
    {
        return $"{MonthName(StartMonthOf(month))} {year}";
    }
}
