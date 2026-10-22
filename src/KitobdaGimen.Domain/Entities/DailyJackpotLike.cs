using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Jackpot (Kun taqrizi/iqtibosi) g'olibiga qo'yilgan like.
/// </summary>
public class DailyJackpotLike : BaseEntity
{
    public int DailyJackpotId { get; set; }
    public DailyJackpot DailyJackpot { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
