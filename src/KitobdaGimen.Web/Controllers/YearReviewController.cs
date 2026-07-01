using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.YearReview.Dtos;
using KitobdaGimen.Application.Features.YearReview.Queries.GetYearReview;
using KitobdaGimen.Domain.Enums;
using KitobdaGimen.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Web.Controllers;

/// <summary>
/// "Yillik Kitob Yakuni" (Year in Review) — har yili 20-dekabrdan 1-yanvargacha foydalanuvchi
/// saytga kirganda modal ko'rinishida chiqadigan bayramona hisobot kartochkasi. Kartochkada:
/// o'qilgan kitoblar soni, jami betlar, o'qilgan kitoblar, eng ko'p like yig'gan post va
/// iqtibos, foydalanuvchiga xos noyob motivatsiya, yangi-yilona dizayn va kitobdagimen.uz
/// logosi. Kartochkani rasm/PDF sifatida yuklab olish va boshqalarga ulashish mumkin.
/// Super admin (va admin) sanaga qaramay istalgan payt sinab ko'rishi mumkin (/preview).
/// </summary>
[Authorize]
[Route("yil-yakuni")]
public class YearReviewController : AppController
{
    private const string SharePurpose = "YearReviewShare.v1";

    private readonly IAppDbContext _db;
    private readonly IDataProtectionProvider _dataProtection;

    public YearReviewController(IAppDbContext db, IDataProtectionProvider dataProtection)
    {
        _db = db;
        _dataProtection = dataProtection;
    }

    // ── Modal kartochkasi (AJAX partial) ─────────────────────────────────────────────

    /// <summary>
    /// Modal uchun kartochka HTML'i. Faqat hisobot oynasi ochiq (20-dekabr – 1-yanvar) bo'lsa yoki
    /// admin oldindan ko'rishida (<c>?preview=1</c>) qaytariladi; aks holda 204 (bo'sh).
    /// </summary>
    [HttpGet("card")]
    public async Task<IActionResult> Card([FromQuery] bool preview = false)
    {
        if (CurrentUserId is not int uid)
        {
            return NoContent();
        }

        var windowOpen = YearReviewCalendar.IsWindowOpenNow();
        var canPreview = preview && await IsAtLeastAdminAsync(uid);

        if (!windowOpen && !canPreview)
        {
            return NoContent();
        }

        var year = YearReviewCalendar.CurrentReportYear();
        var review = await Mediator.Send(new GetYearReviewQuery(uid, year));

        ViewData["ShareUrl"] = BuildShareUrl(uid, year);
        return PartialView("_YearReviewCard", review);
    }

    // ── Ulashish ─────────────────────────────────────────────────────────────────────

    /// <summary>Joriy foydalanuvchi uchun ommaviy ulashish havolasini (absolyut URL) qaytaradi.</summary>
    [HttpGet("share-link")]
    public IActionResult ShareLink([FromQuery] int? year)
    {
        if (CurrentUserId is not int uid)
        {
            return Unauthorized();
        }

        var y = year ?? YearReviewCalendar.CurrentReportYear();
        return Json(new { url = BuildShareUrl(uid, y) });
    }

    /// <summary>
    /// Ommaviy ulashish sahifasi — token orqali (foydalanuvchi id + yil). Anonim ko'ruvchi
    /// ham ochishi mumkin; kartochka o'sha foydalanuvchi uchun serverda render qilinadi.
    /// </summary>
    [HttpGet("share/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> Share(string token)
    {
        if (!TryUnprotectShareToken(token, out var uid, out var year))
        {
            return NotFound();
        }

        var review = await Mediator.Send(new GetYearReviewQuery(uid, year));

        var displayName = string.IsNullOrWhiteSpace(review.FullName) ? "Kitobxon" : review.FullName;
        ViewData["Title"] = $"{displayName} — {year}-yil kitob yakuni";
        ViewData["Description"] =
            $"{displayName} {year}-yilda {review.BooksRead} ta kitobda {review.TotalPages} bet o'qidi. " +
            "Sen ham kitobdagimen.uz da o'z yillik kitob yakuningni yarat!";
        // Ommaviy sahifa — Google indeksiga tushishi mumkin.
        ViewData["Robots"] = "index, follow, max-image-preview:large";

        return View(new YearReviewPageViewModel
        {
            Review = review,
            IsShareView = true,
            ShareUrl = BuildAbsolute($"/yil-yakuni/share/{token}")
        });
    }

    // ── Admin / super admin oldindan ko'rish (sinov) ─────────────────────────────────

    /// <summary>
    /// Sanaga qaramay kartochkani hoziroq ko'rsatadi — admin/super admin sinovi uchun.
    /// Admin panelidagi "Yillik yakunni sinab ko'rish" tugmasi shu sahifaga olib keladi.
    /// </summary>
    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] int? year)
    {
        if (CurrentUserId is not int uid)
        {
            return RedirectToAction("Index", "Feed");
        }

        if (!await IsAtLeastAdminAsync(uid))
        {
            return RedirectToAction("Index", "Feed");
        }

        var y = year ?? YearReviewCalendar.CurrentReportYear();
        var review = await Mediator.Send(new GetYearReviewQuery(uid, y));

        ViewData["Title"] = "Yillik yakun — oldindan ko'rish";

        return View(new YearReviewPageViewModel
        {
            Review = review,
            IsPreview = true,
            ShareUrl = BuildShareUrl(uid, y)
        });
    }

    // ── Yordamchilar ──────────────────────────────────────────────────────────────────

    private async Task<bool> IsAtLeastAdminAsync(int uid)
    {
        var role = await _db.Users.Where(u => u.Id == uid).Select(u => u.Role).FirstOrDefaultAsync();
        return role >= UserRole.Admin;
    }

    private string BuildShareUrl(int uid, int year)
    {
        var protector = _dataProtection.CreateProtector(SharePurpose);
        var token = protector.Protect($"{uid}:{year}");
        // URL-xavfsiz qilib beramiz (Protect natijasi allaqachon base64url).
        return BuildAbsolute($"/yil-yakuni/share/{token}");
    }

    private bool TryUnprotectShareToken(string token, out int uid, out int year)
    {
        uid = 0;
        year = 0;
        try
        {
            var protector = _dataProtection.CreateProtector(SharePurpose);
            var raw = protector.Unprotect(token);
            var parts = raw.Split(':');
            return parts.Length == 2
                   && int.TryParse(parts[0], out uid)
                   && int.TryParse(parts[1], out year);
        }
        catch
        {
            return false;
        }
    }

    private string BuildAbsolute(string relativePath)
        => $"{Request.Scheme}://{Request.Host}{relativePath}";
}
