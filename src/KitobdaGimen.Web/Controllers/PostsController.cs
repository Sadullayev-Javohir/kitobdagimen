using KitobdaGimen.Application.Features.Posts.Commands.AddComment;
using KitobdaGimen.Application.Features.Posts.Commands.CreatePost;
using KitobdaGimen.Application.Features.Posts.Commands.DeleteComment;
using KitobdaGimen.Application.Features.Posts.Commands.DeletePost;
using KitobdaGimen.Application.Features.Posts.Commands.RecordPostView;
using KitobdaGimen.Application.Features.Posts.Commands.ToggleLike;
using KitobdaGimen.Application.Features.Posts.Commands.UpdatePost;
using KitobdaGimen.Application.Features.Posts.Queries.GetPostById;
using KitobdaGimen.Application.Features.Posts.Queries.GetPostBySlug;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
[Route("posts")]
public class PostsController : AppController
{
    // Allowed post image types — re-encoded to WebP regardless of the original format.
    private static readonly HashSet<string> AllowedImageTypes = new()
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxImageBytes = 8 * 1024 * 1024; // 8 MB
    private const int MaxImageDimension = 1600;

    private readonly IWebHostEnvironment _env;

    public PostsController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Post detail page with its comment thread, by internal id. Kept for internal/back-compat
    /// links (e.g. shared posts in chat). Viewable without logging in — records a view only for
    /// authenticated visitors.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        if (IsAuthenticatedUser)
        {
            await Mediator.Send(new RecordPostViewCommand(id));
        }
        var detail = await Mediator.Send(new GetPostByIdQuery(id));
        return View("Details", detail);
    }

    /// <summary>
    /// Canonical, shareable post URL: /post/{username}/{slug}. Viewable without logging in;
    /// records a view only for authenticated visitors. The username segment is for readability —
    /// the random slug alone identifies the post.
    /// </summary>
    [HttpGet("/post/{username}/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> DetailsBySlug(string username, string slug)
    {
        var detail = await Mediator.Send(new GetPostBySlugQuery(slug));
        if (IsAuthenticatedUser)
        {
            await Mediator.Send(new RecordPostViewCommand(detail.Post.Id));
        }
        return View("Details", detail);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePostCommand command)
    {
        var post = await Mediator.Send(command);
        return Redirect(ViewHelpers.PostUrl(post.Author.Username, post.Author.Id, post.Slug));
    }

    /// <summary>Edits a post's review text/image and returns the updated post (JSON).</summary>
    [HttpPost("{id:int}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostCommand command)
    {
        var post = await Mediator.Send(command with { PostId = id });
        return Json(post);
    }

    /// <summary>Deletes a post (only the author may do this).</summary>
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await Mediator.Send(new DeletePostCommand(id));
        return NoContent();
    }

    /// <summary>Uploads a post image, re-encodes it as WebP and returns its public URL (JSON).</summary>
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

            var uploadDir = KitobdaGimen.Web.UploadPaths.Dir("posts");

            var fileName = $"{Guid.NewGuid():N}.webp";
            var fullPath = Path.Combine(uploadDir, fileName);

            await image.SaveAsWebpAsync(fullPath, new WebpEncoder { Quality = 80 });

            var url = $"/uploads/posts/{fileName}";
            return Json(new { url });
        }
    }

    [HttpPost("{id:int}/like")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id)
    {
        var result = await Mediator.Send(new ToggleLikeCommand(id));
        return Json(result);
    }

    [HttpPost("{id:int}/comment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comment(int id, [FromBody] AddCommentCommand command)
    {
        // The post id always comes from the route; ignore any value in the body.
        var comment = await Mediator.Send(command with { PostId = id });
        return Json(comment);
    }

    [HttpPost("comment/{commentId:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        await Mediator.Send(new DeleteCommentCommand(commentId));
        return NoContent();
    }
}
