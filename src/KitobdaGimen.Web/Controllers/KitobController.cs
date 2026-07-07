using KitobdaGimen.Application.Features.Books.Queries.GetBookPage;
using KitobdaGimen.Application.Features.Books.Queries.GetBooksFeed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

/// <summary>
/// Ommaviy, Google indekslaydigan kitob sahifalari: bitta kitobning barcha taqrizlari va
/// iqtiboslari (<c>/kitob/{id}-{nom-slug}</c>), va taqriz yoki iqtibos yozilgan barcha
/// kitoblar ro'yxati (<c>/kitoblar</c>) — bu ro'yxat avvalgi <c>/quotes</c> sahifasi o'rnini
/// bosadi. Maxfiy /books yo'llari (kitob qidiruv/yaratish API) bunga aloqasiz va robots'da yopiq.
/// </summary>
[AllowAnonymous]
public class KitobController : AppController
{
    /// <summary>Initial number of book cards rendered with the page; the rest stream in on scroll.</summary>
    private const int BookPageSize = 12;

    /// <summary>Taqriz yoki iqtibos yozilgan barcha kitoblar ro'yxati (eng so'nggi faollik bo'yicha).</summary>
    [HttpGet("/kitoblar")]
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var books = await Mediator.Send(new GetBooksFeedQuery { Search = q, Page = page, PageSize = BookPageSize });
        ViewData["Title"] = "Kitoblar";
        ViewData["Search"] = q;
        return View("Index", books);
    }

    /// <summary>Returns the next page of book cards as an HTML fragment (infinite scroll).</summary>
    [HttpGet("/kitoblar/cards")]
    public async Task<IActionResult> Cards(string? q, int page = 2)
    {
        var books = await Mediator.Send(new GetBooksFeedQuery { Search = q, Page = page, PageSize = BookPageSize });
        return PartialView("_BookCards", books.Items);
    }

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
