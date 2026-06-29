namespace KitobdaGimen.Web;

/// <summary>
/// Resolves the directory where user uploads (avatars, book covers, post images) are stored.
///
/// CRITICAL: uploads must live OUTSIDE the publish output. Deploys do <c>rm -rf publish</c> +
/// <c>dotnet publish</c>, which would wipe anything under <c>publish/wwwroot/uploads</c>. By
/// default (dev) we still use <c>wwwroot/uploads</c>, but in production set
/// <c>Uploads:RootPath</c> (env: <c>Uploads__RootPath</c>) to a persistent path such as
/// <c>/var/www/kitobdagimen/uploads</c> so user images survive every deploy.
/// </summary>
public static class UploadPaths
{
    /// <summary>Absolute path to the uploads root. Set once at startup via <see cref="Configure"/>.</summary>
    public static string Root { get; private set; } = "";

    public static void Configure(IWebHostEnvironment env, IConfiguration config)
    {
        var configured = config["Uploads:RootPath"];
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        Root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(webRoot, "uploads")
            : configured;

        // Ensure the sub-folders exist so the first upload never fails.
        foreach (var sub in new[] { "covers", "avatars", "posts" })
        {
            Directory.CreateDirectory(Path.Combine(Root, sub));
        }
    }

    /// <summary>Folder for a category ("covers" / "avatars" / "posts"), created if missing.</summary>
    public static string Dir(string category)
    {
        var dir = Path.Combine(Root, category);
        Directory.CreateDirectory(dir);
        return dir;
    }
}
