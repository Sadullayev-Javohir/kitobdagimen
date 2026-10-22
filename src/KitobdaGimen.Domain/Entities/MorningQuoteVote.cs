using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Erta tonggi motivatsion postga foydalanuvchilarning ovozlari (+ qo'shilaman, - qo'shilmayman).
/// </summary>
public class MorningQuoteVote : BaseEntity
{
    public long ChatId { get; set; }
    public int MessageId { get; set; }
    public long TelegramUserId { get; set; }
    public bool IsPositive { get; set; } // true = +, false = -
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
}

