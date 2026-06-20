using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace KitobdaGimen.Web.Seo;

/// <summary>
/// Builds <c>application/ld+json</c> structured-data strings (schema.org) for SEO.
/// Google uses these to render rich results and to understand the site/content.
/// Serialized via System.Text.Json so all values are correctly escaped (no injection
/// into the inline &lt;script&gt;). Output goes into <c>ViewData["JsonLd"]</c> and is
/// rendered in the layout head.
/// </summary>
public static class SeoJsonLd
{
    private static readonly JsonSerializerOptions Options = new()
    {
        // Allow non-ASCII (Uzbek) characters to stay readable; the encoder still escapes
        // '<', '>', '&', and quotes so the JSON cannot break out of the <script> block.
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static string Serialize(object graph) => JsonSerializer.Serialize(graph, Options);

    /// <summary>Site-wide identity for the landing page: WebSite + Organization.</summary>
    public static string Website(string baseUrl) => Serialize(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@graph"] = new object[]
        {
            new Dictionary<string, object?>
            {
                ["@type"] = "WebSite",
                ["@id"] = baseUrl + "/#website",
                ["url"] = baseUrl + "/",
                ["name"] = "kitobdagimen.uz",
                ["description"] = "O'zbek kitobxonlari uchun ijtimoiy platforma — kitob taqrizlari, iqtiboslar, o'qish maqsadlari va suhbat.",
                ["inLanguage"] = "uz",
                ["publisher"] = new Dictionary<string, object?> { ["@id"] = baseUrl + "/#organization" }
            },
            new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["@id"] = baseUrl + "/#organization",
                ["name"] = "kitobdagimen.uz",
                ["url"] = baseUrl + "/",
                ["logo"] = baseUrl + "/img/og-image.png"
            }
        }
    });

    /// <summary>A post (book review) detail page: BlogPosting with author and the book it is about.</summary>
    public static string BlogPosting(
        string url, string headline, string description, string? imageUrl,
        string authorName, string? authorUrl, string datePublishedIso,
        string bookTitle, string bookAuthor) => Serialize(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BlogPosting",
        ["mainEntityOfPage"] = url,
        ["url"] = url,
        ["headline"] = headline,
        ["description"] = description,
        ["image"] = imageUrl,
        ["inLanguage"] = "uz",
        ["datePublished"] = datePublishedIso,
        ["dateModified"] = datePublishedIso,
        ["author"] = new Dictionary<string, object?>
        {
            ["@type"] = "Person",
            ["name"] = authorName,
            ["url"] = authorUrl
        },
        ["publisher"] = new Dictionary<string, object?>
        {
            ["@type"] = "Organization",
            ["name"] = "kitobdagimen.uz"
        },
        ["about"] = new Dictionary<string, object?>
        {
            ["@type"] = "Book",
            ["name"] = bookTitle,
            ["author"] = new Dictionary<string, object?> { ["@type"] = "Person", ["name"] = bookAuthor }
        }
    });

    /// <summary>A public profile page: ProfilePage describing the Person.</summary>
    public static string ProfilePage(string url, string name, string? imageUrl, string? description) => Serialize(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "ProfilePage",
        ["url"] = url,
        ["inLanguage"] = "uz",
        ["mainEntity"] = new Dictionary<string, object?>
        {
            ["@type"] = "Person",
            ["name"] = name,
            ["image"] = imageUrl,
            ["description"] = description,
            ["url"] = url
        }
    });
}
