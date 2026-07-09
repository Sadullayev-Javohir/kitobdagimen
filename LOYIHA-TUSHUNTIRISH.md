# kitobdagimen.uz — Loyihani To'liq Tushuntirish (Intervyu uchun qo'llanma)

> Bu hujjat loyihada nima qilinganini, qaysi texnologiyalar nima uchun
> ishlatilganini va har bir bo'lim ichida nimalar borligini **oddiy tilda**
> tushuntiradi. Maqsad — siz dasturchi bo'lmasangiz ham, HR yoki texnik
> suhbatda loyihaning istalgan qismi haqida ishonch bilan gapira olishingiz.
>
> Hujjat oxirida **"Intervyu savol-javoblari"** bo'limi bor — eng ko'p
> so'raladigan savollar va tayyor javoblar.

---

## 1. Loyiha bir jumlada nima?

**kitobdagimen.uz** — o'zbek kitobxonlar uchun **ijtimoiy tarmoq (veb-sayt)**.
Foydalanuvchilar:
- kitob o'qish jarayonini kuzatadi (kunlik necha bet o'qigani),
- kitoblar haqida post (sharh) yozadi,
- iqtiboslar (kitobdan yoqqan gaplar) ulashadi,
- bir-birini kuzatadi (follow),
- real vaqtda chatda yozishadi,
- "challenge" (musobaqa) da qatnashadi, "yillik yakun" ko'radi.

Butun sayt **o'zbek tilida** va **telefon, planshet, kompyuter** ekranlarida
ishlaydi (responsive).

Texnik jihatdan bu **server tomonda ishlaydigan (server-rendered) veb-ilova** —
sahifalar serverda tayyorlanib brauzerga yuboriladi, ba'zi jonli qismlar
(chat, bildirishnoma) esa JavaScript orqali yangilanadi.

---

## 2. Umumiy texnologiyalar (stack) va NIMA UCHUN ular tanlangan

| Texnologiya | Vazifasi | Nega ishlatilgan (oddiy tilda) |
|---|---|---|
| **.NET 8 / C#** | Butun backend (server) shu tilda yozilgan | Microsoft'ning zamonaviy, tez, xavfsiz va korxonalar ko'p ishlatadigan platformasi. Katta loyihalar uchun barqaror. |
| **ASP.NET Core MVC** | Veb-sahifalar va so'rovlarni boshqarish | .NET'ning veb-freymvorki. Sahifa (View), boshqaruvchi (Controller) va ma'lumot (Model) ni ajratadi. |
| **Razor Views (.cshtml)** | HTML sahifalarni serverda yasash | C# va HTML'ni aralashtirib, dinamik sahifa yasash imkonini beradi. |
| **PostgreSQL** | Ma'lumotlar bazasi (foydalanuvchi, post, kitob...) | Bepul, kuchli, ishonchli ochiq kodli SQL bazasi. |
| **Entity Framework Core 8** | C# kodi bilan bazani bog'lash (ORM) | SQL yozmasdan, C# obyektlari orqali baza bilan ishlash imkonini beradi. |
| **Redis** | Tezkor keshlash (cache) + onlayn holat | Tez-tez so'raladigan ma'lumotni tez qaytarish uchun xotirada saqlaydi. |
| **SignalR** | Real vaqtda xabar (chat, bildirishnoma) | Sahifani yangilamasdan, server foydalanuvchiga darhol xabar yubora oladi. |
| **Hangfire** | Fon vazifalari (background jobs) | Belgilangan vaqtda avtomatik ishlar bajaradi (masalan, kunlik eslatma). |
| **Google OAuth 2.0** | Tizimga kirish | Parol saqlamaslik uchun — foydalanuvchi Google akkaunti bilan kiradi. |
| **JWT (JSON Web Token)** | Kim kirganini eslab qolish | Kirgan foydalanuvchini xavfsiz "token" orqali taniydi. |
| **Serilog** | Loglar (nima sodir bo'lganini yozib borish) | Xatoliklar va so'rovlarni tuzilgan (structured) ko'rinishda yozadi. |
| **xUnit** | Avtomatik testlar | Kod to'g'ri ishlayotganini tekshiruvchi dastur. |
| **MediatR, FluentValidation, Mapster** | Ichki arxitektura vositalari | Quyida "CQRS" bo'limida batafsil. |

> **Intervyuda "nega .NET?" so'ralsa:** "Bu korporativ darajadagi, xavfsiz va
> tez ishlaydigan platforma. Clean Architecture, EF Core, SignalR kabi
> imkoniyatlari bilan ijtimoiy tarmoq kabi murakkab loyihani toza va
> kengaytiriladigan qilib yozishga imkon beradi."

---

## 3. Arxitektura: "Clean Architecture" (Toza Arxitektura)

Bu loyihaning eng muhim tushunchasi. Kod **4 ta qatlamga (layer)** bo'lingan.
Har bir qatlam faqat o'zidan ichkaridagini biladi. Bu — **tartib va toza kod**
degani.

```
┌─────────────────────────────────────────────┐
│  Web (KitobdaGimen.Web)                       │  ← Tashqi qatlam
│  Controllers, Views, wwwroot, SignalR Hubs    │
├─────────────────────────────────────────────┤
│  Infrastructure (KitobdaGimen.Infrastructure) │
│  Baza, Redis, Google login, Hangfire          │
├─────────────────────────────────────────────┤
│  Application (KitobdaGimen.Application)        │
│  Biznes-mantiq: CQRS handler'lar, tekshiruv   │
├─────────────────────────────────────────────┤
│  Domain (KitobdaGimen.Domain)                 │  ← Eng ichki (yadro)
│  Entity'lar (User, Post, Book...), enum'lar   │
└─────────────────────────────────────────────┘
```

### Har bir qatlam nima qiladi:

**1) Domain (yadro)** — `src/KitobdaGimen.Domain/`
- Loyihaning "so'zlari": `User`, `Post`, `Book`, `Quote`, `Message` va h.k.
  bular **Entity** deyiladi — bazadagi bitta jadvalga mos keladi.
