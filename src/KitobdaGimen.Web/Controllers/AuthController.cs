using System.Security.Claims;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Auth.Commands.LoginWithGoogle;
using KitobdaGimen.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

[Route("auth")]
public class AuthController : AppController
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    /// <summary>Starts the Google OAuth challenge.</summary>
    [HttpGet("google-login")]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
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

        // Store the issued JWT in an HttpOnly cookie and clear the temporary external cookie.
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

        if (authResult.RequiresOnboarding)
        {
            return RedirectToAction("Index", "Onboarding", new { returnUrl });
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
        Response.Cookies.Delete(AuthConstants.AccessTokenCookie);
        return RedirectToAction("Index", "Home");
    }
}
