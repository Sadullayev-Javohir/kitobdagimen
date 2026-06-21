# kitobdagimen.uz — SEO optimizatsiya rejasi

> Hozirgi texnik SEO poydevori (meta, Open Graph, Twitter Card, canonical, robots.txt,
> dinamik sitemap, JSON-LD, og-image) **bajarilgan va production'da jonli**.
> Bu hujjat — undan **keyingi** optimizatsiyalar, ta'sir darajasi bo'yicha tartiblangan.
>
> Belgilar: ⚙️ = kod orqali qilinadi (Claude) · 👤 = foydalanuvchi qiladi (off-page)

---

## 🔥 Eng katta ta'sir

### 1. Kitob sahifalari (`/kitob/{nom}`) ⚙️ — ENG MUHIM
Hozir kitoblarning alohida indekslanadigan sahifasi yo'q. Har bir kitob uchun alohida
sahifa (barcha taqrizlar + iqtiboslar + muqova + statistika) yaratsak — "Sariq devni
minib sharhi", "O'tkan kunlar haqida fikrlar" kabi **yuqori qiymatli qidiruvlar**da
chiqadi. Bu o'nlab yangi maqsadli landing sahifa demak. SEO uchun eng katta yutuq.

### 2. Yandex Webmaster 👤 — O'zbekiston uchun shart
O'zbekistonda ko'pchilik **Yandex**dan foydalanadi. `webmaster.yandex.uz` da saytni
qo'shing + sitemap topshiring (Google bilan bir xil). Bu o'tkazib yuborilmasligi kerak.

### 3. FAQ strukturali ma'lumot (FAQPage) ⚙️ — oson yutuq
Landingda allaqachon "Savollar" bo'limi bor. Unga **FAQPage JSON-LD** qo'shilsa —
Google qidiruv natijasida savollar **yoyiladigan ko'rinishda** chiqadi (ko'proq joy,
ko'proq bosish).

---

## ⚡ Tezlik (Core Web Vitals — bevosita reyting omili)

### 4. Rasm optimizatsiyasi ⚙️
Rasmlarga `loading="lazy"` + aniq `width/height` (sahifa sakrashini — CLS —
kamaytiradi), `og:image:alt` va barcha rasmlarga `alt` matn. Postlar allaqachon
WebP — yaxshi.

### 5. nginx Brotli/gzip + uzoq cache 👤 + ⚙️
Statik fayllarni (CSS/JS) siqish va uzoq muddat keshlash.
Tekshirish:
```
curl -sI -H "Accept-Encoding: br" https://kitobdagimen.uz/css/site.css | grep -i content-encoding
```
Yoqilmagan bo'lsa — nginx config qo'shiladi.

### 6. Shrift yuklash ⚙️
Hozir Google Fonts CDN render'ni bloklaydi. Shriftlarni **o'zimizda saqlash**
(self-host) sahifani tezlashtiradi va tashqi so'rovni kamaytiradi.

---

## 📊 O'lchov va boyitish

### 7. Analytics (GA4 yoki Yandex Metrica) ⚙️ + 👤
Qaysi sahifa qancha tashrif olayotganini ko'rasiz. Yengil va bepul.

### 8. JSON-LD boyitish ⚙️
Postlarga **BreadcrumbList** (qidiruvda yo'lakcha), `article:published_time`,
yoqdi/izoh sonlari (**InteractionCounter**).

### 9. Rasm sitemap ⚙️
Muqova/post rasmlarini sitemap'ga qo'shsak — **Google Rasmlar**dan ham tashrif keladi.

### 10. Custom 404 + ichki havolalar (internal linking) ⚙️
"Shu kitobning boshqa taqrizlari", "muallifning boshqa postlari" — crawl chuqurligi +
saytda qolish vaqtini (dwell time) oshiradi.

---

## 👤 Faqat siz (off-page — reytingni eng ko'p ko'taradi)

- **Backlink**: Telegram kanal, kitob bloggerlari, universitet/kutubxona saytlarida
  kitobdagimen.uz havolasi.
- **Muntazam kontent**: sayt qancha jonli (yangi taqrizlar) bo'lsa, shuncha yaxshi.
- **Brend qidiruv**: odamlar "kitobdagimen" deb qidira boshlasa — Google buni kuchli
  ishonch signali deb biladi.
- **Google Business Profile** va ijtimoiy tarmoq sahifalari (Instagram/Telegram/
  Facebook) — barchasida sayt havolasi.

---

## ✅ Allaqachon bajarilgan (poydevor — 2026-06-20)

- `<head>` to'liq meta: description, robots/googlebot, canonical, theme-color, keywords
- Open Graph + Twitter Card (1200×630 og-image)
- Maxfiy sahifalar avto-`noindex`, ommaviylar `index`
- `robots.txt` (statik) + dinamik `/sitemap.xml`
- JSON-LD: WebSite + Organization (landing), BlogPosting (post), ProfilePage (profil)
- Google Search Console HTML-fayl tasdiqlash

---

## Tavsiya etilgan tartib

1. **#1 kitob sahifalari** + **#3 FAQ schema** + **#4 rasm lazy-load** — bitta deploy.
2. **#2 Yandex** + **#7 analytics** — siz tomondan, parallel.
3. **#5–6 tezlik** — keyingi deploy.
4. **#8–10 boyitish** — bosqichma-bosqich.

> Eslatma: "Top 1" — raqobat + tashqi havola + vaqt masalasi. Texnik tomondan sayt
> tayyor; bu reja undan keyingi o'sish uchun.
