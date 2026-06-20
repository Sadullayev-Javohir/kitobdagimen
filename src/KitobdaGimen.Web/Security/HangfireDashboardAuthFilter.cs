using System.Security.Claims;
using Hangfire.Dashboard;

namespace KitobdaGimen.Web.Security;

/// <summary>
/// Hangfire dashboard'ni himoyalaydi. Reverse-proxy (nginx) ortida Hangfire'ning
/// standart "faqat localdan" filtri ISHLAMAYDI — barcha so'rovlar nginx'dan, ya'ni
/// 127.0.0.1 dan kelgandek ko'rinadi, natijada dashboard butun internetga ochilib qoladi.
/// Shu sababli: faqat autentifikatsiyalangan VA email'i konfiguratsiyadagi admin
/// ro'yxatida bo'lgan foydalanuvchiga ruxsat beramiz. Ro'yxat bo'sh bo'lsa — hech kimga
/// ruxsat yo'q (xavfsiz default).
/// </summary>
public sealed class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    private readonly HashSet<string> _allowedEmails;

    public HangfireDashboardAuthFilter(IEnumerable<string> allowedEmails)
    {
        _allowedEmails = new HashSet<string>(
            allowedEmails.Select(e => e.Trim()).Where(e => e.Length > 0),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool Authorize(DashboardContext context)
    {
        if (_allowedEmails.Count == 0)
        {
            return false;
        }

        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("email");

        return email is not null && _allowedEmails.Contains(email);
    }
}
