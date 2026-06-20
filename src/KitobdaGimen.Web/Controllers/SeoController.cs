using System.Text;
using System.Xml.Linq;
using KitobdaGimen.Application.Features.Seo.Queries.GetSitemap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

/// <summary>
/// SEO endpoints. <c>robots.txt</c> is served as a static file from wwwroot; this controller
/// builds <c>/sitemap.xml</c> dynamically from the current public posts and profiles so Google
/// always sees fresh, indexable URLs. The base URL is taken from the request, so it is correct
/// on both production (kitobdagimen.uz) and any other host.
/// </summary>
[AllowAnonymous]
public class SeoController : AppController
{
    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap()
    {
        var data = await Mediator.Send(new GetSitemapQuery());
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urlset = new XElement(ns + "urlset");

        XElement Url(string loc, DateTime? lastMod, string changefreq, string priority)
        {
            var el = new XElement(ns + "url", new XElement(ns + "loc", loc));
            if (lastMod is not null)
                el.Add(new XElement(ns + "lastmod", lastMod.Value.ToString("yyyy-MM-dd")));
            el.Add(new XElement(ns + "changefreq", changefreq));
            el.Add(new XElement(ns + "priority", priority));
            return el;
        }

        static string Seg(string s) => Uri.EscapeDataString(s);

        // Statik ommaviy sahifalar
        urlset.Add(Url(baseUrl + "/", null, "daily", "1.0"));
        urlset.Add(Url(baseUrl + "/qollanma", null, "monthly", "0.7"));

        // Postlar (kanonik /post/{username}/{slug})
        foreach (var p in data.Posts)
            urlset.Add(Url($"{baseUrl}/post/{Seg(p.AuthorRef)}/{Seg(p.Slug)}", p.LastModUtc, "weekly", "0.8"));

        // Ommaviy profillar (/u/{username})
        foreach (var u in data.Profiles)
            urlset.Add(Url($"{baseUrl}/u/{Seg(u.Ref)}", u.LastModUtc, "weekly", "0.6"));

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), urlset);
        var xml = doc.Declaration + Environment.NewLine + doc.ToString(SaveOptions.DisableFormatting);
        return Content(xml, "application/xml", Encoding.UTF8);
    }
}
