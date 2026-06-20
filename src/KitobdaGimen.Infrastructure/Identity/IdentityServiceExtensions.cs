using System.Text;
using KitobdaGimen.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace KitobdaGimen.Infrastructure.Identity;

public static class IdentityServiceExtensions
{
    /// <summary>
    /// Registers identity services and authentication: Google OAuth for the initial login
    /// and JWT (read from an HttpOnly cookie) for authenticating subsequent requests.
    /// </summary>
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<GoogleAuthSettings>(configuration.GetSection(GoogleAuthSettings.SectionName));

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();
        var googleSettings = configuration.GetSection(GoogleAuthSettings.SectionName).Get<GoogleAuthSettings>()
            ?? new GoogleAuthSettings();

        // FAIL-FAST: HS256 imzo kaliti yetarlicha kuchli bo'lishi SHART. Aks holda
        // hujumchi o'zi token soxtalashtirib istalgan hisobga kira oladi. Kalit bo'sh,
        // 32 belgidan qisqa yoki repodagi placeholder bo'lsa — startup'da yiqilamiz.
        // Production kaliti environment variable / user-secrets orqali beriladi:
        //   Configuration: Jwt:Key  (env: Jwt__Key)
        const string placeholderKey = "REPLACE_WITH_A_STRONG_SECRET_AT_LEAST_32_CHARACTERS_LONG";
        if (string.IsNullOrWhiteSpace(jwtSettings.Key)
            || jwtSettings.Key.Length < 32
            || jwtSettings.Key == placeholderKey)
        {
            throw new InvalidOperationException(
                "Jwt:Key sozlanmagan yoki juda zaif. Kamida 32 belgili maxfiy kalit qo'ying " +
                "(environment variable 'Jwt__Key' yoki user-secrets 'Jwt:Key'). " +
                "Placeholder qiymat bilan ishga tushirish mumkin emas.");
        }

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                // The JWT is delivered via an HttpOnly cookie, not the Authorization header.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.TryGetValue(AuthConstants.AccessTokenCookie, out var token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                    // Redirect browser page navigations to the landing page instead of returning 401.
                    OnChallenge = context =>
                    {
                        var request = context.Request;
                        var wantsHtml = request.Method == HttpMethods.Get &&
                            (request.Headers.Accept.ToString().Contains("text/html") ||
                             string.IsNullOrEmpty(request.Headers.Accept));
                        var isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest";

                        if (wantsHtml && !isAjax)
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/");
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            // Temporary cookie used only to correlate the Google OAuth round-trip.
            .AddCookie(AuthConstants.ExternalScheme, options =>
            {
                options.Cookie.Name = "kitobdagimen_external";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddGoogle(options =>
            {
                options.ClientId = string.IsNullOrEmpty(googleSettings.ClientId)
                    ? "placeholder-client-id"
                    : googleSettings.ClientId;
                options.ClientSecret = string.IsNullOrEmpty(googleSettings.ClientSecret)
                    ? "placeholder-client-secret"
                    : googleSettings.ClientSecret;
                options.SignInScheme = AuthConstants.ExternalScheme;
                options.SaveTokens = true;
                options.Scope.Add("email");
                options.Scope.Add("profile");
            });

        services.AddAuthorization();

        return services;
    }
}
