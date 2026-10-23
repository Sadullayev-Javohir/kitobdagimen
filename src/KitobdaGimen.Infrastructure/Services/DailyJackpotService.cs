using System.Globalization;
using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Jackpot.Dtos;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KitobdaGimen.Infrastructure.Services;

public class DailyJackpotService : IDailyJackpotService
{
    private readonly IAppDbContext _db;
    private readonly ITelegramBotService _telegramBotService;
    private readonly ILogger<DailyJackpotService> _logger;

    public DailyJackpotService(
        IAppDbContext db,
        ITelegramBotService telegramBotService,
        ILogger<DailyJackpotService> logger)
    {
        _db = db;
        _telegramBotService = telegramBotService;
        _logger = logger;
    }

    public async Task<DailyJackpotDto?> GetTodayJackpotAsync(int? currentUserId = null, CancellationToken ct = default)
    {
        var today = UzTime.Today;
        var existing = await _db.DailyJackpots
            .Include(j => j.User)
            .Include(j => j.Post!).ThenInclude(p => p.Book)
            .Include(j => j.Quote!).ThenInclude(q => q.Book)
            .Include(j => j.Likes)
            .Include(j => j.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(j => j.Date == today, ct);

        if (existing is null)
        {
            // Bugun uchun hali tanlanmagan — avtomatik aniqlaymiz va guruhga e'lon qilamiz
            existing = await CreateDailyJackpotForDateAsync(today, broadcast: true, ct);
        }

        if (existing is null) return null;

        return MapToDto(existing, currentUserId);
    }

    public async Task<DailyJackpotDto?> GetJackpotByIdAsync(int id, int? currentUserId = null, CancellationToken ct = default)
    {
        var jackpot = await _db.DailyJackpots
            .Include(j => j.User)
            .Include(j => j.Post!).ThenInclude(p => p.Book)
            .Include(j => j.Quote!).ThenInclude(q => q.Book)
            .Include(j => j.Likes)
            .Include(j => j.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        if (jackpot is null) return null;

        return MapToDto(jackpot, currentUserId);
    }

    public async Task<IReadOnlyList<DailyJackpotDto>> GetHistoryAsync(int? currentUserId = null, int limit = 30, CancellationToken ct = default)
    {
        var list = await _db.DailyJackpots
            .AsNoTracking()
            .Include(j => j.User)
            .Include(j => j.Post!).ThenInclude(p => p.Book)
            .Include(j => j.Quote!).ThenInclude(q => q.Book)
            .Include(j => j.Likes)
            .Include(j => j.Comments).ThenInclude(c => c.User)
            .OrderByDescending(j => j.Date)
            .Take(limit)
            .ToListAsync(ct);

        return list.Select(j => MapToDto(j, currentUserId)).ToList();
    }

    public async Task<DailyJackpotDto?> PickJackpotForDateAsync(DateOnly date, bool broadcast = true, CancellationToken ct = default)
    {
        var existing = await _db.DailyJackpots.FirstOrDefaultAsync(j => j.Date == date, ct);
        if (existing is not null)
        {
            // Agar bor bo'lsa, qayta tanlash uchun eskisini o'chirib yangilashimiz mumkin
            _db.DailyJackpots.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }

        var created = await CreateDailyJackpotForDateAsync(date, broadcast, ct);
        if (created is null) return null;

        return MapToDto(created, null);
    }

    public async Task<(bool isLiked, int count)> ToggleLikeAsync(int jackpotId, int userId, CancellationToken ct = default)
    {
        var existing = await _db.DailyJackpotLikes
            .FirstOrDefaultAsync(l => l.DailyJackpotId == jackpotId && l.UserId == userId, ct);

        bool isLiked;
        if (existing is not null)
        {
            _db.DailyJackpotLikes.Remove(existing);
            isLiked = false;
        }
        else
        {
            _db.DailyJackpotLikes.Add(new DailyJackpotLike
            {
                DailyJackpotId = jackpotId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            isLiked = true;
        }

        await _db.SaveChangesAsync(ct);

        var count = await _db.DailyJackpotLikes.CountAsync(l => l.DailyJackpotId == jackpotId, ct);

        // Telegram guruhdagi tugmani sinxronlash
        _ = _telegramBotService.SyncJackpotLikesInGroupAsync(jackpotId, count, CancellationToken.None);

        return (isLiked, count);
    }

    public async Task<JackpotCommentDto> AddCommentAsync(int jackpotId, int userId, string text, CancellationToken ct = default)
    {
        var cleanText = (text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            throw new ArgumentException("Fikr matni bo'sh bo'lishi mumkin emas.");
        }

        if (cleanText.Length > 2000)
        {
            cleanText = cleanText[..2000];
        }

        var comment = new DailyJackpotComment
        {
            DailyJackpotId = jackpotId,
            UserId = userId,
            Text = cleanText,
            CreatedAt = DateTime.UtcNow
        };

        _db.DailyJackpotComments.Add(comment);
        await _db.SaveChangesAsync(ct);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

        return new JackpotCommentDto
        {
            Id = comment.Id,
            UserId = userId,
            UserName = user?.Username,
            UserFullName = user?.FullName ?? "Kitobxon",
            UserAvatarUrl = user?.AvatarUrl,
            Text = comment.Text,
            CreatedAt = comment.CreatedAt,
            TimeAgo = "hozirgina"
        };
    }

    public async Task<IReadOnlyList<JackpotLikerDto>> GetLikersAsync(int jackpotId, CancellationToken ct = default)
    {
        var today = UzTime.Today;
        var todayHeroUserId = await _db.DailyJackpots
            .AsNoTracking()
            .Where(j => j.Date == today)
            .Select(j => (int?)j.UserId)
            .FirstOrDefaultAsync(ct);

        var likers = await _db.DailyJackpotLikes
            .AsNoTracking()
            .Where(l => l.DailyJackpotId == jackpotId)
            .OrderByDescending(l => l.CreatedAt)
            .Include(l => l.User)
            .Select(l => new JackpotLikerDto
            {
                UserId = l.UserId,
                UserName = l.User.Username,
                UserFullName = l.User.FullName,
                UserAvatarUrl = l.User.AvatarUrl,
                IsDailyHero = todayHeroUserId.HasValue && l.UserId == todayHeroUserId.Value,
                LikedAt = l.CreatedAt
            })
            .ToListAsync(ct);

        return likers;
    }

    public async Task<int?> GetTodayHeroUserIdAsync(CancellationToken ct = default)
    {
        var today = UzTime.Today;
        return await _db.DailyJackpots
            .AsNoTracking()
            .Where(j => j.Date == today)
            .Select(j => (int?)j.UserId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> IsUserTodayHeroAsync(int userId, CancellationToken ct = default)
    {
        if (userId <= 0) return false;
        var today = UzTime.Today;
        return await _db.DailyJackpots
            .AsNoTracking()
            .AnyAsync(j => j.Date == today && j.UserId == userId, ct);
    }

    // ── ICHKI YORDAMCHI METODLAR ───────────────────────────────────────────

    private async Task<DailyJackpot?> CreateDailyJackpotForDateAsync(DateOnly date, bool broadcast, CancellationToken ct)
    {
        // Oxirgi 7 kunda g'olib bo'lgan foydalanuvchilar (takrorlanishning oldini olish uchun)
        var recentWinnerUserIds = await _db.DailyJackpots
            .AsNoTracking()
            .Where(j => j.Date >= date.AddDays(-7))
            .Select(j => j.UserId)
            .Distinct()
            .ToListAsync(ct);

        // O'sha kuni yoki oxirgi 48 soatda yozilgan nomzodlar
        var utcStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-5); // Toshkent vaqti UTC
        var utcEnd = utcStart.AddDays(1);

        // Nomzod postlar
        var postCandidates = await _db.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Book)
            .Where(p => p.CreatedAt >= utcStart && p.CreatedAt <= utcEnd)
            .Select(p => new CandidateItem("post", p.Id, p.UserId, p.CreatedAt))
            .ToListAsync(ct);

        // Nomzod iqtiboslar
        var quoteCandidates = await _db.Quotes
            .AsNoTracking()
            .Include(q => q.User)
            .Include(q => q.Book)
            .Where(q => q.CreatedAt >= utcStart && q.CreatedAt <= utcEnd)
            .Select(q => new CandidateItem("quote", q.Id, q.UserId, q.CreatedAt))
            .ToListAsync(ct);

        var allCandidates = postCandidates.Concat(quoteCandidates).ToList();

        // 1-bosqich: Bugun/kecha yozgan va oxirgi 7 kunda g'olib bo'lmaganlar
        var eligible = allCandidates
            .Where(c => !recentWinnerUserIds.Contains(c.UserId))
            .ToList();

        // 2-bosqich: Agar bunday topilmasa, bugungi barcha nomzodlar
        if (eligible.Count == 0)
        {
            eligible = allCandidates;
        }

        // 3-bosqich: Agar bugun umuman post yoki iqtibos bo'lmasa, butun bazadagi oxirgi postlar va iqtiboslar
        if (eligible.Count == 0)
        {
            var fallbackPosts = await _db.Posts
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .Select(p => new CandidateItem("post", p.Id, p.UserId, p.CreatedAt))
                .ToListAsync(ct);

            var fallbackQuotes = await _db.Quotes
                .AsNoTracking()
                .OrderByDescending(q => q.CreatedAt)
                .Take(20)
                .Select(q => new CandidateItem("quote", q.Id, q.UserId, q.CreatedAt))
                .ToListAsync(ct);

            var fallbackAll = fallbackPosts.Concat(fallbackQuotes).ToList();
            eligible = fallbackAll.Where(c => !recentWinnerUserIds.Contains(c.UserId)).ToList();
            if (eligible.Count == 0)
            {
                eligible = fallbackAll;
            }
        }

        if (eligible.Count == 0)
        {
            _logger.LogWarning("Daily Jackpot uchun bitta ham nomzod post/iqtibos topilmadi.");
            return null;
        }

        // Random tanlash (Jeckpot)
        var picked = eligible[Random.Shared.Next(eligible.Count)];

        var jackpot = new DailyJackpot
        {
            Date = date,
            Type = picked.Type,
            PostId = picked.Type == "post" ? picked.Id : null,
            QuoteId = picked.Type == "quote" ? picked.Id : null,
            UserId = picked.UserId,
            SelectedAt = DateTime.UtcNow
        };

        _db.DailyJackpots.Add(jackpot);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Daily Jackpot tanlandi: Sana={Date}, Tur={Type}, Id={Id}, Muallif={UserId}",
            date, picked.Type, picked.Id, picked.UserId);

        if (broadcast)
        {
            // Telegram guruhiga va botga yuborish
            _ = _telegramBotService.BroadcastJackpotToGroupAsync(jackpot.Id, CancellationToken.None);
        }

        // To'liq ma'lumotlar bilan qayta yuklaymiz
        return await _db.DailyJackpots
            .Include(j => j.User)
            .Include(j => j.Post!).ThenInclude(p => p.Book)
            .Include(j => j.Quote!).ThenInclude(q => q.Book)
            .Include(j => j.Likes)
            .Include(j => j.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(j => j.Id == jackpot.Id, ct);
    }

    private static DailyJackpotDto MapToDto(DailyJackpot j, int? currentUserId)
    {
        var isPost = j.Type == "post";
        var book = isPost ? j.Post?.Book : j.Quote?.Book;
        var rawContent = isPost ? (j.Post?.ReviewText ?? "") : (j.Quote?.Text ?? "");
        var content = RichTextSanitizer.StripHtml(rawContent);
        var imageUrl = isPost ? j.Post?.ImageUrl : j.Quote?.ImageUrl;

        var authorUsername = j.User.Username;
        var authorSlug = !string.IsNullOrWhiteSpace(authorUsername) ? authorUsername : j.UserId.ToString();
        var itemSlug = isPost ? j.Post?.Slug : j.Quote?.Slug;

        var targetUrl = isPost
            ? $"/post/{authorSlug}/{itemSlug}"
            : $"/iqtibos/{authorSlug}/{itemSlug}";

        // Sana formati: "22-oktyabr, 2026"
        var dt = j.Date.ToDateTime(TimeOnly.MinValue);
        var monthNameUz = dt.Month switch
        {
            1 => "yanvar",
            2 => "fevral",
            3 => "mart",
            4 => "aprel",
            5 => "may",
            6 => "iyun",
            7 => "iyul",
            8 => "avgust",
            9 => "sentyabr",
            10 => "oktyabr",
            11 => "noyabr",
            12 => "dekabr",
            _ => ""
        };
        var formattedDate = $"{dt.Day}-{monthNameUz}, {dt.Year}";

        var comments = j.Comments
            .OrderBy(c => c.CreatedAt)
            .Select(c => new JackpotCommentDto
            {
                Id = c.Id,
                UserId = c.UserId,
                UserName = c.User.Username,
                UserFullName = c.User.FullName,
                UserAvatarUrl = c.User.AvatarUrl,
                Text = c.Text,
                CreatedAt = c.CreatedAt,
                TimeAgo = FormatRelativeTime(c.CreatedAt)
            })
            .ToList();

        return new DailyJackpotDto
        {
            Id = j.Id,
            Date = j.Date,
            FormattedDate = formattedDate,
            Type = j.Type,
            UserId = j.UserId,
            UserName = j.User.Username,
            UserFullName = j.User.FullName,
            UserAvatarUrl = j.User.AvatarUrl,
            BookId = book?.Id ?? 0,
            BookTitle = book?.Title ?? "Noma'lum kitob",
            BookAuthor = book?.Author,
            BookCoverUrl = book?.CoverUrl,
            Content = content,
            ImageUrl = imageUrl,
            TargetUrl = targetUrl,
            LikeCount = j.Likes.Count,
            IsLikedByMe = currentUserId.HasValue && j.Likes.Any(l => l.UserId == currentUserId.Value),
            CommentCount = j.Comments.Count,
            Comments = comments,
            SelectedAt = j.SelectedAt
        };
    }

    private static string FormatRelativeTime(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalSeconds < 60) return "hozirgina";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} daqiqa oldin";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} soat oldin";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} kun oldin";
        return utc.AddHours(5).ToString("dd.MM.yyyy HH:mm");
    }

    private record CandidateItem(string Type, int Id, int UserId, DateTime CreatedAt);
}
