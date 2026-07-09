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

    /// <summary>
    /// Site-wide identity for the landing page: WebSite + Organization + the founder (Person) +
    /// an optional FAQPage. The founder node lets Google and AI systems answer "kitobdagimen.uz
    /// asoschisi kim?" with a verifiable structured fact. When the landing FAQ items are passed,
    /// a FAQPage is emitted so the same visible questions can earn a rich (accordion) result.
    /// </summary>
    public static string Website(
        string baseUrl,
        IReadOnlyList<(string Question, string Answer)>? faq = null)
    {
        var graph = new List<object>
        {
            new Dictionary<string, object?>
            {
                ["@type"] = "WebSite",
                ["@id"] = baseUrl + "/#website",
                ["url"] = baseUrl + "/",
                ["name"] = "kitobdagimen.uz",
                ["description"] = "O'zbek kitobxonlari uchun bepul ijtimoiy platforma — kitob haqida post yozish, iqtiboslar saqlash, oylik kitobxonlik musobaqasida (Challenge) qatnashib kitob yutish, o'qish maqsadlari va real vaqt suhbat.",
                ["inLanguage"] = "uz",
                ["publisher"] = new Dictionary<string, object?> { ["@id"] = baseUrl + "/#organization" }
            },
            new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["@id"] = baseUrl + "/#organization",
                ["name"] = "kitobdagimen.uz",
                ["url"] = baseUrl + "/",
                ["logo"] = baseUrl + "/img/logo.png",
                ["description"] = "O'zbek kitobxonlari uchun bepul ijtimoiy platforma.",
                ["founder"] = new Dictionary<string, object?> { ["@id"] = baseUrl + "/#founder" }
            },
            new Dictionary<string, object?>
            {
                ["@type"] = "Person",
                ["@id"] = baseUrl + "/#founder",
                ["name"] = "Javohir Sadullayev",
                ["jobTitle"] = "Asoschi va dasturchi",
                ["description"] = "kitobdagimen.uz — o'zbek kitobxonlari uchun bepul ijtimoiy platformaning asoschisi va dasturchisi.",
                ["worksFor"] = new Dictionary<string, object?> { ["@id"] = baseUrl + "/#organization" },
                ["url"] = baseUrl + "/"
            }
        };

        if (faq is { Count: > 0 })
        {
            graph.Add(new Dictionary<string, object?>
            {
                ["@type"] = "FAQPage",
                ["@id"] = baseUrl + "/#faq",
                ["mainEntity"] = faq.Select(qa => new Dictionary<string, object?>
                {
                    ["@type"] = "Question",
                    ["name"] = qa.Question,
                    ["acceptedAnswer"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Answer",
                        ["text"] = qa.Answer
                    }
                }).ToArray()
            });
        }

        return Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = graph
        });
    }

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

    /// <summary>
    /// Kitob sahifasi (/kitob/{id}-{nom}): Book + uni tavsiflovchi CollectionPage.
    /// Kitob nomi bo'yicha qidiruvda Google sahifani aynan shu kitob haqidagi
    /// jamlovchi sahifa deb tushunishi uchun.
    /// </summary>
    public static string BookPage(
        string url, string bookTitle, string bookAuthor, string? imageUrl,
        int reviewCount, int quoteCount) => Serialize(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@graph"] = new object[]
        {
            new Dictionary<string, object?>
            {
                ["@type"] = "Book",
                ["@id"] = url + "#book",
                ["name"] = bookTitle,
                ["image"] = imageUrl,
                ["inLanguage"] = "uz",
                ["author"] = new Dictionary<string, object?> { ["@type"] = "Person", ["name"] = bookAuthor },
                ["url"] = url
            },
            new Dictionary<string, object?>
            {
                ["@type"] = "CollectionPage",
                ["url"] = url,
                ["name"] = $"{bookTitle} — {bookAuthor}: taqrizlar va iqtiboslar",
                ["inLanguage"] = "uz",
                ["about"] = new Dictionary<string, object?> { ["@id"] = url + "#book" },
                ["description"] = $"\"{bookTitle}\" ({bookAuthor}) haqida {reviewCount} ta taqriz va {quoteCount} ta iqtibos.",
                ["publisher"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Organization",
                    ["name"] = "kitobdagimen.uz"
                }
            }
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

    /// <summary>
    /// A quote detail page: schema.org <c>Quotation</c> that is <c>about</c> / <c>isPartOf</c> the
    /// source book, so Google understands the page is a quote from that book (helps it surface for
    /// book-title searches). The quoting user is the <c>creator</c>.
    /// </summary>
    public static string Quotation(
        string url, string text, string authorName, string? authorUrl,
        string datePublishedIso, string bookTitle, string bookAuthor, string? bookImageUrl) => Serialize(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "Quotation",
        ["mainEntityOfPage"] = url,
        ["url"] = url,
        ["text"] = text,
        ["inLanguage"] = "uz",
        ["datePublished"] = datePublishedIso,
        ["about"] = new Dictionary<string, object?>
        {
            ["@type"] = "Book",
            ["name"] = bookTitle,
            ["image"] = bookImageUrl,
            ["author"] = new Dictionary<string, object?> { ["@type"] = "Person", ["name"] = bookAuthor }
        },
        ["isPartOf"] = new Dictionary<string, object?>
        {
            ["@type"] = "Book",
            ["name"] = bookTitle
        },
        ["creator"] = new Dictionary<string, object?>
        {
            ["@type"] = "Person",
            ["name"] = authorName,
            ["url"] = authorUrl
        },
        ["publisher"] = new Dictionary<string, object?>
        {
            ["@type"] = "Organization",
            ["name"] = "kitobdagimen.uz"
        }
    });
}
