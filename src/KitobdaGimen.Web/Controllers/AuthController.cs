using System.Security.Claims;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Auth.Commands.LoginWithGoogle;
using KitobdaGimen.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Web.Controllers;

[Route("auth")]
public class AuthController : AppController
{
    private readonly ITokenService _tokenService;
    private readonly ITelegramBotService _telegramBot;
    private readonly IAppDbContext _db;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ITokenService tokenService,
        ITelegramBotService telegramBot,
        IAppDbContext db,
        ILogger<AuthController> logger)
    {
        _tokenService = tokenService;
        _telegramBot = telegramBot;
        _db = db;
        _logger = logger;
    }

    [HttpPost("telegram-login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TelegramLogin([FromForm] string? code, [FromForm] string? returnUrl = null, CancellationToken ct = default)
    {
        return await SignInWithOtpCodeAsync(code, returnUrl, ct);
    }

    [HttpGet("tg-magic")]
    public async Task<IActionResult> TelegramMagicLogin([FromQuery] string? code, [FromQuery] string? returnUrl = null, CancellationToken ct = default)
    {
        return await SignInWithOtpCodeAsync(code, returnUrl, ct);
    }

    private async Task<IActionResult> SignInWithOtpCodeAsync(string? code, string? returnUrl, CancellationToken ct)
    {
        var entry = _telegramBot.VerifyLoginOtp(code ?? "");
        if (entry is null)
        {
            TempData["LoginError"] = "Kiritilgan kod noto'g'ri yoki 2 daqiqalik muddati tugagan. Botdan «🔄 Qaytadan yuborish» tugmasi orqali yangi kod oling.";
            return RedirectToAction("Login", "Home", new { returnUrl });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == entry.UserId, ct);
        if (user is null)
        {
            TempData["LoginError"] = "Foydalanuvchi profili topilmadi. Iltimos, botda /start buyrug'ini bosing.";
            return RedirectToAction("Login", "Home", new { returnUrl });
        }

        user.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var token = _tokenService.GenerateToken(user);
        var isSecure = Request.IsHttps ||
                       string.Equals(Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);

        Response.Cookies.Append(AuthConstants.AccessTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = isSecure ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(_tokenService.TokenLifetime)
        });

        _ = _telegramBot.NotifyWebLoginSuccessAsync(entry.TelegramId, CancellationToken.None);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Feed");
    }

    /// <summary>
    /// Telegram Mini App (WebApp) ichidan avtomatik kirish: window.Telegram.WebApp.initData
    /// HMAC-SHA256 imzosi tekshiriladi va foydalanuvchiga sessiya beriladi.
    /// </summary>
    [HttpPost("telegram-webapp")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> TelegramWebAppLogin([FromForm] string? initData, [FromForm] string? returnUrl = null, CancellationToken ct = default)
    {
        var tgUser = _telegramBot.ValidateWebAppInitData(initData);
        if (tgUser is null)
        {
            return BadRequest(new { ok = false, message = "Telegram WebApp initData yaroqsiz." });
        }

        var tgKey = $"tg:{tgUser.TelegramId}";
        var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == tgKey, ct);

        if (user is null)
        {
            var fullName = $"{tgUser.FirstName} {tgUser.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName)) fullName = "Kitobxon";

            var baseUsername = !string.IsNullOrWhiteSpace(tgUser.Username)
                ? System.Text.RegularExpressions.Regex.Replace(tgUser.Username.ToLowerInvariant(), @"[^a-z0-9_]", "")
                : $"user_{tgUser.TelegramId}";
            if (baseUsername.Length < 3) baseUsername = $"user_{tgUser.TelegramId}";
            if (baseUsername.Length > 28) baseUsername = baseUsername[..28];

            var taken = await _db.Users.AnyAsync(u => u.Username != null && u.Username.ToLower() == baseUsername, ct);
            var finalUsername = taken ? $"tg_{tgUser.TelegramId}" : baseUsername;

            user = new KitobdaGimen.Domain.Entities.User
            {
                GoogleId = tgKey,
                Email = $"{tgUser.TelegramId}@tg.kitobdagimen.uz",
                FullName = fullName,
                Username = finalUsername,
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            user.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        var token = _tokenService.GenerateToken(user);
        var isSecure = Request.IsHttps ||
                       string.Equals(Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);

        Response.Cookies.Append(AuthConstants.AccessTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = isSecure ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(_tokenService.TokenLifetime)
        });

        var target = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/Feed";

        return Json(new { ok = true, redirectUrl = target, redirect = target });
    }

    /// <summary>Redirects legacy /auth/google-login links to the Telegram Bot login page.</summary>
    [HttpGet("google-login")]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        return RedirectToAction("Login", "Home", new { returnUrl });
    }

    /// <summary>Clears current session and redirects to the Telegram Bot login page.</summary>
    [HttpGet("switch-account")]
    public IActionResult SwitchAccount(string? returnUrl = null)
    {
        Response.Cookies.Delete(AuthConstants.AccessTokenCookie);
        return RedirectToAction("Login", "Home", new { returnUrl });
    }

    /// <summary>Handles the Google callback: signs the user in and stores the JWT in an HttpOnly cookie.</summary>
    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        var result = await HttpContext.AuthenticateAsync(AuthConstants.ExternalScheme);
        if (!result.Succeeded || result.Principal is null)
        {
            return Redirect("/?xato=google");
        }

        var principal = result.Principal;
        var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var fullName = principal.FindFirstValue(ClaimTypes.Name) ?? email;
        var avatarUrl = principal.FindFirstValue("picture") ?? principal.FindFirstValue("urn:google:picture");

        if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
        {
            return Redirect("/?xato=malumot");
        }

        var authResult = await Mediator.Send(new LoginWithGoogleCommand
        {
            GoogleId = googleId,
            Email = email,
            FullName = fullName!,
            AvatarUrl = avatarUrl
        });

        Response.Cookies.Append(AuthConstants.AccessTokenCookie, authResult.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(_tokenService.TokenLifetime)
        });

        await HttpContext.SignOutAsync(AuthConstants.ExternalScheme);

        if (authResult.RequiresProfileSetup)
        {
            return RedirectToAction("Profile", "Onboarding", new { returnUrl });
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Feed");
    }

    /// <summary>Clears the auth cookie and returns to the landing page.</summary>
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        try
        {
            _logger.LogInformation("User logging out: {UserId}", CurrentUserId);
            Response.Cookies.Delete(AuthConstants.AccessTokenCookie);
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed for user {UserId}", CurrentUserId);
            throw;
        }
    }
}
