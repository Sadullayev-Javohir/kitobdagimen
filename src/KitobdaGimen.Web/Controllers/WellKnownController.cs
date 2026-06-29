using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

/// <summary>
/// Digital Asset Links — Android Trusted Web Activity (TWA) uchun.
/// <para>
/// <c>/.well-known/assetlinks.json</c> faylini qaytaradi. Bu fayl Android ilova
/// (paket nomi + imzo SHA-256 barmoq izi) bilan ushbu domen o'rtasidagi ishonchni
/// tasdiqlaydi. Tasdiqdan so'ng TWA ilovada brauzer URL paneli ko'rinmaydi va Chrome
/// saytni "ishonchli" deb biladi.
/// </para>
/// <para>
/// Qiymatlar konfiguratsiyadan olinadi: <c>Android:PackageName</c> va
/// <c>Android:Sha256CertFingerprints</c> (massiv). Barmoq izlari hali sozlanmagan
/// bo'lsa, bo'sh massiv qaytadi (sayt ishlayveradi, lekin TWA tasdiqlanmaydi).
/// </para>
/// </summary>
[AllowAnonymous]
public class WellKnownController : ControllerBase
{
    private readonly IConfiguration _config;

    public WellKnownController(IConfiguration config) => _config = config;

    [HttpGet("/.well-known/assetlinks.json")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult AssetLinks()
    {
        var packageName = _config["Android:PackageName"];
        var fingerprints = _config.GetSection("Android:Sha256CertFingerprints").Get<string[]>()
                           ?? Array.Empty<string>();

        // Barmoq izlarini normalizatsiya qilamiz (bo'sh/None qiymatlarni olib tashlash).
        fingerprints = fingerprints
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Trim())
            .ToArray();

        // Paket nomi yoki barmoq izi yo'q bo'lsa — bo'sh, lekin yaroqli JSON massiv.
        if (string.IsNullOrWhiteSpace(packageName) || fingerprints.Length == 0)
        {
            return Content("[]", "application/json");
        }

        // Digital Asset Links kalitlari ANIQ snake_case bo'lishi shart. MVC'ning
        // standart camelCase siyosatiga bog'lanib qolmaslik uchun qo'lda seriyalaymiz.
        var statement = new List<Dictionary<string, object>>
        {
            new()
            {
                ["relation"] = new[] { "delegate_permission/common.handle_all_urls" },
                ["target"] = new Dictionary<string, object>
                {
                    ["namespace"] = "android_app",
                    ["package_name"] = packageName,
                    ["sha256_cert_fingerprints"] = fingerprints
                }
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(
            statement,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        return Content(json, "application/json");
    }
}
