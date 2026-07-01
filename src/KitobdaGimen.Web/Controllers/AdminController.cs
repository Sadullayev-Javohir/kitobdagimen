using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Features.Admin.Commands.AdminDeletePost;
using KitobdaGimen.Application.Features.Admin.Commands.AdminDeleteQuote;
using KitobdaGimen.Application.Features.Admin.Commands.AdminDeleteUser;
using KitobdaGimen.Application.Features.Admin.Commands.SetUserRole;
using KitobdaGimen.Application.Features.Admin.Analytics;
using KitobdaGimen.Application.Features.Admin.Queries.GetAdminUsers;
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

    private readonly IAppDbContext _db;
    private readonly IAsaxiyBookService _asaxiy;
    private readonly ICacheService _cache;

    public AdminController(IAppDbContext db, IAsaxiyBookService asaxiy, ICacheService cache)
    {
        _db = db;
        _asaxiy = asaxiy;
        _cache = cache;
    }
    /// <summary>User list with exact registration and last-seen timestamps.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var users = await Mediator.Send(new GetAdminUsersQuery());
            var myRole = users.FirstOrDefault(u => u.Id == CurrentUserId)?.Role ?? UserRole.User;
            ViewData["MyRole"] = myRole;

            // Server holati — Admin va SuperAdmin ko'radi (agregat metrikalar, maxfiy ma'lumotsiz).
            if (myRole >= UserRole.Admin)
            {
                var snapshot = await Mediator.Send(new GetServerSnapshotQuery());
                ViewData["ServerSnapshot"] = snapshot;
            }

            // Yillik yakun hisobotini boshqarish holati — faqat super admin uchun.
            if (myRole == UserRole.SuperAdmin)
            {
                var reportYear = YearReviewCalendar.CurrentReportYear();
                ViewData["YrReportYear"] = reportYear;
                var pubVal = await _db.AppSettings
                    .Where(s => s.Key == AppSettingKeys.YearReviewPublishedYear)
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync();
                ViewData["YrPublished"] = int.TryParse(pubVal, out var py) && py == reportYear;
            }

            return View(users);
        }
        catch (Exception ex) when (ex is ForbiddenAccessException or UnauthorizedAccessException)
        {
            // Hide the panel from non-admins.
            return RedirectToAction("Index", "Feed");
        }
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

    [HttpPost("users/{id:int}/role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(int id, [FromForm] bool makeAdmin)
    {
        await Mediator.Send(new SetUserRoleCommand(id, makeAdmin));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("users/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await Mediator.Send(new AdminDeleteUserCommand(id));
        return RedirectToAction(nameof(Index));
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
