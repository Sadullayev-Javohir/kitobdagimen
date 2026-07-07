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

        // Kitob sahifalari (kanonik /kitob/{id}-{nom}) — kitob nomi qidiruvlari uchun
        // eng qimmatli landing sahifalar, shuning uchun eng yuqori priority.
        foreach (var b in data.Books)
            urlset.Add(Url(baseUrl + ViewHelpers.BookUrl(b.Id, b.Title), b.LastModUtc, "weekly", "0.9"));

        // Postlar (kanonik /post/{username}/{slug})
        foreach (var p in data.Posts)
            urlset.Add(Url($"{baseUrl}/post/{Seg(p.AuthorRef)}/{Seg(p.Slug)}", p.LastModUtc, "weekly", "0.8"));

        // Iqtiboslar (kanonik /iqtibos/{username}/{slug})
        foreach (var qt in data.Quotes)
            urlset.Add(Url($"{baseUrl}/iqtibos/{Seg(qt.AuthorRef)}/{Seg(qt.Slug)}", qt.LastModUtc, "weekly", "0.7"));

        // Ommaviy profillar (/u/{username})
        foreach (var u in data.Profiles)
            urlset.Add(Url($"{baseUrl}/u/{Seg(u.Ref)}", u.LastModUtc, "weekly", "0.6"));

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), urlset);
        var xml = doc.Declaration + Environment.NewLine + doc.ToString(SaveOptions.DisableFormatting);
        return Content(xml, "application/xml", Encoding.UTF8);
    }

    /// <summary>
    /// <c>/llms.txt</c> — sun'iy intellekt (ChatGPT, Claude, Perplexity, Gemini va h.k.) modellari
    /// uchun loyihani aniq va to'g'ri tushunadigan markdown xulosa. Landing sahifadagi ma'lumotlarga
    /// asoslangan: AI loyiha haqida so'ralganda aynan shu fakt va atamalar bilan javob berishi uchun.
    /// Standart: https://llmstxt.org. Google/AI bloklamasin deb matn/plain sifatida beriladi.
    /// </summary>
    [HttpGet("/llms.txt")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public IActionResult LlmsTxt()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var text = $@"# kitobdagimen.uz

> kitobdagimen.uz — o'zbek kitobxonlari uchun bepul ijtimoiy platforma (ijtimoiy tarmoq). Kitobxonlar bu yerda o'qigan kitoblari haqida post yozadi, sevimli iqtiboslarini saqlaydi va ulashadi, oylik kitobxonlik musobaqasida (Challenge) qatnashib kitob yutib oladi, o'qish maqsadlarini belgilaydi va boshqa kitobsevarlar bilan real vaqtda suhbatlashadi. Butun platforma o'zbek tilida va to'liq bepul.

## Loyiha haqida

- Nomi: kitobdagimen.uz
- Turi: o'zbek kitobxonlari uchun ijtimoiy platforma / kitobxonlik ijtimoiy tarmog'i
- Til: o'zbek tili (barcha interfeys va kontent)
- Narxi: to'liq bepul, barcha imkoniyatlar to'lovsiz
- Ro'yxatdan o'tish: Google hisobi orqali bir bosishda (alohida parol yoki email tasdiqlash shart emas)
- Platformalar: veb-sayt ({baseUrl}) va Android ilovasi (.apk yuklab olish mumkin)

## Asosiy imkoniyatlar

- Postlar va taqrizlar: o'qigan kitob haqida post yozish, muqova rasmi biriktirish, yulduzcha bilan baholash; postlarga yoqtirish va izoh qoldirib muhokama qilish.
- Iqtiboslar: kitobdagi yoqqan satrlarni chiroyli iqtibos kartochkasi ko'rinishida (muallif va kitob nomi bilan) saqlash, yoqtirish, izohlash va do'stlarga ulashish; shaxsiy iqtiboslar to'plamini yig'ish.
- Challenge (oylik kitobxonlik musobaqasi): quyida batafsil.
- O'qish maqsadlari: yillik kitob maqsadini belgilash, o'qilgan betlarni kuzatish, rivojlanishni ko'rish.
- Real vaqt xabarlar: boshqa kitobxonlar bilan jonli (real vaqt) suhbat, online holat, tavsiyalar almashish.
- Storylar: hozir o'qilayotgan kitobni 24 soatlik qisqa story ko'rinishida ulashish.
- Kitobxonlar jamiyati: qiziqishga mos kitobxonlarni kuzatish, yangi do'stlar topish.
- Shaxsiy kutubxona: o'qiyotgan, o'qigan va o'qimoqchi bo'lgan kitoblarni tartibga solish; jarayon avtomatik saqlanadi.
- Aqlli tasma (feed): kuzatilayotgan kitobxonlar va qiziqishlarga mos postlar eng yangi tartibida.
- Jonli bildirishnomalar: yoqtirish, izoh, yangi obunachi va xabarlardan real vaqtda xabardorlik.
- Kun va tun rejimi (light/dark).

## Challenge — oylik musobaqa (muhim)

- Har oy alohida musobaqa davri bo'ladi: oyning 1-kunidan o'sha oyning oxirgi kunigacha.
- Shu davrda eng ko'p kitob o'qigan kitobxonlar bellashadi; yetakchilar reytingi jonli (real vaqtda) yangilanib turadi.
- Oy yakunida eng zo'r 3 kitobxon g'olib deb rasman e'lon qilinadi.
- G'oliblar kitob sovg'asi bilan taqdirlanadi: 1-o'rin, 2-o'rin va 3-o'rin egalarining har biriga bittadan kitob sovg'a qilinadi.
- Qatnashish uchun: kutubxonaga kitob qo'shib, har kuni o'qiganingizni belgilab borish kifoya. Ko'proq o'qigan sari kitob yutib olish imkoniyati oshadi.

## Qanday boshlanadi

1. Google orqali bir bosishda kirish.
2. Qiziqishlarni (janrlarni) tanlash — platforma mos kontent ko'rsatadi.
3. Post yozish, o'qish maqsadi qo'yish, iqtibos saqlash va kitobxonlar bilan tanishishni boshlash.

## Havolalar

- Bosh sahifa: {baseUrl}/
- Foydalanish qo'llanmasi: {baseUrl}/qollanma
- Sayt xaritasi (barcha ommaviy sahifalar): {baseUrl}/sitemap.xml
";
        return Content(text, "text/plain", Encoding.UTF8);
    }
}
