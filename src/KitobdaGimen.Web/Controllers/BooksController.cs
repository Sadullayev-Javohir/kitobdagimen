using KitobdaGimen.Application.Common.Interfaces;
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
    private readonly IAsaxiyBookService _asaxiy;

    public BooksController(IWebHostEnvironment env, IAsaxiyBookService asaxiy)
    {
        _env = env;
        _asaxiy = asaxiy;
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

    /// <summary>
    /// "Kitoblarni yangilash" — asaxiy qidiruvini qayta ishga tushiradi: transport tanlash
    /// holatini tiklaydi va jonli sinov qidiruvi bajarib, ishlayotgan yo'lni topadi. Qidiruv
    /// o'chib qolgan bo'lsa, shu tugma orqali qayta tiklanadi.
    /// </summary>
    [HttpPost("asaxiy-refresh")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsaxiyRefresh()
    {
        var result = await _asaxiy.RefreshAsync();
        return Json(new
        {
            ok = result.Healthy,
            transport = result.Transport,
            count = result.Count,
            message = result.Message
        });
    }

    /// <summary>
    /// Searches the asaxiy.uz book catalogue (live). Returns lightweight results the
    /// picker shows below local matches; the actual book is created only on import.
    /// </summary>
    [HttpGet("asaxiy-search")]
    public async Task<IActionResult> AsaxiySearch(string? q)
    {
        var results = await _asaxiy.SearchAsync(q ?? string.Empty);
        return Json(results.Select(r => new
        {
            title = r.Title,
            author = r.Author,
            coverUrl = r.CoverUrl,
            url = r.Url
        }));
    }

    /// <summary>
    /// Imports a book from asaxiy.uz into the local catalogue: fetches its details,
    /// downloads and re-encodes the cover, then creates (or reuses) the book.
    /// Returns the local <see cref="Application.Features.Books.Dtos.BookDto"/> (JSON).
    /// </summary>
    [HttpPost("import-asaxiy")]
    public async Task<IActionResult> ImportAsaxiy([FromBody] ImportAsaxiyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Url))
        {
            return BadRequest(new { message = "Kitob manzili ko'rsatilmadi." });
        }

        var details = await _asaxiy.GetDetailsAsync(request.Url);
        if (details is null)
        {
            return BadRequest(new { message = "Kitob ma'lumotlarini asaxiy.uz dan olishning iloji bo'lmadi." });
        }

        // Muqova rasmini asaxiy CDN'dan yuklab, o'zimizda WebP qilib saqlaymiz —
        // tashqi havolaga bog'lanib qolmaymiz. Yuklab bo'lmasa, muqovasiz davom etamiz.
        string? coverUrl = null;
        if (!string.IsNullOrEmpty(details.CoverUrl))
        {
            var bytes = await _asaxiy.DownloadCoverAsync(details.CoverUrl);
            if (bytes is not null && bytes.Length > 0 && bytes.Length <= MaxCoverBytes)
            {
                try
                {
                    await using var ms = new MemoryStream(bytes);
                    coverUrl = await SaveCoverImageAsync(ms);
                }
                catch (UnknownImageFormatException)
                {
                    // asaxiy rasm formatini taniy olmadik — muqovasiz import qilamiz.
                }
            }
        }

        var command = new CreateBookCommand
        {
            Title = details.Title,
            Author = details.Author,
            // asaxiy sahifasida "Betlar soni" bo'lmasa, validatsiya uchun minimal qiymat.
            TotalPages = details.TotalPages > 0 ? details.TotalPages : 1,
            CoverUrl = coverUrl,
            GenreId = null,
            Source = "asaxiy.uz"
        };

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

        try
        {
            await using var input = file.OpenReadStream();
            var url = await SaveCoverImageAsync(input);
            return Json(new { url });
        }
        catch (UnknownImageFormatException)
        {
            return BadRequest(new { message = "Fayl rasm formatida emas." });
        }
    }

    /// <summary>
    /// Dekodes the stream as an image (rejecting non-images / polyglots), downsizes it
    /// and re-encodes to WebP, returning the saved public URL. Throws
    /// <see cref="UnknownImageFormatException"/> if the stream isn't a valid image.
    /// </summary>
    private async Task<string> SaveCoverImageAsync(Stream input)
    {
        // Faylni rasm sifatida dekod qilamiz (haqiqiy rasm ekanini tasdiqlaydi) va
        // WebP'ga qayta-kodlaymiz. Shu bilan yuklangan "rasm" ichiga yashiringan
        // HTML/skript (polyglot) butunlay yo'qoladi — content-type'ga ishonmaymiz.
        using var image = await Image.LoadAsync(input);

        if (image.Width > MaxCoverDimension || image.Height > MaxCoverDimension)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(MaxCoverDimension, MaxCoverDimension)
            }));
        }

        var uploadDir = KitobdaGimen.Web.UploadPaths.Dir("covers");

        var fileName = $"{Guid.NewGuid():N}.webp";
        var fullPath = Path.Combine(uploadDir, fileName);

        await image.SaveAsWebpAsync(fullPath, new WebpEncoder { Quality = 82 });

        return $"/uploads/covers/{fileName}";
    }

    /// <summary>Body of the asaxiy import request.</summary>
    public record ImportAsaxiyRequest
    {
        public string? Url { get; init; }
    }
}
