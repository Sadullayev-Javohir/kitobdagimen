using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

public class HomeController : AppController
{
    private readonly IWebHostEnvironment _env;

    public HomeController(IWebHostEnvironment env) => _env = env;

    /// <summary>Landing page for anonymous users; authenticated users go straight to the feed.
    /// Mobile visitors get the lightweight login page instead of the marketing landing.</summary>
    public IActionResult Index()
    {
        if (IsAuthenticatedUser)
        {
            return RedirectToAction("Index", "Feed");
        }

        if (IsMobile())
        {
            return RedirectToAction(nameof(Login));
        }

        return View();
    }

    /// <summary>Renders the marketing landing page on desktop; mobile is redirected to login/feed.</summary>
    public IActionResult Landing()
    {
        if (IsMobile())
        {
            return IsAuthenticatedUser
                ? RedirectToAction("Index", "Feed")
                : RedirectToAction(nameof(Login));
        }

        return View("Index");
    }

    /// <summary>Lightweight login page (Google sign-in + app download). Used on mobile.</summary>
    [HttpGet("/login")]
    public IActionResult Login()
    {
        if (IsAuthenticatedUser)
        {
            return RedirectToAction("Index", "Feed");
        }

        return View();
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
