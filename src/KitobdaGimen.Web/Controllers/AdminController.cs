using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Features.Admin.Commands.AdminDeletePost;
using KitobdaGimen.Application.Features.Admin.Commands.AdminDeleteQuote;
using KitobdaGimen.Application.Features.Admin.Commands.AdminDeleteUser;
using KitobdaGimen.Application.Features.Admin.Commands.BroadcastNotification;
using KitobdaGimen.Application.Features.Admin.Commands.SetUserRole;
using KitobdaGimen.Application.Features.Admin.Analytics;
using KitobdaGimen.Application.Features.Admin.Queries.GetAdminUsers;
using KitobdaGimen.Application.Features.Home.Queries.GetLandingStats;
using KitobdaGimen.Application.Features.Home.Queries.GetBackgroundVideoUrl;
using KitobdaGimen.Application.Features.Admin.Queries.GetServerSnapshot;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace KitobdaGimen.Web.Controllers;

/// <summary>
/// Admin panel. Authorization is enforced in the Application handlers (role read from DB).
/// Admin: delete any post/quote. SuperAdmin: also promote/demote admins and delete users.
/// </summary>
[Authorize]
[Route("admin")]
public class AdminController : AppController
{
    private const int CoverBatch = 12;
    private const int MaxCoverDimension = 1200;
    private const long MaxVideoBytes = 150L * 1024 * 1024; // 150 MB

