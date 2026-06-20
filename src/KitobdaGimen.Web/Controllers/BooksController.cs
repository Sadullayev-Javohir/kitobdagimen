using KitobdaGimen.Application.Features.Books.Commands.CreateBook;
using KitobdaGimen.Application.Features.Books.Queries.GetBooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
[Route("books")]
public class BooksController : AppController
{
    // Allowed cover image types. The file is fully re-encoded to WebP regardless of
    // the original format, so a content-type header alone can't smuggle anything.
    private static readonly HashSet<string> AllowedImageTypes = new()
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxCoverBytes = 5 * 1024 * 1024; // 5 MB
    private const int MaxCoverDimension = 1200;

    private readonly IWebHostEnvironment _env;

    public BooksController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>Book search used by post/quote/goal pickers (JSON).</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(string? q)
    {
        var books = await Mediator.Send(new GetBooksQuery { Search = q });
        return Json(books);
    }

    /// <summary>Adds a book (or reuses an identical one) and returns it (JSON).</summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateBookCommand command)
    {
        var book = await Mediator.Send(command);
        return Json(book);
    }

    /// <summary>Uploads a book cover image and returns its public URL (JSON).</summary>
    [HttpPost("upload-cover")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadCover(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Rasm tanlanmadi." });
        }

        if (file.Length > MaxCoverBytes)
        {
            return BadRequest(new { message = "Rasm hajmi 5 MB dan oshmasligi kerak." });
        }

        if (!AllowedImageTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Faqat JPG, PNG, WEBP yoki GIF rasm yuklash mumkin." });
        }

        // Faylni rasm sifatida dekod qilamiz (haqiqiy rasm ekanini tasdiqlaydi) va
        // WebP'ga qayta-kodlaymiz. Shu bilan yuklangan "rasm" ichiga yashiringan
        // HTML/skript (polyglot) butunlay yo'qoladi — content-type'ga ishonmaymiz.
        Image image;
        try
        {
            await using var input = file.OpenReadStream();
            image = await Image.LoadAsync(input);
        }
        catch (UnknownImageFormatException)
        {
            return BadRequest(new { message = "Fayl rasm formatida emas." });
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

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadDir = Path.Combine(webRoot, "uploads", "covers");
            Directory.CreateDirectory(uploadDir);

            var fileName = $"{Guid.NewGuid():N}.webp";
            var fullPath = Path.Combine(uploadDir, fileName);

            await image.SaveAsWebpAsync(fullPath, new WebpEncoder { Quality = 82 });

            var url = $"/uploads/covers/{fileName}";
            return Json(new { url });
        }
    }
}
