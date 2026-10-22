using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Jackpot (Kun taqrizi/iqtibosi) ga yozilgan sharh / fikr / tabrik.
/// </summary>
public class DailyJackpotComment : BaseEntity
{
    public int DailyJackpotId { get; set; }
    public DailyJackpot DailyJackpot { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Text { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
