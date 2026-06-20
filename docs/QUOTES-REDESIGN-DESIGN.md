# /quotes sahifasi — qayta dizayn (LOYIHALASHTIRILDI 2026-06-19)

Talab (foydalanuvchi):
1. `/quotes` iqtibos kartalarini `design-reference/07-iqtiboslar.html` dagi
   karta dizayniga o'tkazish.
2. "Yangi iqtibos" formasida — kitob qidiruv inputining O'NG tomonida
   "Yangi kitob" tugmasi bo'lsin (xuddi `/feed` dagidek), to'liq kitob
   qo'shish oqimi bilan.
3. **Iqtiboslarda LIKE (yurak) ham bo'lsin** — saqlash (bookmark) bilan yonma-yon,
   alohida hisoblagich bilan.

(3) backend ham o'zgartiradi — `QuoteLike` entity'si kerak (hozir faqat
`SavedQuote` bor). (1)+(2) faqat frontend.

Tegiladigan fayllar:
- **Backend (like uchun)**: `Domain/Entities/QuoteLike.cs` (yangi),
  `Domain/Entities/Quote.cs` (+`LikedBy` nav), `Domain/Entities/User.cs`
  (+`QuoteLikes` nav), `Infrastructure/.../Configurations/QuoteLikeConfiguration.cs`
  (yangi), `Application/Common/Interfaces/IAppDbContext.cs`,
  `Infrastructure/Persistence/AppDbContext.cs`,
  `tests/.../Support/TestDbContext.cs` (+`DbSet<QuoteLike> QuoteLikes`),
  yangi migratsiya, `QuoteDto`, `QuoteQueryableExtensions`, yangi
  `LikeQuoteResultDto` + `ToggleLikeQuoteCommand`(+Handler),
  `QuotesController` (+`/{id}/like`), `DeleteAccountCommandHandler` (QuoteLikes
  tozalash — Restrict FK), tests (`ToggleLikeQuoteCommandHandlerTests`).
- **Frontend**: `src/KitobdaGimen.Web/Views/Quotes/Index.cshtml`
  (markup + `@section Scripts`),
  `src/KitobdaGimen.Web/wwwroot/css/site.css` (quote-card bloki + grid).

Mavjud va o'zgarmaydigan backend: `/quotes/create|/{id}/delete|/{id}/save`,
`/books/create`, `/books/search`, `/books/upload-cover`, `GetGenresQuery`.

---

## A. Iqtibos kartasi qayta dizayni

Dizayndan (07-iqtiboslar.html) OLINADIGAN vizual elementlar — Tailwind emas,
loyiha CSS o'zgaruvchilari (`--primary`, `--border`, `--accent`, `--serif`,
`--text-secondary`, `--surface`/`--primary-soft`) bilan:

- **Karta**: `border-radius: 20px`, `border: 1px var(--border)`, yengil soya
  (`0 4px 20px rgba(27,77,62,.05)`), `padding: 24px`, `position: relative`,
  `overflow: hidden`. Navbatma-navbat fon: `:nth-child(2n)` ga `--primary-soft`
  (dizayndagi paper / surface-container almashinuvi).
- **Dekorativ belgi**: `format_quote` material ikonkasi — `position:absolute`,
  yuqori-chap, juda xira (`opacity:.08`), ~80px, `--primary` rang. (`.quote-mark`)
- **Iqtibos matni**: serif, **italic**, ~20px, `--primary`. Mavjud
  `.quote-text::before` (qo'shtirnoq) OLIB TASHLANADI — dekorativ ikonka
  endi shu motivni beradi (ikki marta qo'shtirnoq bo'lmasin).
- **Ajratuvchi**: nozik `<hr>` (`--border`).  (`.quote-divider`)
- **Manba**: kitob nomi (`<strong>`, `--primary`) + muallif (kichik,
  `--text-secondary`).
- **Futer** (`border-top`):
  - chap: muallif avatari (28px dumaloq — `Author.AvatarUrl` bo'lsa rasm,
    bo'lmasa `ViewHelpers.Initial(Author.FullName)` bilan harf-fallback,
    `_PostCard` kabidir) + `Author.FullName`. Agar `Author.Username` bo'lsa
    `/{username}` profilga link.
  - o'ng: **Like** tugmasi (`favorite` — yoqtirilgan bo'lsa to'ldirilgan/
    `--accent`, `data-like-quote="@q.Id"` + `LikeCount`), **Saqlash** tugmasi
    (`bookmark` + `SaveCount`, mavjud `data-save-quote="@q.Id"` xulqi saqlanadi)
    va — agar `q.Author.Id == meId` — **O'chirish** tugmasi (mavjud forma).
