using KitobdaGimen.Application.Features.Stories.Commands.CreateStory;
using KitobdaGimen.Application.Features.Stories.Commands.DeleteStory;
using KitobdaGimen.Application.Features.Stories.Commands.RecordStoryView;
using KitobdaGimen.Application.Features.Stories.Commands.ToggleStoryLike;
using KitobdaGimen.Application.Features.Stories.Queries.GetStoryById;
using KitobdaGimen.Application.Features.Stories.Queries.GetUserStories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
[Route("stories")]
public class StoriesController : AppController
{
    // Allowed story image types — re-encoded to WebP regardless of the original format.
    private static readonly HashSet<string> AllowedImageTypes = new()
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxImageBytes = 8 * 1024 * 1024; // 8 MB
    private const int MaxImageDimension = 1600;

    private readonly IWebHostEnvironment _env;

    public StoriesController(IWebHostEnvironment env)
    {
        _env = env;
    }
    /// <summary>Returns a user's stories for the viewer (JSON).</summary>
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> ForUser(int userId)
    {
        var stories = await Mediator.Send(new GetUserStoriesQuery(userId));
        return Json(stories);
    }

    /// <summary>
    /// A single story's detail page: image, title, text, date, like and view counts.
    /// Anonymous-friendly so a shared link works for not-yet-registered users.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var story = await Mediator.Send(new GetStoryByIdQuery(id));
        ViewData["Title"] = string.IsNullOrWhiteSpace(story.Title) ? "Story" : story.Title;
        return View(story);
    }

    /// <summary>Creates a story and returns it (JSON).</summary>
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateStoryCommand command)
    {
        var story = await Mediator.Send(command);
        return Json(story);
    }

    /// <summary>Uploads a story image, re-encodes it as WebP and returns its public URL (JSON).</summary>
    [HttpPost("upload-image")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Rasm tanlanmadi." });
        }

        if (file.Length > MaxImageBytes)
        {
            return BadRequest(new { message = "Rasm hajmi 8 MB dan oshmasligi kerak." });
        }

        if (!AllowedImageTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Faqat JPG, PNG, WEBP yoki GIF rasm yuklash mumkin." });
        }

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
            if (image.Width > MaxImageDimension || image.Height > MaxImageDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxImageDimension, MaxImageDimension)
                }));
            }

            var uploadDir = KitobdaGimen.Web.UploadPaths.Dir("stories");

            var fileName = $"{Guid.NewGuid():N}.webp";
            var fullPath = Path.Combine(uploadDir, fileName);

            await image.SaveAsWebpAsync(fullPath, new WebpEncoder { Quality = 80 });

            var url = $"/uploads/stories/{fileName}";
            return Json(new { url });
        }
    }

    /// <summary>Records a view for the current user and returns the new view count (JSON).</summary>
    [HttpPost("{id:int}/view")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> View(int id)
    {
        var viewCount = await Mediator.Send(new RecordStoryViewCommand(id));
        return Json(new { viewCount });
    }

    /// <summary>Likes or un-likes a story (JSON).</summary>
    [HttpPost("{id:int}/like")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id)
    {
        var result = await Mediator.Send(new ToggleStoryLikeCommand(id));
        return Json(result);
    }

    /// <summary>Deletes a story (only the author may do this).</summary>
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await Mediator.Send(new DeleteStoryCommand(id));
        return NoContent();
    }
}
