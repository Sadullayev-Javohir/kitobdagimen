namespace KitobdaGimen.Infrastructure.Identity;

/// <summary>Bound from the <c>Jwt</c> configuration section.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = "kitobdagimen.uz";
    public string Audience { get; set; } = "kitobdagimen.uz";
    public int ExpiryMinutes { get; set; } = 10080; // 7 kun
}
