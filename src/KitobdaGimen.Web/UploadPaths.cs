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
        if (!string.IsNullOrWhiteSpace(configured))
        {
            Root = configured;
        }
        else if (env.IsDevelopment())
        {
            // Lokalda — odatdagi wwwroot/uploads.
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            Root = Path.Combine(webRoot, "uploads");
        }
        else
        {
            // Productionда — publish'dan TASHQARIDA (ContentRoot = .../publish, uning ota
            // papkasi = .../kitobdagimen). `rm -rf publish` buni o'chirmaydi, shuning uchun
            // yuklamalar har deployda saqlanadi. Env (Uploads__RootPath) bilan ham bekor qilsa bo'ladi.
            Root = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "uploads"));
        }

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
