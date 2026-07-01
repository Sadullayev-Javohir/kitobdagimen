using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Oylik "Challenge" g'olibi — bir oy davomida (oyning 1-kunidan oxirgi kunigacha) eng ko'p
/// kitob o'qigan foydalanuvchilar orasidan 1-, 2- va 3-o'rinlar. Oy yakunlanganda
/// (avtomatik Hangfire jobi yoki admin qo'lda) hisoblanib, bu jadvalga yoziladi va
/// "e'lon qilinadi". 1-o'rin g'olibiga super admin kitob sovg'a qilishi mumkin
/// (<see cref="GiftBookTitle"/> va h.k.).
/// </summary>
public class ChallengeWinner : BaseEntity
{
    /// <summary>Challenge davri — yil (masalan 2026).</summary>
    public int Year { get; set; }

    /// <summary>Challenge davri — oy (1..12).</summary>
    public int Month { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>O'rin: 1, 2 yoki 3.</summary>
    public int Rank { get; set; }

    /// <summary>Davr ichida o'qilgan jami betlar.</summary>
    public int PagesRead { get; set; }

    /// <summary>Davr ichida o'qilgan (alohida) kitoblar soni.</summary>
    public int BooksRead { get; set; }

    /// <summary>Davr ichida kamida bir bet o'qilgan kunlar soni.</summary>
    public int ActiveDays { get; set; }

    /// <summary>Kuniga o'rtacha o'qilgan betlar (davr kunlariga bo'lingan).</summary>
    public double AvgPagesPerDay { get; set; }

    // ── Sovg'a kitob (faqat 1-o'rin uchun, super admin kiritadi) ──────────────────
    public string? GiftBookTitle { get; set; }
    public string? GiftBookAuthor { get; set; }
    public string? GiftBookCoverUrl { get; set; }

    /// <summary>Sovg'ani kiritgan super admin id'si (audit uchun).</summary>
    public int? GiftedByUserId { get; set; }

    /// <summary>Sovg'a kiritilgan vaqt (UTC), null = hali sovg'a yo'q.</summary>
    public DateTime? GiftedAt { get; set; }

    /// <summary>G'olib e'lon qilingan (finalizatsiya) vaqti — UTC.</summary>
    public DateTime AnnouncedAt { get; set; }

    // Navigation
    public ICollection<ChallengeWinnerLike> Likes { get; set; } = new List<ChallengeWinnerLike>();
}
