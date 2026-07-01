using KitobdaGimen.Application.Features.Books.Queries.GetBookPage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

/// <summary>
/// Ommaviy, Google indekslaydigan kitob sahifasi: <c>/kitob/{id}-{nom-slug}</c> —
/// bitta kitobning barcha taqrizlari va iqtiboslari bir joyda. Bu sahifa "kitob nomi"
/// qidiruvlariga eng mos landing (SEO-OPTIMIZATSIYA.md dagi №1 band).
/// URL'dagi id yetakchi; slug qismi noto'g'ri/eskirgan bo'lsa kanonik manzilga 301.
/// Maxfiy /books yo'llari (kitob qidiruv/yaratish API) bunga aloqasiz va robots'da yopiq.
/// </summary>
[AllowAnonymous]
public class KitobController : AppController
{
    [HttpGet("/kitob/{idSlug}")]
    public async Task<IActionResult> Details(string idSlug)
    {
        // "42-sariq-devni-minib" → 42; faqat "42" ham qabul qilinadi.
        var dashAt = idSlug.IndexOf('-');
        var idPart = dashAt < 0 ? idSlug : idSlug[..dashAt];
        if (!int.TryParse(idPart, out var bookId) || bookId <= 0)
            return NotFound();

        var book = await Mediator.Send(new GetBookPageQuery(bookId));
        if (book is null)
            return NotFound();

        // Kanonik bo'lmagan slug (nom o'zgargan, qo'lda buzilgan) — 301 kanonikka.
        var canonical = ViewHelpers.BookUrl(book.Id, book.Title);
        if (!string.Equals(Request.Path.Value, canonical, StringComparison.Ordinal))
            return RedirectPermanent(canonical);

        return View("Details", book);
    }
}
