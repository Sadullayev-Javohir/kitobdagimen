using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Challenge.Commands.FinalizeChallengeMonth;
using KitobdaGimen.Application.Features.Challenge.Commands.SetWinnerGiftBook;
using KitobdaGimen.Application.Features.Challenge.Commands.ToggleChallengeWinnerLike;
using KitobdaGimen.Application.Features.Challenge.Queries.GetAnnouncedWinners;
using KitobdaGimen.Application.Features.Challenge.Queries.GetChallengeStandings;
using KitobdaGimen.Application.Features.Challenge.Queries.GetRandomBookCovers;
using KitobdaGimen.Application.Features.Challenge.Queries.GetUserChallengeStats;
using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Domain.Enums;
using KitobdaGimen.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace KitobdaGimen.Web.Controllers;

/// <summary>
/// Oylik "Challenge" — har oyning 1-kunidan oxirgi kunigacha eng ko'p kitob o'qigan
/// 3 kitobxon. Oy yakunida g'oliblar e'lon qilinadi (modal + bayram dekoratsiyasi 24 soat),
/// g'oliblarga like qo'yish mumkin, sahifada three.js orqali shaxsiy statistika ko'rsatiladi.
/// Admin/super admin oldindan ko'rish sahifasiga ega; super admin 1-o'rin g'olibiga kitob
/// sovg'a qiladi.
/// </summary>
[Authorize]
[Route("challenge")]
public class ChallengeController : AppController
{
    private const int DecorationCoverCount = 18;
    private const long MaxGiftCoverBytes = 5 * 1024 * 1024; // 5 MB
    private const int MaxCoverDimension = 1200;

    private readonly IAppDbContext _db;

    public ChallengeController(IAppDbContext db)
    {
        _db = db;
    }

    // ── Foydalanuvchi sahifasi ────────────────────────────────────────────────────

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var (year, month) = ChallengeCalendar.CurrentPeriod();

        var standings = await Mediator.Send(new GetChallengeStandingsQuery
        {
            Year = year, Month = month, Limit = 3
        });

        var announced = await Mediator.Send(new GetAnnouncedWinnersQuery());
        var covers = await Mediator.Send(new GetRandomBookCoversQuery { Count = DecorationCoverCount });

        var stats = CurrentUserId is int uid
            ? await Mediator.Send(new GetUserChallengeStatsQuery(uid))
            : new Application.Features.Challenge.Dtos.UserChallengeStatsDto();

        ViewData["Title"] = "Challenge — Kitobxonlar bellashuvi";

