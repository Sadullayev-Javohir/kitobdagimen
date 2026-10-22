using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Har kungi "Kun taqrizi" yoki "Kun iqtibosi" omadli tanlovi (Jackpot).
/// Har kuni 23:59 gacha yozilgan taqriz va iqtiboslar orasidan random bittasi tanlanadi.
/// Tanlangan foydalanuvchi bir kun davomida saytda "Kun Qahramoni" (Premium) maqomiga ega bo'ladi.
/// </summary>
public class DailyJackpot : BaseEntity
{
    /// <summary>Tanlov sanasi (Toshkent vaqti bo'yicha).</summary>
    public DateOnly Date { get; set; }

    /// <summary>"post" (Kun taqrizi) yoki "quote" (Kun iqtibosi).</summary>
    public string Type { get; set; } = null!;

    public int? PostId { get; set; }
    public Post? Post { get; set; }

    public int? QuoteId { get; set; }
    public Quote? Quote { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime SelectedAt { get; set; }

    /// <summary>KitobdagiMen Telegram guruhiga yuborilgan xabar chat va message IDsi.</summary>
    public long? TelegramChatId { get; set; }
    public int? TelegramMessageId { get; set; }

    // Alohida like va commentlar
    public ICollection<DailyJackpotLike> Likes { get; set; } = new List<DailyJackpotLike>();
    public ICollection<DailyJackpotComment> Comments { get; set; } = new List<DailyJackpotComment>();
}
