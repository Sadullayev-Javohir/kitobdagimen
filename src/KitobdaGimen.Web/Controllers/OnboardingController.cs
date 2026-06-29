using KitobdaGimen.Application.Features.Auth.Queries.GetCurrentUser;
using KitobdaGimen.Application.Features.Onboarding.Commands.CompleteProfile;
using KitobdaGimen.Application.Features.Onboarding.Commands.SaveUserGenres;
using KitobdaGimen.Application.Features.Onboarding.Queries.GetGenres;
using KitobdaGimen.Application.Features.Onboarding.Queries.GetOnboardingStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
public class OnboardingController : AppController
{
    private const long MaxAvatarBytes = 5 * 1024 * 1024; // 5 MB
    private const int MaxAvatarDimension = 600;
    private static readonly HashSet<string> AllowedAvatarTypes = new()
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private readonly IWebHostEnvironment _env;

    public OnboardingController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>Mandatory username + full name step shown right after a user's first Google signup.</summary>
    [HttpGet("Onboarding/Profile")]
    public async Task<IActionResult> Profile(string? returnUrl = null)
    {
        // The profile step is part of the post-signup flow only. Once the user has
        // picked a username it is done, so they can never reach it again by URL.
        var status = await Mediator.Send(new GetOnboardingStatusQuery());
        if (status.HasUsername)
        {
            return status.HasGenres
                ? RedirectToAction("Index", "Feed")
                : RedirectToAction(nameof(Index), new { returnUrl });
        }

        var me = await Mediator.Send(new GetCurrentUserQuery());
        ViewData["ReturnUrl"] = returnUrl;
        return View(me);
    }

    [HttpPost("Onboarding/Profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile([FromBody] CompleteProfileCommand command, string? returnUrl = null)
    {
        var user = await Mediator.Send(command);
        return Json(new { user, nextUrl = Url.Action("Index", "Onboarding", new { returnUrl }) });
    }

    /// <summary>Uploads an optional profile picture during onboarding and returns its public URL (JSON).</summary>
    [HttpPost("Onboarding/upload-avatar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Rasm tanlanmadi." });
        }

        if (file.Length > MaxAvatarBytes)
        {
            return BadRequest(new { message = "Rasm hajmi 5 MB dan oshmasligi kerak." });
        }

        if (!AllowedAvatarTypes.Contains(file.ContentType))
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
            if (image.Width > MaxAvatarDimension || image.Height > MaxAvatarDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxAvatarDimension, MaxAvatarDimension)
                }));
            }

            var uploadDir = KitobdaGimen.Web.UploadPaths.Dir("avatars");

            var fileName = $"{Guid.NewGuid():N}.webp";
            var fullPath = Path.Combine(uploadDir, fileName);

            await image.SaveAsWebpAsync(fullPath, new WebpEncoder { Quality = 80 });

            var url = $"/uploads/avatars/{fileName}";
            return Json(new { url });
        }
    }

    /// <summary>Genre interest picker shown after first login.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        // /Onboarding is reachable only as the next step right after signup.
        // Users who already finished onboarding (have genres) can't open it by URL,
        // and users who haven't set a username yet are sent to the profile step first.
        var status = await Mediator.Send(new GetOnboardingStatusQuery());
        if (status.HasGenres)
        {
            return RedirectToAction("Index", "Feed");
        }
        if (!status.HasUsername)
        {
            return RedirectToAction(nameof(Profile), new { returnUrl });
        }

        var genres = await Mediator.Send(new GetGenresQuery());
        ViewData["ReturnUrl"] = returnUrl;
        return View(genres);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int[] genreIds, string? returnUrl = null)
    {
        await Mediator.Send(new SaveUserGenresCommand { GenreIds = genreIds ?? Array.Empty<int>() });
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Feed");
    }
}