- Like endi **backend bilan ta'minlangan** (D-bo'lim) — dizayndagi yurak
  ikonkasi real funksiya. To'ldirilgan holat `IsLikedByCurrentUser` bilan
  server-render qilinadi (`favorite` FILL 1 / `--accent`).

**Grid**: `.quote-grid` hozir CSS `columns` (masonry). Dizayn bir tekis 2-ustun
grid ishlatadi → `display:grid; grid-template-columns:1fr; gap:16px` va
`@media (min-width:720px){ grid-template-columns:1fr 1fr }`. Mobil 1 ustun.

**Tablar**: real route'lar saqlanadi — Barcha (`/quotes`) / Mening
(`/quotes/my`) / Saqlangan (`/quotes/saved`). Mavjud `btn` tab implementatsiyasi
ishlaydi; dizaynning underline uslubi ixtiyoriy (asosiy talab — kartalar).

CSS (site.css `.quote-*` bloki): `.quote-card` (qayta), `.quote-mark`,
`.quote-divider`, `.quote-user` (avatar+ism flex), `.quote-actions`,
`.quote-grid` (columns→grid). `.quote-text::before` o'chiriladi.

---

## B. "Yangi iqtibos" formasiga "Yangi kitob" tugmasi

`/feed` kompozeridagi kitob-tanlash oqimini ko'chirish (Feed/Index.cshtml
44–77 va JS 279–356 qatorlar namuna):

**Razor (Quotes/Index.cshtml):**
- Yuqoriga: `@using KitobdaGimen.Application.Features.Onboarding.Queries.GetGenres`,
  `@using MediatR`, `@inject ISender Mediator`; formadan oldin
  `var genres = await Mediator.Send(new GetGenresQuery());`.
- `composer-book` blokiga search inputning o'ngiga:
  `<button type="button" class="btn btn-outline btn-sm" id="qNewBookToggle">Yangi kitob</button>`.
- Tanlangan kitob uchun `composer-selected` chip (ikonka + nom + `×` clear),
  mavjud oddiy `#quoteSelectedBook` o'rniga.
- Yashirin `newBookForm` bloki (`composer-newbook stack`): `qNbTitle`,
  `qNbAuthor`, `qNbPages` (number), `qNbGenre` (`<select>` — `genres` dan
  server-render, majburiy), `nb-cover` (muqova preview + `/books/upload-cover`
  yuklash). Hammasi `q`-prefiksli ID bilan.
- `<input type="hidden" name="BookId" id="quoteBookId">` saqlanadi.

**JS (`@section Scripts` qayta yoziladi, Feed mantiqidan moslab):**
- `pickBook(b)` → `quoteBookId.value=b.id`, chip ko'rsatadi, picker yashiradi.
- `clearBook()` → chip yashiradi, picker qaytaradi, BookId tozalaydi.
- Kitob qidiruv (`/books/search`, 250ms debounce) — mavjud kabi.
- `qNewBookToggle` → newBookForm toggle.
- `qNbSave` → muqova bo'lsa avval `/books/upload-cover`, so'ng
  `kitob.apiPost('/books/create', {title,author,totalPages,genreId,coverUrl})`,
  qaytgan kitobni `pickBook` qiladi. Genre majburiy (tanlanmasa alert).
- Submit'da `BookId` bo'sh bo'lsa `preventDefault` + alert.

**CSS**: deyarli yangi kerak emas — `composer-book`, `composer-newbook`,
`nb-cover*`, `composer-selected`, `composer-clear`, `book-suggestions`
allaqachon global (site.css 315–370). Faqat `.composer-book .btn` o'lchami
mos kelishini tekshirish.

---

## D. Iqtiboslarda LIKE (full-stack) — `SavedQuote` pattern'ining nusxasi

`QuoteLike` entity'si `SavedQuote` ga 1:1 mos (faqat nom farqi). Mavjud
save oqimini namuna sifatida ko'chiramiz.

**Domain:**
- `Entities/QuoteLike.cs` (yangi): `QuoteId`→`Quote`, `UserId`→`User`,
  `CreatedAt` — `SavedQuote` bilan bir xil struktura.
- `Quote.cs`: `public ICollection<QuoteLike> LikedBy { get; set; } = new List<QuoteLike>();`
  (`SavedBy` yonida).
- `User.cs`: `public ICollection<QuoteLike> QuoteLikes { get; set; } = ...;`
  (`SavedQuotes` yonida).

**Infrastructure:**
- `Configurations/QuoteLikeConfiguration.cs` (yangi) — `SavedQuoteConfiguration`
  nusxasi: `HasOne(Quote).WithMany(LikedBy)...OnDelete(Cascade)`,
  `HasOne(User).WithMany(QuoteLikes)...OnDelete(Restrict)`,
  `HasIndex({QuoteId,UserId}).IsUnique()`.
