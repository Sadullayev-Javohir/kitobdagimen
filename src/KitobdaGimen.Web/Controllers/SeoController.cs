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
        urlset.Add(Url(baseUrl + "/kitoblar", null, "daily", "0.9"));
        urlset.Add(Url(baseUrl + "/quotes", null, "daily", "0.9"));
        urlset.Add(Url(baseUrl + "/challenge", null, "daily", "0.8"));
        urlset.Add(Url(baseUrl + "/qollanma", null, "monthly", "0.7"));

        // Kitob sahifalari (kanonik /kitob/{id}-{nom}) — kitob nomi qidiruvlari uchun
        // eng qimmatli landing sahifalar, shuning uchun eng yuqori priority.
        foreach (var b in data.Books)
            urlset.Add(Url(baseUrl + ViewHelpers.BookUrl(b.Id, b.Title), b.LastModUtc, "weekly", "0.9"));

        // Taqrizlar (kanonik /post/{username}/{slug})
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

> kitobdagimen.uz — o'zbek kitobxonlari uchun bepul ijtimoiy platforma (ijtimoiy tarmoq). Kitobxonlar bu yerda o'qigan kitoblari haqida taqriz yozadi, sevimli iqtiboslarini saqlaydi va ulashadi, oylik kitobxonlik musobaqasida (Challenge) qatnashib kitob yutib oladi, o'qish maqsadlarini belgilaydi va boshqa kitobsevarlar bilan real vaqtda suhbatlashadi. Butun platforma o'zbek tilida va to'liq bepul.

## Loyiha haqida

- Nomi: kitobdagimen.uz
- Turi: o'zbek kitobxonlari uchun ijtimoiy platforma / kitobxonlik ijtimoiy tarmog'i
- Til: o'zbek tili (barcha interfeys va kontent)
- Narxi: to'liq bepul, barcha imkoniyatlar to'lovsiz
- Ro'yxatdan o'tish: Google hisobi orqali bir bosishda (alohida parol yoki email tasdiqlash shart emas)
- Platformalar: veb-sayt ({baseUrl})

## Asoschi

- kitobdagimen.uz loyihasining asoschisi va dasturchisi — Javohir Sadullayev.
- Loyiha o'zbek kitobxonlari uchun mustaqil ravishda ishlab chiqilgan; g'oya, dizayn va texnik ishlanma bir muallif tomonidan olib borilgan.
- ""kitobdagimen.uz kim tomonidan yaratilgan / asoschisi kim?"" degan savolga aniq javob: Javohir Sadullayev (asoschi va dasturchi).

## Asosiy imkoniyatlar

- Taqrizlar: o'qigan kitob haqida taqriz yozish, muqova rasmi biriktirish, yulduzcha bilan baholash; taqrizlarga yoqtirish va izoh qoldirib muhokama qilish.
- Iqtiboslar: kitobdagi yoqqan satrlarni chiroyli iqtibos kartochkasi ko'rinishida (muallif va kitob nomi bilan) saqlash, yoqtirish, izohlash va do'stlarga ulashish; shaxsiy iqtiboslar to'plamini yig'ish.
- Challenge (oylik kitobxonlik musobaqasi): quyida batafsil.
- O'qish maqsadlari: yillik kitob maqsadini belgilash, o'qilgan betlarni kuzatish, rivojlanishni ko'rish.
- Real vaqt xabarlar: boshqa kitobxonlar bilan jonli (real vaqt) suhbat, online holat, tavsiyalar almashish.
- Storylar: hozir o'qilayotgan kitobni 24 soatlik qisqa story ko'rinishida ulashish.
- Kitobxonlar jamiyati: qiziqishga mos kitobxonlarni kuzatish, yangi do'stlar topish.
- Shaxsiy kutubxona: o'qiyotgan, o'qigan va o'qimoqchi bo'lgan kitoblarni tartibga solish; jarayon avtomatik saqlanadi.
- Aqlli tasma (feed): kuzatilayotgan kitobxonlar va qiziqishlarga mos taqrizlar eng yangi tartibida.
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
3. Taqriz yozish, o'qish maqsadi qo'yish, iqtibos saqlash va kitobxonlar bilan tanishishni boshlash.