        return View(new ChallengePageViewModel
        {
            CurrentYear = year,
            CurrentMonth = month,
            CurrentPeriodLabel = ChallengeCalendar.PeriodLabel(year, month),
            LiveStandings = standings,
            Announced = announced,
            Stats = stats,
            DecorationCovers = covers,
            CurrentUserId = CurrentUserId,
            MyRole = await GetMyRoleAsync()
        });
    }

    /// <summary>Eski /leaderboards manzilini yangi /challenge ga yo'naltiradi (orqaga moslik).</summary>
    [HttpGet("/leaderboards")]
    public IActionResult LegacyRedirect() => RedirectPermanent("/challenge");

    /// <summary>Dekoratsiya uchun tasodifiy kitob muqovalari (JSON) — mijoz vaqti-vaqti bilan yangilaydi.</summary>
    [HttpGet("covers")]
    public async Task<IActionResult> Covers()
    {
        var covers = await Mediator.Send(new GetRandomBookCoversQuery { Count = DecorationCoverCount });
        return Json(covers);
    }

    /// <summary>Challenge g'olibiga like (toggle) — JSON.</summary>
    [HttpPost("winners/{id:int}/like")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(int id)
    {
        var result = await Mediator.Send(new ToggleChallengeWinnerLikeCommand(id));
        return Json(result);
    }

    // ── Admin / super admin oldindan ko'rish ────────────────────────────────────────

    [HttpGet("admin")]
    public async Task<IActionResult> Admin(int? year, int? month)
    {
        var role = await GetMyRoleAsync();
        if (role < UserRole.Admin)
        {
            return RedirectToAction("Index");
        }

        // Ko'rsatilmasa — oxirgi yakunlangan oy (e'lon qilinadigan davr).
        int y, m;
        if (year is int yy && month is int mm)
        {
            y = yy; m = mm;
        }
        else
        {
            (y, m) = ChallengeCalendar.LastCompletedPeriod();
        }

        var standings = await Mediator.Send(new GetChallengeStandingsQuery { Year = y, Month = m, Limit = 3 });
        var announced = await Mediator.Send(new GetAnnouncedWinnersQuery { Year = y, Month = m });
        var covers = await Mediator.Send(new GetRandomBookCoversQuery { Count = DecorationCoverCount });

        ViewData["Title"] = "Challenge — admin oldindan ko'rish";

        return View(new ChallengeAdminViewModel
        {
            Year = y,
            Month = m,
            PeriodLabel = ChallengeCalendar.PeriodLabel(y, m),
            Standings = standings,
            Announced = announced,
            IsPeriodCompleted = ChallengeCalendar.IsCompleted(y, m),
            DecorationCovers = covers,
            MyRole = role
        });
    }

    /// <summary>Berilgan oyni e'lon qiladi (g'oliblarni saqlaydi). Admin+.</summary>
    [HttpPost("admin/finalize")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(int year, int month)
    {
        try
        {
            var count = await Mediator.Send(new FinalizeChallengeMonthCommand(year, month));
            TempData["ChallengeMsg"] = count > 0
                ? $"{ChallengeCalendar.PeriodLabel(year, month)} uchun {count} ta g'olib e'lon qilindi."
                : $"{ChallengeCalendar.PeriodLabel(year, month)} allaqachon e'lon qilingan yoki g'olib topilmadi.";
        }
        catch (Exception ex) when (ex is ForbiddenAccessException or UnauthorizedAccessException)
        {
            return RedirectToAction("Index");
        }

        return RedirectToAction("Admin", new { year, month });
    }

    /// <summary>1-o'rin g'olibiga kitob sovg'a qiladi (super admin). Muqovani fayl sifatida yuklash mumkin.</summary>
    [HttpPost("admin/gift")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gift(
        int year, int month, int rank, string? title, string? author, IFormFile? cover, string? coverUrl)
    {
        try
        {
            var savedCoverUrl = coverUrl;
            if (cover is not null && cover.Length > 0)
            {
                savedCoverUrl = await SaveGiftCoverAsync(cover);
            }

            await Mediator.Send(new SetWinnerGiftBookCommand
            {
                Year = year,
                Month = month,
                Rank = rank <= 0 ? 1 : rank,
                GiftBookTitle = title,
                GiftBookAuthor = author,
                GiftBookCoverUrl = savedCoverUrl
            });

            TempData["ChallengeMsg"] = "Sovg'a kitob saqlandi.";
        }
        catch (Exception ex) when (ex is ForbiddenAccessException or UnauthorizedAccessException)
        {
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["ChallengeMsg"] = $"Xato: {ex.Message}";
        }

        return RedirectToAction("Admin", new { year, month });
    }

    // ── Yordamchilar ────────────────────────────────────────────────────────────────

    private async Task<UserRole> GetMyRoleAsync()
    {
        if (CurrentUserId is not int uid)
        {
            return UserRole.User;
        }

        return await _db.Users.Where(u => u.Id == uid).Select(u => u.Role).FirstOrDefaultAsync();
    }

    /// <summary>Sovg'a kitob muqovasini rasm sifatida dekod qilib, WebP qilib saqlaydi.</summary>
    private async Task<string?> SaveGiftCoverAsync(IFormFile file)
    {
        if (file.Length > MaxGiftCoverBytes)
        {
            throw new InvalidOperationException("Rasm hajmi 5 MB dan oshmasligi kerak.");
        }

        await using var input = file.OpenReadStream();
        using var image = await Image.LoadAsync(input);

        if (image.Width > MaxCoverDimension || image.Height > MaxCoverDimension)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(MaxCoverDimension, MaxCoverDimension)
            }));
        }

        var coversDir = KitobdaGimen.Web.UploadPaths.Dir("covers");
        var fileName = $"{Guid.NewGuid():N}.webp";
        await image.SaveAsWebpAsync(Path.Combine(coversDir, fileName), new WebpEncoder { Quality = 82 });
        return $"/uploads/covers/{fileName}";
    }
}
