namespace KitobdaGimen.Application.Common;

/// <summary>
/// Challenge davri bilan ishlash uchun yagona yordamchi. Har bir davr — 2 oy: oyning
/// 1-kunidan keyingi oyning oxirgi kunigacha (masalan Yanvar 1 – Fevral 28/29). G'oliblar
/// davrning oxirgi kunida aniqlanadi. Davrlar takrorlanmaydigan 2 oylik bloklar sifatida
/// belgilanadi va boshlanish oyi (Year, Month) bo'yicha saqlanadi (Month — toq: 1,3,5,7,9,11).
/// "Hozir" tushunchasi O'zbekiston vaqtiga (<see cref="UzTime"/>) bog'langan.
/// </summary>
public static class ChallengeCalendar
{
    /// <summary>Davr uzunligi — oylarda.</summary>
    public const int PeriodMonths = 2;

    /// <summary>Berilgan oy uchun davr boshlanish oyi (toq oy: 1,3,5,7,9,11).</summary>
    private static int StartMonthOf(int month) => month % 2 == 1 ? month : month - 1;

    /// <summary>Hozirgi (davom etayotgan) challenge davri — boshlanish (yil, oy).</summary>
    public static (int Year, int Month) CurrentPeriod()
    {
        var today = UzTime.Today;
        return (today.Year, StartMonthOf(today.Month));
    }

    /// <summary>Berilgan davrdan bir davr (2 oy) oldingi davr.</summary>
    public static (int Year, int Month) PreviousPeriod(int year, int month)
    {
        var d = new DateOnly(year, StartMonthOf(month), 1).AddMonths(-PeriodMonths);
        return (d.Year, d.Month);
    }

    /// <summary>Berilgan davrdan bir davr (2 oy) keyingi davr.</summary>
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

    /// <summary>Davrning sana chegaralari: [birinchi kun .. oxirgi kun] (2 oylik).</summary>
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

    /// <summary>"Iyul–Avgust 2026" ko'rinishidagi davr sarlavhasi (2 oy).</summary>
    public static string PeriodLabel(int year, int month)
    {
        var start = StartMonthOf(month);
        var endMonth = start + 1;
        return $"{MonthName(start)}–{MonthName(endMonth)} {year}";
    }
}
