using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

public class HomeController : AppController
{
    /// <summary>Landing page for anonymous users; authenticated users go straight to the feed.</summary>
    public IActionResult Index()
    {
        if (IsAuthenticatedUser)
        {
            return RedirectToAction("Index", "Feed");
        }

        return View();
    }

    /// <summary>Always renders the landing page, regardless of auth state (no redirect).</summary>
    public IActionResult Landing()
    {
        return View("Index");
    }

    /// <summary>Foydalanish qo'llanmasi — platformadan qanday foydalanishning to'liq bayoni.</summary>
    public IActionResult Guide()
    {
        return View();
    }
}