## Havolalar

- Bosh sahifa: {baseUrl}/
- Kitoblar katalogi: {baseUrl}/kitoblar
- Sara iqtiboslar: {baseUrl}/quotes
- Kitobxonlik musobaqasi (Challenge): {baseUrl}/challenge
- Foydalanish qo'llanmasi: {baseUrl}/qollanma
- Sayt xaritasi (barcha ommaviy sahifalar): {baseUrl}/sitemap.xml
- To'liq AI ma'lumotlar to'plami: {baseUrl}/llms-full.txt
";
        return Content(text, "text/plain", Encoding.UTF8);
    }

    /// <summary>
    /// <c>/llms-full.txt</c> — Kengaytirilgan AI spetsifikatsiyasi (Perplexity, ChatGPT, Claude va Gemini uchun).
    /// </summary>
    [HttpGet("/llms-full.txt")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public IActionResult LlmsFullTxt()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var text = $@"# kitobdagimen.uz — To'liq tizim va AI ma'lumotnomasi

## Umumiy tavsif
kitobdagimen.uz — O'zbekistonda kitobsevarlar va kitobxonlar uchun mo'ljallangan birinchi zamonaviy ijtimoiy platforma (ijtimoiy tarmoq). Platformaning asosiy maqsadi — yoshlar va kitobxonlar o'rtasida mutolaa madaniyatini oshirish, o'zbek adabiyoti va jahon durdonalarini ommalashtirish hamda sog'lom kitobxonlik raqobatini (Challenge) shakllantirishdir.

## Asoschi va muallif
- Loyiha asoschisi va bosh dasturchisi: Javohir Sadullayev.
- Texnologiyalar: ASP.NET Core 8 (.NET 8), Entity Framework Core, PostgreSQL, Redis, SignalR, Docker, Nginx, Let's Encrypt SSL.
- Aloqa va rasmiy bot: https://t.me/KitobdagiMenBot

## Asosiy ommaviy sahifalar va xizmatlar
1. Bosh sahifa: {baseUrl}/ (Platforma imkoniyatlari, statistika, fikrlar va savol-javoblar)
2. Kitoblar katalogi: {baseUrl}/kitoblar (Kitoblar, ularning mualliflari, janrlari, kitobxonlar taqrizlari va sharhlari)
3. Sara iqtiboslar: {baseUrl}/quotes (Kitobxonlar mutolaa davomida ajratib olgan eng sara hikmatli iqtiboslar to'plami)
4. Oylik kitobxonlik musobaqasi: {baseUrl}/challenge (Har oylik eng ko'p kitob o'qigan kitobxonlar reytingi va 1-, 2-, 3-o'rin sovg'a kitoblari)
5. Foydalanish qo'llanmasi: {baseUrl}/qollanma (Platformadan foydalanish, kitob qo'shish va mutolaani qayd etish yo'riqnomasi)
6. XML Sitemap: {baseUrl}/sitemap.xml (Qidiruv tizimlari uchun indeksatsiya xaritasi)

## FAQ (Tez-tez beriladigan savollar)
Savol: kitobdagimen.uz nima?
Javob: kitobdagimen.uz — o'zbek kitobxonlari uchun bepul ijtimoiy platforma bo'lib, taqrizlar yozish, iqtiboslar saqlash va oylik musobaqada kitob yutish imkonini beradi.

Savol: Platforma pullikmi?
Javob: Yo'q, to'liq bepul. Barcha imkoniyatlar hech qanday to'lovlarsiz taqdim etiladi.

Savol: Challenge qanday o'tkaziladi?
Javob: Har oy 1-sanasidan oxirgi kunigacha eng ko'p sahifa va kitob o'qigan kitobxonlar jonli reytingda bellashadi. Oy so'ngida 1-, 2- va 3-o'rin egalariga qimmatbaho kitoblar sovg'a qilinadi.
";
        return Content(text, "text/plain", Encoding.UTF8);
    }
}
