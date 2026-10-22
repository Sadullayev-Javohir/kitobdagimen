using KitobdaGimen.Application.Features.Jackpot.Dtos;

namespace KitobdaGimen.Application.Common.Interfaces;

public interface IDailyJackpotService
{
    /// <summary>
    /// Bugungi kun g'olibini qaytaradi. Agar hali tanlanmagan bo'lsa, avtomatik tanlab bazaga saqlaydi va qaytaradi.
    /// </summary>
    Task<DailyJackpotDto?> GetTodayJackpotAsync(int? currentUserId = null, CancellationToken ct = default);

    /// <summary>
    /// Id bo'yicha jackpot ma'lumotini qaytaradi.
    /// </summary>
    Task<DailyJackpotDto?> GetJackpotByIdAsync(int id, int? currentUserId = null, CancellationToken ct = default);

    /// <summary>
    /// O'tgan kunlar g'oliblari arxivi (Jackpot tarixi).
    /// </summary>
    Task<IReadOnlyList<DailyJackpotDto>> GetHistoryAsync(int? currentUserId = null, int limit = 30, CancellationToken ct = default);

    /// <summary>
    /// Berilgan sana uchun random jackpot tanlaydi (job yoki admin qayta tanlash uchun).
    /// </summary>
    Task<DailyJackpotDto?> PickJackpotForDateAsync(DateOnly date, bool broadcast = true, CancellationToken ct = default);

    /// <summary>
    /// Like qo'yish yoki qaytarib olish.
    /// </summary>
    Task<(bool isLiked, int count)> ToggleLikeAsync(int jackpotId, int userId, CancellationToken ct = default);

    /// <summary>
    /// Izoh yozish.
    /// </summary>
    Task<JackpotCommentDto> AddCommentAsync(int jackpotId, int userId, string text, CancellationToken ct = default);

    /// <summary>
    /// Jackpotga like bosgan barcha foydalanuvchilar ro'yxati.
    /// </summary>
    Task<IReadOnlyList<JackpotLikerDto>> GetLikersAsync(int jackpotId, CancellationToken ct = default);

    /// <summary>
    /// Bugungi kun qahramoni (jackpot g'olibi) bo'lgan foydalanuvchi IDsi.
    /// </summary>
    Task<int?> GetTodayHeroUserIdAsync(CancellationToken ct = default);

    /// <summary>
    /// Foydalanuvchi bugun jackpot g'olibimi?
    /// </summary>
    Task<bool> IsUserTodayHeroAsync(int userId, CancellationToken ct = default);
}