    private static readonly HashSet<string> AllowedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".ogv", ".ogg", ".mov", ".mkv", ".avi"
    };

    private readonly IAppDbContext _db;
    private readonly IAsaxiyBookService _asaxiy;
    private readonly ICacheService _cache;

    public AdminController(IAppDbContext db, IAsaxiyBookService asaxiy, ICacheService cache)
    {
        _db = db;
        _asaxiy = asaxiy;
        _cache = cache;
    }
    /// <summary>Admin hub — faqat navigatsiya kartochkalari (icon/tugma orqali sahifalarga o'tish).</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var role = await _db.Users.Where(u => u.Id == CurrentUserId).Select(u => u.Role).FirstOrDefaultAsync();
        if (role < UserRole.Admin)
        {
            // Hide the panel from non-admins.
            return RedirectToAction("Index", "Feed");
        }

        ViewData["MyRole"] = role;
        ViewData["UserCount"] = await _db.Users.CountAsync();

        if (role == UserRole.SuperAdmin)
        {
            ViewData["BackgroundVideoUrl"] = await _db.AppSettings
                .Where(s => s.Key == AppSettingKeys.BackgroundVideoUrl)
                .Select(s => s.Value)
                .FirstOrDefaultAsync();
        }

        return View();
    }

    /// <summary>Foydalanuvchilar ro'yxati — aniq ro'yxatdan o'tgan/oxirgi kirgan vaqtlari bilan.</summary>
    [HttpGet("allusers")]
    public async Task<IActionResult> AllUsers()
    {
        try
        {
            var users = await Mediator.Send(new GetAdminUsersQuery());
            var myRole = users.FirstOrDefault(u => u.Id == CurrentUserId)?.Role ?? UserRole.User;
            ViewData["MyRole"] = myRole;
            return View(users);
        }
        catch (Exception ex) when (ex is ForbiddenAccessException or UnauthorizedAccessException)
        {
            return RedirectToAction("Index", "Feed");
        }
    }

    /// <summary>Yillik kitob yakunini yuborish/to'xtatish — faqat super admin.</summary>
    [HttpGet("year-review")]
    public async Task<IActionResult> YearReview()
    {
        if (!await IsSuperAdminAsync())
        {
            return RedirectToAction("Index", "Feed");
        }

        var reportYear = YearReviewCalendar.CurrentReportYear();
        ViewData["YrReportYear"] = reportYear;
        var pubVal = await _db.AppSettings
            .Where(s => s.Key == AppSettingKeys.YearReviewPublishedYear)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
        ViewData["YrPublished"] = int.TryParse(pubVal, out var py) && py == reportYear;

        return View();
    }

    /// <summary>Barcha foydalanuvchilarga xabar (broadcast) — faqat super admin.</summary>
    [HttpGet("broadcast")]
    public async Task<IActionResult> BroadcastPage()
    {
        if (!await IsSuperAdminAsync())
        {
            return RedirectToAction("Index", "Feed");
        }

        return View();
    }

    /// <summary>Server holati — Admin va SuperAdmin ko'radi (agregat metrikalar, maxfiy ma'lumotsiz).</summary>
    [HttpGet("server-status")]
    public async Task<IActionResult> ServerStatus()
    {
        var role = await _db.Users.Where(u => u.Id == CurrentUserId).Select(u => u.Role).FirstOrDefaultAsync();
        if (role < UserRole.Admin)
        {
            return RedirectToAction("Index", "Feed");
        }

        var snapshot = await Mediator.Send(new GetServerSnapshotQuery());
        ViewData["ServerSnapshot"] = snapshot;
        return View();
    }

    /// <summary>Tizim sozlamalari (kesh, muqovalar, landing statistikasi) — faqat super admin.</summary>
    [HttpGet("settings")]
    public async Task<IActionResult> Settings()
    {
        if (!await IsSuperAdminAsync())
        {
            return RedirectToAction("Index", "Feed");
        }

        return View();
    }

    /// <summary>Founder analytics dashboard — DAU/WAU/MAU, growth, funnel, retention (SuperAdmin).</summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics()
    {
        try
        {
            var data = await Mediator.Send(new GetFounderAnalyticsQuery());
            return View(data);
        }
        catch (Exception ex) when (ex is ForbiddenAccessException or UnauthorizedAccessException)
        {
            // Hide the page from non-SuperAdmins.
            return RedirectToAction("Index", "Feed");
        }
    }

    /// <summary>
    /// SuperAdmin: chat monitoring hub. Shows every private conversation (both participants) as a
    /// Telegram-style list; opening one renders the full thread exactly like the public /chat page,
    /// but read-only — the admin only watches. Data is served by the JSON endpoints below; this
    /// renders the shell (optionally pre-opening a conversation via ?conversationId=).
    /// </summary>
    [HttpGet("chat-monitor")]
    public async Task<IActionResult> ChatMonitor(int? conversationId)
    {
        if (!await IsSuperAdminAsync())
        {
            return RedirectToAction("Index", "Feed");
        }

        ViewData["PreselectedConversationId"] = conversationId;
        return View();
    }

    /// <summary>JSON: searchable conversation list for the monitor. Each row carries BOTH participants
    /// and the last message preview, so the admin sees exactly who chatted with whom.</summary>
    [HttpGet("chat-monitor/conversations")]
    public async Task<IActionResult> ChatMonitorConversations(string? q, int page = 1)
    {
        if (!await IsSuperAdminAsync()) return Forbid();

        var viewerEmail = (await _db.Users
            .Where(u => u.Id == CurrentUserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync())?.ToLowerInvariant();

        var term = (q ?? "").Trim().ToLowerInvariant();
        var pageSize = 30;
        var p = Math.Max(1, page);

        var query = _db.Conversations.AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(c =>
                c.User1.FullName.ToLower().Contains(term) ||
                c.User2.FullName.ToLower().Contains(term) ||
                (c.User1.Username != null && c.User1.Username.ToLower().Contains(term)) ||
                (c.User2.Username != null && c.User2.Username.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();

        var rows = await query
            .Select(c => new
            {
                c.Id,
                c.CreatedAt,
                User1 = new { c.User1.Id, c.User1.FullName, c.User1.Username, c.User1.AvatarUrl, c.User1.Email },
                User2 = new { c.User2.Id, c.User2.FullName, c.User2.Username, c.User2.AvatarUrl, c.User2.Email },
                LastMessageAt = c.Messages.OrderByDescending(m => m.SentAt).Select(m => (DateTime?)m.SentAt).FirstOrDefault(),
                LastMessageText = c.Messages.OrderByDescending(m => m.SentAt).Select(m => m.Text
                    ?? (m.ImageUrl != null ? "📷 Rasm"
                        : (m.VoiceUrl != null ? "🎤 Ovozli xabar"
                            : (m.StickerKey != null ? "Stiker"
                                : (m.SharedPostId != null ? "📚 Post" : ""))))).FirstOrDefault()
            })
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .Skip((p - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = rows.Select(x => new
        {
            id = x.Id,
            user1 = MaskUser(x.User1.Id, x.User1.FullName, x.User1.Username, x.User1.AvatarUrl, x.User1.Email, viewerEmail),
            user2 = MaskUser(x.User2.Id, x.User2.FullName, x.User2.Username, x.User2.AvatarUrl, x.User2.Email, viewerEmail),
            lastMessageText = x.LastMessageText,
            lastMessageAt = x.LastMessageAt
        }).ToList();

        return Json(new { total, page = p, pageSize, items });
    }

    /// <summary>JSON: full read-only thread for one conversation (soft-deleted included, flagged).
    /// Rendered by the client exactly like /chat, so the admin watches a real Telegram-style thread.</summary>
    [HttpGet("chat-monitor/conversation/{conversationId:int}/messages")]
    public async Task<IActionResult> ChatMonitorMessages(int conversationId, int page = 1)
    {
        if (!await IsSuperAdminAsync()) return Forbid();

        var viewerEmail = (await _db.Users
            .Where(u => u.Id == CurrentUserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync())?.ToLowerInvariant();

        var conv = await _db.Conversations
            .Where(c => c.Id == conversationId)
            .Select(c => new
            {
                User1 = new { c.User1.Id, c.User1.FullName, c.User1.Username, c.User1.AvatarUrl, c.User1.Email },
                User2 = new { c.User2.Id, c.User2.FullName, c.User2.Username, c.User2.AvatarUrl, c.User2.Email }
            })
            .FirstOrDefaultAsync();
        if (conv == null) return NotFound();

        var pageSize = 50;
        var p = Math.Max(1, page);

        // Monitoring shows everything, including soft-deleted messages (flagged on the client).
        var source = _db.Messages.Where(m => m.ConversationId == conversationId);
        var totalCount = await source.CountAsync();

        var raw = await source
            .OrderByDescending(m => m.SentAt)
            .Skip((p - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                Id = m.Id,
                SenderId = m.Sender.Id,
                SenderName = m.Sender.FullName,
                SenderEmail = m.Sender.Email,
                SenderAvatar = m.Sender.AvatarUrl,
                m.Text,
                m.ImageUrl,
                m.StickerKey,
                m.VoiceUrl,
                m.VoiceDurationSeconds,
                m.IsRead,
                m.ReplyToMessageId,
                ReplyToSenderName = m.ReplyToMessage == null ? null : m.ReplyToMessage.Sender.FullName,
                ReplyToText = m.ReplyToMessage == null ? null : m.ReplyToMessage.Text,
                ReplyToImageUrl = m.ReplyToMessage == null ? null : m.ReplyToMessage.ImageUrl,
                ReplyToVoiceUrl = m.ReplyToMessage == null ? null : m.ReplyToMessage.VoiceUrl,
                ReplyToStickerKey = m.ReplyToMessage == null ? null : m.ReplyToMessage.StickerKey,
                ReplyToSharedPostId = m.ReplyToMessage == null ? null : m.ReplyToMessage.SharedPostId,
                m.EditedAt,
                m.IsDeleted,
                m.SentAt,
                SharedPost = m.SharedPost == null
                    ? null
                    : new { PostId = m.SharedPost.Id, BookTitle = m.SharedPost.Book.Title, BookAuthor = m.SharedPost.Book.Author }
            })
            .ToListAsync();

        raw.Reverse();

        var ids = raw.Select(r => r.Id).ToList();
        var reactionRows = await _db.MessageReactions
            .Where(r => ids.Contains(r.MessageId))
            .Select(r => new { r.MessageId, r.Emoji })
            .ToListAsync();
        var reactionsByMsg = reactionRows
            .GroupBy(r => r.MessageId)
            .ToDictionary(g => g.Key, g => (object)g
                .GroupBy(x => x.Emoji)
                .Select(e => new { emoji = e.Key, count = e.Count() })
                .OrderByDescending(e => e.count)
                .ToList());

        var items = raw.Select(r =>
        {
            var replyPreview = r.ReplyToText != null ? r.ReplyToText
                : r.ReplyToImageUrl != null ? "📷 Rasm"
                : r.ReplyToVoiceUrl != null ? "🎤 Ovozli xabar"
                : r.ReplyToStickerKey != null ? "Stiker"
                : r.ReplyToSharedPostId != null ? "📚 Post"
                : "";
            reactionsByMsg.TryGetValue(r.Id, out var reactions);
            return new
            {
                id = r.Id,
                senderId = r.SenderId,
                senderName = r.SenderName,
                senderAvatar = AvatarPrivacy.Resolve(r.SenderEmail?.ToLowerInvariant(), r.SenderAvatar, viewerEmail),
                text = r.Text,
                imageUrl = r.ImageUrl,
                stickerKey = r.StickerKey,
                voiceUrl = r.VoiceUrl,
                voiceDurationSeconds = r.VoiceDurationSeconds,
                isRead = r.IsRead,
                replyToId = r.ReplyToMessageId,
                replyToSenderName = r.ReplyToSenderName,
                replyToPreview = replyPreview,
                editedAt = r.EditedAt,
                isDeleted = r.IsDeleted,
                sentAt = r.SentAt,
                sharedPost = r.SharedPost,
                reactions = (object)(reactions ?? new List<object>())
            };
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return Json(new
        {
            conversationId,
            user1 = MaskUser(conv.User1.Id, conv.User1.FullName, conv.User1.Username, conv.User1.AvatarUrl, conv.User1.Email, viewerEmail),
            user2 = MaskUser(conv.User2.Id, conv.User2.FullName, conv.User2.Username, conv.User2.AvatarUrl, conv.User2.Email, viewerEmail),
            page = p,
            totalPages,
            totalCount,
            items
        });
    }

    /// <summary>Maps a user row to the monitor DTO, hiding the restricted user's avatar for
    /// everyone except the allowed super-admin viewer (mirrors <see cref="AvatarPrivacy"/>).</summary>
    private static MonitorUserDto MaskUser(
        int id, string fullName, string? username, string? avatarUrl, string? email, string? viewerEmail)
        => new(id, fullName, username, AvatarPrivacy.Resolve(email?.ToLowerInvariant(), avatarUrl, viewerEmail));

    /// <summary>Minimal participant shape returned to the monitor client.</summary>
    private record MonitorUserDto(int Id, string FullName, string? Username, string? AvatarUrl);

    [HttpPost("users/{id:int}/role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(int id, [FromForm] bool makeAdmin)
    {
        await Mediator.Send(new SetUserRoleCommand(id, makeAdmin));
        return RedirectToAction(nameof(AllUsers));
    }

    [HttpPost("users/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await Mediator.Send(new AdminDeleteUserCommand(id));
        return RedirectToAction(nameof(AllUsers));
    }

    [HttpPost("posts/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(int id)
    {
        await Mediator.Send(new AdminDeletePostCommand(id));
        return Ok();
    }

    [HttpPost("quotes/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuote(int id)
    {
        await Mediator.Send(new AdminDeleteQuoteCommand(id));
        return Ok();
    }

    /// <summary>
    /// API endpoint — server holatini olish (SuperAdmin uchun).
    /// </summary>
    [HttpGet("api/status")]
    public async Task<IActionResult> GetSystemStatus()
    {
        var snapshot = await Mediator.Send(new GetServerSnapshotQuery());
        if (snapshot == null)
        {
            return Json(new { error = "Server holati hali to'planmagan" });
        }
        return Json(snapshot);
    }

    /// <summary>
    /// SuperAdmin: Redis keshlarini tozalash.
    /// </summary>
    [HttpPost("clear-cache")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearCache()
    {
        var role = await _db.Users.Where(u => u.Id == CurrentUserId).Select(u => u.Role).FirstOrDefaultAsync();
        if (role != UserRole.SuperAdmin)
        {
            return Forbid();
        }

        try
        {
            await _cache.FlushAsync();
            return Json(new { success = true, message = "Barcha keshlar tozalandi" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Xato: {ex.Message}" });
        }
    }

    /// <summary>
    /// SuperAdmin: landing sahifa statistikasini majburan qayta hisoblash. Odatda snapshot
    /// har kuni o'zi yangilanadi; bu tugma avtomatika ishlamay qolgan holat uchun.
    /// </summary>
    [HttpPost("landing-stats/refresh")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshLandingStats()
    {
        if (!await IsSuperAdminAsync())
        {
            return Forbid();
        }

        try
        {
            var stats = await Mediator.Send(new GetLandingStatsQuery(ForceRefresh: true));
            return Json(new
            {
                success = true,
                stats.UserCount,
                stats.BooksRead,
                stats.PagesRead,
                message = $"Yangilandi: {stats.UserCount} kitobxon, {stats.BooksRead} kitob, {stats.PagesRead} bet."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Xato: {ex.Message}" });
        }
    }

    /// <summary>
    /// SuperAdmin: re-fetches REAL book covers from asaxiy.uz for asaxiy books whose cover file
    /// is missing (the old files were lost). Processes a batch per call (re-run until done).
    /// </summary>
    [HttpPost("refresh-covers")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshCovers()
    {
        var role = await _db.Users.Where(u => u.Id == CurrentUserId).Select(u => u.Role).FirstOrDefaultAsync();
        if (role != UserRole.SuperAdmin)
        {
            return Forbid();
        }

        var coversDir = KitobdaGimen.Web.UploadPaths.Dir("covers");
        var books = await _db.Books.Where(b => b.Source == "asaxiy.uz").ToListAsync();

        int fixedCount = 0, attempted = 0, remaining = 0;
        foreach (var book in books)
        {
            // Skip books whose cover file already exists on disk.
            if (!string.IsNullOrEmpty(book.CoverUrl) && book.CoverUrl.StartsWith("/uploads/covers/"))
            {
                var existing = Path.Combine(coversDir, Path.GetFileName(book.CoverUrl));
                if (System.IO.File.Exists(existing))
                {
                    continue;
                }
            }

            if (attempted >= CoverBatch)
            {
                remaining++;
                continue;
            }
            attempted++;

            try
            {
                var results = await _asaxiy.SearchAsync($"{book.Title} {book.Author}");
                var match = results.FirstOrDefault(r => !string.IsNullOrEmpty(r.CoverUrl));
                if (match?.CoverUrl is null)
                {
                    continue;
                }

                var bytes = await _asaxiy.DownloadCoverAsync(match.CoverUrl);
                if (bytes is null || bytes.Length == 0)
                {
                    continue;
                }

                var url = await SaveCoverWebpAsync(bytes, coversDir);
                if (url is not null)
                {
                    book.CoverUrl = url;
                    fixedCount++;
                }
            }
            catch
            {
                // Bitta kitob xato bersa, qolganlarini davom ettiramiz.
            }
        }

        await _db.SaveChangesAsync();
        return Json(new { fixedCount, attempted, remaining, totalAsaxiy = books.Count });
    }

    /// <summary>
    /// SuperAdmin: fon videoni (barcha sahifalarda, shu jumladan landingda ko'rinadigan) yangi
    /// video bilan almashtiradi. .mp4 va boshqa keng tarqalgan video formatlari qabul qilinadi.
    /// </summary>
    [HttpPost("background-video/upload")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxVideoBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoBytes)]
    public async Task<IActionResult> UploadBackgroundVideo(IFormFile? file)
    {
        if (!await IsSuperAdminAsync())
        {
            return Forbid();
        }

        if (file is null || file.Length == 0)
        {
            return Json(new { success = false, message = "Video tanlanmadi." });
        }
        if (file.Length > MaxVideoBytes)
        {
            return Json(new { success = false, message = "Video hajmi 150 MB dan oshmasligi kerak." });
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedVideoExtensions.Contains(ext))
        {
            return Json(new { success = false, message = "Faqat video fayllar (.mp4, .webm, .mov, .mkv, .ogg, .avi) yuklash mumkin." });
        }

        var oldUrl = await _db.AppSettings
            .Where(s => s.Key == AppSettingKeys.BackgroundVideoUrl)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        var videosDir = KitobdaGimen.Web.UploadPaths.Dir("videos");
        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(videosDir, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"/uploads/videos/{fileName}";
        await SetSettingAsync(AppSettingKeys.BackgroundVideoUrl, url);

        if (!string.IsNullOrEmpty(oldUrl) && oldUrl.StartsWith("/uploads/videos/"))
        {
            var oldPath = Path.Combine(videosDir, Path.GetFileName(oldUrl));
            if (System.IO.File.Exists(oldPath))
            {
                try { System.IO.File.Delete(oldPath); } catch { /* eski faylni o'chirib bo'lmasa ham davom etamiz */ }
            }
        }

        return Json(new { success = true, url, message = "Fon video muvaffaqiyatli yangilandi — barcha foydalanuvchilarga qo'llanadi." });
    }

    /// <summary>
    /// SuperAdmin: yillik yakun hisobotini "yuborish" — barcha foydalanuvchilarga ochadi.
    /// Odatda 20-dekabrda bosiladi. Boshqa hech qanday avtomatik tizimga bog'liq emas,
    /// shuning uchun har qanday holatda ham (avtomatika ishlamay qolsa ham) ishlaydi.
    /// </summary>
    [HttpPost("year-review/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishYearReview()
    {
        if (!await IsSuperAdminAsync())
        {
            return Forbid();
        }

        var year = YearReviewCalendar.CurrentReportYear();
        await SetSettingAsync(AppSettingKeys.YearReviewPublishedYear, year.ToString());
        return Json(new
        {
            success = true,
            year,
            message = $"{year}-yil kitob yakuni barcha foydalanuvchilarga yuborildi."
        });
    }

    /// <summary>SuperAdmin: yillik yakun hisobotini to'xtatish (foydalanuvchilarga ko'rinmaydi).</summary>
    [HttpPost("year-review/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnpublishYearReview()
    {
        if (!await IsSuperAdminAsync())
        {
            return Forbid();
        }

        await SetSettingAsync(AppSettingKeys.YearReviewPublishedYear, null);
        return Json(new
        {
            success = true,
            message = "Yillik yakun to'xtatildi — foydalanuvchilarga ko'rinmaydi."
        });
    }

    /// <summary>
    /// SuperAdmin: barcha foydalanuvchilarga e'lon (sarlavha + matn, ixtiyoriy havola) yuboradi.
    /// Onlayn foydalanuvchilar SignalR orqali real-time, refreshsiz ko'radi.
    /// </summary>
    [HttpPost("broadcast")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Broadcast([FromForm] BroadcastRequest body)
    {
        if (string.IsNullOrWhiteSpace(body?.Title))
        {
            return Json(new { success = false, message = "Sarlavha kiritilishi shart." });
        }
        if (string.IsNullOrWhiteSpace(body?.Message))
        {
            return Json(new { success = false, message = "Xabar matni kiritilishi shart." });
        }

        try
        {
            var count = await Mediator.Send(new BroadcastNotificationCommand(body.Title, body.Message, body.Url));
            return Json(new { success = true, count, message = $"Xabar {count} foydalanuvchiga yuborildi." });
        }
        catch (Exception ex) when (ex is ForbiddenAccessException or UnauthorizedAccessException)
        {
            return Json(new { success = false, message = "Bu amalni bajarishga ruxsatingiz yo'q." });
        }
    }

    /// <summary>Forma tanasi <see cref="Broadcast"/> uchun: sarlavha, matn va ixtiyoriy havola.</summary>
    public record BroadcastRequest(string Title, string Message, string? Url);

    private async Task<bool> IsSuperAdminAsync()
    {
        var role = await _db.Users.Where(u => u.Id == CurrentUserId).Select(u => u.Role).FirstOrDefaultAsync();
        return role == UserRole.SuperAdmin;
    }

    /// <summary>Kalit-qiymat sozlamani o'rnatadi (yozadi/yangilaydi); <c>value=null</c> — o'chiradi.</summary>
    private async Task SetSettingAsync(string key, string? value)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (value is null)
        {
            if (setting is not null)
            {
                _db.AppSettings.Remove(setting);
            }
        }
        else if (setting is null)
        {
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value, UpdatedAt = DateTime.UtcNow });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    private static async Task<string?> SaveCoverWebpAsync(byte[] bytes, string coversDir)
    {
        try
        {
            using var image = Image.Load(bytes);
            if (image.Width > MaxCoverDimension || image.Height > MaxCoverDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxCoverDimension, MaxCoverDimension)
                }));
            }

            var fileName = $"{Guid.NewGuid():N}.webp";
            await image.SaveAsWebpAsync(Path.Combine(coversDir, fileName), new WebpEncoder { Quality = 82 });
            return $"/uploads/covers/{fileName}";
        }
        catch
        {
            return null;
        }
    }
}