- Hech qanday texnologiyaga bog'liq emas. Faqat "bu loyihada nima bor" ni
  ta'riflaydi. Masalan `User` ning `Email`, `FullName`, `Role` maydonlari bor.
- `BaseEntity` — barcha entity'larda umumiy bo'lgan `Id` (raqamli identifikator).
- `Enums/UserRole.cs` — foydalanuvchi roli: `User` (oddiy), `Admin`,
  `SuperAdmin`.

**2) Application (biznes-mantiq)** — `src/KitobdaGimen.Application/`
- "Nima qilish kerak" mantig'i shu yerda. Masalan: "post yaratish", "like
  bosish", "iqtibos saqlash".
- **Features/** papkasi — har bir imkoniyat alohida papkada (pastda batafsil).
- Bu qatlam bazani to'g'ridan-to'g'ri bilmaydi — u faqat **interfeys** bilan
  ishlaydi (`IAppDbContext`). Bu — bazani ertaga almashtirsa ham mantiq
  buzilmasligini ta'minlaydi.

**3) Infrastructure (texnik amalga oshirish)** — `src/KitobdaGimen.Infrastructure/`
- Application "men bazaga yozishim kerak" desa, **qanday** yozishni shu qatlam
  biladi: PostgreSQL, Redis, Google login, Hangfire — barchasi shu yerda.
- Ya'ni Application "buyruq beradi", Infrastructure "bajaradi".

**4) Web (tashqi yuz)** — `src/KitobdaGimen.Web/`
- Foydalanuvchi ko'radigan qism: sahifalar (Views), so'rovlarni qabul qiluvchi
  Controller'lar, CSS/JS fayllar (wwwroot), chat serveri (Hubs).

> **Nega bunday bo'lingan?** "Har bir qism o'z ishi bilan shug'ullanadi. Kodni
> o'qish, tuzatish va kengaytirish oson. Masalan, bazani PostgreSQL'dan
> boshqasiga almashtirsak, faqat Infrastructure o'zgaradi — qolgan hammasi
> tegilmaydi."

---

## 4. CQRS + MediatR — biznes-mantiq qanday tashkil qilingan

Bu loyihaning ikkinchi muhim tushunchasi. **CQRS** = "Command Query
Responsibility Segregation" — ya'ni **o'zgartiruvchi amallar (Command)** va
**o'qish amallari (Query)** ni ajratish.

- **Command (buyruq)** — biror narsani o'zgartiradi: post yaratish, o'chirish,
  like bosish. Misol: `CreatePostCommand`.
- **Query (so'rov)** — faqat ma'lumot oladi, hech narsa o'zgartirmaydi: feed'ni
  ko'rish, profilni olish. Misol: `GetFeedQuery`.

### Har bir amal 3 ta fayldan iborat (naqsh):

`Features/Posts/Commands/CreatePost/` papkasida:

1. **`CreatePostCommand.cs`** — so'rovning "so'rovnomasi" (qanday ma'lumot
   kiritilishi kerak: `BookId`, `ReviewText`, `ImageUrl`).
2. **`CreatePostCommandValidator.cs`** — kiritilgan ma'lumotni tekshiradi
   (matn bo'sh emasmi, 5000 belgidan oshmaydimi va h.k.).
3. **`CreatePostCommandHandler.cs`** — asosiy ish shu yerda bajariladi:
   kitob bor-yo'qligini tekshiradi, postni bazaga yozadi, keyin post
   egasining kuzatuvchilariga bildirishnoma yuboradi.

### MediatR nima qiladi?

Controller "post yarat" demoqchi bo'lsa, to'g'ridan-to'g'ri Handler'ni
chaqirmaydi. U faqat `Mediator.Send(command)` deydi — **MediatR** esa mos
Handler'ni topib ishga tushiradi. Bu — Controller va mantiqni ajratadi
(bir-biriga bog'lanib qolmaydi).

### ValidationBehavior — "pipeline" (quvur)

Har bir Command Handler'ga borishdan **oldin** avtomatik tekshiruvdan o'tadi.
`ValidationBehavior.cs` shu ishni qiladi: agar ma'lumot noto'g'ri bo'lsa,
Handler'gacha yetmasdan xato qaytaradi. Bu — barcha joyda bir xil, ishonchli
tekshiruv degani.

### FluentValidation

Tekshiruv qoidalarini o'qishga qulay tilda yozish vositasi. Misol:
```
RuleFor(x => x.ReviewText)
    .NotEmpty().WithMessage("Fikr matnini kiriting.")
    .MaximumLength(5000).WithMessage("Fikr 5000 belgidan oshmasligi kerak.");
```

### Mapster

Bazadagi to'liq `Post` obyektini brauzerga yuborishdan oldin **DTO** (Data
Transfer Object — faqat kerakli maydonlari bor soddalashtirilgan nusxa) ga
o'giradi. Nega? Xavfsizlik va tezlik — ortiqcha yoki maxfiy ma'lumot
yuborilmaydi.

> **Loyihada 20 ta feature papkasi, ~95 ta handler, 14 ta validator bor.** Bu
> sonlar loyihaning katta va yaxshi tuzilganini ko'rsatadi.

---

## 5. Loyihadagi BO'LIMLAR (Features) — har biri ichida nima bor

`src/KitobdaGimen.Application/Features/` ichida 20 ta bo'lim. Mana ularning
har biri:

1. **Auth (Kirish)** — Google orqali kirish (`LoginWithGoogle`), joriy
   foydalanuvchini olish (`GetCurrentUser`). Parol yo'q, faqat Google.

2. **Onboarding (Ro'yxatdan o'tish jarayoni)** — birinchi kirganda ism/username
   to'ldirish (`CompleteProfile`), qiziqish janrlarini tanlash
   (`SaveUserGenres`), janrlar ro'yxati (`GetGenres`).

3. **Posts (Postlar)** — kitob haqida sharh yozish (`CreatePost`), tahrirlash,
   o'chirish, like bosish (`ToggleLike`), izoh qo'shish (`AddComment`),
   ko'rishlar sonini yozish (`RecordPostView`), feed va bitta postni ochish.

4. **Profile (Profil)** — profilni tahrirlash (`UpdateProfile`), username
   band-emasligini tekshirish (`CheckUsername`), foydalanuvchi postlari,
   akkauntni o'chirish (`DeleteAccount`).

5. **Follow (Kuzatish)** — kuzatish/bekor qilish (`ToggleFollow`),
   kuzatuvchilar (`GetFollowers`) va kuzatilayotganlar (`GetFollowing`).

6. **ReadingGoals (O'qish maqsadlari)** — kunlik bet maqsadi qo'yish
   (`CreateReadingGoal`), progressni yangilash (`UpdateReadingProgress`),
   faol va tugagan maqsadlar. Progress-bar bilan ko'rsatiladi.

7. **Quotes (Iqtiboslar)** — iqtibos yaratish (`CreateQuote`), saqlash
   (`ToggleSaveQuote`), like, izoh, o'chirish; "barchasi / meniki /
   saqlangan" tablari.

8. **Chat** — suhbat ochish (`GetOrCreateConversation`), xabar yuborish
   (`SendMessage`), tahrirlash/o'chirish, o'qildi belgilash
   (`MarkMessagesRead`), reaksiya (emoji) qo'yish (`ToggleReaction`),
   o'qilmagan xabarlar soni.

9. **Connections (Ulanishlar)** — chatga ruxsat so'rash tizimi:
   so'rov yuborish (`SendConnectionRequest`), qabul/rad qilish
   (`RespondToConnection`), bekor qilish. Ya'ni suhbatlashish uchun avval
   "do'stlik" so'rovi kerak.

10. **Notifications (Bildirishnomalar)** — like/izoh/follow bo'lganda real
    vaqtda bildirishnoma; o'qilgan deb belgilash.

11. **Stories (Hikoyalar)** — Instagram'dagi kabi 24 soatdan keyin o'chadigan
    "hikoya" (`CreateStory`, `RecordStoryView`, `ToggleStoryLike`).

12. **Books (Kitoblar)** — kitob qo'shish (`CreateBook`), kitob sahifasi,
    kitoblar ro'yxati va feed'i. asaxiy.uz'dan kitob ma'lumotlari olinadi.

13. **Challenge (Musobaqa)** — oylik o'qish musobaqasi: reyting jadvali
    (`GetChallengeStandings`), oyni yakunlash (`FinalizeChallengeMonth`),
    g'oliblarga sovg'a kitob (`SetWinnerGiftBook`), g'oliblar taxtasi.

14. **Leaderboard (Reyting)** — eng ko'p o'qiganlar reytingi
    (`GetReadingLeaderboard`).

15. **YearReview (Yillik yakun)** — yil davomida o'qilgan kitoblar bo'yicha
    chiroyli statistika sahifasi (`GetYearReview`).

16. **Users (Foydalanuvchilar)** — foydalanuvchilarni qidirish (`SearchUsers`).

17. **Home (Bosh sahifa)** — tanishuv sahifasi statistikasi
    (`GetLandingStats`), fon videosi manzili.

18. **Push (Push-bildirishnoma)** — telefon/brauzer push obunasini saqlash
    (`SavePushSubscription`) — sayt yopiq bo'lsa ham bildirishnoma keladi.

19. **Seo (Qidiruv tizimlari uchun)** — Google topishi uchun `sitemap.xml`
    yaratish (`GetSitemap`).

20. **Admin (Boshqaruv)** — postlarni/iqtiboslarni/foydalanuvchilarni
    o'chirish (moderatsiya), rol berish (`SetUserRole`), ommaviy bildirishnoma
    (`BroadcastNotification`), server holati monitoringi (`GetServerSnapshot`,
    `GetSystemStatus`) va analitika.

---

## 6. Ma'lumotlar bazasi (Database)

**PostgreSQL** ishlatilgan. Baza bilan **Entity Framework Core 8 (EF Core)**
orqali ishlanadi — bu **ORM** (Object-Relational Mapper), ya'ni SQL yozmasdan
C# obyektlari bilan bazaga yozib-o'qish imkonini beradi.

### Asosiy jadvallar (28 ta Entity):

`User` (foydalanuvchi), `Post` (post), `Book` (kitob), `Comment` (izoh),
`Like` (like), `Follow` (kuzatish), `Quote` (iqtibos), `Message` (xabar),
`Conversation` (suhbat), `Notification` (bildirishnoma), `ReadingGoal`
(o'qish maqsadi), `Story` (hikoya), `Genre` (janr), `Connection` (ulanish),
`ChallengeWinner` (musobaqa g'olibi) va boshqalar.

### Muhim tushunchalar:

- **Migrations (migratsiyalar)** — `Persistence/Migrations/` da 22 ta migratsiya
  fayli bor. Bular bazaning "tarixi": har safar jadval qo'shilsa yoki
  o'zgartirilsa, EF Core avtomatik shunday fayl yaratadi. Bu — bazani boshqa
  kompyuterda ham xuddi shunday qayta qurish imkonini beradi.
- **Configurations/** — har bir jadvalning qoidalari (qaysi maydon majburiy,
  uzunligi, bog'lanishlari). Masalan `PostConfiguration.cs`.
- **DbInitializer.cs** — sayt birinchi ishga tushganda janrlarni va namuna
  kitoblarni bazaga avtomatik joylaydi (seed).
- **AppDbContext.cs** — barcha jadvallarni bitta joyda birlashtiruvchi asosiy
  klass ("baza bilan aloqa nuqtasi").

> **"Best-effort" xususiyati:** Agar baza yoki Redis ulanmasa ham, sayt
> yiqilmaydi — ogohlantirish bilan ko'tariladi. Bu ishonchlilikni oshiradi.

---

## 7. Autentifikatsiya va xavfsizlik (Kim kirgan?)

### Google OAuth + JWT (2 bosqichli)

1. Foydalanuvchi "Google bilan kirish" ni bosadi → Google'ga o'tadi → akkaunt
   tanlaydi → sayt orqaga qaytadi.
2. Sayt Google'dan foydalanuvchi ma'lumotini oladi, bazada bor-yo'qligini
   tekshiradi (yo'q bo'lsa yaratadi), so'ng **JWT token** yasaydi.
3. Bu token **HttpOnly cookie** ichida saqlanadi — ya'ni JavaScript uni o'qiy
   olmaydi (o'g'irlashdan himoya). Keyingi har bir so'rovda shu token orqali
   foydalanuvchi tanilib turadi.

**JWT** — imzolangan (HS256) matn. Ichida foydalanuvchi id, email, ism bor.
Server imzoni tekshirib, tokenning haqiqiyligiga ishonch hosil qiladi.

### Xavfsizlik choralari (juda yaxshi ishlangan):

- **Fail-fast JWT kalit:** Agar maxfiy kalit 32 belgidan qisqa yoki
  placeholder bo'lsa, sayt umuman ishga tushmaydi. Bu — token soxtalashtirishning
  oldini oladi.
- **Rate limiting** — har bir IP daqiqasiga 600 ta so'rov bilan cheklangan
  (DoS/brute-force hujumlariga qarshi).
- **Xavfsizlik sarlavhalari (Security Headers):** `X-Frame-Options` (clickjacking),
  `X-Content-Type-Options` (MIME-sniffing), **CSP** (Content-Security-Policy —
  faqat ishonchli manbalardan skript/rasm yuklanadi).
- **Antiforgery (CSRF himoyasi)** — soxta formalar orqali amal bajarishni
  bloklaydi.
- **RichTextSanitizer** — foydalanuvchi kiritgan matndan zararli HTML/skriptni
  tozalaydi (XSS hujumiga qarshi).
- **Rasmlarni qayta kodlash:** Yuklangan rasmlar **ImageSharp** bilan WebP
  formatiga o'giriladi (ichiga yashiringan zararli kod olib tashlanadi, hajm
  kichrayadi). Maksimal 8 MB, 1600px.
- **Rollarga asoslangan ruxsat:** `[Authorize]` atributi bilan sahifalar
  himoyalangan; Admin/SuperAdmin amallar rol tekshiruvidan o'tadi.

> **Intervyuda xavfsizlik so'ralsa** — yuqoridagilardan 3-4 tasini aytsangiz
> kifoya: "JWT HttpOnly cookie'da, CSP va security header'lar, rate limiting,
> XSS uchun sanitizer, rasmlar qayta kodlanadi."

---

## 8. Real vaqt (Real-time) — SignalR

**SignalR** — server bilan brauzer o'rtasida doimiy aloqa (WebSocket)
ochib turuvchi texnologiya. Sahifani yangilamasdan xabar keladi.

Loyihada 2 ta **Hub** (real-time server nuqtasi):

- **`ChatHub.cs`** (`/hubs/chat`) — chat xabarlari darhol yetib boradi;
  onlayn/offlayn holat (Redis orqali) kuzatiladi; "yozmoqda..." ko'rsatiladi.
  Har foydalanuvchi barcha qurilmalarida bir vaqtda xabar oladi
  (`user-{id}` guruhi orqali).
- **`NotificationHub.cs`** (`/hubs/notifications`) — like/izoh/follow bo'lganda
  real vaqtda "toast" (kichik xabarcha) chiqadi.

Har ikkisi ham JWT cookie orqali himoyalangan (`[Authorize]`).

---

## 9. Keshlash (Caching) — Redis

**Redis** — ma'lumotni xotirada (RAM) saqlaydigan tezkor ombor. Tez-tez
so'raladigan, lekin kam o'zgaradigan ma'lumot (masalan reyting, statistika)
Redis'da saqlanib, bazaga qayta-qayta bormaydi — bu saytni **tezlashtiradi**.

- `RedisCacheService.cs` — keshga yozish/o'qish.
- `RedisPresenceService` — kim onlaynligini TTL (muddatli) yozuvlar bilan
  kuzatadi.
- **Muhim:** Redis o'chiq bo'lsa ham sayt ishlaydi — shunchaki keshsiz,
  sekinroq (best-effort).

---

## 10. Fon vazifalari (Background Jobs) — Hangfire

**Hangfire** — belgilangan vaqtda avtomatik ishlarni bajaradigan tizim
(vazifalar PostgreSQL'da saqlanadi). Loyihada:

- **`ReadingReminderJob.cs`** — har kuni O'zbekiston vaqti bilan soat 20:00 da
  faol maqsadi bor, lekin bugun o'qimagan foydalanuvchilarga "boyo'g'li"
  eslatma yuboradi.
- **`ChallengeFinalizeJob.cs`** — musobaqa oyini yakunlash (hozir avtomatik
  emas, faqat admin qo'lda ishga tushiradi).
- **`/hangfire`** — boshqaruv paneli, faqat sozlamadagi admin email'lar kira
  oladi (aks holda hamma uchun ochiq qolardi — xavfli).

---

## 11. Frontend (foydalanuvchi ko'radigan qism)

- **Razor Views (.cshtml)** — 42 ta sahifa. Server HTML'ni tayyorlab yuboradi.
- **`Views/Shared/_Layout.cshtml`** — barcha sahifalarning umumiy "ramkasi"
  (navbar, footer). Mobil holatda burger menyu.
- **`wwwroot/css/site.css`** (~165 KB) — asosiy dizayn tizimi. Ranglar:
  Primary `#1B4D3E` (to'q yashil), Accent `#E8703A` (to'q sariq). Sarlavhalar
  serif (Lora), matn sans-serif (Source Sans 3).
- **`wwwroot/js/site.js`** (~66 KB) — sof (vanilla) JavaScript. Hech qanday
  og'ir freymvork (React/Vue) ishlatilmagan — bu saytni **yengil va tez**
  qiladi.
- **Three.js** — "Yillik yakun" va "Challenge" sahifalarida 3D animatsiyalar
  uchun (`year-review-scene.js`).
- **PWA / Push** — `push.js` orqali brauzer/telefon push-bildirishnomalari;
  sayt Android ilova (TWA) sifatida ham o'ralishi mumkin (`android/` papka).

---

## 12. Testlar (Sifat nazorati)

**xUnit** freymvorkida **23 ta test fayli** (README'da 35+ test). Testlar
haqiqiy bazaga ulanmasdan, **EF Core InMemory** (xotiradagi soxta baza) bilan
ishlaydi — tez va xavfsiz.

Nimalar tekshiriladi:
- Handler'lar to'g'ri ishlaydimi (masalan `CreateQuoteHandlerTests`,
  `ToggleFollowCommandHandlerTests`).
- Validator'lar noto'g'ri ma'lumotni rad etadimi (`ValidatorTests`).
- `RichTextSanitizerTests` — zararli HTML tozalanadimi.

> **Nega test kerak?** "Kodni o'zgartirganda eski funksiya buzilib
> qolmaganini avtomatik tekshiradi. Ishonch bilan rivojlantirish imkonini
> beradi."

Ishga tushirish: `dotnet test KitobdaGimen.sln`

---

## 13. Monitoring (Server holati kuzatuvi)

Admin panel uchun maxsus tizim (`Web/Monitoring/`):
- **`MetricsCollectorService.cs`** — har bir necha soniyada server sog'ligini
  (CPU, xotira, so'rovlar soni, kechikish) yig'ib, xotiradagi "ring buffer"ga
  yozadi.
- Admin `/admin` panelida serverning jonli holatini ko'radi.
- `RequestMetricsMiddleware.cs` — har bir so'rovning tezligini va statusini
  o'lchaydi.

---

## 14. asaxiy.uz integratsiyasi (qiziq muhandislik yechimi)

Kitob ma'lumotlari **asaxiy.uz** saytidan olinadi. Muammo: asaxiy.uz
Cloudflare himoyasi ortida turadi va xorijiy server IP'larini bloklaydi.

Yechim (`AsaxiyBookService.cs`) — **4 ta yo'lni avtomatik navbat bilan
sinaydi** va ishlaganini eslab qoladi:
1. **Worker** — bepul Cloudflare Worker proksi.
2. **Proxy** — O'zbekiston IP'sidagi proksi.
3. **Direct** — to'g'ridan-to'g'ri.
4. **Jina** — `r.jina.ai` zaxira o'qigich (sozlamasiz ham ishlaydi).

> Bu "umrbod ishlash" yondashuvi — bitta yo'l o'chsa, keyingisiga o'tadi. Bu
> intervyuda **muammoni hal qilish qobiliyatingizni** ko'rsatadi.

---

## 15. Loyihada BO'LISHI MUMKIN bo'lgan xatoliklar / zaif tomonlar

Intervyuda "kamchiliklari nima?" deb so'rashlari mumkin. Halol va o'ylangan
javob berish yaxshi taassurot qoldiradi:

1. **Tashqi saytga bog'liqlik (asaxiy.uz):** Kitob ma'lumoti tashqi saytdan
   olingani uchun, u sayt o'zgarsa yoki bloklasa, muammo bo'lishi mumkin
   (shuning uchun 4 ta zaxira yo'l qilingan).
2. **Sof JS (freymvorksiz):** `site.js` katta (66 KB) — juda ko'p mantiq bitta
   faylda. Katta jamoada React/Vue qulayroq bo'lishi mumkin edi, ammo bu yerda
   yengillik uchun ataylab tanlangan.
3. **Bitta serverga bog'liq monitoring/kesh:** Monitoring xotirada (in-memory),
   SignalR presence Redis'da — ilova bir nechta serverga tarqalganda (scale-out)
   qo'shimcha sozlash kerak bo'ladi.
4. **JWT bekor qilib bo'lmaydi (stateless):** Token muddати tugaguncha amal
   qiladi; darhol "chiqarib yuborish" uchun qo'shimcha mexanizm kerak
   (odatda blacklist yoki qisqa muddat + refresh token).
5. **Fayllar diskda saqlanadi** (`wwwroot/uploads`) — bulutli saqlash (S3 kabi)
   ga o'tish kelajakda kengayish uchun yaxshiroq bo'lardi.
6. **Rate limit IP bo'yicha** — bir NAT ortidagi ko'p foydalanuvchi bitta IP
   bo'lib ko'rinishi mumkin.

> Bularning aksariyati **ataylab qilingan soddalashtirish** (loyiha hajmiga
> mos). Buni tushunib aytish — professional yondashuv belgisi.

---

## 16. Loyihani qanday ishga tushirish (amaliy)

```bash
# 1. Kerakli vositalarni tiklash
dotnet tool restore

# 2. Bazani tayyorlash (migratsiyalar)
dotnet dotnet-ef database update \
  --project src/KitobdaGimen.Infrastructure \
  --startup-project src/KitobdaGimen.Web

# 3. Ishga tushirish
dotnet run --project src/KitobdaGimen.Web

# 4. Build va test
dotnet build KitobdaGimen.sln -c Release
dotnet test KitobdaGimen.sln
```

Sozlamalar (`appsettings.json` yoki user-secrets/environment):
- `ConnectionStrings:DefaultConnection` — PostgreSQL manzili.
- `ConnectionStrings:Redis` — Redis (ixtiyoriy).
- `Jwt:Key` — kamida 32 belgili maxfiy kalit.
- `Authentication:Google:ClientId` / `ClientSecret` — Google login kalitlari.

---

## 17. INTERVYU SAVOL-JAVOBLARI (eng muhim qism)

### S: Bu loyiha nima haqida? (30 soniyalik javob)
**J:** "Bu — o'zbek kitobxonlar uchun ijtimoiy tarmoq. Foydalanuvchilar kitob
o'qish jarayonini kuzatadi, sharh va iqtibos ulashadi, bir-birini kuzatadi va
real vaqtda chatlashadi. .NET 8 da Clean Architecture asosida yozilgan,
ma'lumotlar bazasi PostgreSQL, real vaqt uchun SignalR, keshlash uchun Redis
ishlatilgan."

### S: Qanday arxitektura ishlatgansiz va nega?
**J:** "Clean Architecture — 4 qatlam: Domain, Application, Infrastructure, Web.
Har qatlam faqat o'z ishi bilan shug'ullanadi. Bu kodni toza, tuzatishga oson
va kengaytiriladigan qiladi. Biznes-mantiq CQRS naqshida MediatR orqali tashkil
qilingan — o'zgartiruvchi Command va o'qiydigan Query alohida."

### S: CQRS nima va nega kerak?
**J:** "Command Query Responsibility Segregation. O'zgartiruvchi amallar
(Command) va o'quvchi amallar (Query) ajratiladi. Har amal alohida Handler'da
bo'ladi, MediatR mos Handler'ni topib chaqiradi. Bu mantiqni Controller'dan
ajratadi va har bir imkoniyat mustaqil, testlanadigan bo'ladi."

### S: Foydalanuvchi qanday tizimga kiradi? (Auth)
**J:** "Parol yo'q — faqat Google OAuth. Google'dan qaytgach, server JWT token
yasaydi va uni HttpOnly cookie'da saqlaydi, shunda JavaScript o'qiy olmaydi.
Keyingi so'rovlarda shu token orqali foydalanuvchi tanilib turadi."

### S: Ma'lumotlar bazasi bilan qanday ishlaysiz?
**J:** "PostgreSQL, Entity Framework Core 8 ORM orqali. SQL o'rniga C#
obyektlari bilan ishlanadi. Bazadagi o'zgarishlar Migration fayllar orqali
boshqariladi — bu bazani boshqa muhitda ham xuddi shunday qayta qurishga imkon
beradi."

### S: Chat qanday real vaqtda ishlaydi?
**J:** "SignalR orqali. Server bilan brauzer o'rtasida doimiy WebSocket aloqasi
ochiladi. Xabar yuborilganda server uni darhol qabul qiluvchining barcha
qurilmalariga yuboradi — sahifa yangilanmaydi. Onlayn holat Redis'da kuzatiladi."

### S: Xavfsizlikni qanday ta'minlagansiz?
**J:** "Bir necha qatlam: JWT HttpOnly cookie'da, CSP va boshqa security
header'lar, IP bo'yicha rate limiting, CSRF himoyasi (antiforgery), foydalanuvchi
matni XSS uchun sanitizer'dan o'tadi, yuklangan rasmlar WebP'ga qayta kodlanadi.
JWT maxfiy kaliti zaif bo'lsa, sayt umuman ishga tushmaydi."

### S: Kesh nima uchun kerak?
**J:** "Tez-tez so'raladigan, kam o'zgaradigan ma'lumotni Redis xotirasida
saqlaymiz, bazaga qayta bormaymiz — sayt tezlashadi. Redis o'chsa ham sayt
ishlayveradi, shunchaki sekinroq."

### S: Testlar bormi?
**J:** "Ha, xUnit'da 20+ test fayli. EF Core InMemory bilan haqiqiy bazasiz
ishlaydi. Handler'lar va validator'lar to'g'ri ishlashini tekshiradi. Bu kodni
o'zgartirganda eski funksiya buzilmasligini kafolatlaydi."

### S: Eng qiyin qism nima edi? (Muammo hal qilish)
**J:** "asaxiy.uz'dan kitob ma'lumoti olish. Ular Cloudflare bilan xorijiy
serverni bloklaydi. Buning uchun 4 ta zaxira yo'l (Worker, Proxy, Direct, Jina)
qilib, tizim avtomatik ishlaydiganini tanlaydigan va eslab qoladigan qildik —
bitta yo'l o'chsa keyingisiga o'tadi."

### S: Loyihaning kamchiligi nima?
**J:** (15-bo'limdagilardan 2-3 tasini ayting) "Masalan, kitob ma'lumoti tashqi
saytga bog'liq; JWT'ni muddatidan oldin bekor qilish uchun qo'shimcha mexanizm
kerak; fayllar hozircha diskда saqlanadi — kengayish uchun bulutli saqlashga
o'tish yaxshiroq bo'lardi."

### S: Nima uchun freymvork (React) ishlatmadingiz?
**J:** "Sayt asosan server-rendered (Razor). Interaktiv qismlar uchun sof
JavaScript yetarli bo'ldi — bu saytni yengil va tez qiladi, ortiqcha
bog'liqliksiz."

---

## 18. Qisqacha "shpargalka" (bir qarashda)

| Savol | Bir so'zli javob |
|---|---|
| Til | C# (.NET 8) |
| Arxitektura | Clean Architecture (4 qatlam) |
| Naqsh | CQRS + MediatR |
| Baza | PostgreSQL + EF Core 8 |
| Kesh | Redis |
| Real vaqt | SignalR |
| Fon vazifalar | Hangfire |
| Kirish | Google OAuth + JWT (HttpOnly cookie) |
| Tekshiruv | FluentValidation |
| Loglar | Serilog |
| Testlar | xUnit + EF InMemory |
| Frontend | Razor Views + vanilla JS + custom CSS |
| Rasm | ImageSharp (WebP) |
| Push | WebPush (VAPID) |

---

*Omad tilaymiz! Bu hujjatni bir necha marta o'qib, 17-bo'limdagi javoblarni
o'z so'zlaringiz bilan takrorlab mashq qiling. Har bir bo'lim uchun "bu nima?",
"nega kerak?", "qanday ishlaydi?" degan 3 savolga javob bera olsangiz —
suhbatda ishonch bilan gapira olasiz.*
