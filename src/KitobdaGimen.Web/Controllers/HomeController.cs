using System.IO;
using KitobdaGimen.Application.Features.Home.Queries.GetLandingStats;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

public class HomeController : AppController
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IWebHostEnvironment env, ILogger<HomeController> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>x.com kabi: har qanday tashrif buyuruvchi (mehmon yoki a'zo) to'g'ridan-to'g'ri tasmaga yo'naltiriladi.</summary>
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Feed");
    }

    /// <summary>Marketing landing sahifasi alohida /haqida va /landing manzillarida saqlanadi.</summary>
    [HttpGet("/haqida")]
    [HttpGet("/landing")]
    [HttpGet("/about")]
    public async Task<IActionResult> Landing()
    {
        try
        {
            await LoadLandingStatsAsync();
            return View("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Home/Landing failed");
            throw;
        }
    }

    /// <summary>Lightweight login page (Google sign-in + app download). Used on mobile.</summary>
    [HttpGet("/login")]
    public IActionResult Login()
    {
        try
        {
            if (IsAuthenticatedUser)
            {
                return RedirectToAction("Index", "Feed");
            }

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Home/Login failed");
            throw;
        }
    }

    /// <summary>Foydalanish qo'llanmasi — platformadan qanday foydalanishning to'liq bayoni.</summary>
    public IActionResult Guide()
    {
        return View();
    }

    /// <summary>Serves the signed Android app (.apk) for direct download (used by the landing button).</summary>
    [HttpGet("/download/kitobdagimen.apk")]
    public IActionResult DownloadApk()
    {
        var path = Path.Combine(_env.WebRootPath, "download", "kitobdagimen.apk");
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        return PhysicalFile(path, "application/vnd.android.package-archive", "kitobdagimen.apk");
    }

    /// <summary>
    /// Landing statistikasi (kunlik snapshot) — ViewData orqali View'ga uzatiladi.
    /// Xato bo'lsa landing baribir ochilishi kerak, shuning uchun best-effort.
    /// </summary>
    private async Task LoadLandingStatsAsync()
    {
        try
        {
            ViewData["LandingStats"] = await Mediator.Send(new GetLandingStatsQuery());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Landing statistikasi yuklanmadi — bo'limsiz davom etamiz.");
        }
    }

    /// <summary>Crude mobile detection from the User-Agent (good enough for the landing→login split).</summary>
    private bool IsMobile()
    {
        var ua = Request.Headers.UserAgent.ToString();
        if (string.IsNullOrEmpty(ua))
        {
            return false;
        }

        return ua.Contains("Android", StringComparison.OrdinalIgnoreCase)
            || ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
            || ua.Contains("iPod", StringComparison.OrdinalIgnoreCase)
            || (ua.Contains("Mobile", StringComparison.OrdinalIgnoreCase)
                && !ua.Contains("iPad", StringComparison.OrdinalIgnoreCase));
    }
}
