using KitobdaGimen.Application.Features.PhysicalBooks.Commands.AddPhysicalBook;
using KitobdaGimen.Application.Features.PhysicalBooks.Commands.CancelReservation;
using KitobdaGimen.Application.Features.PhysicalBooks.Commands.ConfirmHandover;
using KitobdaGimen.Application.Features.PhysicalBooks.Commands.RemovePhysicalBook;
using KitobdaGimen.Application.Features.PhysicalBooks.Commands.ReservePhysicalBook;
using KitobdaGimen.Application.Features.PhysicalBooks.Commands.ReturnPhysicalBook;
using KitobdaGimen.Application.Features.PhysicalBooks.Queries.GetLibrary;
using KitobdaGimen.Application.Features.PhysicalBooks.Queries.GetMyPhysicalBooks;
using KitobdaGimen.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

/// <summary>
/// "Almashish" — foydalanuvchilar o'zidagi jismoniy kitoblarni qo'shib, boshqalar bilan
/// almashadigan sahifa. Kitobni "O'qimoqchiman" bilan 24 soatga band qilish, egasi topshirishni
/// tasdiqlashi va qaytarilganini belgilashi shu yerda amalga oshiriladi.
/// </summary>
[Authorize]
[Route("almashish")]
public class PhysicalBooksController : AppController
{
    /// <summary>Almashish sahifasi: kutubxona + mening kitoblarim.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var library = await Mediator.Send(new GetLibraryQuery());
        var mine = await Mediator.Send(new GetMyPhysicalBooksQuery());

        ViewData["Title"] = "Kitob almashish";
        ViewData["Description"] = "O'zingizdagi kitoblarni ulashing va boshqalarnikini o'qing — kitobdagimen.uz almashish kutubxonasi.";

        return View(new PhysicalBooksPageViewModel
        {
            Library = library,
            Mine = mine,
            CurrentUserId = CurrentUserId
        });
    }

    /// <summary>Kutubxona ro'yxatini qidiruv bilan qaytaradi (JSON).</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(string? q)
    {
        var books = await Mediator.Send(new GetLibraryQuery { Search = q });
        return Json(books);
    }

    /// <summary>Yangi jismoniy kitob qo'shadi (JSON).</summary>
    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromBody] AddPhysicalBookCommand command)
    {
        var book = await Mediator.Send(command);
        return Json(book);
    }

    /// <summary>Kitobni 24 soatga band qiladi ("O'qimoqchiman").</summary>
    [HttpPost("{id:int}/reserve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(int id)
    {
        var book = await Mediator.Send(new ReservePhysicalBookCommand(id));
        return Json(book);
    }

    /// <summary>Egasi kitobni topshirganini tasdiqlaydi ("Topshirdim").</summary>
    [HttpPost("{id:int}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var book = await Mediator.Send(new ConfirmHandoverCommand(id));
        return Json(book);
    }

    /// <summary>Egasi kitob qaytarib olinganini belgilaydi ("Qaytarildi").</summary>
    [HttpPost("{id:int}/return")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id)
    {
        var book = await Mediator.Send(new ReturnPhysicalBookCommand(id));
        return Json(book);
    }

    /// <summary>Band qilishni bekor qiladi (band qilgan foydalanuvchi yoki egasi).</summary>
    [HttpPost("{id:int}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var book = await Mediator.Send(new CancelReservationCommand(id));
        return Json(book);
    }

    /// <summary>Kitobni almashish ro'yxatidan o'chiradi (faqat egasi, faqat "Mavjud").</summary>
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await Mediator.Send(new RemovePhysicalBookCommand(id));
        return Json(new { ok = true });
    }
}
