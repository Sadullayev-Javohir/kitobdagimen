using KitobdaGimen.Application.Features.Follow.Commands.ToggleFollow;
using KitobdaGimen.Application.Features.Follow.Queries.GetFollowers;
using KitobdaGimen.Application.Features.Follow.Queries.GetFollowing;
using KitobdaGimen.Application.Features.Profile.Commands.DeleteAccount;
using KitobdaGimen.Application.Features.Profile.Commands.UpdateProfile;
using KitobdaGimen.Application.Features.Profile.Queries.CheckUsername;
using KitobdaGimen.Application.Features.Profile.Queries.GetUserByUsername;
using KitobdaGimen.Application.Features.Profile.Queries.GetUserPosts;
using KitobdaGimen.Application.Features.Profile.Queries.GetUserProfile;
using KitobdaGimen.Application.Features.Quotes.Queries.GetUserQuotes;
using KitobdaGimen.Application.Features.ReadingGoals.Queries.GetActiveReadingGoals;
using KitobdaGimen.Application.Features.ReadingGoals.Queries.GetFinishedReadingGoals;
using KitobdaGimen.Application.Features.Stories.Queries.GetUserStoryHistory;
using KitobdaGimen.Infrastructure.Identity;
using KitobdaGimen.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
[Route("profile")]
public class ProfileController : AppController
{
    // Allowed avatar types — always re-encoded to WebP, the original format is discarded.
    private static readonly HashSet<string> AllowedImageTypes = new()
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxImageBytes = 8 * 1024 * 1024; // 8 MB
    private const int MaxAvatarDimension = 512;

    private readonly IWebHostEnvironment _env;

    public ProfileController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// A user's profile with their posts. The route segment is a username (/profile/asadbek);
    /// a numeric value is still accepted for backward compatibility, and an empty segment
    /// defaults to the current user.
    /// </summary>
    [HttpGet("{id?}")]
    public async Task<IActionResult> Index(string? id, int page = 1)
    {
        int userId;
        if (string.IsNullOrWhiteSpace(id))
            userId = CurrentUserId!.Value;
        else if (int.TryParse(id, out var numericId))
            userId = numericId;
        else
            userId = await Mediator.Send(new GetUserIdByUsernameQuery(id));

        var profile = await Mediator.Send(new GetUserProfileQuery(userId));
        var posts = await Mediator.Send(new GetUserPostsQuery { UserId = userId, Page = page });
        var finishedBooks = await Mediator.Send(new GetFinishedReadingGoalsQuery(userId));
        var activeBooks = await Mediator.Send(new GetActiveReadingGoalsQuery(userId));
        var stories = await Mediator.Send(new GetUserStoryHistoryQuery(userId));

        // The viewed user's quotes are public — shown to everyone in the "Iqtiboslar" tab
        // (the delete control stays owner-only in the view).
        var quotes = (await Mediator.Send(new GetUserQuotesQuery(userId) { PageSize = 50 })).Items;

        return View(new ProfilePageViewModel
        {
            Profile = profile,
            Posts = posts,
            FinishedBooks = finishedBooks,
            CurrentBooks = activeBooks,
            Stories = stories,
            MyQuotes = quotes
        });
    }

    [HttpGet("edit")]
    public async Task<IActionResult> Edit()
    {
        var profile = await Mediator.Send(new GetUserProfileQuery(CurrentUserId!.Value));
        return View(profile);
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateProfileCommand command)
    {
        // Remember the previous avatar so we can delete the now-orphaned file after a successful change.
        var previous = await Mediator.Send(new GetUserProfileQuery(CurrentUserId!.Value));

        await Mediator.Send(command);

        if (!string.IsNullOrEmpty(previous.AvatarUrl) &&
            previous.AvatarUrl != command.AvatarUrl)
        {
            DeleteLocalUpload(previous.AvatarUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Live username availability check for the edit form (JSON).</summary>
    [HttpGet("check-username")]
    public async Task<IActionResult> CheckUsername(string? username)
    {
        var result = await Mediator.Send(new CheckUsernameQuery(username));
        return Json(result);
    }

    /// <summary>
    /// Permanently deletes the current account after the user re-types their own email.
    /// On success clears the auth cookie and tells the client where to go next.
    /// </summary>
    [HttpPost("delete-account")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string? email)
    {
        await Mediator.Send(new DeleteAccountCommand(email));
        Response.Cookies.Delete(AuthConstants.AccessTokenCookie);
        return Json(new { redirect = "/" });
    }

    /// <summary>Uploads a profile avatar, re-encodes it as a square-ish WebP and returns its public URL (JSON).</summary>
    [HttpPost("upload-avatar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile? file)
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
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
        {
            return BadRequest(new { message = "Fayl rasm formatida emas yoki buzilgan." });
        }

        using (image)
        {
            if (image.Width > MaxAvatarDimension || image.Height > MaxAvatarDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxAvatarDimension, MaxAvatarDimension)
                }));
            }

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadDir = Path.Combine(webRoot, "uploads", "avatars");
            Directory.CreateDirectory(uploadDir);

            var fileName = $"{Guid.NewGuid():N}.webp";
            var fullPath = Path.Combine(uploadDir, fileName);

            // Always saved as .webp — the original-format upload is never persisted.
            await image.SaveAsWebpAsync(fullPath, new WebpEncoder { Quality = 82 });

            var url = $"/uploads/avatars/{fileName}";
            return Json(new { url });
        }
    }

    /// <summary>Deletes a previously-uploaded avatar file living under /uploads/avatars/. Ignores anything else (e.g. external URLs).</summary>
    private void DeleteLocalUpload(string url)
    {
        const string prefix = "/uploads/avatars/";
        if (!url.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        var fileName = Path.GetFileName(url);
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, "uploads", "avatars", fileName);
        try
        {
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        catch
        {
            // Best-effort cleanup — never fail the request because an old file could not be removed.
        }
    }

    [HttpPost("{id:int}/follow")]
    public async Task<IActionResult> Follow(int id)
    {
        var result = await Mediator.Send(new ToggleFollowCommand(id));
        return Json(result);
    }

    [HttpGet("{id:int}/followers")]
    public async Task<IActionResult> Followers(int id, int page = 1)
    {
        var followers = await Mediator.Send(new GetFollowersQuery { UserId = id, Page = page });
        ViewData["Title"] = "Kuzatuvchilar";
        return View("FollowList", followers);
    }

    [HttpGet("{id:int}/following")]
    public async Task<IActionResult> Following(int id, int page = 1)
    {
        var following = await Mediator.Send(new GetFollowingQuery { UserId = id, Page = page });
        ViewData["Title"] = "Kuzatilayotganlar";
        return View("FollowList", following);
    }
}
