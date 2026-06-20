namespace KitobdaGimen.Infrastructure.Identity;

/// <summary>Bound from the <c>Authentication:Google</c> configuration section.</summary>
public class GoogleAuthSettings
{
    public const string SectionName = "Authentication:Google";

    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
}
