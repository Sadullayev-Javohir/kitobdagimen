using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Challenge g'olibiga qo'yilgan "like" (tabrik). Har bir foydalanuvchi bitta g'olib
/// yozuviga faqat bir marta like qo'ya oladi (unikal indeks).
/// </summary>
public class ChallengeWinnerLike : BaseEntity
{
    public int ChallengeWinnerId { get; set; }
    public ChallengeWinner ChallengeWinner { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
