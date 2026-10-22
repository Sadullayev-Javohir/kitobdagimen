using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Challenge.Commands.FinalizeChallengeMonth;
using KitobdaGimen.Application.Features.Challenge.Commands.SetWinnerGiftBook;
using KitobdaGimen.Application.Features.Challenge.Commands.ToggleChallengeWinnerLike;
using KitobdaGimen.Application.Features.Challenge.Queries.GetAnnouncedWinners;
using KitobdaGimen.Application.Features.Challenge.Queries.GetChallengeBoard;
using KitobdaGimen.Application.Features.Challenge.Queries.GetChallengeStandings;
using KitobdaGimen.Application.Features.Challenge.Queries.GetRandomBookCovers;
using KitobdaGimen.Application.Features.Challenge.Queries.GetUserChallengeStats;
using KitobdaGimen.Application.Features.Challenge.Queries.GetUserYearCalendar;
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
    private const int DecorationCoverCount = 60;
    private const long MaxGiftCoverBytes = 5 * 1024 * 1024; // 5 MB
    private const int MaxCoverDimension = 1200;

    // Faqat brauzer va ImageSharp ishonchli dekod qila oladigan formatlar. iPhone'ning
    // HEIC/HEIF rasmlari va boshqa qo'llab-quvvatlanmaydigan formatlar (accept="image/*"
    // ular ham tanlanishiga yo'l qo'yadi) bu yerda oldindan rad etiladi.
    private static readonly HashSet<string> AllowedImageTypes = new()
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private readonly IAppDbContext _db;
    private readonly ITelegramBotService _telegramBot;

    public ChallengeController(IAppDbContext db, ITelegramBotService telegramBot)
    {
        _db = db;
        _telegramBot = telegramBot;
    }

    // ── Foydalanuvchi sahifasi ────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var (year, month) = ChallengeCalendar.CurrentPeriod();
        var config = await ChallengeConfigStore.GetOrDefaultAsync(_db);

        var board = await Mediator.Send(new GetChallengeBoardQuery
        {
            Year = year, Month = month, PodiumCount = 3, ListCount = 20
        });

        var announced = await Mediator.Send(new GetAnnouncedWinnersQuery());
        var covers = await Mediator.Send(new GetRandomBookCoversQuery { Count = DecorationCoverCount });
        covers = FilterExistingCovers(covers);

        var stats = CurrentUserId is int uid
            ? await Mediator.Send(new GetUserChallengeStatsQuery(uid))
            : new Application.Features.Challenge.Dtos.UserChallengeStatsDto();

        ViewData["Title"] = "Challenge — Kitobxonlar bellashuvi";

        return View(new ChallengePageViewModel
        {
            Config = config,
            CurrentYear = year,
            CurrentMonth = month,
            CurrentPeriodLabel = !string.IsNullOrWhiteSpace(config.Title) ? config.Title : ChallengeCalendar.PeriodLabel(year, month),
            LiveStandings = board.Podium,
            OtherStandings = board.Others,
            MyStanding = board.MyStanding,
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
        return Json(FilterExistingCovers(covers));
    }

    /// <summary>Berilgan yil uchun o'qish kalendari (GitHub uslubidagi heatmap) — JSON.
    /// Avvalgi yillarni ko'rish uchun mijoz AJAX bilan chaqiradi.</summary>
    [HttpGet("stats/{year:int}")]
    public async Task<IActionResult> YearStats(int year)
    {
        if (CurrentUserId is not int uid)
        {
            return Unauthorized();
        }

        if (year < 2000 || year > UzTime.Today.Year + 1)
        {
            return BadRequest();
        }

        var calendar = await Mediator.Send(new GetUserYearCalendarQuery(uid, year));
        return Json(calendar);
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

        // Ko'rsatilmasa — joriy davr (admin challenge sanalarini va sovg'a kitoblarni boshqarishi uchun).
        int y, m;
        if (year is int yy && month is int mm)
        {
            y = yy; m = mm;
        }
        else
        {
            (y, m) = ChallengeCalendar.CurrentPeriod();
        }

        var config = await ChallengeConfigStore.GetOrDefaultAsync(_db);
        var groupRow = await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == ChallengeConfigStore.BookClubGroupIdKey);

        var standings = await Mediator.Send(new GetChallengeStandingsQuery { Year = y, Month = m, Limit = 3 });
        var announced = await Mediator.Send(new GetAnnouncedWinnersQuery { Year = y, Month = m });
        var covers = await Mediator.Send(new GetRandomBookCoversQuery { Count = DecorationCoverCount });
        covers = FilterExistingCovers(covers);

        ViewData["Title"] = "Challenge — Admin boshqaruvi";

        return View(new ChallengeAdminViewModel
        {
            Config = config,
            BookClubGroupId = groupRow?.Value,
            Year = y,
            Month = m,
            PeriodLabel = !string.IsNullOrWhiteSpace(config.Title) ? config.Title : ChallengeCalendar.PeriodLabel(y, m),
            Standings = standings,
            Announced = announced,
            IsPeriodCompleted = ChallengeCalendar.IsCompleted(y, m),
            DecorationCovers = covers,
            MyRole = role
        });
    }

    [HttpPost("admin/save-config")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> SaveConfig(
        string? title,
        string? startDate,
        string? endDate,
        string? bookClubGroupId,
        CancellationToken ct)
    {
        var role = await GetMyRoleAsync();
        if (role < UserRole.Admin)
        {
            return RedirectToAction("Index");
        }

        try
        {
            var cfg = await ChallengeConfigStore.GetOrDefaultAsync(_db, ct);
            if (!string.IsNullOrWhiteSpace(title))
            {
                cfg.Title = title.Trim();
            }

            if (DateOnly.TryParse(startDate, out var sd))
            {
                cfg.StartDate = sd;
            }

            if (DateOnly.TryParse(endDate, out var ed))
            {
                cfg.EndDate = ed < cfg.StartDate ? cfg.StartDate : ed;
            }

            for (var rank = 1; rank <= 3; rank++)
            {
                var p = cfg.GetPrize(rank);
                var pTitle = Request.Form[$"prizeTitle_{rank}"].ToString();
                var pAuthor = Request.Form[$"prizeAuthor_{rank}"].ToString();
                var pCover = Request.Form.Files.GetFile($"prizeCover_{rank}");
                var pCoverUrl = Request.Form[$"prizeCoverUrl_{rank}"].ToString();

                p.Title = string.IsNullOrWhiteSpace(pTitle) ? null : pTitle.Trim();
                p.Author = string.IsNullOrWhiteSpace(pAuthor) ? null : pAuthor.Trim();
                if (pCover is not null && pCover.Length > 0)
                {
                    p.CoverUrl = await SaveGiftCoverAsync(pCover);
                }
                else if (!string.IsNullOrWhiteSpace(pCoverUrl))
                {
                    p.CoverUrl = pCoverUrl.Trim();
                }

                // Agar kitob nomi kiritilgan bo'lsa va bazada mavjud bo'lmasa, yangi kitob yaratamiz
                if (!string.IsNullOrWhiteSpace(p.Title))
                {
                    var normTitle = TextNormalizer.NormalizeApostrophes(p.Title).ToLower();
                    var normAuthor = TextNormalizer.NormalizeApostrophes(p.Author ?? "").ToLower();
                    var existingBook = await _db.Books.FirstOrDefaultAsync(b =>
                        b.Title.ToLower()
                            .Replace("‘", "'")
                            .Replace("’", "'")
                            .Replace("ʻ", "'")
                            .Replace("ʼ", "'")
                            .Replace("`", "'")
                            .Replace("´", "'")
                            .Replace("ʹ", "'") == normTitle &&
                        (string.IsNullOrEmpty(normAuthor) || b.Author.ToLower()
                            .Replace("‘", "'")
                            .Replace("’", "'")
                            .Replace("ʻ", "'")
                            .Replace("ʼ", "'")
                            .Replace("`", "'")
                            .Replace("´", "'")
                            .Replace("ʹ", "'") == normAuthor), ct);

                    if (existingBook is null)
                    {
                        var newBook = new KitobdaGimen.Domain.Entities.Book
                        {
                            Title = p.Title,
                            Author = string.IsNullOrWhiteSpace(p.Author) ? "Muallif" : p.Author,
                            CoverUrl = p.CoverUrl,
                            TotalPages = 200,
                            GenreId = 1,
                            Source = null // Qo'lda yaratilgan kitob
                        };
                        _db.Books.Add(newBook);
                        await _db.SaveChangesAsync(ct);
                    }
                }
            }

            await ChallengeConfigStore.SaveAsync(_db, cfg, ct);

            if (bookClubGroupId is not null)
            {
                var groupRow = await _db.AppSettings
                    .FirstOrDefaultAsync(s => s.Key == ChallengeConfigStore.BookClubGroupIdKey, ct);
                if (groupRow is null)
                {
                    _db.AppSettings.Add(new KitobdaGimen.Domain.Entities.AppSetting
                    {
                        Key = ChallengeConfigStore.BookClubGroupIdKey,
                        Value = bookClubGroupId.Trim(),
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    groupRow.Value = bookClubGroupId.Trim();
                    groupRow.UpdatedAt = DateTime.UtcNow;
                }
                await _db.SaveChangesAsync(ct);
            }

            TempData["ChallengeMsg"] = "✅ Challenge muddati, 1-2-3-o'rin sovg'a kitoblari va «KitobdagiMen» guruhi sozlamalari saqlandi!";
        }
        catch (Exception ex)
        {
            TempData["ChallengeMsg"] = $"Xato: {ex.Message}";
        }

        return RedirectToAction("Admin");
    }

    [HttpPost("admin/send-daily-report")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendDailyReportNow(CancellationToken ct)
    {
        var role = await GetMyRoleAsync();
        if (role < UserRole.Admin)
        {
            return RedirectToAction("Index");
        }

        var ok = await _telegramBot.SendDailyGroupReportAsync(ct);
        TempData["ChallengeMsg"] = ok
            ? "✅ Kunlik mutolaa sarhisobi (23:59) Telegram orqali yuborildi!"
            : "⚠️ Hisobotni yuborib bo'lmadi — «KitobdagiMen» guruhi ID ulanganini yoki Admin Telegram hisobini tekshiring.";
        return RedirectToAction("Admin");
    }

    /// <summary>Berilgan oyni e'lon qiladi (g'oliblarni saqlaydi). Admin+.</summary>
    [HttpPost("admin/finalize")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(int year, int month)
    {
        try
        {
            var count = await Mediator.Send(new FinalizeChallengeMonthCommand(year, month));
            if (count > 0)
            {
                _ = _telegramBot.BroadcastChallengeWinnersAsync(year, month);
            }
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

    /// <summary>Berilgan oyni e'lon qiladi (g'oliblarni saqlaydi) va shu bilan birga har bir
    /// g'olibga sovg'a kitob (nom, muallif, yuklangan muqova rasmi) biriktiradi. Super admin.</summary>
    [HttpPost("admin/announce")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Announce(int year, int month)
    {
        try
        {
            var count = await Mediator.Send(new FinalizeChallengeMonthCommand(year, month));

            // 1-, 2-, 3-o'rin g'oliblariga sovg'a kitoblarni saqlaymiz (kiritilgan bo'lsa).
            for (var rank = 1; rank <= 3; rank++)
            {
                var title = Request.Form[$"title_{rank}"].ToString();
                var author = Request.Form[$"author_{rank}"].ToString();
                var file = Request.Form.Files.GetFile($"cover_{rank}");
                var hasFile = file is not null && file.Length > 0;

                if (string.IsNullOrWhiteSpace(title) && !hasFile)
                {
                    continue;
                }

                var savedCoverUrl = hasFile ? await SaveGiftCoverAsync(file!) : null;

                try
                {
                    await Mediator.Send(new SetWinnerGiftBookCommand
                    {
                        Year = year,
                        Month = month,
                        Rank = rank,
                        GiftBookTitle = title,
                        GiftBookAuthor = author,
                        GiftBookCoverUrl = savedCoverUrl
                    });
                }
                catch (NotFoundException)
                {
                    // Bu o'rin uchun g'olib yo'q (masalan reytingda 2 kishi) — o'tkazamiz.
                }
            }

            _ = _telegramBot.BroadcastChallengeWinnersAsync(year, month);

            TempData["ChallengeMsg"] = count > 0
                ? $"{ChallengeCalendar.PeriodLabel(year, month)} uchun {count} ta g'olib e'lon qilindi va sovg'alar saqlandi."
                : $"{ChallengeCalendar.PeriodLabel(year, month)} allaqachon e'lon qilingan yoki g'olib topilmadi.";
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

    /// <summary>1-o'rin g'olibiga kitob sovg'a qiladi (super admin). Muqovani fayl sifatida yuklash mumkin.</summary>
    [HttpPost("admin/gift")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gift(
        int year, int month, int rank, string? title, string? author, IFormFile? cover)
    {
        try
        {
            string? savedCoverUrl = null;
            if (cover is not null && cover.Length > 0)
            {
                savedCoverUrl = await SaveGiftCoverAsync(cover);
            }

            var chosenCoverUrl = savedCoverUrl;
            if (string.IsNullOrWhiteSpace(chosenCoverUrl))
            {
                var cUrl = Request.Form["coverUrl"].ToString();
                if (!string.IsNullOrWhiteSpace(cUrl))
                {
                    chosenCoverUrl = cUrl.Trim();
                }
            }

            await Mediator.Send(new SetWinnerGiftBookCommand
            {
                Year = year,
                Month = month,
                Rank = rank <= 0 ? 1 : rank,
                GiftBookTitle = title,
                GiftBookAuthor = author,
                GiftBookCoverUrl = chosenCoverUrl
            });

            // Agar kitob bazada mavjud bo'lmasa, avtomatik yangi kitob yaratamiz
            if (!string.IsNullOrWhiteSpace(title))
            {
                var normTitle = TextNormalizer.NormalizeApostrophes(title).ToLower();
                var normAuthor = TextNormalizer.NormalizeApostrophes(author ?? "").ToLower();
                var existingBook = await _db.Books.FirstOrDefaultAsync(b =>
                    b.Title.ToLower()
                        .Replace("‘", "'")
                        .Replace("’", "'")
                        .Replace("ʻ", "'")
                        .Replace("ʼ", "'")
                        .Replace("`", "'")
                        .Replace("´", "'")
                        .Replace("ʹ", "'") == normTitle &&
                    (string.IsNullOrEmpty(normAuthor) || b.Author.ToLower()
                        .Replace("‘", "'")
                        .Replace("’", "'")
                        .Replace("ʻ", "'")
                        .Replace("ʼ", "'")
                        .Replace("`", "'")
                        .Replace("´", "'")
                        .Replace("ʹ", "'") == normAuthor));

                if (existingBook is null)
                {
                    var newBook = new KitobdaGimen.Domain.Entities.Book
                    {
                        Title = title.Trim(),
                        Author = string.IsNullOrWhiteSpace(author) ? "Muallif" : author.Trim(),
                        CoverUrl = chosenCoverUrl,
                        TotalPages = 200,
                        GenreId = 1,
                        Source = null
                    };
                    _db.Books.Add(newBook);
                    await _db.SaveChangesAsync();
                }
            }

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

    /// <summary>
    /// Dekoratsiya muqovalaridan fayli mavjud bo'lmaganlarini (o'chirilgan/ko'chirilmagan
    /// yuklamalar) chiqarib tashlaydi — shunda sahifada 404 so'rovlari bo'lmaydi. Tashqi
    /// (http/https) manzillar tegilmasdan o'tkaziladi.
    /// </summary>
    private static IReadOnlyList<string> FilterExistingCovers(IReadOnlyList<string> covers)
    {
        var result = new List<string>(covers.Count);
        foreach (var url in covers)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(url);
                continue;
            }

            // Lokal yuklama (/uploads/...) — faqat fayli diskda mavjud bo'lsa qoldiramiz.
            if (url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var rel = url["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar);
                var full = Path.Combine(KitobdaGimen.Web.UploadPaths.Root, rel);
                if (System.IO.File.Exists(full))
                {
                    result.Add(url);
                }
                continue;
            }

            // Boshqa lokal manzillar (mas. /images/...) — o'zgarishsiz qoldiramiz.
            result.Add(url);
        }

        return result;
    }

    /// <summary>Sovg'a kitob muqovasini rasm sifatida dekod qilib, WebP qilib saqlaydi.</summary>
    private async Task<string?> SaveGiftCoverAsync(IFormFile file)
    {
        if (file.Length > MaxGiftCoverBytes)
        {
            throw new InvalidOperationException("Rasm hajmi 5 MB dan oshmasligi kerak.");
        }

        // Content-type'ni oldindan tekshiramiz — HEIC/HEIF (iPhone), AVIF va boshqa
        // qo'llab-quvvatlanmaydigan formatlar tushunarli xato bilan rad etiladi.
        if (!AllowedImageTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException(
                "Rasm formati qo'llab-quvvatlanmaydi. Iltimos JPG, PNG, WEBP yoki GIF " +
                "formatidagi rasm yuklang (iPhone HEIC rasmlarini avval JPG/PNG'ga o'giring).");
        }

        Image image;
        try
        {
            await using var input = file.OpenReadStream();
            // Faylni rasm sifatida dekod qilamiz — content-type'ga ishonmasdan, haqiqiy
            // rasm ekanini tasdiqlaymiz va WebP'ga qayta-kodlaymiz.
            image = await Image.LoadAsync(input);
        }
        catch (UnknownImageFormatException)
        {
            throw new InvalidOperationException(
                "Yuklangan fayl rasm formatida emas yoki buzilgan. Iltimos JPG, PNG, " +
                "WEBP yoki GIF formatidagi to'g'ri rasm yuklang.");
        }

        using (image)
        {
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
}