- `AppDbContext.cs`: `public DbSet<QuoteLike> QuoteLikes => Set<QuoteLike>();`
- Yangi migratsiya `AddQuoteLikes` → real DB'ga startupda (DbInitializer) qo'llanadi.

**Interfaces / tests:**
- `IAppDbContext.cs`: `DbSet<QuoteLike> QuoteLikes { get; }`
- `tests/.../Support/TestDbContext.cs`: o'sha DbSet (aks holda build/test buziladi).

**Application:**
- `QuoteDto`: `+ int LikeCount`, `+ bool IsLikedByCurrentUser`.
- `QuoteQueryableExtensions.ToQuoteDto`: `LikeCount = q.LikedBy.Count`,
  `IsLikedByCurrentUser = currentUserId != null && q.LikedBy.Any(l => l.UserId == currentUserId)`.
- `Dtos/LikeQuoteResultDto.cs` (yangi): `{ bool IsLiked; int LikeCount }`.
- `Commands/ToggleLikeQuote/` — `ToggleLikeQuoteCommand(int QuoteId)` +
  `ToggleLikeQuoteCommandHandler` — `ToggleSaveQuote` nusxasi
  (`_db.QuoteLikes` bilan, quote mavjudligini tekshiradi, toggle, qayta count).

**Web:**
- `QuotesController`: `[HttpPost("{id:int}/like")] ToggleLike(int id)` →
  `Json(await Mediator.Send(new ToggleLikeQuoteCommand(id)))` — save endpoint kabi.

**DeleteAccount:**
- `DeleteAccountCommandHandler` — SavedQuotes tozalash yonida:
  `_db.QuoteLikes.RemoveRange(... l.UserId == userId ...)` (User FK Restrict
  bo'lgani uchun foydalanuvchining boshqa iqtiboslarga bosgan like'lari ham
  qo'lda o'chirilishi shart; o'z iqtiboslaridagi like'lar Quote o'chganda
  Cascade bo'ladi).

**Tests:**
- `ToggleLikeQuoteCommandHandlerTests` — `ToggleSaveQuoteCommandHandlerTests`
  nusxasi (yoqtirish qo'shiladi / qayta bosishda olib tashlanadi / count).

**Frontend (A-bo'lim futeriga ulanadi):**
- `data-like-quote="@q.Id"` tugmasi, `IsLikedByCurrentUser` → boshlang'ich
  to'ldirilgan/`is-liked` holat + `LikeCount`.
- `@section Scripts`: `data-save-quote` bilan bir xil — `kitob.apiPost`
  `/quotes/{id}/like`, javobdagi `isLiked`/`likeCount` bilan ikonka FILL +
  hisoblagichni yangilaydi.

---

## Tekshiruv rejasi
1. `dotnet build` → 0/0. **Rebuild + restart SHART** (Razor runtime
   compilation o'chiq — `.cshtml`/CSS o'zgarishi qayta ishga tushirishni
   talab qiladi), port 5261.
2. `dotnet test` → hammasi yashil (yangi `ToggleLikeQuote` testlari bilan).
   Migratsiya real DB'ga qo'llangani (`QuoteLikes` jadvali, unique index) tekshiriladi.
3. Dev JWT: `GET /quotes` — yangi kartalar (dekorativ belgi, serif italic,
   futer avatar+nom, **like + save** + delete) render bo'ladi.
4. `POST /quotes/{id}/like` → `{isLiked:true, likeCount:1}`, qayta bosish →
   `{isLiked:false, likeCount:0}`; ikonka FILL va hisoblagich yangilanadi.
5. "Yangi iqtibos" → forma ochiladi; search o'ngida "Yangi kitob" tugmasi;
   tugma → kitob qo'shish formasi (nom/muallif/sahifa/janr/muqova) →
   `/books/create` → chip → iqtibos matni → Saqlash → `/quotes/create` 302.
6. Test ma'lumotlari (qo'shilgan test kitob/iqtibos/like) tozalanadi — DB asl holatda.

## Amalga oshirish tartibi (keyingi sessiya uchun)
1. **Like backend (D)**: QuoteLike entity + nav'lar + Configuration + DbSet'lar
   (IAppDbContext/AppDbContext/TestDbContext) + migratsiya + QuoteDto/projeksiya +
   LikeQuoteResultDto + ToggleLikeQuote command/handler + QuotesController endpoint +
   DeleteAccount cleanup + testlar. `dotnet build` + `dotnet test`.
2. site.css `.quote-*` bloki (A — like/save tugmalari bilan).
3. Quotes/Index.cshtml markup (A futer: like+save+delete + B forma).
4. Quotes/Index.cshtml `@section Scripts` (B kitob picker JS + like/save toggle JS).
5. Build + restart + 5261 tekshiruv + PROGRESS.md yangilash.
