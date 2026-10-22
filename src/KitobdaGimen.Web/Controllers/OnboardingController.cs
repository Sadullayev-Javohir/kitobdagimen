using KitobdaGimen.Application.Features.Auth.Queries.GetCurrentUser;
using KitobdaGimen.Application.Features.Onboarding.Commands.CompleteProfile;
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
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("Index", "Feed");
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
        var targetUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action("Index", "Feed");
        return Json(new { user, nextUrl = targetUrl });
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

        // ContentType tekshiruvini yumshatamiz - ba'zi brauzerlar noto'g'ri type yuboradi.
        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        var isAllowedType = AllowedAvatarTypes.Contains(contentType);
        var hasImageExtension = file.FileName?.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) == true
            || file.FileName?.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) == true
            || file.FileName?.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == true
            || file.FileName?.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) == true
            || file.FileName?.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) == true;

        if (!isAllowedType && !hasImageExtension)
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

    /// <summary>Eski /Onboarding havolalarini /Feed ga yo'naltirish.</summary>
    [HttpGet]
    public IActionResult Index(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Feed");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Feed");
    }
}
