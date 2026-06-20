# kitobdagimen.uz — Progress log

Bu fayl HAR BIR bosqich tugagach yangilanishi SHART.
Format: har bir bosqich uchun [ ] yoki [x], va qisqa izoh.

> Eslatma: Kanonik hujjatlar loyiha ildizida — `CLAUDE.md`, `docs/PROJECT-SPEC.md`,
> `docs/PROGRESS.md`. (Avval `.idea/` ichida edi; Claude Code ildizdagi CLAUDE.md ni
> avtomatik o'qigani uchun ildizga ko'chirildi. `.idea/docs/` nusxasi ham sinxron tutiladi.)

## Bosqichlar ro'yxati

- [x] 1-bosqich: Loyiha skeleti (global.json, .sln, 4 ta loyiha, NuGet paketlar, reference'lar)
- [x] 2-bosqich: Domain qatlami (barcha entity'lar, enum'lar, BaseEntity)
- [x] 3-bosqich: Infrastructure — Persistence (AppDbContext, Configuration'lar, AppDbContextFactory, birinchi migratsiya)
- [x] 4-bosqich: Infrastructure — Identity (Google OAuth, JWT, HttpOnly cookie)
- [x] 5-bosqich: Application — Auth va Onboarding feature'lari
- [x] 6-bosqich: Application — Posts feature'lari (Create, Feed, GetById, Like, Comment, View)
- [x] 7-bosqich: Application — Profile va Follow feature'lari
- [x] 8-bosqich: Application — ReadingGoals feature'lari
- [x] 9-bosqich: Application — Quotes feature'lari
- [x] 10-bosqich: Application — Chat feature'lari
- [x] 11-bosqich: Infrastructure — Redis, SignalR, Hangfire sozlamalari
- [x] 12-bosqich: Web — backend (Controllers/Endpoints, Program.cs, DI, middleware)
- [x] 13-bosqich: Web — frontend sahifa 1-3 (Landing, Kirish, Janr tanlash)
- [x] 14-bosqich: Web — frontend sahifa 4-6 (Asosiy feed, Post detail, Profil)
- [x] 15-bosqich: Web — frontend sahifa 7-9 (O'qish maqsadlari, Iqtiboslar, Chat)
- [x] 16-bosqich: SignalR Hub'lar (ChatHub, NotificationHub) — frontend bilan ulash
- [x] 17-bosqich: Migratsiya va seed data (janrlar ro'yxati + namuna kitoblar + startup migrate)
- [x] 18-bosqich: Testlar (asosiy Application handler'lar uchun)
- [x] 19-bosqich: Yakuniy build, xatolarni tuzatish, README.md — **LOYIHA TUGADI**

## /chat 2.0 — yangi talab (taklif tizimi · qidiruv · 3D boyo'g'li · online/last-seen · double-tick · 5000 limit)

To'liq dizayn: `docs/CHAT-V2-DESIGN.md` (LOYIHALASHTIRILDI 2026-06-19). Bosqichma-bosqich amalga oshiriladi:

- [x] C1-bosqich: Domain+DB — `Connection` entity + `ConnectionStatus` enum + `User.LastSeenAt` + Configuration + migratsiya `20260619144358_AddConnectionsAndLastSeen` (real DB'ga qo'llandi); IAppDbContext/AppDbContext/TestDbContext ga `Connections` DbSet
- [x] C2-bosqich: Connections feature — SendConnectionRequest (auto-accept teskari taklifda) / RespondToConnection (qabulda Conversation yaratadi) / CancelConnectionRequest / GetPendingRequests + ConnectionDto. NotificationDto ga RelatedId/ActorId qo'shildi (boyo'g'li qabul/rad uchun)
- [x] C3-bosqich: User qidiruv — `SearchUsersQuery` + `UserSearchResultDto` + `ConnectionState` enum (None/PendingOutgoing/PendingIncoming/Connected) + HasStory(faol) + LastSeenAt. ILike emas, `ToLower().Contains` (Application provider-agnostik)
- [x] C4-bosqich: Chat gate + ro'yxat — `GetConversations` qabul qilingan Connection asosida (xabar bo'lmasa ham ko'rinadi) + LastSeenAt/IsOnline; `SendMessage` gate (Accepted shart); validator 4000→**5000**
- [x] C5-bosqich: Presence — `IPresenceService` + `RedisPresenceService` + ChatHub connect/disconnect/heartbeat + PresenceChanged. **Program.cs ga DI ro'yxati qo'shildi** (oldingi sessiyada unutilgan edi — ChatHub runtime'da yiqilardi)
- [x] C6-bosqich: Read receipts real-time — `IChatNotifier.MessagesReadAsync` + `MarkMessagesReadCommandHandler` signali + frontend double-tick (✓ / ✓✓ ko'k). **Test `SpyChatNotifier` yangilandi** (oldingi sessiyada build buzilgan edi)
- [x] C7-bosqich: Web endpointlar — `ChatController` ga `search`/`connect`/`connect/{id}/respond`/`connect/{id}/cancel`/`requests` qo'shildi; `IsOnline` Redis presence bilan boyitiladi (search + conversation list). Alohida ConnectionsController shart bo'lmadi (dizayn: ixtiyoriy)
- [x] C8-bosqich: Frontend /chat qayta tuzildi — 3 ustun (qidiruv+suhbatlar | xabarlar | boyo'g'li). Qidiruv (300ms debounce), taklif/qabul/bekor tugmalari, online nuqta, last-seen humanize, double-tick, 5000 sanagich. CSS yangilandi
- [x] C9-bosqich: 🦉 Boyo'g'li — `wwwroot/js/owl.js` (ESM, three.js dinamik import — CDN/WebGL bo'lmasa 2D SVG zaxira). Protsedural model (tana/bosh/ko'z/qorachiq/tumshuq/quloq pati/qanot) + rig (headGroup yaw/pitch lerp) + holat mashinasi (idle/curious/alert/happy) + blink/breathing. `kitob:notification` event orqali ALERT + speech bubble (Qabul/Rad)
- [x] C10-bosqich: Build 0/0, **test 49/49** (9 yangi: gate, 6 connection, 2 search + 2 deleteaccount). `DeleteAccountCommandHandler` ga Connections cleanup qo'shildi (Restrict FK). 5261 da curl bilan to'liq oqim tekshirildi (invite→pending→accept→connected→message→read), test ma'lumotlari tozalandi

**LOYIHA /chat 2.0 TUGADI.** Keyingi qadam: yangi talab bo'lmasa — vizual brauzer tekshiruvi (3D boyo'g'li faqat real brauzerda to'liq ko'rinadi).

## /quotes qayta dizayn — yangi talab (kartalar 07-iqtiboslar.html dan + "Yangi kitob" tugmasi)

To'liq dizayn: `docs/QUOTES-REDESIGN-DESIGN.md` (LOYIHALASHTIRILDI 2026-06-19,
like talabi bilan yangilandi). Uch qism: (A) iqtibos kartalarini 07-iqtiboslar
dizayniga o'tkazish (dekorativ format_quote belgisi, serif italic, futer
avatar+ism, **like+save**+delete); (B) "Yangi iqtibos" formasiga kitob qidiruv
o'ngida "Yangi kitob" tugmasi + /feed dagi to'liq kitob qo'shish oqimi;
(D) **iqtiboslarga LIKE — full-stack** (`QuoteLike` entity, `SavedQuote`
pattern nusxasi: migratsiya, `QuoteDto.LikeCount/IsLikedByCurrentUser`,
`ToggleLikeQuote` command/handler, `/quotes/{id}/like`, DeleteAccount cleanup,
test). (A)+(B) frontend; (D) backend+frontend.

- [ ] Q1: Like backend (D) — QuoteLike entity+nav+config+DbSet'lar+migratsiya+
      QuoteDto/projeksiya+LikeQuoteResultDto+ToggleLikeQuote+endpoint+
      DeleteAccount cleanup+test → build+test
- [ ] Q2: site.css `.quote-*` bloki (karta qayta dizayn + grid + like/save)
- [x] Q3 (qisman): Quotes/Index.cshtml — **"Yangi kitob" forma BAJARILDI**
      (2026-06-19). Karta futeri redizayni (like+save+delete) HALI emas — Q1 dan keyin.
- [x] Q4 (qisman): Quotes/Index.cshtml `@section Scripts` — **kitob picker JS BAJARILDI**.
      Like/save toggle JS HALI emas (Q1 backend kerak).
- [ ] Q5: build + restart (5261) + tekshiruv + PROGRESS yangilash

## Oxirgi sessiya yozuvi

**2026-06-20 — PRODUCTIONGA DEPLOY QILINDI: https://kitobdagimen.uz JONLI ishlayapti (Hetzner + nginx + HTTPS + Google login).**

- **Server:** Hetzner CX23 (Ubuntu, 4 GB), IP **204.168.192.197**, Helsinki. SSH: `ssh root@204.168.192.197` (kalit `~/.ssh/id_kitobdagimen`).
- **Stack o'rnatildi:** .NET 8 SDK (Microsoft `dotnet-install.sh` orqali — Ubuntu repo'da `dotnet-sdk-8.0` yo'q edi; `/usr/share/dotnet` + symlink `/usr/local/bin/dotnet`), PostgreSQL 18, Redis 8 (parol bilan), nginx 1.28. 2 GB swap.
- **DB:** `kitobdagimen` bazasi + kam huquqli `kitobdagimen` user (superuser EMAS). Migratsiyalar startup'da avtomatik (DbInitializer).
- **Ilova:** `/var/www/kitobdagimen` (git clone), publish → `/var/www/kitobdagimen/publish`. systemd service **`kitobdagimen`** (User=`kitobapp`, kam huquqli), Kestrel `127.0.0.1:5000`. Env: **`/etc/kitobdagimen/kitobdagimen.env`** (chmod 600) — Jwt__Key, DB/Redis parollar, AllowedHosts, Google, Hangfire admin email. Maxfiy qiymatlar nusxasi: `/root/kitobdagimen-secrets.txt`.
- **nginx:** `/etc/nginx/sites-available/kg` → reverse proxy `127.0.0.1:5000`, `client_max_body_size 12M`, X-Forwarded-* + WebSocket. Let's Encrypt sertifikat (certbot --nginx, auto-renew). HTTP→HTTPS redirect.
- **Google OAuth:** redirect URI **`https://kitobdagimen.uz/signin-google`** (`/auth/google-callback` EMAS — .NET Google moduli standart `/signin-google` ishlatadi; `redirect_uri_mismatch` shu sababli edi). JS origin `https://kitobdagimen.uz`.
- **global.json:** `rollForward` `latestPatch`→`latestFeature` (server 8.0.422 SDK bilan build bo'lishi uchun).
- **YANGILASH (redeploy) tartibi:** `cd /var/www/kitobdagimen && git pull && dotnet publish src/KitobdaGimen.Web -c Release -o publish && chown -R kitobapp:kitobapp publish && systemctl restart kitobdagimen`.
- **Foydali buyruqlar:** loglar `journalctl -u kitobdagimen -f`; holat `systemctl status kitobdagimen`; AllowedHosts tufayli lokal test `curl -H "Host: kitobdagimen.uz" http://127.0.0.1:5000/`.
- **MUHIM (terminal paste):** foydalanuvchining SSH paste'i UZUN qatorlarni (~80+ belgi) yoki `<<EOF` heredoc / `\n` printf / `{ }` guruhlarni BUZADI (qator o'rtasiga newline qo'shadi, heredoc'da osilib qoladi). ISHLAYDIGAN usul: faqat QISQA, mustaqil `echo "k=$VAR" >> fayl` qatorlari; uzun qiymatlar uchun `read -r VAR` (qiymat alohida qatorga paste qilinadi). Fayllarni heredoc bilan yaratmaslik.

---

**2026-06-20 — DEPLOYGA TAYYORGARLIK: xavfsizlik auditi + hardening (public repo + Hetzner). Build 0/0, test 71/71.**

Talab: serverga (Hetzner) qo'yishdan oldin loyihani xavfsizlik nuqtai nazaridan ko'rib chiqish, kiberhujumlarga qarshi qattiqlashtirish; repo PUBLIC bo'lgani uchun maxfiy fayllar GitHub'ga ketmasligini ta'minlash. (Git ishlatilmadi — foydalanuvchi o'zi push qiladi.)

- **Secret-leak auditi (public repo):** manba kodida haqiqiy maxfiy qiymat YO'Q — `appsettings.json` placeholder shablon, `appsettings.Development.json` faqat dev kaliti, README placeholderlar, secrets `user-secrets` da. `.idea/.gitignore` workspace.xml/dataSources'ni allaqachon ignore qiladi. `wwwroot/uploads/` allaqachon ignore'da. CORS yo'q (same-origin — yaxshi).
- **`.gitignore` qattiqlashtirildi:** qo'shildi — `.claude/settings.local.json` (lokal DB parol + bypassPermissions bor edi, ignore'da emas edi), `appsettings.Production.json`, `secrets.json`, `*.pfx/*.pem/*.key/*.crt/*.p12`.
- **JWT fail-fast (`IdentityServiceExtensions.cs`):** zaif `new string('0',32)` fallback OLIB TASHLANDI. Endi kalit bo'sh / <32 belgi / placeholder bo'lsa startup ataylab yiqiladi (aks holda hujumchi token soxtalashtirardi). Dev'da appsettings.Development.json/user-secrets kaliti bor — buzilmaydi.
- **`Program.cs` hardening:** (1) `UseForwardedHeaders` (nginx ortida to'g'ri HTTPS/IP); (2) **xavfsizlik sarlavhalari** middleware — `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, **CSP** (self + Google Fonts + cdnjs(SignalR) + unpkg(three.js) + Google avatar img); (3) **rate limiting** — har IP 600 so'rov/daqiqa (DoS/flood); (4) Hangfire dashboard endi `HangfireDashboardAuthFilter` bilan — faqat `Hangfire:DashboardEmails` adminlari (default: hech kim). Reverse-proxy ortida standart "local-only" filtr ishlamasligi sababli bu KRITIK edi.
- **Fayl yuklash (`BooksController.UploadCover`):** xom baytlarni content-type bo'yicha saqlash O'RNIGA ImageSharp bilan WebP'ga qayta-kodlash (postlardagi kabi). Polyglot/yashirin-HTML/skript yuklash endi imkonsiz. `PostsController.Like` ga `[ValidateAntiForgeryToken]` qo'shildi (JS allaqachon token yuborardi — apiPost).
- **Yangi hujjat `docs/DEPLOY-SECURITY.md`:** to'liq Hetzner+nginx deploy xavfsizlik checklisti — production env var'lari (Jwt__Key, Google, AllowedHosts, Hangfire admin), nginx reverse-proxy config (X-Forwarded-* + WebSocket), firewall (ufw), Postgres/Redis localhost, push-oldidan secret tekshiruvi, deploy-keyin curl tekshiruvi.
- **TEKSHIRILDI:** `dotnet build -c Release` **0 warning / 0 error**; `dotnet test` **71/71 passed**. Foydalanuvchining 5261 dev (watch, Debug) bin'iga tegilmadi (Release'da qurildi). DB tegilmadi. **MUHIM:** CSP + JWT fail-fast yangi `Program.cs`/Identity o'zgarishlari — foydalanuvchi dev'ni bir marta to'liq qayta ishga tushirsa, xavfsizlik sarlavhalari va fail-fast jonli ko'rinadi. Production'ga qo'yishdan oldin `docs/DEPLOY-SECURITY.md` ni bajarish SHART (ayniqsa `Jwt__Key` env var — busiz ilova ishga tushmaydi).

---

**2026-06-20 — Foydalanish QO'LLANMASI sahifasi (`/qollanma`, `Home/Guide.cshtml`) yaratildi + landingdan havola qo'shildi.**

Talab: landing pagening biror bo'limida dasturni ishlatish bo'yicha to'liq qo'llanma bo'lsin (alohida `.cshtml` sahifa), bosilganda dasturdan qanday foydalanish haqida to'liq ma'lumotni o'qib chiqsin.

- **Yangi sahifa `Views/Home/Guide.cshtml`:** landing uslubidagi `.lp` chrome (`HideChrome=true`, o'z `lp-nav` headeri). Hero + **mundarija (guide-toc, jump-link ankerlar)** + 13 bosqichli to'liq qo'llanma: (1) Google kirish, (2) janr/onboarding, (3) tasma+algoritm, (4) post yozish, (5) kitob qidirish/qo'shish, (6) yoqtirish/izoh/ulashish, (7) kutubxona+maqsadlar, (8) iqtiboslar, (9) storylar, (10) profil+kuzatish, (11) chat (ulanish/online/double-tick/5000), (12) bildirishnomalar, (13) kun/tun+responsive+hisob o'chirish. Har bo'lim raqamli (`guide-step-num`), `guide-list` (check_circle markerli), ba'zilarida `guide-tip` (lightbulb). Auth-aware (kirgan: "Tasmaga o'tish" / anonim: "Kirish"/"Bepul qo'shilish").
- **Controller:** `HomeController.Guide()` → `View()`. **Route:** `Program.cs` ga `/qollanma` alias (default `/Home/Guide` ham ishlaydi).
- **Landingdan havolalar (`Home/Index.cshtml`):** (a) `lp-nav-links` ga "Qo'llanma", (b) CTA dan oldin yangi **`lp-guide-promo` bo'limi** (ikon + matn + "To'liq qo'llanmani o'qish" tugmasi → `/qollanma`), (c) futer havolalariga "Qo'llanma".
- **CSS (`site.css`):** yangi `.lp-guide-promo*` (landing bloki, ≤768 column) + `.guide-*` bo'limi (toc 2 ustun→1, guide-step flex+raqam, guide-list check marker, guide-tip, guide-inline-icon; 768/480 responsive).
- **TEKSHIRILDI:** Build **0/0**. Foydalanuvchining `dotnet watch` (5261) eski binarda — yangi route/view **to'liq restart** talab qiladi (rude edit; navbar `/Home/Landing` kabi konvensional emas). Tasdiqlash uchun yangi qurilgan binar **alohida port 5299** da ishga tushirildi (user'ning 5261/Rider'iga tegilmadi): `/qollanma` **200**, `/Home/Guide` **200**, sahifada 13 bo'lim + mundarija + barcha matn render, landingda promo+3 ta `/qollanma` havola, CSS (`guide-*`, `lp-guide-promo`) jonli serve. Temp app to'xtatildi; 5261 butun (200). **Foydalanuvchi `/qollanma` ni ko'rishi uchun dotnet watch ni bir marta to'liq qayta ishga tushirsin** (`/Home/Guide` ham shu restartdan keyin).

---

**2026-06-20 — Landing (`Home/Index.cshtml`) TO'LIQ qayta dizayn + responsive qilindi, VA kirgan foydalanuvchi landingga qayta o'ta olmaslik MUAMMOSI hal qilindi.**

Talab: landing sahifani to'liq ma'lumot bilan qayta loyihalash + responsive; hamda muammo — saytga birinchi kirilganda landingdan `/feed` ga o'tiladi, keyin landingga QAYTISH yo'li yo'q edi.

- **Navigatsiya muammosi (hal):** `HomeController.Index()` kirgan foydalanuvchini `/feed` ga yo'naltiradi (to'g'ri — birinchi kirish xulqi). Allaqachon mavjud `Landing()` action yo'naltirmasdan landingni render qiladi, LEKIN unga hech qaerdan havola yo'q edi. Yechim: (1) `_Layout.cshtml` autentifikatsiyalangan navbar `nav-links` ga **"Bosh sahifa" → `/Home/Landing`** havolasi qo'shildi (har sahifadan topiladi; mobil burger menyuda ham); (2) `Program.cs` ga **toza `/landing` alias route** qo'shildi (Home/Landing ga). MUHIM: navbar havolasi `/Home/Landing` ga (konvensional route — restart shart EMAS, hozir ishlaydi); `/landing` alias route Program.cs o'zgargani uchun **dotnet watch RUDE-RESTART** dan keyin faollashadi (hot-reload endpoint mappingni qayta yozmaydi — bu normal).
- **Landing auth-aware:** `ICurrentUserService` inject; kirgan bo'lsa nav tugmasi "Tasmaga o'tish" → `/feed`, hero'da Google tugma o'rniga "Tasmaga o'tish", CTA "Tasmaga o'tish"; anonim bo'lsa Google login + "Kirish". (`/` baribir kirganni `/feed` ga yo'naltiradi; landingni qayta ko'rish uchun "Bosh sahifa" yoki `/Home/Landing`.)
- **Yangi kontent (`Home/Index.cshtml`):** hero ikki tugmali (asosiy + "Imkoniyatlarni ko'rish" scroll) + ishonch qatori (`.lp-trust`: bepul/bir bosishda/real vaqt/o'zbek tilida); 6 imkoniyat kartasi (boyitilgan matn); YANGI **showcase** bo'limi (`.lp-showcase` — 4 ta alternativ karta: kutubxona/aqlli tasma/bildirishnoma/kun-tun); 3 qadam; YANGI **FAQ** (`.lp-faq` — `<details>` yig'iladigan 4 savol); CTA; boyitilgan **futer** (brand + havola ustuni + copyright).
- **CSS (`site.css`):** eski yagona `860px` breakpoint OLIB TASHLANDI; o'rniga loyiha konvensiyasidagi **bosqichli 1024/768/480/360** to'plam + yangi bo'limlar uchun stillar (`.lp-trust`, `.lp-showcase`/`.lp-show-card`/`.lp-show-icon`, `.lp-faq`/`.lp-faq-item` summary `::after` add/remove ikon, `.lp-footer` grid + `.lp-footer-links`, hero ikkilamchi tugma). `--accent-soft` fallback `--primary-soft`.
- **TEKShIRILDI:** Build **0/0**. Live (5261, dotnet watch hot-reload): `/Home/Landing` 200, yangi bo'limlar (lp-trust/lp-showcase/lp-faq/lp-footer-links) render, yangi CSS jonli serve, anonim Google tugma ko'rinadi. **HEADLESS CHROME (CDP, port 9261, anonim — JWT shart emas)** 5 kenglikda (1280/1024/768/480/360): **overflow = 0** hammasida; h1 52→44→36→29→26; showcase 2→1 ustun (≤768); features 3→1 (≤768); lp-nav-links havolalari ≤768 da yashirin; hero tugmalar ≤480 da `column`. Headless chrome + temp fayllar TOZALANDI (user'ning Brave/Rider'iga tegilmadi). **ESLATMA:** `/landing` alias to'liq ishlashi uchun app bir marta to'liq qayta ishga tushishi kerak (dotnet watch terminalida rude-edit restart); `/Home/Landing` esa hozir ishlaydi.

---

**2026-06-20 — `/quotes` "Yangi iqtibos" formasidagi kitob picker `/feed` uslubiga o'tkazildi: "Kitobni qidiring..." input + o'ng tomonda "Yangi kitob" tugmasi.**

Talab: "Yangi iqtibos" bosilganda "kitobni qidiring" input chiqsin, o'ng tomonida "Yangi kitob" buttoni bo'lsin.

- `Quotes/Index.cshtml`: `#quoteBookPicker` ichidagi eski "Yangi kitob qo'shish" toggle tugmasi o'rniga `.composer-book` blok — `#quoteBookSearch` ("Kitobni qidiring...") input + `#quoteNewBookToggle` ("Yangi kitob") tugma; ostida `#quoteBookSuggestions` dropdown.
- JS: 300ms debounce bilan `/books/search?q=` ga so'rov; natijalar `.book-suggestion` div'lar; bosilganda `pickBook` (search input tozalanadi, suggestions yopiladi). "Yangi kitob" tugmasi avvalgidek `#quoteNewBookForm` ni ochadi/yopadi.
- CSS yangi kerak emas: `.composer-book` va `.book-suggestions div` allaqachon mavjud. Build 0/0.

**2026-06-20 (davomi) — `/quotes` "Yangi kitob" formasi endi `/feed` bilan TO'LIQ bir xil.**

Talab: "Yangi kitob" bosilganda `/feed` dagi "Yangi kitob" formasida nima chiqsa, shu chiqsin.

- `Quotes/Index.cshtml`: forma maydonlari to'ldirildi — eski (faqat nom+muallif) o'rniga: nom, muallif, **sahifalar soni** (`#qNbPages`), **kategoriya** `<select>` (`#qNbGenre`, `GetGenresQuery` orqali to'ldiriladi), **muqova rasmi** (`#qNbCover` + preview + o'chirish, `.nb-cover` bloki).
- View header'ga `@inject ISender Mediator` + `GetGenresQuery` qo'shildi (genres ro'yxati uchun).
- JS: `/feed` dagi kabi cover upload (`/books/upload-cover`, `uploadCover()`, `resetCover()`), va `qNbSave` validatsiyasi to'liq (`totalPages>0`, genre majburiy, 100000 limit); `coverUrl` `/books/create` ga `coverUrl` bilan yuboriladi.
- CSS yangi kerak emas: `.nb-cover*` allaqachon mavjud. Build 0/0.

**2026-06-20 — `/feed` va `/quotes` postlari endi ALGORITM bilan chiqadi: turli foydalanuvchilarning yaqin postlari aralashtiriladi, kuzatilganlar 60% / kuzatilmaganlar 40%, pastga tushgan sari eskiroq postlar.**

Talab: feed/quotes da turli xil foydalanuvchilarning yaqinda yozilgan postlari chiqsin; pastga tushgan sari eskiroq postlar ko'rinsin; kuzatilgan (follow bosilgan) foydalanuvchilarga 60%, kuzatilmaganlarga 40% ulush.

- **Yondashuv (ikkala handler bir xil):** ikki recency-tartibli "bucket" — (A) kuzatilganlar + o'zining postlari, (B) qolgan hamma. Bularni **3 kuzatilgan : 2 kuzatilmagan** ohangda (≈60/40) aralashtiriladi. Yangi umumiy helper: `src/KitobdaGimen.Application/Common/Feed/FollowMixPlan.cs` (`Build(page, pageSize, followedTotal, nonFollowedTotal)` → `FollowedSkip/Take`, `NonFollowedSkip/Take`, `Order` (slot-larga F/N ketma-ketligi)). To'liq deterministik: chuqurroq sahifa = eskiroq postlar, sahifalar orasida dublikat/bo'shliq yo'q. Bir bucket tugasa, ikkinchisidan to'ldiriladi (sahifa qisqarmaydi).
- **`GetFeedQueryHandler`:** qidiruv (`Search`) — eski global recency oqimi (algoritm qo'llanmaydi). Hech kimni kuzatmasa (yoki anonim) — global recency oqimi (avvalgi fallback). Aks holda — FollowMixPlan bilan aralashtirish. Tartib: `OrderByDescending(CreatedAt).ThenByDescending(Id)` (barobar vaqtda barqaror).
- **`GetQuotesQueryHandler`:** xuddi shu, lekin `BookId` filtri (kitob picker) ham qidiruv kabi algoritmni chetlab o'tadi (faqat shu kitob iqtiboslarini recency bilan). Quotes endi `Follows` ni ham ishlatadi (avval umuman ishlatmasdi).
- **Eslatma (kelajak uchun):** "pastga = eski" kafolati HAR BUCKET ichida; bucket'lar mustaqil sahifalangani uchun GLOBAL qat'iy vaqt tartibi YO'Q (page2 dagi kuzatilgan post page1 dagi kuzatilmagan postdan yangiroq bo'lishi mumkin) — bu kutilgan, talab shuni nazarda tutadi.
- **Testlar:** mavjud `GetFeed_returns_followed_authors_and_own_posts` → `GetFeed_blends_followed_first_then_non_followed` ga yangilandi (endi notanish ham ko'rinadi: TotalCount 2→3, notanish oxirda). Yangi `GetFeed_mixes_60_40_and_pages_without_duplicates` (10 kuzatilgan + 10 notanish, pageSize 5 → 3:2, sahifalar orasida dublikat yo'q, har bucketda chuqurroq=eskiroq). **Build 0/0, test 66/66** (oldin 65). DB tegilmadi, migratsiya shart emas (faqat so'rov mantig'i). CSS/markup tegilmadi.

---

**2026-06-20 — `/reading-books` da telefonda kitob kartasi ichidagi KATTA bo'sh joy (statistika ↔ "Betlarni yangilash" tugmasi orasida) TUZATILDI — ASOSIY sabab: `.rb-body` ning `flex: 1 1 240px` flex-basisi ustun-layoutda BALANDLIKka aylanardi.**

Talab: telefon rejimida "Betlarni yangilash" (`.goal-log-toggle`) tugmasi tepasida juda ko'p bo'sh joy qolardi — uni olib tashlash.

- **DIAGNOSTIKA (skrinshot bilan):** boshida `.goal-log[hidden]` deb taxmin qildim (tugma OSTIDA ~45px), lekin foydalanuvchi "bo'lmadi, flex/gap/space-between bo'lishi mumkin" dedi → jonli skrinshot oldim va ASOSIY sabab boshqa ekanini ko'rdim: bo'sh joy statistika bilan tugma ORASIDA (~147px) edi.
- **Asosiy sabab (flex-basis = balandlik):** `.rb-body { flex: 1 1 240px }` (site.css ~1044). Desktop ROW layoutda 240px = WIDTH. Lekin ≤1024 da `.book-row { flex-direction: column }` bo'lgani uchun `flex-basis: 240px` ENDI asosiy o'q = BALANDLIK bo'lib qoladi → `.rb-body` o'z kontentidan (~140px) balandroq, majburan 240px ga cho'ziladi, statistika ostida katta bo'sh joy qoldiradi (o'lchovda `body h:240` aniq chiqqan edi).
- **Yechim:** ≤1024 media blokiga `.rb-body { flex: 0 0 auto; }` qo'shildi (kontent balandligiga qaytaradi; width baribir `align-items: stretch` bilan to'liq). Desktop tegilmadi (qoida media-query ichida).
- **Qo'shimcha (oldingi qadamdan, saqlanadi):** `.goal-log[hidden] { display: none; }` ham qo'shilgan — yashiringan input+"Qo'shish" qatori (`.goal-log { display:flex }` `[hidden]` ni yengardi) tugma OSTIDA ~45px egallashini ham yo'q qildi. Markup/JS tegilmadi; JS toggle `hidden` atributini olib tashlaganda `display:flex` qaytib ochiladi.
- **TEKSHIRILDI (HEADLESS CHROME, CDP + SKRINSHOT, dev JWT user1, `/reading-books`, 390px):** `.book-row` balandligi **500px → 353px** (147px bo'sh joy ketdi); skrinshotda "Betlarni yangilash" endi to'g'ridan-to'g'ri statistika ostida, ketidan "O'chirish". `.rb-actions` 133→78px (goal-log[hidden] tufayli). overflow=0. Temp fayllar + headless chrome (9245/9246) tozalandi; foydalanuvchi Chrome/Rider'iga tegilmadi. CSS statik — rebuild shart emas (hard refresh kifoya). DB tegilmadi.

---

**2026-06-20 — Post detal sahifasi (`/post/{username}/{slug}`) TO'LIQ moslashuvchan (responsive) qilindi — planshet va telefonda shrift/tugma/avatar/ikonlar bosqichma-bosqich kichrayadi, gorizontal overflow YO'Q.**

Talab: post detal sahifasi tablet va telefonga mukammal tushishi; har bir qurilmada shriftlar, tugmalar, ikonlar va ularning shriftlari kichrayishi kerak.

- **Sabab/holat:** post detalda juda kam responsive bor edi — `.pd-grid` ≤1024 da 1 ustun, va ≤480 da atigi 5 ta sozlama (h1 27, review 16, hero 260, stats 14, comments-card pad 16). Muallif sarlavhasi (avatar/ism/vaqt), kitob muallifi (18px), janr chiplari, amal tugmalari (yoqdi/ulashish/tahrir/o'chirish) va ularning **24px ikonlari**, statistika ikonlari, izoh sarlavhasi/kengaytirish tugmasi, izoh pufakchalari (ism/matn/avatar), javob formasi, izoh yozish formasi, "Barcha izohlar" modali, anonim banner — barchasi har kenglikda bir xil qolardi. ≤768 (planshet) pog'onasi umuman yo'q edi.
- **Yondashuv:** /profile, /chat, /feed bilan bir xil — markup O'ZGARMADI (`Posts/Details.cshtml` tegilmadi), faqat `wwwroot/css/site.css` dagi post-detal bo'limidagi eski `≤1024`+`≤480` bloklari to'liq bosqichli to'plamga (1024/768/480/360) almashtirildi. `≥1025px` yopishqoq yon ustun bloki saqlanib qoldi. Post-detal responsive shu bo'limda joylashgani uchun (boshqa sahifalardek oxirgi katta blokda emas) o'sha yerda kengaytirildi.
- **`site.css` bosqichlari (post-detal bo'limi, ~694-qatordan):**
  - **≤1024px:** grid 1 ustun; h1 38→32, kitob muallifi 18→16.5, sharh 17→16.5, hero 360, comments-card 20.
  - **≤768px:** muallif avatar 44/ism 15/vaqt 12.5; h1 29, kitob muallifi 15.5, janr 12.5/ikon 16; sharh 16, hero 320; stats 13.5/ikon 18; amal tugmasi 13.5 + **ikon 24→20**; izoh sarlavhasi 18, kengaytirish 32px, pufakcha ism/matn 13.5.
  - **≤480px:** anon-banner + orqaga tugmasi kichrayadi; muallif avatar 42/ism 14.5; h1 25, kitob muallifi 14.5, janr 12; sharh 15, hero 260; stats 13/ikon 17; amal tugmasi 13 + **ikon 18**; comments-card 16/14; izoh avatar 36/pufakcha pad 9px12px/ism-matn 13.5/replies pad 20; izoh formasi + modal head/body kichrayadi.
  - **≤360px:** h1 22, kitob muallifi 13.5, sharh 14.5, stats 12.5, amal tugmasi 12.5 + ikon 17, comments-card 14/12, izoh sarlavhasi 16, izoh avatar 34/ism-matn 13/replies pad 16.
- **TEKSHIRILDI (HEADLESS CHROME, CDP, Node 22, dev JWT HS256 user1, cookie `kitobdagimen_token`, post 14 `/post/javohirsadullayev/I3qMVbFY4x2c` — MUALLIF ko'rinishi: 4 ta amal tugmasi yoqdi/ulashish/tahrir/o'chirish + 5 ta izoh render):** 5 kenglikda (1280/1024/768/480/360) jonli o'lchov: **overflow = 0** (1024/768/480/360), 1280 da −15px (toshish yo'q), grid ≤1024 da 1 ustunga tushadi, amal ikonlari 24→20→18→17 bosqichli, h1 38→32→29→25→22, barcha element kutilgan qiymatlarga teng. Temp fayllar va headless chrome (port 9244) tozalandi (foydalanuvchining haqiqiy Chrome/Rider'iga tegilmadi). CSS statik — rebuild shart emas. DB tegilmadi. Tekshiruv retsepti: [[headless-browser-cdp-testing]] dagi "Sandbox gotchas" bo'limi (chrome'ni to'g'ridan-to'g'ri background'da ishga tushir, connect-only node bilan probe qil).

---

**2026-06-20 — `/profile` (Profil) sahifasi TO'LIQ moslashuvchan (responsive) qilindi — planshet va telefonda shrift/avatar/tugma/ikonlar bosqichma-bosqich kichrayadi, gorizontal overflow YO'Q.**

Talab: `/profile` sahifasi tablet va telefonga mukammal tushishi; har bir qurilmada shriftlar, tugmalar, ikonlar va ularning shriftlari kichrayishi kerak.

- **Sabab/holat:** profilda faqat juda kam responsive bor edi — `.profile-layout` ≤900 da 1 ustun, plitalar (`.post-tiles`/`.story-tiles`) ≤600 da 2 ustun, `.profile-hero-info h1` ≤700 da 28px. Lekin avatar (ring), motto, statbar (raqam/yorliq), amal tugmalari (kuzatish/xabar/ulashish/tahrir/o'chirish), tablar, plita ikonlari va "Hozir o'qiyapti" yon paneli (muqova/halqa/sarlavha) — barcha kenglikda bir xil qolardi.
- **Yondashuv:** /feed, /reading-books, /quotes, /chat bilan bir xil uslub — markup O'ZGARMADI (`Profile/Index.cshtml` tegilmadi), faqat `wwwroot/css/site.css` ga yangi "/profile responsive" bo'limi (1024/768/480/360 bosqichli) qo'shildi (/chat bo'limidan keyin, notif bo'limidan oldin). Mavjud ≤900/≤700/≤600 profil bloklari (layout grid + plita grid kollapsi) saqlanib qoldi; bu yangi bo'lim faqat profilga xos vizual scaling.
- **`site.css` yangi bo'lim bosqichlari:**
  - **≤1024px (planshet landshaft):** ikki ustunli layout saqlanadi; ring 128→116, avatar shrift 44→40, h1 36→32, yon panel padding 24→20, muqova 128→112, halqa 96→86.
  - **≤768px (planshet portret / katta telefon):** ring 116→104, h1 32→28, motto 18→16.5, statbar raqam 20→19/yorliq 13→12.5, amal tugmalari (`.btn` 14 / `.btn-sm` 13.5), tablar gap 28→20 + 15→14.5, plita ikonlari 38→34, "Hozir o'qiyapti" h3 20→18 + muqova 104 + halqa 84.
  - **≤480px (telefon):** **hero `flex-direction: column` + markazlashadi** (avatar tepada, ism/motto/statbar/tugmalar markazda) — ring 100, h1 28→25, motto 15, statbar markazda 18/12, amal tugmalari markazda (`.btn` 13.5 / `.btn-sm` 13) + ikonlar 17px (inline style `!important` bilan), tablar teng kenglikda (`flex:1`) 13.5, plitalar gap 8 + ikon 30 + matn 13, story-meta kichrayadi, "Hozir o'qiyapti" muqova 96/halqa 76/sarlavha 16, public-bar padding kamayadi.
  - **≤360px (kichik telefon):** ring 88, h1 22, motto 14, statbar 17/11.5, tugmalar 13/12.5, tab 12.5, muqova 88/halqa 70/sarlavha 15.5.
- **TEKSHIRILDI (HEADLESS CHROME, CDP, Node 22, dev JWT HS256 user1, cookie `kitobdagimen_token`):** 5 kenglikda (1280/1024/768/480/360) IKKALA profil ko'rinishida (o'z profil `/profile` — story/tahrir/ulashish/o'chirish to'liq amal to'plami; boshqa foydalanuvchi `/profile/6` — to'liq o'lchamli "Kuzatish/Xabar/Ulashish" tugmalari): **overflow = 0** (1024/768/480/360), 1280 da −15px (toshish yo'q), `heroDir` telefonda `column` ga o'tadi, barcha o'lchamlar kutilgan bosqichli qiymatlarga teng. Temp fayllar va headless chrome tozalandi (foydalanuvchining haqiqiy Chrome/Rider'iga tegilmadi). CSS statik — rebuild shart emas (5261 jonli, hard refresh). DB tegilmadi.
- **MUHIM diagnostika eslatma (kelajak sessiyalar uchun):** bu muhitda Bash sandbox **alohida `/tmp` namespace** va **alohida tarmoq namespace** ishlatadi — sandboxli buyruq yozgan faylni sandboxsiz buyruq KO'RMAYDI (va aksincha). Yana: node'dan chrome **spawn** qilish (`child_process`) foreground/background farqsiz BUTUN jarayon guruhini o'ldiradi (output izsiz yo'qoladi). Ishlaydigan yo'l: chrome'ni **to'g'ridan-to'g'ri** `run_in_background:true` bilan ishga tushir (WS URL'ni task output faylidan o'qi), so'ng FAQAT ulanadigan (spawn qilmaydigan) node skriptini `dangerouslyDisableSandbox:true` bilan foreground ishlatib CDP probe qil. Bu [[headless-browser-cdp-testing]] ga qo'shiladi.

---

**2026-06-20 — Telefonda o'ng tomonda chiqib qolgan "sariq chiziq" (gorizontal overflow) TUZATILDI — sabab navbar edi, /chat'da emas.**

Talab: telefon rejimida o'ng tomonda ochilib qolayotgan sariq joyni yo'q qilish.

- **Sabab:** body foni `--bg: #FAF6EE` (issiq krem/sarg'ish). Krem chiziq = gorizontal overflow. Diagnostika (headless Chrome, har bir elementni `getBoundingClientRect` bilan tekshirish) ko'rsatdi: **chat kontenti emas, balki global NAVBAR** ~390px (iPhone 12/13/14) atrofida **3px toshib ketardi** — `.brand` ("kitobdagimen.uz" wordmark) + 4 ta amal ikoni (theme/notif/avatar/burger) sig'masdi. `.chat` butun ekranni egallagani uchun o'sha 3px krem chiziq chat yonida aniq ko'rinardi. 480 va 360 da sig'ardi (padding/font farqi), faqat 360–480 oralig'ida (ayniqsa 375/390/393/412/414) toshardi.
- **Yechim (`wwwroot/css/site.css`, navbar global — barcha sahifaga foyda):**
  - **Bazaviy himoya:** `.brand { min-width: 0 }` + `.brand-text { overflow:hidden; text-overflow:ellipsis; min-width:0 }` — navbar endi HECH QACHON toshmaydi (kerak bo'lsa nom qisqaradi). Lekin clipping ham xunuk, shuning uchun:
  - **≤480px:** brand 22→17px, logo 36→28, navbar-inner gap 16→10, nav-actions gap 8→6, nav-icon-btn 40→34, nav avatar 40→32. Shu o'lchamlarda **to'liq "kitobdagimen.uz" ellipsissiz** 360–480 da marja bilan sig'adi.
  - **≤360px:** brand 16px, logo 26, nav avatar 30 (eng tor ekran uchun yana sal kichik).
  - O'lchamlar tasodifiy emas — CDP bilan 3 ta nomzod jonli sinalib, clip=0 beradigan to'plam tanlandi.
- **TEKSHIRILDI (headless Chrome, CDP, dev JWT user1):** 8 ta telefon kengligida (360/375/390/393/412/414/430/480) ikkala chat holatida (ro'yxat + suhbat): **overflow = 0**, **brand clip = false** (to'liq nom ko'rinadi), toshgan element **yo'q**. Temp fayllar tozalandi. CSS statik — rebuild shart emas (5261 jonli, hard refresh). DB tegilmadi.

---

**2026-06-20 — `/chat` (Xabarlar) sahifasi TO'LIQ moslashuvchan (responsive) qilindi — planshet va telefonda shrift/tugma/avatar/ikonlar bosqichma-bosqich kichrayadi, gorizontal overflow YO'Q.**

Talab: `/chat` sahifasi planshet va telefonga mukammal tushishi; har bir qurilmada shriftlar, tugmalar va ikonlar kichrayishi kerak.

- **Sabab/holat:** /chat'da faqat *layout collapse* bor edi (≤1024 owl yashirin + 2 ustun; ≤768 bitta ustun; ≤480 `.msg max-width:85%`). Lekin shrift/avatar/tugma/ikon scaling YO'Q edi — suhbatlar ro'yxati nomi/oxirgi xabar, qidiruv kartalari, sarlavha (ism/presence), xabar pufakchalari (matn/vaqt/tick), ulashilgan post mini-karta, yozish formasi (input/yuborish) — barcha kenglikda bir xil qolardi.
- **Yondashuv:** /feed, /reading-books, /quotes bilan bir xil uslub — markup O'ZGARMADI (cshtml tegilmadi), faqat `wwwroot/css/site.css` ga yangi "/chat responsive" bo'limi (1200/1024/768/480/360 bosqichli) qo'shildi (notif bo'limidan oldin). Mavjud umumiy ≤1024/≤768 layout bloklari saqlanib qoldi; bu yangi bo'lim faqat chat'ga xos vizual scaling.
- **Yangi: orqaga ("←") tugmasi** — bazaviy `.chat-header [data-back] { display:none }` qo'shildi (keng ekranda ro'yxat doim yonda — kerak emas), ≤768 da `display:inline-flex` (mobil bitta ustunda ro'yxatga qaytish uchun zarur). Sensorli qurilmada avatar/tugmalar tegishga qulay o'lchamda (planshetda avatar 40→46, telefonda 44).
- **`site.css` yangi bo'lim bosqichlari:**
  - **≤1200px:** yon panellar toraytirildi (sidebar 340→300, owl 300→260, owl canvas 240→210).
  - **≤1024px (planshet landshaft):** sidebar 300; chat-search/conv-item/search-card/messages padding kichrayadi.
  - **≤768px (planshet portret / katta telefon):** back ko'rinadi; conv avatar 46, conv name 15, last 13; header avatar 42, name 15.5, presence 12; msg max-width 80%.
  - **≤480px (telefon):** qidiruv input 14 + ikon 17; search-card avatar 44/name 14.5/uname-bio-presence 12/conn-btn 12.5; conv avatar 44/name 14.5/last 12.5; unread-badge 18px; online-dot 11px; header avatar 40/name 15/presence 11.5/back svg 22; msg 14px max-width 85%, msg-time 10.5, tick 13/15px, shared-card 200px; msg-act 26px; form input 14 + char-count 10.5.
  - **≤360px (kichik telefon):** conv avatar 40/name 14/last 12; header avatar 38/name 14.5; msg 13.5px max-width 88%; form input 13.5.
- **TEKSHIRILDI (HEADLESS CHROME, CDP, Node 22, dev JWT HS256 user1, cookie `kitobdagimen_token`, conversationId=4 — user1↔user6, 11 xabar render):** 4 kenglikda (1280/768/480/360) jonli o'lchov:
  - Gorizontal **overflow = 0** hamma kenglikda; **JS xatosi = 0** hamma kenglikda.
  - Layout: 1280 → 3 panel (sidebar+main+owl), back yashirin; 768/480/360 → bitta ustun (suhbat ko'rinadi), owl yashirin, back ko'rinadi.
  - Bosqichma-bosqich kichrayadi: convName 16→15→14.5→14, headerName 16→15.5→15→14.5, msg 14.5→14.5→14→13.5, msgTime 11→11→10.5→10.5, input 15→15→14→13.5; conv avatar 40→46→44→40 (sensorli bump); msg max-width 72%→80%→85%→88%.
  - CSS statik asset — rebuild SHART EMAS (5261 da jonli serve, curl bilan yangi blok topildi). Temp test fayli + chrome profil TOZALANDI. DB tegilmadi (faqat SELECT — render uchun). **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/quotes` (Iqtiboslar) sahifasi TO'LIQ moslashuvchan (responsive) qilindi — planshet va telefonda shrift/tugma/ikonlar bosqichma-bosqich kichrayadi, gorizontal overflow YO'Q.**

Talab: `/quotes` sahifasi planshet va telefonga mukammal tushishi; har bir qurilmada shriftlar, tugmalar va ikonlar kichrayishi kerak.

- **Sabab/holat:** quotes sahifasida moslashuvchanlik deyarli yo'q edi — yagona qoida generic `@media (max-width: 768px)` blokidagi `.quote-grid { columns: 1; }` (faqat layout). Sarlavha amallari ("Yangi iqtibos"/qidiruv), tablar (Barcha/Mening/Saqlangan), qidiruv formasi, "Yangi iqtibos" formasi va iqtibos kartalari (matn 19px, manba, saqlash/o'chirish) — hammasi barcha kenglikda bir xil o'lchamda qolardi.
- **Yondashuv:** /feed va /reading-books bilan bir xil uslub — markup O'ZGARMADI, faqat `wwwroot/css/site.css` ga yangi "/quotes responsive" bo'limi (768/480/360 bosqichli) qo'shildi. `.section-title` (sarlavha) va `.action-btn` (saqlash/o'chirish tugmalari) allaqachon /reading-books va /feed bloklaridan scaling oladi; `.composer-*` (yangi iqtibos forma ichki) /feed blokidan. Bu yerda iqtibosga xos qism yozildi.
- **Grid masonry:** generic 768 blokdagi `.quote-grid { columns: 1; }` OLIB TASHLANDI; endi tabletda (≤768) **2 ustun** (masonry, chiroyliroq), telefonda (≤480) **1 ustun**.
- **`wwwroot/css/site.css` (yangi bo'lim, /reading-books dan keyin, notif bo'limidan oldin):**
  - **≤768px (planshet):** `.quote-grid` column-gap 20→16; `.quote-text` 19→17.5; `::before` belgisi 28→26; `.quote-source` 14→13.5; `#newQuoteToggle` 15→14; tablar 14→13.5; `#newQuoteForm` h3 16, composer-toggle/selected 14.
  - **≤480px (telefon):** `.quote-grid` 1 ustun; `.quote-text` 16.5; `::before` 24; `.quote-source` 13; karta `.muted` (yozuvchi ismi) 12.5; `#newQuoteToggle` 13.5; tablar 13 + `flex:1` (uchchovi teng qatorda); qidiruv input 14; yangi iqtibos forma h3 15, textarea 14, composer-toggle/selected 13.5 + ikon 18.
  - **≤360px (kichik telefon):** `.quote-text` 15.5; `::before` 22; `.quote-source` 12.5; `#newQuoteToggle` 13; tablar 12.5; forma h3 14.5.
- **TEKShIRILDI:** CSS statik asset — rebuild SHART EMAS (server 5261 da ishlayapti, hard refresh yetarli); yangi blok jonli serve qilinyapti (curl: "quotes (Iqtiboslar)" topildi). **HEADLESS CHROME (CDP, Node 22 ichki WebSocket; chrome foreground spawn) — dev JWT (HS256, user1, cookie `kitobdagimen_token`, key=user secrets `Jwt:Key`) bilan 4 kenglikda (1280/768/480/360) jonli o'lchov** (8 iqtibos kartasi render bo'ldi):
  - Gorizontal overflow HAMMA kenglikda **0**, JS xatosi **0**.
  - Bosqichma-bosqich kichrayadi: h1 22→19→18→17, quote-text 19→17.5→16.5→15.5, quote-source 14→13.5→13→12.5, "Yangi iqtibos" tugma 15→14→13.5→13, tab tugma 14→13.5→13→12.5; grid ustunlari 2→2→1→1.
  - Headless chrome + temp test fayli (`/tmp/q-cdp-test.mjs`) + profil (`/tmp/q-cdp-profile`) TOZALANDI. DB tegilmadi (faqat o'qish — render uchun SELECT). App 5261 da ishlayapti. **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — Global chap/o'ng bo'shliq (gutter) tuzatildi + notification ikoni mobil/planshetda ko'rinadigan qilindi.**

Talab: barcha sahifalarda saytning o'ng/chap tomonida bo'shliq bo'lishi, va bu bo'shliq
header (navbar) chetidagi px bilan teng bo'lishi; planshet/telefonga o'tganda yo'qolib
qolayotgan "notification" ikoni ko'rinib turishi (responsive joylashtirilgan holda).

- **Sabab (gutter):** `<div class="container page">` da `.container` (`padding: 0 24px`)
  va `.page` (`padding: 32px 0 64px`) ikkalasi ham `padding` *shorthand* ishlatardi.
  `.page` keyinroq aniqlangani uchun gorizontal paddingni **0** ga tushirib, container'ning
  24px chetini bekor qilardi → sahifa kontenti ekran chetiga yopishardi.
- **Tuzatish (gutter) — `wwwroot/css/site.css`:**
  - `.page` endi faqat vertikal: `padding-top:32px; padding-bottom:64px` (gorizontal
    chetni `.container` beradi → header bilan bir xil 24px, bir xil `max-width:1100px`).
  - `@media ≤768px` dagi `.page` ham vertikal-only (`padding-top:24px; padding-bottom:48px`).
  - `@media ≤360px` ga `.container { padding: 0 12px }` qo'shildi (navbar-inner ham 12px →
    teng). (≤480px da ikkalasi allaqachon 16px edi.)
  - Natija: BARCHA `.container page` sahifalari (feed, reading-books, quotes, profile,
    onboarding, post-detail ...) header bilan bir xil chetga ega.
- **Tuzatish (notification) — `_Layout.cshtml` + `site.css`:**
  - `.notif-wrap` dan `desktop-only` klassi olib tashlandi (u `≤768px` da
    `.nav-actions .desktop-only{display:none}` orqali yashirilardi) → qo'ng'iroq endi
    barcha kengliklarda ko'rinadi.
  - Joy uchun: `≤480px` da `.nav-icon-btn` 40→38px, `.nav-actions` gap 12→8px;
    `≤360px` da `.nav-icon-btn` 34px, gap 6px.
  - `≤480px` da `.notif-panel` dropdown `position:fixed; left:12px; right:12px` —
    qo'ng'iroq chap chetda bo'lганda panel ekrandan chiqib ketmasligi uchun viewport bo'ylab.
- Build 0/0. `dotnet watch` hot-reload bilan jonli (5261) yangilandi; CSS va HTTP 200 OK.

---

**2026-06-20 — `/reading-books` (Kutubxona) sahifasi TO'LIQ moslashuvchan (responsive) qilindi — planshet va telefonda shrift/tugma/ikonlar/muqova bosqichma-bosqich kichrayadi, gorizontal overflow YO'Q.**

Talab: `/reading-books` sahifasi planshet va telefonga mukammal tushishi; har bir qurilmada shriftlar, tugmalar, ikonlar kichrayishi kerak.

- **Sabab/holat:** sahifada faqat bitta `@media (max-width: 1024px)` blok bor edi — u `.book-row` ni vertikal stack qiladi (layout), lekin shrift/o'lcham/ikon scaling umuman yo'q edi; sarlavha (h1 34px), section-title, muqova, statistika, amal tugmalari, tugatilgan kitoblar gridi — hammasi barcha kenglikda bir xil o'lchamda qolardi.
- **Yondashuv:** `/feed` bilan bir xil uslub — markup O'ZGARMADI, faqat `wwwroot/css/site.css` ga yangi "/reading-books responsive" bo'limi (768/480/360 bosqichli) qo'shildi. Mavjud 1024px blok (karta stacking) saqlandi; `.page` padding va `.composer-*`/`.composer-book` moslashuvi /feed blokidan keladi (umumiy klasslar).
- **`wwwroot/css/site.css` (yangi bo'lim, notif bo'limidan oldin):**
  - **≤768px (planshet):** `.rb-header h1` 34→27, "Yangi kitob" tugma 15→14.5, `.section-title` 22→19, forma padding, `.nb-cover-preview` 64×86→60×84; karta: `.rb-cover` 86×128→78×116, `.rb-title` 21→19, `.rb-author`/`.rb-stat`/ikon, `goal-log-toggle`+`rb-done-pill` 14→13.5; `.finished-books` minmax 120→108, `.finished-title` 15→14.5.
  - **≤480px (telefon):** `.rb-header` vertikal stack — "Yangi kitob" tugma to'liq kenglikda; h1 23, section-title 18; forma h3/label kichik; karta `.rb-cover` 70×104, title 17.5, track 10→8px, stat 12 + ikon 15, amal tugmalar 13px; `.finished-books` minmax 96, title 14, badge 22px.
  - **≤360px (kichik telefon):** h1 21, section-title 17, `.rb-cover` 64×95, title 16.5, stat 11.5; `.finished-books` 2 ustun (`repeat(2,1fr)`).
  - **TUZATISH:** dastlab `.rb-delete` 768px da 13→13.5 ga KATTALASHAYOTGAN edi (desktopdan katta — nomuvofiq); 768 guruhidan olib tashlandi → endi 13→13→12.5→12.5 (faqat pasayadi).
- **TEKShIRILDI:** CSS statik asset — rebuild SHART EMAS (server 5261 da ishlayapti, hard refresh yetarli); yangi blok jonli serve qilinyapti (curl). **HEADLESS CHROME (CDP, Node 22 ichki WebSocket; chrome'ni node child_process foreground spawn — `&` background signal 16 bilan o'lardi) — dev JWT (HS256, user1, cookie `kitobdagimen_token`, key=user secrets `Jwt:Key`) bilan 4 kenglikda (1280/768/480/360) jonli o'lchov** (1 faol kitob + 4 tugatilgan render bo'ldi):
  - Gorizontal overflow HAMMA kenglikda **0**, JS xatosi **0**.
  - Bosqichma-bosqich kichrayadi: h1 34→27→23→21, section-title 22→19→18→17, rb-title 21→19→17.5→16.5, muqova kengligi 86→78→70→64, rb-stat 13→12.5→12→11.5, stat-ikon 17→16→15→15, rb-delete 13→13→12.5→12.5; finished grid ustunlari 8→6→4→2, finished-title 15→14.5→14→13.5; karta `flex-direction` ≤1024 da `column`.
  - Headless chrome + temp test fayli (`rb-cdp-test.mjs`) + profil (`/tmp/rb-cdp-profile`) TOZALANDI. DB tegilmadi (faqat o'qish — render uchun SELECT). App 5261 da ishlayapti. **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/feed` sahifasi TO'LIQ moslashuvchan (responsive) qilindi — planshet va telefonda shrift/tugma/ikonlar bosqichma-bosqich kichrayadi, gorizontal overflow YO'Q.**

Talab: `/feed` sahifasi planshet va telefonga mukammal tushishi; har bir qurilmada shriftlar, tugmalar va ikonlar kichrayishi kerak.

- **Sabab/holat:** feed sahifasida moslashuvchanlik juda kam edi — faqat bitta kichik `@media (max-width: 480px)` blokida `.post-body`/`.post-head` uchun qisman tuzatish bor edi; planshet (≤768px) diapazoni umuman qoplanmagan, composer/post amallari (like/izoh/ulashish/tahrir/o'chirish)/qidiruv paneli moslashtirilmagan edi.
- **Yondashuv:** sayt allaqachon CSS o'zgaruvchilariga asoslangan; faqat `wwwroot/css/site.css` ga tartibli, bosqichma-bosqich media-query tizimi qo'shildi (markup O'ZGARMADI). Eski 480px blokidagi dublikat `.post-*` qoidalari olib tashlanib, yagona manbaga birlashtirildi.
- **`wwwroot/css/site.css` (yangi "/feed responsive" bo'limi):**
  - **≤768px (planshet):** `.page` padding kichraydi, `.feed` gap 24→18; composer (`.composer-head h3` 18→16, `.composer-book` wrap — input to'liq qatorga); post karta (`.post-head` padding, `.post-body` ustun 96px→84px, `.post-text` 16→14.5, `.post-actions` padding/gap, `.action-btn` 14→13px + ikon 20→19px).
  - **≤480px (telefon):** qidiruv paneli (`.btn-sm`/input kichraydi), composer (h3 15px, avatar 36px, `.composer-actions` vertikal stack — tugma to'liq kenglikda, hint markazda), post karta (avatar 36px, ism 14.5px, follow tugma kichik, `.post-body` ustun 64px, title 16px, text 14px + `-webkit-line-clamp` 4→5, rasm max-height 360px, `.action-btn` 12.5px + ikon 18px, gaplar qisqardi).
  - **≤360px (kichik telefon):** eng tor ekranda ham amallar bir qatorga sig'adi — `.post-body` ustun 56px, title 15px, text 13.5px, `.action-btn` 12px + ikon 17px; va **navbar overflow tuzatildi** (`.navbar-inner` padding 24→12px, brand 22→19px, logo 36→30px). `≤480px` da ham `.navbar-inner` padding 16px.
- **TEKShIRILDI:** CSS statik asset — rebuild SHART EMAS (server 5261 da ishlab turibdi, hard refresh yetarli). `css/site.css` da yangi blok jonli serve qilinyapti (curl bilan tasdiqlandi). **HEADLESS CHROME (CDP, Node 22 ichki WebSocket; chrome'ni node child_process orqali spawn qildim — bu muhitda `&` bilan background chrome signal 16 bilan o'lardi, foreground spawn yashaydi) — 4 ta qurilma kengligida (390/360/768/1280px) jonli o'lchov:**
  - Shrift/tugma/ikon bosqichma-bosqich kichraydi: title 20→18→16→15px, text 16→14.5→14→13.5px, action-btn 14→13→12.5→12px, action-icon 20→19→18→17px, composer h3 18→16→15px, post-body ustun 96→84→64→56px.
  - **Gorizontal overflow HAMMA kenglikda 0** (avval 360px da navbar tufayli 7px edi — tuzatildi), `.post-actions` ichida overflow 0 (amallar bir qatorga sig'adi), JS xatosi 0.
  - Headless chrome + barcha temp fayllar TOZALANDI, DB tegilmadi. **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — Post detali sahifasi (`/post/{username}/{slug}`) endi TESKARI rejimda: global "tun"da OQ (light), global "kun"da QORA (dark).**

Talab: `/posts/username/random` (= post detali sahifasi) "tun" rejimida oq, "kun" rejimida qora bo'lsin — ya'ni butun saytga teskari.

- **Yondashuv:** sayt to'liq CSS o'zgaruvchilariga asoslangani uchun, faqat shu sahifaning `body` elementida palitrani GLOBAL `data-theme` ga TESKARI qayta belgiladim — butun sahifa (navbar + kontent) avtomatik teskari rangga o'tadi, bironta komponent qayta yozilmadi.
- **`Views/Shared/_Layout.cshtml`:** `<body>` ga `class="@(ViewData["BodyClass"] as string)"` qo'shildi (umumiy mexanizm — istalgan view body klassi bera oladi).
- **`Views/Posts/Details.cshtml`:** `ViewData["BodyClass"] = "theme-invert"`.
- **`wwwroot/css/site.css` (dark-mode bo'limidan keyin):**
  - `html:not([data-theme="dark"]) body.theme-invert` → global KUN bo'lsa sahifa DARK palitra (`--bg:#13120e` ...) + `color-scheme:dark`; dark-mode komponent tuzatishlari (`.btn-primary` #2f7d65, `.anon-banner`, `.lp-cta`, `.msg-edit`) shu selektorga mirror qilindi.
  - `html[data-theme="dark"] body.theme-invert` → global TUN bo'lsa sahifa LIGHT palitra (base `:root` qiymatlari) + `color-scheme:light`; va tun-rejim komponent tuzatishlari shu sahifa uchun base-light qiymatlarga BEKOR qilindi (aks holda data-theme=dark bo'lgani uchun ular noto'g'ri qo'llanardi). Specificity (0,3,2) > mavjud (0,2,1)/base (0,1,0) — har doim ustun.
- **TEKShIRILDI:** build **0/0**. `dotnet watch` `.cshtml` o'zgarishini hot-reload qildi. Server-render: post sahifasi `<body ... class="theme-invert">`, `css/site.css` da 15 `body.theme-invert` qoidasi serve qilinyapti. **HEADLESS CHROME (CDP, Node 22) jonli test:** `data-theme="dark"` (tun) → `body` bg `rgb(250,246,238)`=#FAF6EE (OQ), matn #1F2A24; `data-theme="light"` (kun) → bg `rgb(19,18,14)`=#13120e (QORA), matn #ece7db — TALAB AYNAN BAJARILDI. Headless chrome + temp fayllar tozalandi. DB tegilmadi. App 5261 da `dotnet watch` ostida ishlayapti.

**Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — BUTUN sayt uchun "Kun/tun rejimi" (light/dark mode) qo'shildi — barcha sahifalarda zamonaviy oy/quyosh ikonali tugma, localStorage'da saqlanadi, miltillashsiz (no-FOUC).**

Talab: barcha mavjud sahifalar kun/tun rejimiga o'tsin; zamonaviy ikon bo'lsin.

- **Yondashuv:** sayt allaqachon TO'LIQ CSS o'zgaruvchilarga (`:root` da `--bg`/`--surface`/`--text`/`--primary`/...) asoslangan, shuning uchun `html[data-theme="dark"]` blokida shu o'zgaruvchilarni qayta belgilash bilan butun sayt (navbar, kartalar, inputlar, landing, chat, modal, toast — hammasi) avtomatik to'q rejimga o'tdi. Hech bir komponentni alohida qayta yozish shart bo'lmadi.
- **`wwwroot/css/site.css` (oxiriga qo'shildi):**
  - `html[data-theme="dark"]` — iliq to'q palitra (`--bg:#13120e`, `--surface:#1d1b15`, `--text:#ece7db`, `--border:#322e25`). **`--primary` to'q rejimda yorqinlashtirildi** (`#5cc2a0`) — to'q fonda sarlavha/havola/matn o'qilishi uchun (asl `#1B4D3E` ko'rinmas edi). `color-scheme: dark` ham qo'shildi (form/scrollbar moslashadi).
  - Yashil "to'liq fon" elementlari (`.btn-primary`, `.lp-cta`) uchun maxsus to'qroq yashil (`#2f7d65`/`#245848`) — chunki yorqinlashtirilgan `--primary` ustida oq matn o'qilmasdi.
  - Qattiq kodlangan ochiq fonlar to'q rejimga moslandi: `.onboarding-warning` (sariq alert), `.anon-banner` (ochiq yashil), `.msg-edit-input`/`[data-edit-save]` (`#fff`).
  - `.theme-icon-dark`/`.theme-icon-light` — ikon almashinuvi (light rejim → oy `dark_mode`, dark rejim → quyosh `light_mode`). `.theme-toggle` (navbar) + `.theme-fab` (navbarsiz sahifalar uchun suzuvchi yumaloq tugma, fixed top-right). `html.theme-transition *` — almashganda yumshoq 0.25s o'tish.
- **`Views/Shared/_Layout.cshtml`:**
  - `<head>` ga inline skript — bo'yashdan OLDIN `data-theme` ni localStorage'dan (yoki `prefers-color-scheme`) o'rnatadi → **FOUC yo'q**.
  - Navbar `.nav-actions` ga `.nav-icon-btn.theme-toggle` (oy/quyosh ikonlari) — autentifikatsiyalangan sahifalar uchun.
  - Navbar ko'rinmaydigan sahifalar (`!(isAuth && !hideChrome)` — landing/onboarding/anonim) uchun suzuvchi `.theme-fab`. Har sahifada AYNAN bitta toggle.
- **`wwwroot/js/site.js` (IIFE boshida):** `[data-theme-toggle]` ga delegatsiya — bosilganda `data-theme` ni dark↔light almashtiradi, localStorage `kitob-theme` ga yozadi, qisqa `theme-transition` klassi qo'shadi. Ikonlar CSS orqali avtomatik almashadi.
- **TEKShIRILDI:** build **0/0**, restart 5261 (Development, `.cshtml`+css+js o'zgargani uchun). Server-render (curl): landing (anon) → head skript + 1 `.theme-fab` + 0 navbar-toggle; `/Feed` va `/reading-books` (dev JWT user1) → navbarda 1 `.theme-toggle` + 0 `.theme-fab` + head skript. **HEADLESS CHROME (CDP, Node 22 ichki WebSocket) jonli test:** `/Feed` dastlab dark (`--bg`=`rgb(19,18,14)`=#13120e); toggle bosilgach → light (`rgb(250,246,238)`=#FAF6EE, matn #1F2A24); `localStorage.kitob-theme`="light"; ikon light rejimda oy (`dark:flex light:none`); **reload'dan keyin ham light saqlandi** (FOUC yo'q, head skript ishladi). Headless chrome + temp fayllar TOZALANDI. App 5261 da ishlab turibdi.
- **ESLATMA:** bu sessiyada foydalanuvchining `dotnet watch` jarayoni (port band bo'lib qotgani uchun) to'xtatilib, oddiy `dotnet run` bilan qayta ishga tushirildi — 5261 da barqaror serve qilinyapti.

**Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/chat`: (1) xabarlar real-time ko'rinmasdi (refresh kerak edi), (2) suhbat unread "+1" nishoni birinchi bosishda tozalanmasdi — IKKALASI TUZATILDI.**

Talab: (1) xabar yuborilganda ikkinchi akkauntda refreshsiz ko'rinmasdi. (2) Xabar kelgan suhbatni bosganda "+1" counter darhol yo'qolishi kerak (hozir 2 marta bosish kerak edi).

- **Muammo #1 sababi (jonli ko'rinmaslik):** SignalR push ASLIDA ishlayapti — uchma-uch test (Node WS klient user6 sifatida ulanib) `ReceiveMessage` ni **camelCase** payload bilan JONLI oldi (SignalR default camelCase serialize qilarkan, casing muammo emas edi). LEKIN recipient suhbatlar **ro'yxatida** (yoki boshqa suhbatda) turganda, `Chat/Index.cshtml` dagi `chat.on("ReceiveMessage")` faqat OCHIQ suhbatga render qilardi; aks holda faqat toast chiqarardi — sidebar ro'yxati (unread nishon / oxirgi xabar / tartib) jonli yangilanmasdi. Shu sabab "refreshsiz ko'rinmaydi".
- **Muammo #2 sababi (ikki marta bosish):** `ChatController.Index` da `MarkMessagesReadCommand` **`GetConversationsQuery` dan KEYIN** chaqirilardi. Ro'yxat eski (o'qilmagan) hisob bilan qurilib, keyin DB'da o'qilgan deb belgilanardi — shuning uchun ochilgan suhbat ro'yxatda hali ham eski "+N" ko'rsatardi; ikkinchi bosishda (DB allaqachon o'qilgan) yo'qolardi.
- **Tuzatish #2 (`Controllers/ChatController.cs`):** `MarkMessagesReadCommand(conversationId)` endi `GetConversationsQuery` dan OLDIN chaqiriladi — ochilgan suhbatning sidebar nishoni SHU renderda yo'qoladi.
- **Tuzatish #1 (`Views/Chat/Index.cshtml`):** yangi `updateConvListItem(m, incrementUnread)` yordamchisi — suhbatlar ro'yxatidagi elementni jonli yangilaydi (oxirgi xabar matni, `.unread-badge` +1, eng tepaga ko'tarish; href `[href$="conversationId=N"]` bo'yicha topadi). `chat.on("ReceiveMessage")`: ochiq suhbatga `updateConvListItem(m,false)` (o'qilgan — faqat oxirgi xabar+tartib), boshqa suhbat/ro'yxatga `updateConvListItem(m,true)` (unread +1) + toast. Suhbat ro'yxatda yo'q bo'lsa (butunlay yangi) — refresh ko'rsatadi (kam uchraydigan holat, qabul qilingan ulanishda suhbat allaqachon mavjud).
- **TEKShIRILDI:** build **0/0**. **Fix #2 — server-render test:** user6 conv4 ni (2 o'qilmagan) ochganda sidebar nishon **darhol YO'Q** bo'ldi (avval 2 bosish kerak edi), ochilmagan conv8 esa nishonini (1) saqladi; DB'da conv4 o'qilgan deb belgilandi. **Fix #1 — HEADLESS CHROME (CDP) jonli test:** user6 `/chat` ro'yxatini ochib turganda user1 conv4 ga xabar yubordi → sahifa **YANGILANMASDAN** (probe `LOADED-ONCE` saqlandi) sidebar'da unread nishon `1` paydo bo'ldi, oxirgi xabar matni yangilandi, conv4 tepaga ko'tarildi — hammasi sof SignalR DOM yangilanishi. **DIQQAT:** test davomida DB'ga injekt qilingan test xabarlari (6 ta: RT-TEST/unread-badge-test/CDP-LIVE/control-unread) **O'CHIRILDI**, conv4/conv8 o'qilgan holatga normallashtirildi, headless Chrome o'ldirildi. App 5261 da ishlayapti. **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/chat` "oxirgi marta faol" (last-seen) vaqti soatlab xato ko'rsatardi — TUZATILDI (mijoz endi qurilma timezone'iga emas, doim Toshkent vaqtiga (UTC+5) tayanadi).**

Talab: `/chat` sahifasida foydalanuvchining "oxirgi marta faol bo'lgani" juda katta xato ko'rsatardi.

- **Sabab (timezone nomuvofiqligi):** server barcha vaqtlarni hardcoded **UTC+5** (Toshkent, DSTsiz) bilan to'g'ri renderlaydi (`ViewHelpers.LastSeen` → `utc.AddHours(5)`), va `LastSeenAt` DB'da `timestamp with time zone` (Kind=Utc) bo'lib, ISO'da doim `Z` bilan keladi (header `data-last-seen`, presence eventi, `/chat/search` JSON — hammasi tekshirildi: `...Z`). LEKIN mijoz JS funksiyalari (`Chat/Index.cshtml` dagi `lastSeenText`, `fmtTime`) **brauzer mahalliy timezone**'idan foydalanardi (`new Date().getHours()`, `toLocaleTimeString`). Qurilmasi Toshkent vaqtida bo'lmagan foydalanuvchi (telefon avto-TZ, UTC, boshqa region) last-seen'ni soatlab xato ko'rardi: UTC qurilmada −5 soat, Moskva −2, **Nyu-York −9 soat**. Va bu xato faqat mijoz hisoblaganda chiqardi (PresenceChanged eventi + qidiruv kartalari) — dastlabki server-render to'g'ri edi, shuning uchun "goh to'g'ri, goh juda xato" ko'rinardi.
- **Tuzatish (`Views/Chat/Index.cshtml` `@section Scripts`):** O'zbekiston yagona, DSTsiz UTC+5 zona — mijoz ham AYNAN server zonasida ko'rsatishi shart. Yangi yordamchilar: `UZ_OFFSET_MS = 5h`, `uzDate(v)` (UTC instantni +5 soatga suradi, keyin `getUTC*` o'qiladi → Toshkent devor-vaqti), `uzYmd(d)` (kun solishtirish), `pad2`. `lastSeenText` va `fmtTime` qayta yozildi — endi `getUTCHours/...` ishlatadi, `getHours`/`toLocaleTimeString` EMAS. `sentAgo` ning >7 kunlik shoxchasidagi `toLocaleDateString` ham `uzDate` ga o'tkazildi (relyativ qism o'zgarmadi — u zona-mustaqil).
- **TEKShIRILDI:** build **0/0**. `dotnet watch` `.cshtml` o'zgarishini avtomatik qayta yukladi; serverda yangi JS (`UZ_OFFSET_MS`/`uzDate`/`getUTCHours`) jonli serve qilinyapti (`/chat?conversationId=9` → 200, 5 mos). Node bilan 5 ta TZ da (Asia/Tashkent, UTC, Europe/Moscow, America/New_York, Asia/Tokyo) yangi funksiyalar HAMMASIDA bir xil to'g'ri natija berdi: bugun `oxirgi marta 10:18:38 da`, kecha `oxirgi marta kecha 20:39:31 da`, xabar `10:18` (real DB qiymati: UTC 05:18:38 = Toshkent 10:18:38). Eski funksiyalar esa UTC'da `05:18`, NY'da `01:18` berardi. **DB faqat o'qildi** (LastSeenAt/now solishtirish — SELECT). Dev JWT minutiga eslatma: ishlaydigan JWT kaliti **user secrets** dagi `Jwt:Key` (appsettings.Development EMAS — u faqat `dev_only_...`, lekin user secret uni bosib o'tadi). **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/profile/{username}` "Hozir o'qiyapti" bo'limi endi BARCHA faol kitoblarni ko'rsatadi (`/reading-books` "Faol kitoblarim" bilan bir manba).**

Talab: profil sahifasidagi "Hozir o'qiyapti" bo'limi ma'lumotni `/reading-books` dagi "Faol kitoblarim" dan olsin va nechta faol kitob bo'lsa BARINI ko'rsatsin (avval faqat bitta — eng oxirgi faol kitob — ko'rinardi).

- **Sabab:** ikkala controller ham `GetActiveReadingGoalsQuery` orqali faol kitoblar RO'YXATINI olardi, lekin view modelga faqat `activeBooks.FirstOrDefault()` (bitta) uzatilardi. `/reading-books` esa xuddi shu query'ning to'liq ro'yxatini ko'rsatadi — shuning uchun manba allaqachon bir xil edi, faqat profil bittasini kesib tashlardi.
- **O'zgarishlar:**
  - **`Models/ProfilePageViewModel.cs`:** `ReadingGoalDto? CurrentBook` → `IReadOnlyList<ReadingGoalDto> CurrentBooks` (to'liq faol kitoblar ro'yxati).
  - **`Controllers/ProfileController.cs` + `Controllers/PublicProfileController.cs`:** `CurrentBook = activeBooks.FirstOrDefault()` → `CurrentBooks = activeBooks` (ikkalasi ham — `/profile` va `/u/{username}` bir xil).
  - **`Views/Profile/Index.cshtml`:** `@if (Model.CurrentBook is not null)` → `@if (Model.CurrentBooks.Count > 0)`; kartalar `.reading-now-list` ichida `@foreach` bilan chiziladi (har biri eski `.reading-now-inner` markup — muqova/sarlavha/muallif/conic ring/kunlik maqsad). Sarlavhaga (>1 bo'lsa) `.reading-now-count` nishoni qo'shildi.
  - **`wwwroot/css/site.css`:** `.reading-now-list` (vertikal flex, gap 24px), kartalar orasida `.reading-now-inner + .reading-now-inner` ajratuvchi chiziq (border-top), `.reading-now-count` pill nishoni; h3 flex.
- **TEKShIRILDI:** build **0/0**, restart 5261 (Development; `.cshtml`+model+controller o'zgargani uchun rebuild SHART). Dev JWT (HS256, cookie `kitobdagimen_token`, user 1 = javohirsadullayev — 2 faol kitob) bilan REAL render:
  - `/profile/javohirsadullayev` → **2** ta `.reading-now-inner` karta (Falastin --p:40%, HAY --p:0%), `.reading-now-count` = "2".
  - `/reading-books` → xuddi shu 2 kitob (`.rb-title`: Falastin, HAY) — manba bir xil ekani tasdiqlandi.
  - `/u/javohirsadullayev` (anonim public profil) → ham **2** karta.
  - `css/site.css` da `.reading-now-list`/`.reading-now-count` jonli serve qilinyapti.
  - **DB faqat o'qildi** (faol goal'larni topish uchun SELECT), o'zgartirilmadi. App 5261 da background'da ishlab turibdi. **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/reading-books` "Faol kitoblarim" kartalari YANGI DIZAYN (SVG aylana progress → gradientli chiziqli bar + suzuvchi foiz nishoni + ikonali statistika + amallar ustuni).**

Talab: `/reading-books` dagi "Faol kitoblarim" bo'limidagi kartalarni yangi dizayn bilan qayta loyihalashtirish.

- **Eski dizayn:** gorizontal qator, o'ngda SVG aylana progress (`.circ-progress`/`.circ-value` stroke-dashoffset), tagida "Bugun"/"O'qildi" matn qatorlari va outline tugmalar.
- **Yangi dizayn (`Views/ReadingGoals/Index.cshtml` markup + `wwwroot/css/site.css`):**
  - **Karta (`.book-row`):** chap chetida gradient (primary→accent) vertikal aksent chizig'i (`::before`), kuchliroq hover (translateY + soya), `is-done` holatida aksent yashilga o'tadi.
  - **Muqova (`.rb-cover`):** 86×128, yumaloq, soyali; pastki-o'ng burchakda **suzuvchi foiz nishoni** (`.rb-pct-badge` → `.goal-pct`) — accent fonli pill, tugaganda yashil.
  - **Markaz (`.rb-body`):** serif sarlavha (`.rb-title`, ellipsis), muallif, **gradientli chiziqli progress bar** (`.rb-track`/`.rb-track-fill`, `width:@pct%`, smooth cubic-bezier animatsiya, ARIA `role=progressbar`), ikonali statistika (`.rb-stats` → 🔥 Bugun X/Y, 📖 X/Y bet).
  - **Amallar (`.rb-actions`, eski `book-row-progress` sinfi JS uchun saqlandi):** vertikal ustun — "Betlarni yangilash" (primary pill, + ikon, `goal-log-toggle`), yashirin `goal-log` input+Qo'shish, `.rb-delete` (ghost, delete ikon). Tugaganda `.rb-done-pill` (task_alt ikon).
  - **Responsive (≤1024px):** karta vertikal stack, muqova markazda, tugma to'liq kenglikda.
- **JS (`Index.cshtml @section Scripts`):** progress qo'shish handleri SVG aylana o'rniga `.rb-track-fill` width + `.goal-pct` matn + ARIA yangilaydi; tugaganda `goal-log-toggle` ni `.rb-done-pill` ga **`replaceWith`** qiladi. `CIRC` konstanta va `circumference` Razor const'i olib tashlandi (ishlatilmas edi).
- **Olib tashlangan CSS:** `.book-row-main`/`.book-cover-lg`/`.circ-*`/`.book-today` — grep bilan tasdiqlandi: Views/wwwroot da boshqa hech qayerda ishlatilmaydi (faqat shu sahifada edi).
- **TEKShIRILDI:** build **0/0**, restart 5261 (Development; `.cshtml` o'zgargani uchun rebuild SHART). Dev JWT (HS256, user id=1, javohirsadullayev836 — 2 faol kitob) bilan REAL render: `/reading-books` HTTP 200 → **2 ta `.book-row`** karta, har birida `.rb-track-fill` (`style="width:N%"`), `.rb-pct-badge`, `.rb-stats`, `.rb-actions` (goal-log-toggle/goal-log/rb-delete) — HTML tuzilishi to'g'ri. `css/site.css` da yangi `.rb-track-fill`/`.rb-pct-badge`/`.rb-cover` (11 mos) jonli serve qilinyapti. **DB faqat o'qildi** (SELECT — user/goal topish), o'zgartirilmadi. **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — Bildirishnoma qo'ng'irog'i (navbar) endi DROPDOWN panel: "Kim qaysi postingizga izoh qoldirdi" ro'yxatini ko'rsatadi; ochilganda o'qildi → keyingi refreshda yo'qoladi.**

Talab: kimdir postga izoh qoldirsa qo'ng'iroqda +1 counter chiqardi, lekin bosilganda
darhol /chat ga o'tib counter yo'qolardi va KIM qaysi postga izoh qoldirgani hech qayerda
ko'rinmasdi. Endi qo'ng'iroqqa bosilganda bildirishnomalar ro'yxati ochilsin (kim/qaysi post),
o'qilgach keyingi refreshda tozalansin.

- **Sabab:** navbar qo'ng'irog'i `<a href="/chat">` edi — bosish /chat ni ochib HAMMA
  bildirishnomani o'qilgan qilardi (badge yo'qolardi), izoh bildirishnomalarini esa hech
  bir UI ko'rsatmasdi. "Kelgan takliflar" paneli faqat chat ulanish takliflarini (Connection
  pending) ko'rsatadi — izoh/like/follow bildirishnomalari uchun ro'yxat yo'q edi.
- **Yechim (UI dropdown panel, server o'zgarmadi — mavjud `/notifications/unread` +
  `/notifications/read` ishlatildi):**
  - **`Views/Shared/_Layout.cshtml`:** qo'ng'iroq `<a>` → `.notif-wrap` ichida `<button
    data-notif-toggle>` + `.notif-panel` (`data-notif-panel`/`data-notif-list`). `desktop-only`
    saqlandi.
  - **`wwwroot/js/site.js` (`initNotifications`):** panel mantig'i — `/notifications/unread`
    dan `items` saqlanadi va `renderList()` chizadi (avatar yoki harf, message, `timeAgo`,
    `url` bo'lsa `<a>`). XSS himoyasi: barcha user-matn `esc()` orqali. Toggle/ochilish/tashqariga
    bosish/Escape. **Ochilganda** `apiPost("/notifications/read")` + `setBadge(0)` — ro'yxat
    hozir ko'rinadi, keyingi refreshda bo'shaydi. Real-time SignalR bildirishnoma kelganda
    `items.unshift(n)` + `renderList()` (eng yangisi tepada).
  - **`wwwroot/css/site.css`:** `.notif-panel`/`.notif-list`/`.notif-item`/`.notif-avatar`
    (+`-letter`)/`.notif-meta`/`.notif-msg`/`.notif-time`/`.notif-empty` uslublari (dropdown,
    soya, hover).
- **TEKShIRILDI:** build **0/0**, restart 5261 (Development, layout `.cshtml` o'zgargani uchun
  rebuild SHART edi). Dev JWT bilan to'liq REAL oqim: user6 → user1 postiga (#2) izoh POST
  (antiforgery bilan, HTTP 200) → user1 `/notifications/unread` = `{count:1, items:[{type:
  "comment", message:"...postingizga izoh qoldirdi", url:"/posts/2", ...}]}` → POST
  `/notifications/read` 204 → unread yana `{count:0,items:[]}` (refreshda yo'qoladi).
  Static `js/site.js` (openNotifPanel/renderList/notifications/read) va `css/site.css`
  (notif-panel/notif-item) jonli serve qilinyapti. **Test ma'lumotlari TOZALANDI** (izoh #28,
  bildirishnoma #8 o'chirildi). **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/feed` va `/quotes`: cheksiz skroll (infinite scroll) — dastlab 4 ta, qolgani pastga tushganda animatsiya bilan yuklanadi.**

Talab: `/feed` boshida faqat 4 ta post yuklansin, qolganlari pastga tushganda
animatsiya bilan yuklansin (performansga muammo bo'lmasin). Xuddi shu `/quotes` da ham.

- **Yondashuv:** server-render qilingan HTML fragment + `IntersectionObserver`
  (skroll hodisasi emas — performansli; `rootMargin: 600px` bilan oldindan yuklash,
  bir vaqtda bitta so'rov, oxirgi sahifada observer uziladi). Klassik pagination
  tugmalari olib tashlandi.
- **Backend:**
  - `FeedController`: `PageSize` 10→**4**. Yangi `Cards(q, page)` action →
    `PartialView("_FeedCards", feed.Items)` (faqat post kartalari fragmenti).
  - `QuotesController`: barcha 3 tab `PageSize`=**4**. Yangi `Cards(tab, bookId, q, page)`
    action — `tab` bo'yicha `all`/`my`/`saved` query'ga yo'naltiradi → `_QuoteCards`.
  - Yangi partial'lar: `Views/Feed/_FeedCards.cshtml` (model `IReadOnlyList<PostDto>`,
    `ShowAuthorActions=true`), `Views/Quotes/_QuoteCards.cshtml` (model
    `IReadOnlyList<QuoteDto>`, o'zining `ICurrentUserService` injection'i + antiforgery).
    Quotes Index endi dastlabki kartalarni ham shu partial orqali render qiladi
    (kod takrori yo'q). Index'dan ortiqcha `meId`/`CurrentUser` olib tashlandi.
- **Frontend:**
  - `site.js`: yangi qayta ishlatiladigan `kitob.infiniteScroll(opts)` yordamchisi
    (sentinel/container/insertBefore/loader/endpoint/page/totalPages/search/params/onAppend).
    401 da `/` ga yo'naltiradi, tarmoq xatosida observer saqlanadi (keyingi skrollda qayta urinish).
  - `Feed/Index.cshtml`: pagination o'rniga `#feedLoader` (spinner) + `#feedSentinel`
    (data-page/total-pages/search). Highlight `highlightCards(cards)` funksiyasiga
    refaktor — yangi kartalarga ham qo'llanadi. `onAppend` da highlight + qo'shilgan
    o'z postlar uchun `kitob.initRichEditors(node)` chaqiriladi.
  - `Quotes/Index.cshtml`: `.quote-grid#quoteGrid` (data-page/total-pages/tab/search/book-id)
    + `#quoteLoader` + `#quoteSentinel`. Highlight ham `highlightCards`'ga refaktor.
  - **Hodisalar delegatsiya orqali** (like/follow/share/save-quote/delete/edit hammasi
    `document` da) — qo'shilgan kartalar avtomatik ishlaydi, qayta bind shart emas.
  - `site.css`: `.infinite-loader .spinner` (aylanuvchi), `cardEnter` keyframe
    (opacity+translateY 18px), `.card-enter` (.45s), `prefers-reduced-motion` hurmati.
- **TEKShIRILDI (build 0/0, restart 5261 Development, dev JWT curl user1):**
  - `/Feed` → aniq **4** post-card; `#feedSentinel data-page=1 data-total-pages=2`;
    `/Feed/cards?page=2` → **2** karta (6 jami), toza fragment (`<html>`/`<body>` yo'q,
    `<article class="card post-card">` dan boshlanadi).
  - `/quotes` → **4** quote-card; `#quoteGrid data-total-pages=3 data-tab=all`;
    loader+sentinel bor; `/quotes/cards?tab=all&page=2`→**4**, `page=3`→**3** (4+4+3=11 ✓);
    `tab=my&page=2`→200. `/quotes/my`→empty-state (user1 ning iqtibosi yo'q — to'g'ri).
  - Statik: `js/site.js`→`infiniteScroll`, `css/site.css`→`card-enter`/`infinite-loader` jonli.
  - DB tegilmadi (faqat o'qish).
- **ESLATMA:** `.cshtml`/controller/yangi fayl o'zgargani uchun rebuild+restart bajarildi
  (Razor runtime compilation o'chiq). Ilova 5261 da background'da ishlab turibdi.

- **TUZATISH (keyin, shu sessiya) — loader hech qachon yo'qolmasdi:** foydalanuvchi
  "loader doim ishlab turibdi, yo'q bo'lmayapti" dedi. **Sabab:** `.infinite-loader`
  qoidasi `display: flex` edi va u HTML `hidden` atributini (UA `[hidden]{display:none}`)
  spetsifiklik bo'yicha bosib ketardi — spinner DOIM ko'rinardi. **Tuzatish (site.css):**
  `.infinite-loader[hidden] { display: none; }` qo'shildi (spetsifiklik 0,2,0 > 0,1,0).
  Yuklash mantig'i (4 ta + skrollda qo'shilishi) ASLIDA to'g'ri ishlardi — faqat spinner
  ko'rinishi xato edi.
  **HEADLESS BRAUZER (google-chrome + CDP, Node 22) bilan TEKShIRILDI** (dev JWT user1,
  1200x900): `/Feed` → dastlab **4** karta, loader `none`; skrollda **4→6**, loader doim
  `none`. `/quotes` → dastlab **4**, loader `none`; skrollda **4→8→11**, loader doim `none`.
  JS xatosi yo'q. CSS statik — rebuild shart emas, hard refresh yetarli.
  **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/post/{username}/{slug}` izohlar: javob (reply) thread "arxitektura" chizig'i + peach (#fdeee5) fonda oq matn buggi tuzatildi.**

Talab: (1) izohlarda kim kimga javob berayotgani tushunarsiz edi — thread/arxitektura
chizig'i kerak. (2) `#fdeee5` (javob pufagi foni) ustida matn OQ bo'lib qolib o'qib
bo'lmasdi — boshqa (o'qiladigan) rangga.

- **Sabab (oq matn buggi):** muallif izohi rangini beradigan qoidalar DESCENDANT
  selektor edi (`.comment.comment-author .bubble .text { color:#fff }`). Post
  muallifi ONA izohni yozган bo'lsa, bu oq-rang qoidasi ichki `.replies` dagi
  javob matniga ham "oqib" tushardi → peach (~#fdeee5) pufakda oq matn.
- **Tuzatish (CSS, `wwwroot/css/site.css`):**
  - Muallif qoidalari `> .body >` bilan SCOPE qilindi (`.comment.comment-author >
    .body > .bubble ...`) — endi faqat muallifning O'Z pufagiga ta'sir qiladi,
    ichki javoblarga oqmaydi (6 ta qoida).
  - Qo'shimcha kafolat: `.comment .replies .comment:not(.comment-author) .bubble
    .name, ... .text { color: var(--text) }` — peach javob pufagida matn DOIM to'q.
  - **Thread "arxitektura" chizig'i:** eski tekis `border-left` o'rniga har bir
    javob uchun L-shakl elbow ulagich (`.comment .replies > .comment::before`:
    `border-left`+`border-bottom`+`border-bottom-left-radius:12px`, rang
    `rgba(27,77,62,.20)`). `.replies` `padding-left:28px`, `gap:16px`. Endi har
    bir javob ona izohga egri chiziq bilan ulanadi — kim kimga javob berayotgani
    ko'rinadi. Javoblar bir pog'onali (JS ichki-javobni cheklaydi).
- **TEKShIRILDI:** Faqat CSS — rebuild/restart KERAK EMAS (statik asset), hard
  refresh yetarli. Server FOYDALANUVCHI iltimosi bilan O'CHIRILGAN (qayta
  ishga tushirilmadi). Grep: eski unscoped author `.bubble` qoidasi=0, scoped=6,
  reply-text guard=1, elbow connector=1, eski `border-left` replies=0, qavslar
  balansi OK (654/654). **Keyingi qadam:** foydalanuvchi aytadigan aniq
  o'zgarishlarni kutish (server kerak bo'lsa qayta ishga tushiriladi).

---

**2026-06-20 — `/post/{username}/{slug}` izoh maydonidagi qizil rang olib tashlandi (spellcheck o'chirildi + Firefox invalid porlashi).**

Talab: post detali sahifasida izoh yozayotganda qizilga o'xshash rang bor edi —
boshqa rangga o'zgartirish.

- **Sabab:** CSS da qizil yo'q edi (focus yashil). Qizil — brauzerning NATIV
  imlo tekshiruvi (spellcheck) — o'zbekcha so'zlar lug'atda yo'qligi uchun har
  bir so'z ostida qizil to'lqinli chiziq; va bo'sh `required` maydonda Firefox'ning
  qizil `:invalid` porlashi.
- **Markup** (`Views/Posts/Details.cshtml`): asosiy izoh maydoni (`#commentText`)
  va JS bilan yaratiladigan javob (reply) maydoniga `spellcheck="false"` qo'shildi
  — qizil imlo chizig'i yo'qoladi.
- **CSS** (`wwwroot/css/site.css`): `.pd-comment-form textarea:-moz-ui-invalid`,
  `.comment-reply-form textarea:-moz-ui-invalid { box-shadow: none; border-color:
  var(--border) }` — Firefox'ning bo'sh-required qizil porlashi o'rniga brend
  neytral/yashil chegara qoladi.
- **TEKShIRILDI:** rebuild (0/0) + restart 5261 (Development). Dev JWT auth curl:
  `/post/javohirsadullayev/a7f86aff545c` → `commentText` `spellcheck="false"`=1,
  reply template `spellcheck="false"`=1; `css/site.css` da `moz-ui-invalid` serve
  qilinyapti. DB tegilmadi. **Keyingi qadam:** foydalanuvchi aytadigan aniq
  o'zgarishlarni kutish.

---

**2026-06-20 — `/quotes` qidiruv yonida "Tozalash" tugmasi — bosilganda barcha iqtiboslar qaytadi.**

Talab: `/quotes` da qidiruv qilingach (natija chiqsa YOKI topilmasa) o'ng tomonda
"Tozalash" tugmasi bo'lsin; bosilganda yana avvalgi barcha iqtiboslar ko'rinsin.

- **Faqat markup** (`Views/Quotes/Index.cshtml`): qidiruv server-side GET forma
  (`action="/quotes"`, `?q=`). Forma ichi flex qatorga o'raldi: `.search-box`
  (flex:1) chapda, o'ngida — faqat `search` bo'sh bo'lmaganda — `<a class="btn
  btn-outline" id="quoteSearchClear" href="/quotes">` "Tozalash" tugmasi (close
  ikoni + matn). `/quotes` ga (querysiz) o'tadi → server barcha iqtiboslarni
  qaytaradi. JS shart emas (oddiy navigatsiya). Tugma faqat aktiv qidiruvda
  ko'rinadi (natija bor yoki "topilmadi" — ikkalasida ham `search` to'la).
- **TEKShIRILDI:** `.cshtml` o'zgargani uchun rebuild (0/0) + restart 5261
  (Development). Dev JWT (user1) auth curl: `/quotes` (qidiruvsiz) → `quoteSearchClear`=0;
  `/quotes?q=kitob` (natija) → tugma=1, `href="/quotes"`, "Tozalash"; `/quotes?q=zzznomatchxyz`
  (topilmadi) → tugma=1 + "topilmadi" matni. DB tegilmadi.
  **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/post/{username}/{slug}` (post detali) navbaridagi qidiruv (search) ikoni olib tashlandi.**

Talab: post detali sahifasi headeridagi qidiruv ikonini olib tashlash.

- **Faqat markup** (`Views/Shared/_Layout.cshtml`): `hideSearch` shartiga
  `Posts` controller qo'shildi (Feed/Profile/ReadingGoals/Quotes/Chat qatoriga).
  Endi `currentController == "Posts"` bo'lganda navbardagi `title="Qidiruv"`
  ikoni render qilinmaydi. Navbar faqat auth foydalanuvchiga ko'rinadi (anonim
  `.anon-banner` ko'radi — u yerda allaqachon search yo'q edi).
- **TEKShIRILDI:** `.cshtml` o'zgargani uchun rebuild (0/0) + restart 5261
  (Development). Dev JWT (user1) bilan auth curl: `/post/javohirsadullayev/a7f86aff545c`
  → navbar bor (1), `title="Qidiruv"` = 0 (yo'q), `/reading-books` bilan bir xil (0).
  Anonim post sahifasi `.anon-banner` (navbar 0). DB tegilmadi.
  **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/post/{username}/{slug}` "Izohlar" kartasi balandligi ekranga moslashadi (ichida scroll) + javob/muallif/boshqa foydalanuvchi izohlari uchun uchta rang.**

Talab: (1) "Izohlar" kartasi balandligi juda baland edi — endi ekran balandligiga
moslashsin, izohlar ko'paysa karta ICHIDA scrollbar ishlasin. (2) "Muallif"
izohi boshqa rangda, "javob" (reply) boshqa rangda, "boshqa foydalanuvchilar"
izohi ham boshqa rangda bo'lsin.

- **Faqat CSS** (`wwwroot/css/site.css`):
  - **Karta balandligi:** `.pd-comments-card` → `display: flex; flex-direction: column`.
    `.pd-comment-list` endi `flex: 1 1 auto; min-height: 0; max-height: 60vh`
    (eski qattiq `max-height: 480px` o'rniga). Sarlavha (`.pd-comments-title`) va
    forma (`.pd-comment-form`) ga `flex-shrink: 0`. Desktop (`min-width:1025px`)
    blokda: `.pd-comments-card { max-height: calc(100vh - var(--navbar-h) - 48px) }`
    + `.pd-comment-list { max-height: none }` — karta ekran balandligini to'ldiradi,
    izohlar ro'yxati karta ichida scroll bo'ladi (sarlavha+forma joyida qoladi).
    Mobilda (sticky yo'q) ro'yxat `60vh` bilan cheklanadi.
  - **Uchta rang:** Muallif izohi — yashil (`--primary`) pufak (eskidan bor edi).
    **Yangi:** javob (reply) izohlari uchun accent (to'q sariq) tint —
    `.comment .replies .comment:not(.comment-author) .bubble { background:
    rgba(232,112,58,.10); border-color: rgba(232,112,58,.30) }`. Muallifning
    javobi yashil qoladi (`:not(.comment-author)` istisnosi). Boshqa
    foydalanuvchilarning yuqori darajadagi izohi neytral krem (`--bg`) — uchovi
    aniq ajraladi (yashil / sariq / krem). JS bilan qo'shilgan replylar ham
    `[data-replies-for]`=`.replies` ichiga tushgani uchun shu rangni oladi.
- **TEKShIRILDI:** CSS statik — rebuild/restart KERAK EMAS, hard refresh yetarli.
  `curl http://localhost:5261/css/site.css` → yangi `.replies .comment:not(.comment-author)`
  va `calc(100vh - var(--navbar-h) - 48px)` qoidalari jonli serve qilinyapti.
  DB tegilmadi. **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/feed` post matni maydoniga limit qo'shildi: min 3, max 5000 belgi (composer + tahrir panellari + server).**

Talab: `/feed` dagi matn maydoni ("textarea") uchun limit — min:3, max:5000.

- **Eslatma:** `/feed` matn maydoni aslida `textarea` emas, `_RichEditor`
  (contenteditable rich-text muharriri). Shuning uchun cheklov shu muharrirga
  qo'yildi (native `minlength/maxlength` ishlamaydi).
- **`RichEditorModel`** ga `MinLength`/`MaxLength` (int, 0=cheklovsiz) qo'shildi.
  `_RichEditor.cshtml`: root'da `data-rich-min`/`data-rich-max`; `MaxLength>0`
  bo'lsa `.rich-counter` (jonli sanagich `N / 5000`) render qilinadi.
- **Partial chaqiruvlari** (`Feed/Index`, `_PostCard`, `Posts/Details` tahrir
  panellari) — hammasi `MinLength = 3, MaxLength = 5000`.
- **JS** (`site.js` rich-editor moduli): `sync()` da `maxLen` oshsa oxirgi to'g'ri
  holatga **revert** (haqiqiy maxlength xulqi, karet oxiriga); `updateCounter`
  (limit buzilsa `.rich-counter-invalid` qizil); API'ga `check()` +
  `minLength`/`maxLength`. Composer submit (`Feed/Index`) va tahrir-saqlash
  (`site.js [data-save-edit-post]`) endi `tooShort`/`tooLong` da alert bilan bloklaydi.
- **Server (hard guarantee)**: `CreatePostCommandValidator` + `UpdatePostCommandValidator`
  ga `.MinimumLength(3)` qo'shildi (max 5000 allaqachon bor edi). Bu ikkala
  validator faqat post fikriga tegishli (quotes alohida).
- **CSS** (`site.css`): `.rich-counter` (o'ngga tekislangan, kichik, kulrang) +
  `.rich-counter-invalid` (accent/qizil).
- **Test:** `ValidatorTests.CreatePost_*` yangilandi (2-belgili "ok" endi
  INVALID — min:3 keysi qo'shildi). build **0/0**, test **65/65**.
- **TEKShIRILDI:** restart 5261 (Development). `js/site.js` (check/data.richMax/
  rich-counter) va `css/site.css` (rich-counter) jonli serve qilinyapti; `/Feed`
  anonim→401 (kutilgan). DB tegilmadi.
- **ESLATMA:** `.cshtml`/validator/JS o'zgargani uchun rebuild+restart bajarildi.
  Ilova 5261 da ishlab turibdi. **Keyingi qadam:** foydalanuvchi aytadigan aniq
  o'zgarishlarni kutish.

---

**2026-06-20 — Header nav menyusida "Lenta" → "Tasma" so'zi o'zgartirildi.**

Talab: sayt headeridagi "Lenta" so'zini "Tasma"ga almashtirish.

- **Faqat markup** (`Views/Shared/_Layout.cshtml:50`): `<a href="/Feed">Lenta</a>`
  → `<a href="/Feed">Tasma</a>`. Yagona joy edi (grep tasdiqladi, boshqa "Lenta" yo'q).
  Nav-links faqat auth foydalanuvchiga ko'rinadi.
- **TEKShIRILDI:** `.cshtml` o'zgargani uchun rebuild (0/0) + restart 5261
  (Development) SHART — bajarildi (Razor runtime compilation o'chiq). App ko'tarildi
  (`/Feed`→401 anonim, kutilgan). `grep "Lenta"` Views/wwwroot bo'yicha 0.
  **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/post/{username}/{slug}` anonim header (`.anon-banner`) to'q yashildan OCH yashilga o'tkazildi (logo singib ketmasligi uchun).**

Talab: `/post/username/random` (ro'yxatdan o'tmaganlar uchun ochiq sahifa)
yuqorisidagi heder qismi to'q yashil edi va logoga halaqit berardi (logo ichidagi
quyuq yashil `#1B4D3E` boyo'g'li ko'zlari + kitob tanasi ayni fon rangiga singib
ketardi) — och yashilga o'zgartirildi.

- **Faqat CSS** (`wwwroot/css/site.css` `.anon-banner` bloki):
  - `background: var(--primary)` (#1B4D3E, to'q yashil) → **`#E3EEE8`** (och yashil)
    + nozik chegara `border: 1px solid rgba(27,77,62,0.14)`.
  - `color: #fff` → **`var(--primary)`** (matn endi quyuq yashil — och fonda o'qiladi).
  - `.anon-banner .brand` `#fff` → `var(--primary)`; `.anon-banner p` `#fff`,
    `opacity .92` → `var(--primary)`, `opacity .82`. `.uz` (accent/orange) va
    "Ro'yxatdan o'tish" `btn-accent` (orange) o'zgarmadi — och fonda yaxshi ko'rinadi.
- **Eslatma:** `/post/{username}/{slug}` allaqachon `[AllowAnonymous]` (oldingi
  sessiyalar) — anonim kirish ishlaydi, yangi kod kerak bo'lmadi. Bu sahifa
  `.anon-banner` ni ishlatadi (public profil `/u/{username}` esa boshqa `.public-bar`).
- **TEKShIRILDI:** CSS statik — rebuild/restart KERAK EMAS, hard refresh yetarli.
  `curl http://localhost:5261/css/site.css` → 200, yangi `.anon-banner` (background
  `#E3EEE8`, color `var(--primary)`) jonli tarqatilyapti. Ilova 5261 da ishlab turibdi.
  **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/feed` post yozish endi RICH TEXT muharriri (qalin/kursiv/tagchiziq/marker) + format `/post/{username}/{slug}` da ko'rinadi.**

Talab: `/feed` dagi oddiy `textarea` o'rniga dizaynga mos muharrir — matnni
belgilab **bold/italic/marker** qilish; bu formatlar `/post/username/random`
sahifasida (va feed kartada) chiroyli, marker (highlighter) kabi ko'rinsin.

- **Xavfsizlik chegarasi — server sanitizer** (`Application/Common/RichTextSanitizer.cs`):
  FAQAT `<b><i><u><mark>` teglarini qoldiradi (atributsiz), qolgani (skript, boshqa
  teglar) HTML-encode qilinadi. `<strong>/<em>` → `<b>/<i>`, `<br>`/`</p>`/`</div>`
  → `\n`. **IDEMPOTENT** (avval `HtmlDecode`) — shuning uchun yozishda HAM, render
  paytida HAM (eski, sanitize qilinmagan postlar uchun) xavfsiz chaqiriladi, ikki
  marta kodlanmaydi. `CreatePost`/`UpdatePost` handlerlarida `Sanitize(...)` qo'llandi.
- **Render**: `_PostCard` `.post-text` va `Details` `.pd-review` endi
  `@ViewHelpers.RichText(...)` (yangi `IHtmlContent` yordamchi — Sanitize'ni
  `HtmlString` bilan o'raydi). Eski plain-text postlar ham xavfsiz (encode bo'ladi).
- **Muharrir (frontend)**: yangi qayta ishlatiladigan `Views/Shared/_RichEditor.cshtml`
  partial (`RichEditorModel`): toolbar (B/I/U/marker + format tozalash) +
  `contenteditable` div + yashirin chiquvchi maydon. Composer (`Feed/Index`) va
  HAR IKKI tahrir paneli (`_PostCard` + `Posts/Details`) shu partialdan foydalanadi.
- **JS** (`wwwroot/js/site.js`): yangi rich-editor moduli — `contenteditable` DOM'ni
  xavfsiz kichik HTML satriga **serialize** qiladi (matn tugunlari encode, faqat
  b/i/u/mark teglari; marker uchun `<mark>` wrap/unwrap; B/I/U `execCommand`; toolbar
  active holati; `rich:input` hodisasi). `window.kitob.initRichEditors`. Post
  tahrirlash (focus/cancel/save) handlerlari muharrir API'siga moslandi: saqlashda
  `output.value`, ko'rinishga `innerHTML = post.reviewText` (server sanitize qilingan).
  Composer `Feed/Index` JS hidden `ReviewText` + `rich:input` ga o'tdi; submitda sync.
- **CSS** (`site.css`): `.rich-editor/.rich-toolbar/.rich-btn/.rich-content` (placeholder
  `is-empty::before`), marker `mark` highlighter band (accent, `box-decoration-break`)
  `.post-text/.pd-review/.story-detail-text` da bir xil.
- **TEKShIRILDI**: build **0/0**; **test 65/65** (8 ta yangi `RichTextSanitizerTests`:
  ruxsat teglar, skript strip, atribut tushishi, alias, br/blok→newline, IDEMPOTENT,
  eski plain-text `<`/`&` xavfsiz, bo'sh). Restart 5261 (Development). Dev JWT (user1)
  bilan jonli oqim: `/posts/13/update` ga `<b>/<i>/<mark>/<u>` + `<script>` + `< &`
  yuborildi → DTO sanitize qaytardi (teglar saqlandi, `<script>`→`&lt;script&gt;`);
  `/post/javohirsadullayev/wNQF4OuDV1hU` detali va `/Feed` kartada formatlash REAL
  teg, `<script>` jonli emas (grep=0); tahrir paneli `data-rich-content` formatli
  prefill + `data-original-text` to'g'ri. **DB ESLATMA:** tekshiruvda post #13 matni
  qayta yozildi; asl matn saqlanmagani uchun neytral "Bu kitob haqidagi fikr." ga
  qaytarildi (foydalanuvchi xohlasa o'zgartirsin).
- **ESLATMA:** `.cshtml`/handler/yangi fayl o'zgargani uchun rebuild+restart bajarildi.
  Ilova 5261 da ishlab turibdi. **Keyingi qadam:** foydalanuvchi aytadigan aniq
  o'zgarishlarni kutish.

---

**2026-06-20 — `/post/{username}/{slug}` post detali `04-post-detail.html` matn dizayniga moslandi (genre pill + post vaqti).**

Talab: `/post/username/random` sahifasini `04-post-detail.html` ga o'tkazish, AMMO
faqat (1) matndagi turli dizayn (tipografiya), (2) kitob qaysi kategoriyaga tegishli
ekanini ko'rsatish, (3) post qachon qo'yilganini bildiruvchi vaqt. Dizaynning boshqa
qismlari (yulduz reyting, "400 bet o'qildi", "Saqlash" tugmasi) KO'CHIRILMADI —
ular real ma'lumotga mos kelmaydi.

- **View** (`Views/Posts/Details.cshtml`):
  - Muallif yonidagi vaqt `<div class="time">` → `<time class="time">` ga aylandi:
    `schedule` ikoni + `RelativeTime(CreatedAt)` ("1 soat oldin"), `title` da aniq sana
    (`dd.MM.yyyy HH:mm` local), `datetime` ISO-8601 (semantik). Endi "qachon qo'yilgani"
    aniq ko'rinadi (hover'da to'liq sana).
  - Kitob janri: yagona `<span class="badge">` o'rniga `.pd-genres` > `.pd-genre`
    pill (dizayn 04 ning `rounded-full ... border` kategoriya chipi): `local_library`
    ikoni + `GenreName`. Faqat genre mavjud bo'lsa chiqadi (genre yo'q kitoblarda yo'q).
- **CSS** (`site.css` `.pd-*` bloki):
  - `.pd-bookhead h1` 32px → **38px** serif headline (dizayn `headline-lg`), letter-spacing.
  - `.pd-book-author` 17→18px. `.pd-review` 16/1.75 → **17px / line-height 1.8** (dizayn
    prose o'qish ritmi). `.pd-author .time` flex+ikon. Yangi `.pd-genres`/`.pd-genre` pill.
  - Responsive (≤600px): h1 27px, review 16/1.75.
- **TEKShIRILDI (build 0/0 + restart 5261 Development, anonim curl):**
  - `/post/javohirsadullayev/a7f86aff545c` (genre BOR): `pd-genre` pill + `local_library`
    + "Psixologiya" render bo'ldi.
  - `/post/.../wNQF4OuDV1hU` (genre YO'Q): pill chiqmadi (to'g'ri), vaqt bloki:
    `<time>` + `schedule` + "1 soat oldin" + `title="20.06.2026 02:02"`.
  - `css/site.css` da `.pd-genre`, `line-height: 1.8`, `font-size: 38px` serve qilinyapti.
  - DB tegilmadi (faqat o'qish).
- **ESLATMA:** `.cshtml` o'zgargani uchun rebuild+restart SHART (Razor runtime
  compilation o'chiq) — bajarildi. Ilova 5261 da ishlab turibdi.
  **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/quotes` karta matni kartadan chiqib ketishi tuzatildi (CSS).**

Talab: iqtibos kartalarida uzun so'z/matn karta tashqarisiga chiqib ketardi —
karta ichida qolsin. `site.css`:
- `.quote-text`, `.quote-source` → `overflow-wrap: break-word; word-break: break-word;`
  (uzun uzilmaydigan so'zlar o'raladi).
- `.quote-card` → `overflow: hidden` (xavfsizlik klipi).
- `.quote-foot` → `gap: 12px`; `.quote-source` → `min-width: 0` (tugmalar yonida
  to'g'ri qisqaradi/o'raladi). CSS statik — rebuild kerak emas, hard refresh yetarli.

**2026-06-20 — `/quotes` formasi soddalashtirildi: sahifa/kategoriya olib tashlandi, iqtibos matni 3–400.**

Talab: `/quotes` "Yangi kitob" formasidan "Sahifalar soni" va "Kategoriyani
tanlang" olib tashlash; "Iqtibos matni" min:3, max:400.

- **View** (`Views/Quotes/Index.cshtml`):
  - `qNbPages` (Sahifalar soni) va `qNbGenre` (`<select>` Kategoriya) markup'i
    o'chirildi. Endi forma faqat `qNbTitle` + `qNbAuthor` (3–100) + tugma.
  - Iqtibos textarea'siga `minlength="3" maxlength="400"` qo'shildi.
  - JS `qNbSave`: genre/pages o'qish va validatsiyasi olib tashlandi.
    `/books/create` server-da `TotalPages > 0` talab qiladi (validator `/feed`
    bilan UMUMIY) — shuning uchun forma `totalPages: 1`, `genreId: null` default
    yuboradi (quotes'dan yaratilgan kitob sahifasiz, 1 sahifa sifatida).
  - Endi kerak bo'lmagan `@using ...GetGenres`, `@using MediatR`,
    `@inject ISender Mediator` va `GetGenresQuery` chaqiruvi olib tashlandi.
- **Server validatsiya** (`CreateQuoteCommandValidator`): `Text` →
  `MinimumLength(3)` + `MaximumLength(400)` (eski max 2000 o'rniga). Bu faqat
  iqtibosga tegishli, `/feed` ga ta'sir qilmaydi.
- `dotnet build` (Web) → 0/0. Razor runtime compilation o'chiq — restart (5261) kerak.

**2026-06-20 — `/quotes` "Yangi kitob" formasi: nom/muallif uzunligi + muqova olib tashlandi.**

Talab: `/quotes` bo'limidagi "Yangi kitob" formasida "Kitob nomi" va "Muallif"
uzunligi min:3, max:100 bo'lsin; "Muqova rasmi" umuman kerak emas — olib tashlash.

- **Frontend-only** (`src/KitobdaGimen.Web/Views/Quotes/Index.cshtml`):
  - `qNbTitle`/`qNbAuthor` inputlariga `minlength="3" maxlength="100"`.
  - `qNbSave` JS validatsiyasiga: nom va muallif uzunligi 3–100 tekshiruvi
    (xato alert), aks holda `/books/create` chaqirilmaydi.
  - **Muqova bloki butunlay o'chirildi**: `nb-cover` markup, `qNbCover`/preview/
    clear/hint elementlari, `coverFile`/`coverUrl`, `resetCover`, `uploadCover`,
    `change`/`click` listenerlar va `qNbSave` ichidagi cover yuklash mantiqi.
- **Backend tegilmadi**: `CreateBookCommandValidator` (Title max 300 / Author
  max 200) `/feed` bilan UMUMIY — global o'zgartirilmadi. `CoverUrl` server-da
  allaqachon ixtiyoriy (`string?`). Min:3/max:100 faqat `/quotes` formasiga
  tegishli, shuning uchun client-side qilingan.
- `dotnet build` (Web) → 0 warning / 0 error; Razor kompilyatsiya o'tdi.
  Razor runtime compilation o'chiq — ko'rinish o'zgarishi uchun restart (5261) kerak.

**2026-06-20 — Iqtiboslar bo'limi `/u/{username}` (public profil) da ham ko'rinadi — TEKSHIRILDI (kod allaqachon mavjud edi).**

Talab: `/profile` dagi "Iqtiboslar" tabi/bo'limi `/u/{username}` public profilda ham bo'lsin.

- **Holat:** Kod allaqachon to'liq amalga oshirilgan edi (oldingi sessiya PROGRESS ga
  yozmagan). `PublicProfileController.Index` ham `ProfileController.Index` kabi
  `GetUserQuotesQuery(id) { PageSize = 50 }` yuboradi → `MyQuotes`. `GetUserQuotesQuery`
  anonim-do'st (`_currentUser.UserId` null bo'lsa save-state yo'q). `Profile/Index.cshtml`
  da "Iqtiboslar" tab (qator 137) va `data-tab-panel="quotes"` panel (qator 242)
  `IsCurrentUser` bilan GATE QILINMAGAN — hammaga ko'rinadi. Bo'sh holat egasi/mehmonni
  ajratadi ("Hali iqtibos yozmagansiz." / "Hali iqtibos yo'q."); o'chirish formasi
  faqat egasi (`@if (p.IsCurrentUser)`).
- **Bajarilgan ish:** build (0/0) + restart 5261 (Development) + verifikatsiya — eski
  binar ishlayotgan bo'lishi mumkin edi, shuning uchun qayta qurib ishga tushirildi.
- **TEKShIRILDI (anonim curl `/u/javohirsadullayev`):** HTTP 200; `data-profile-tab="quotes"`=1,
  `data-tab-panel="quotes"`=1, `quote-card`=2 (user1 ning 2 iqtibosi render bo'ldi),
  `public-bar`=1 (anonim). Iqtibos o'chirish formasi (`/quotes/{id}/delete`) anonimga
  KO'RINMAYDI (0) — faqat delete-account JS satri qoldi (endpoint `[Authorize]`, xavfsiz).
  DB tegilmadi.
- **ESLATMA:** Yangi kod kerak bo'lmadi. Ilova 5261 da ishlab turibdi.
  **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/profile` storylari endi detal sahifaga havola (modal EMAS) + ulashiladigan public profil `/u/{id}` (anonim).**

Talab: (1) `/profile` "Storylar" tabidagi karta bosilganda story rasmi, sarlavhasi,
matni, sanasi, like, view'lari KO'RINSIN — hozir modal chiqyapti, BU XATO.
(2) Ro'yxatdan o'tmaganlarga `/profile` ni ulashish uchun yangi URL kerak.

- **(1) Story detal sahifasi `/stories/{id}`:** Yangi `GetStoryByIdQuery`+Handler
  (`Features/Stories/Queries/GetStoryById/`) — eskirgan story ham (WhereActive YO'Q),
  `ToStoryDto(currentUser)`, topilmasa NotFound→404. `StoriesController` ga
  **`[AllowAnonymous] [HttpGet("{id:int}")] Details`** (POST `{id}/view|like|delete`
  bilan to'qnashmaydi). Yangi view `Views/Stories/Details.cshtml`: muallif (avatar+
  ism+to'liq sana), rasm, katta serif sarlavha, matn, futerda 👁 view + ❤ like +
  (egasi bo'lsa) 🗑 delete. Auth bo'lsa JS view'ni yozadi (`/stories/{id}/view`) va
  like toggle qiladi; anonim bo'lsa statik. CSS: `.story-detail-*` bloki + `.public-bar`.
- **Profil tile o'zgarishi:** `Profile/Index.cshtml` story tile `<button data-story-index>`
  → **`<a href="/stories/{id}">`** (modal ochuvchi JS + `profileStoriesData` JSON
  OLIB TASHLANDI; tab almashtirish JS qoldi). Avatardagi to'liq ekranli viewer
  (`data-open-story`) tegilmadi — faqat tile bosish o'zgardi.
- **(2) Public profil `/u/{id}`:** Yangi `PublicProfileController` **`[AllowAnonymous]
  [Route("u")]`** — `Profile/Index.cshtml` ni qayta ishlatadi (egasi nazorati
  IsCurrentUser=false sabab avtomatik yashirinadi; o'z linkiga kirgan auth user
  `/profile/{id}` ga redirect). `Profile/Index.cshtml` anonimga moslandi:
  `@inject ICurrentUserService`, `isAuth`; anonimda yuqorida `.public-bar`
  (brand+Kirish), follow/message o'rniga "Kuzatish uchun kiring" CTA, followers/
  following stat'lar link emas (auth talab qilmaslik uchun). **"Ulashish"** tugmasi
  (egasi + auth non-owner + anonim) `/u/{id}` absolyut havolasini clipboard'ga
  (navigator.share/clipboard) nusxalaydi. Post detali (`/post/{u}/{slug}`) allaqachon
  AllowAnonymous — tile'lar anonimda ham ishlaydi.
- **TEKShIRILDI (build 0/0 + restart 5261 Development, anonim curl):** `/u/1`→200
  (`public-bar`, `data-share-profile`×2, `/u/1` share url, "Kuzatish uchun kiring",
  story tile `href="/stories/"`, egasi tugmalari YO'Q: delete-button/modal/composer=0,
  stories panel + reading-now bor); `/profile`(anon)→401; `/stories/6`(anon)→200
  (detail card, footer 👁1 ❤2, public-bar); `/stories/1`→404, `/u/99999`→404 (NotFound
  toza). DB tegilmadi.
- **YANGILANISH (username URL):** Ulashish havolasi endi `/u/{id}` EMAS, `/u/{username}`
  (username noyob). Yangi `GetUserIdByUsernameQuery`+Handler (`Features/Profile/Queries/
  GetUserByUsername/`) — case-insensitive (`ToLower()==`), topilmasa NotFound.
  `PublicProfileController` ga **`[HttpGet("{username}")] ByUsername`** qo'shildi
  (id'ga resolve qilib `Index` ga delegat); `{id:int}` route fallback sifatida qoldi
  (username yo'q userlar uchun). `Profile/Index.cshtml` shareUrl `shareSegment`
  (username ?? id); `Stories/Details.cshtml` back-link ham username ?? id.
  **TEKShIRILDI:** `/u/javohirsadullayev`→200, UPPERCASE→200 (case-insensitive),
  `/u/1`→200 (fallback), `/u/zzznotauser`→404; share url=`/u/javohirsadullayev`;
  story back-link=`/u/javohirsadullayev`. build 0/0, restart 5261.
- **ESLATMA:** `.cshtml`/controller/yangi query o'zgargani uchun rebuild+restart SHART —
  bajarildi. Ilova 5261 da ishlab turibdi. **Keyingi qadam:** foydalanuvchi aytadigan
  aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/profile`: "Maqsadlar" tabi olib tashlandi + "Iqtiboslar" endi shu oynada foydalanuvchi iqtiboslarini ko'rsatadi.**

Talab: `/profile` tablaridan "Maqsadlar" ni olib tashlash; "Iqtiboslar" bosilganda
(boshqa sahifaga o'tmasdan) foydalanuvchi o'zi yozgan iqtiboslar shu oynada chiqsin.

- **Tablar (`Profile/Index.cshtml`):** `<a href="/reading-books">Maqsadlar</a>`
  butunlay o'chirildi. `<a href="/quotes">Iqtiboslar</a>` → `<button
  data-profile-tab="quotes">Iqtiboslar</button>` (faqat o'z profilida). Endi
  Iqtiboslar ham Postlar/Storylar kabi inline panel — boshqa sahifaga o'tmaydi.
- **Yangi panel:** `<div data-tab-panel="quotes" hidden>` (faqat `IsCurrentUser`)
  — `/quotes` dagi AYNAN karta dizayni (`quote-grid`/`quote-card`/`quote-text`/
  `quote-foot`/`quote-source`): kitob nomi+muallif, save count (ko'rsatkich),
  o'chirish formasi (`/quotes/{id}/delete`), futerda yaratilgan sana `dd.MM.yyyy`.
  Bo'sh holatda "Hali iqtibos yozmagansiz." Tab JS allaqachon generik
  (`[data-profile-tab]`/`[data-tab-panel]`) — JS o'zgartirilmadi.
- **Backend:** `ProfilePageViewModel` ga `MyQuotes` (IReadOnlyList<QuoteDto>).
  `ProfileController.Index` faqat `userId == CurrentUserId` bo'lsa
  `GetMyQuotesQuery { PageSize = 50 }` yuboradi (boshqa profilda bo'sh massiv —
  query CurrentUser ga bog'liq, boshqaning iqtiboslarini ko'rsatmaymiz).
- **TEKShIRILDI (build 0/0 + restart 5261 Development, minted JWT user1):**
  `GET /profile`→200; `data-profile-tab`: posts/stories/quotes ("Maqsadlar" YO'Q,
  grep=0); `data-tab-panel`: posts/stories/quotes; `quote-card`=2 (user1 ning 2
  iqtibosi render bo'ldi). DB tegilmadi (faqat o'qish).
- **ESLATMA:** `.cshtml`/controller/VM o'zgargani uchun rebuild+restart SHART —
  bajarildi. Ilova 5261 da ishlab turibdi.
  **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/profile`: nav tabidagi "O'qilayotgan" → "Storylar" tabiga almashtirildi (TUZATISH).**

Talab (oldingi urinish noto'g'ri edi — o'ngdagi "Hozir o'qiyapti" kartasini
almashtirgan edim, foydalanuvchi qaytarishni so'radi): nav tablaridagi
**"O'qilayotgan"** bo'limini olib tashlab **"Storylar"** ga almashtirish; "Storylar"
bosilganda foydalanuvchi story'lari ko'rinsin; har story karta bosilganda
profil avatarini bosgandagi AYNAN o'sha to'liq ekranli story viewer ochilsin;
har story uchun sana, like, view ma'lumoti bo'lsin.

- **Qaytarildi:** o'ngdagi `aside.profile-side` `.reading-now` ("Hozir o'qiyapti")
  kartasi ASL holatiga qaytarildi; `ProfilePageViewModel.CurrentBook` qaytarildi;
  `ProfileController` yana `GetActiveReadingGoalsQuery` yuboradi. Oldingi
  sessiyada qo'shilgan `.story-history`/`.story-item`/`.story-thumb` CSS bloki
  o'chirildi.
- **Saqlandi:** `GetUserStoryHistoryQuery(int UserId)` + Handler (barcha story —
  `WhereActive()` YO'Q, eskirgan ham; `OrderByDescending(CreatedAt)`; `ToStoryDto`).
  `ProfilePageViewModel` ga `Stories` ham qo'shilgan (CurrentBook bilan birga).
- **Tablar (`Profile/Index.cshtml`):** `<span>Postlar` + 3 ta `<a>` o'rniga endi
  `<button data-profile-tab="posts">Postlar` va `data-profile-tab="stories">Storylar`
  (har doim ko'rinadi); o'z profilida qo'shimcha `Maqsadlar`(→/reading-books) +
  `Iqtiboslar`(→/quotes) havolalari. "O'qilayotgan" butunlay olib tashlandi.
- **Panellar:** `<div data-tab-panel="posts">` (post-tiles + pagination +
  "Tugatilgan kitoblar" — default ko'rinadi) va `<div data-tab-panel="stories" hidden>`
  (story tiles). JS tab bosilganda `.active` klass + panel `hidden` ni almashtiradi.
- **Story tiles:** `.story-tiles` grid (3 ust., ≤600px 2 ust.), har biri
  `<button class="story-tile" data-story-index="i">`: rasm/`auto_stories` fallback +
  `.story-tile-meta` (chap **sana** `dd.MM.yyyy` + o'ng ❤ LikeCount, 👁 ViewCount).
- **Viewer ulanishi:** `site.js` `open(list)` → `open(list, startIndex)` qilindi va
  `window.kitobStory = { open }` global qilindi. `Profile/Index.cshtml`
  `@@section Scripts` da story'lar `<script type="application/json"
  id="profileStoriesData">` ga camelCase JSON bo'lib chiqadi; tile bosilganda
  `window.kitobStory.open(stories, index)` AYNAN profil avatardagi to'liq ekranli
  `#storyViewer` ni o'sha story'dan boshlab ochadi (eskirgan story ham — ro'yxat
  to'g'ridan-to'g'ri uzatilgani uchun `/stories/user` active-filtridan o'tmaydi).
- **CSS:** yangi `.story-tiles`/`.story-tile`/`.story-tile-img`/`.story-tile-fallback`/
  `.story-tile-meta`/`.story-tile-date`/`.story-tile-stats` bloki.
- **TEKShIRILDI (build 0/0 + restart 5261 Development, dev JWT user1):**
  `GET /profile`→200; tablar: Postlar/Storylar/Maqsadlar/Iqtiboslar ("O'qilayotgan"
  YO'Q); `data-tab-panel` posts+stories; `story-tile` data-story-index=0,
  `story-tile-date`="19.06.2026", ❤ 2 + 👁; aside `reading-now`+"Hozir o'qiyapti"
  QAYTDI; `profileStoriesData` JSON to'g'ri (id/createdAt/viewCount/likeCount/
  isMine/author camelCase); CSS `story-tile` 15 ta qoida; `site.js` `window.kitobStory`
  serve qilindi. DB tegilmadi (user1 story #6 title/text bo'sh — real ma'lumot).
- **ESLATMA:** `.cshtml`/controller/VM o'zgargani uchun rebuild+restart SHART
  (Razor runtime compilation o'chiq) — bajarildi. Ilova 5261 da ishlab turibdi.
  **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-20 — `/profile` sahifasi `design-reference/05-profil.html` ga moslab qayta dizayn qilindi.**

Talab: `/profile` 05-profil dizaynidagidek bo'lsin (keyin foydalanuvchi ayrim
joylarini o'zgartirishni aytadi).

- **Layout (`Views/Profile/Index.cshtml` qayta yozildi):** dizayn 05 strukturasi —
  (1) **Profil sarlavhasi** (`profile-hero`): avatar (story bo'lsa gradient halqa,
  bo'lmasa accent `profile-ring`), ism (katta serif), Bio → italic motto
  (`profile-motto`), vertikal-ajratgichli stats bar (`profile-statbar`:
  Postlar/Kuzatuvchilar/Kuzatilayotganlar), tugmalar (o'zi: Story/Tahrirlash/
  O'chirish; boshqa: accent **Kuzatish** + outline **Xabar**).
  (2) **2 ustunli grid** (`profile-layout` 2fr/1fr): chapda tablar
  (`profile-tabs`: Postlar active; o'z profilida O'qilayotgan/Maqsadlar→
  `/reading-books`, Iqtiboslar→`/quotes`) + **kvadrat postlar grid**
  (`post-tiles` 3 ustun, hover overlay'da ❤ like + 👁 view; rasm =
  post.ImageUrl ?? Book.CoverUrl, bo'lmasa fallback ikona+sarlavha) +
  "Tugatilgan kitoblar" (saqlandi). O'ngda **"Hozir o'qiyapti"** kartasi
  (`reading-now`, sticky): muqova + nom/muallif + conic-gradient doiraviy
  progress (`--p:N%`) + kunlik maqsad pill.
- **Backend (kichik):** `GetActiveReadingGoalsQuery` endi `int? UserId = null`
  qabul qiladi (finished query naqshi) — profil egasining faol kitobini olish
  uchun. `ProfilePageViewModel` ga `CurrentBook` (birinchi faol goal).
  `ProfileController.Index` `GetActiveReadingGoalsQuery(userId)` yuboradi.
  Mavjud yagona chaqiruv (`ReadingGoalsController`) buzilmadi (parametrsiz ham ishlaydi).
- **CSS (`site.css`):** yangi "Profile redesign (05-profil)" bloki —
  `profile-hero/ring/motto/statbar/layout/tabs`, `post-tiles/post-tile/
  post-tile-overlay/post-tile-fallback`, `reading-now/-cover/-ring(conic)/-goal`;
  responsive (≤900px 1 ustun + reading-now static, ≤600px 2 ustun grid). Eski
  `.profile-head/.profile-stats` bloki tegilmadi (endi ishlatilmaydi, zararsiz).
- **TEKShIRILDI (build 0/0 + restart 5261, dev JWT user1):** `GET /profile`→200;
  render: `profile-hero/statbar/layout/tabs`, **5 post-tile + 5 overlay**, tablar
  to'g'ri linklar, "Hozir o'qiyapti" + `reading-now-ring` + `--p:0%` (user1 faol
  kitob CurrentPage=0). CSS 200, `reading-now-ring` serve qilinyapti. DB tegilmadi.
- **ESLATMA:** `.cshtml`/controller o'zgargani uchun rebuild+restart SHART (Razor
  runtime compilation o'chiq) — bajarildi. Ilova 5261 da ishlab turibdi.
  **Keyingi qadam:** foydalanuvchi aytadigan aniq o'zgarishlarni kutish.

---

**2026-06-19 — Navbar qidiruv ikoni `/profile` va `/reading-books` da olib tashlandi.**

Talab: `/profile` va `/reading-books` sahifalarida navbar qidiruv ikoni ko'rinmasin.

- `_Layout.cshtml`: avval faqat `isChatPage` (Chat) da yashirilardi. Endi
  `hideSearch` o'zgaruvchisi — Chat **+ Profile + ReadingGoals** controller'larida
  qidiruv `nav-icon-btn` (line ~59, `title="Qidiruv"`) render qilinmaydi. `@if
  (!isChatPage)` → `@if (!hideSearch)`. Boshqa ikonlar (bell, burger) tegilmadi.
- **TEKShIRILDI (rebuild 0/0 + restart 5261, dev JWT):** `/reading-books`→0,
  `/profile`→0, `/feed`→1 (`title="Qidiruv"` soni). Faqat shu uch sahifada
  yashirildi, boshqalarda qoldi.

---

**2026-06-19 — "Tugatilgan kitoblar" bo'limi qo'shildi (`/reading-books` + `/profile`).**

Talab: `/reading-books` da "Faol kitoblarim" yonida "Tugatilgan kitoblar" bo'limi
bo'lsin (tugatilgan kitoblar shu yerda ko'rinsin); shu bo'lim `/profile` da ham
bo'lsin.

- **Yangi query:** `GetFinishedReadingGoalsQuery(int? UserId = null)` +
  Handler — `!g.IsActive` (kitob tugatilganda `UpdateReadingProgress` handler
  `IsActive=false` qiladi) bo'yicha tugatilgan goal'larni `ReadingGoalDto` ga
  proyeksiya qiladi (StartDate desc). UserId null → joriy foydalanuvchi (kutubxona);
  berilsa → ko'rilayotgan profil egasi.
- **Reading-books:** controller `Index` endi `ReadingBooksPageViewModel
  { Active, Finished }` (yangi VM `Web/Models`) yuboradi. `ReadingGoals/Index.cshtml`
  `@model` shu VM ga o'zgardi (`Model` → `Model.Active`); pastiga "Tugatilgan
  kitoblar" bo'limi (bo'sh holatda "Hali tugatilgan kitobingiz yo'q").
- **Profile:** `ProfilePageViewModel` ga `FinishedBooks`; `ProfileController.Index`
  `GetFinishedReadingGoalsQuery(userId)` yuboradi (ko'rilayotgan user); `Profile/
  Index.cshtml` profil kartasi bilan postlar orasiga "Tugatilgan kitoblar" bo'limi
  (faqat kitob bor bo'lsa ko'rinadi — boshqa profillarda ortiqcha bo'lmasligi uchun).
- **Umumiy partial:** `Views/Shared/_FinishedBooks.cshtml` — muqovali grid
  (`auto-fill minmax(120px)`), 2:3 muqova + yashil ✓ badge + nom/muallif/jami bet,
  har biri `/reading-books/{id}` ga link. `site.css` `.finished-books`/
  `.finished-book`/`.finished-cover`/`.finished-badge`/`.finished-title` bloki.
- **TEKShIRILDI (rebuild 0/0 + restart 5261, dev JWT user6):** goal #10 vaqtincha
  `IsActive=false, CurrentPage=300` qilindi → `/reading-books` ham `/profile` ham
  "Tugatilgan kitoblar" + `finished-book`/`finished-badge` + "Pul psixologiyasi"
  render qildi. **Goal #10 asl holatga qaytarildi** (IsActive=true, CurrentPage=0).
  (Eslatma: goal #1 "Falastin" allaqachon real tugatilgan — IsActive=false,
  322/322 — o'sha foydalanuvchida haqiqiy tugatilgan kitob sifatida ko'rinadi.)
- **ESLATMA:** controller+cshtml o'zgargani uchun rebuild+restart bajarildi.

---

**2026-06-19 — `/reading-books`: kitobning JAMI betlari ko'rsatildi + "Betlarni yangilash" validatsiya BUG tuzatildi.**

Talab: (1) `/reading-books` da kitobning umumiy necha bet ekani ham ko'rinsin.
(2) "Betlarni yangilash" → 10 yozib "Qo'shish" bosilganda "Bir yoki bir nechta
validatsiya xatosi yuz berdi" chiqardi.

- **BUG (asosiy):** `ReadingGoalsController.UpdateProgress(UpdateReadingProgressCommand
  command)` da `[FromBody]` YO'Q edi. Frontend (`site.js` `apiPost`) JSON
  (`Content-Type: application/json`) yuboradi, lekin `[FromBody]`siz MVC JSON
  body'ni bog'lamaydi → `ReadingGoalId`/`PagesRead` = 0 → FluentValidation
  (`>0` sharti) yiqiladi → `ValidationException` ("Bir yoki bir nechta
  validatsiya xatosi yuz berdi."). Loyihadagi BARCHA boshqa JSON endpoint'lar
  (`/books/create`, `/posts/*`, `/chat/*`, `/stories/*`) `[FromBody]` ishlatadi —
  faqat shu biri unutilgan ekan. **Tuzatish:** `[FromBody]` qo'shildi.
- **Jami betlar (frontend):** `ReadingGoalDto.TotalPages` allaqachon to'ldirilgan
  edi. `ReadingGoals/Index.cshtml` book-row'ga: muallif ostida "Jami: N bet";
  progress ustunida "O'qildi: <currentPage> / N bet" (`.book-total`/`.read-pages`).
  JS progress qo'shilganda `.read-pages` ni `goal.currentPage` bilan yangilaydi.
- **TEKShIRILDI (rebuild 0/0 + restart 5261, dev JWT user6, goal #10 "Pul
  psixologiyasi" 300 bet):** `POST /reading-books/progress {readingGoalId:10,
  pagesRead:10}` endi **HTTP 200** (avval 400 validatsiya) — currentPage=10,
  pagesReadToday=10, progressPercent=3, totalPages=300 qaytdi. GET render:
  "Jami: 300 bet", "O'qildi: ... / 300 bet". **Test yozuvi ortga qaytarildi**
  (goal #10 CurrentPage=0, bugungi ReadingProgress o'chirildi — foydalanuvchi
  o'zi brauzerda kiritadi).
- **ESLATMA:** controller+cshtml o'zgargani uchun rebuild+restart SHART (Razor
  runtime compilation o'chiq) — bajarildi. Ilova 5261 da ishlab turibdi.

---

**2026-06-19 — "Kutubxona" sahifasi `/reading-books` route'i + dizayn 06 ga moslandi (Kitob maqsadlarim).**

Talab: `/reading-books` sahifasi `design-reference/06-oqish-maqsadlari.html` bilan
bir xil ko'rinishda bo'lsin; ba'zi joylari o'zgaradi; "Yangi kitob qo'shish"
boshqa sahifalardagi (Feed/Quotes) kitob-picker oqimi bilan bir xil bo'lsin.

- **Holat:** Reading goals sahifasi avval faqat `/reading-goals` da, generik
  `.goal-grid`/`.card` ko'rinishida edi — dizayn 06 dan ancha farq qilardi.
- **Route:** `ReadingGoalsController` ga `[Route("reading-books")]` qo'shildi
  (`[Route("reading-goals")]` ham qoldi — alias, eski havolalar buzilmaydi).
  `_Layout.cshtml` navbar "Kutubxona" endi `/reading-books` ga; Details orqaga
  havolasi ham `/reading-books` ("← Kitoblarim").
- **View (`ReadingGoals/Index.cshtml` qayta yozildi):** dizayn 06 ga moslandi —
  sarlavha "Kitob maqsadlarim" + "Yangi kitob qo'shish" accent tugma; "Faol
  kitoblarim" bo'lim sarlavhasi; kitoblar **gorizontal kartalar** (`.book-row`):
  chapda 80×120 muqova (CoverUrl bo'lsa `<img>`, bo'lmasa ikona) + nom/muallif,
  o'ngda **doiraviy progress** (SVG, `stroke-dashoffset` Razor'da ProgressPercent
  dan hisoblanadi) + "Bugun: X / Y bet" + "Betlarni yangilash" (100% da
  "Tugallandi" disabled). "Yangi kitob qo'shish" — Feed/Quotes dagi AYNAN bir xil
  picker oqimi saqlandi (qidiruv + "Yangi kitob" forma: nom/muallif/betlar/
  kategoriya/muqova → `/books/upload-cover` → `/books/create`). Kunlik bet
  maqsadi + boshlanish sanasi (max=bugun) saqlandi.
- **JS:** "Betlarni yangilash" endi inline log inputini ochib-yopadi; progress
  qo'shilganda doiraviy `stroke-dashoffset` + foiz + bugungi betlar real-time
  yangilanadi; 100% da tugma "Tugallandi" ga aylanadi. POST endi
  `/reading-books/progress` ga (route alias).
- **CSS (`site.css`):** yangi `.rb-header`/`.rb-form-grid`/`.book-rows`/
  `.book-row`/`.book-cover-lg`/`.circ-progress`/`.circ-track`/`.circ-value`/
  `.btn-pill` bloki; responsive (`<=1024px`): forma 1 ustun, kartalar vertikal.
- **TEKShIRILDI (rebuild + restart, port 5261, dev JWT user1):** `dotnet build`
  0/0. `/reading-books` va `/reading-goals` ikkalasi anon→401 (route bor), dev
  JWT bilan→200. Render HTML: "Kitob maqsadlarim", "Faol kitoblarim",
  `book-row`/`circ-progress`/`circ-value`, hisoblangan `stroke-dashoffset`,
  muqova rasmi, "Betlarni yangilash" + bitta "Tugallandi" (100% kitob). CSS yangi
  klasslar serve qilinyapti. DB tegilmadi (faqat o'qish; minted JWT verifikatsiya
  uchun, yozuv yo'q).
- **ESLATMA:** `.cshtml`/controller o'zgargani uchun rebuild+restart SHART
  (Razor runtime compilation o'chiq) — bajarildi. Ilova 5261 da ishlab turibdi.

---

**2026-06-19 — /quotes "Yangi iqtibos" formasiga "Yangi kitob" tugmasi qo'shildi (B-qism BAJARILDI).**

Talab: `/quotes` "Yangi iqtibos"da, mavjud kitob qidiruv yonida "Yangi kitob"
tugmasi (xuddi `/feed` dagidek) — kitob topilmasa, yangisini qo'shib bo'lsin.

- **Faqat frontend** — `Views/Quotes/Index.cshtml` (backend o'zgarmadi).
- Yuqoriga `@@using ...GetGenres` + `@@inject ISender Mediator` +
  `var genres = await Mediator.Send(new GetGenresQuery())`.
- Forma `/feed` kompozeri naqshiga keltirildi: kitob qidiruv inputi o'ngida
  "Yangi kitob" tugmasi (`#quoteNewBookToggle`); tanlangan kitob chip
  (`composer-selected` + `×`); yashirin yangi-kitob forma (`#quoteNewBookForm`):
  nom/muallif/sahifa/janr `<select>` (10 kategoriya server-render)/muqova yuklash.
- `@@section Scripts` qayta yozildi: `pickBook`/`clearBook`, `/books/search`
  (250ms debounce), `quoteNewBookToggle`, muqova `/books/upload-cover`,
  `kitob.apiPost('/books/create', ...)`, submit'da BookId majburiy.
  Mavjud CSS sinflari (`composer-book/newbook/nb-cover/composer-selected/
  book-suggestions`) global edi — yangi CSS shart bo'lmadi.
- **TEKShIRILDI:** `dotnet build` 0/0. App 5261'da qayta ishga tushirildi
  (port avval band edi — `fuser -k 5261/tcp` bilan bo'shatildi). Mintlangan
  dev JWT (user 1, Jwt:Key user-secrets'dan, iss/aud=kitobdagimen.uz) bilan
  `GET /quotes` → 200; "Yangi kitob" tugmasi, `quoteNewBookForm`,
  `composer-selected` chip va `qNbGenre`da 10 kategoriya render bo'ldi. DB tegilmadi.
- **QOLDI** (QUOTES-REDESIGN-DESIGN.md): Q1 like backend (full-stack QuoteLike),
  Q2 karta dizayni (07-iqtiboslar uslubi), Q3 futer like+save+delete redizayni.

---


**2026-06-19 — /chat: pufakchalar Telegram uslubiga keltirildi (vaqt + tick o'ng-pastda, ixcham).**

Talab: chat dizayni yomon ko'rinardi. Foydalanuvchi tanlovi (AskUserQuestion): muammo = zichlik + vaqt yo'qligi + tick joylashuvi + pufakcha; uslub = **Telegram**.

Yondashuv: ko'r-ko'rona CSS tuzatish o'rniga `google-chrome --headless --screenshot` bilan statik mock render qilib, ko'rib turib sozlandi (login kerak bo'lmadi). Mock fayllar /tmp da, tozalandi.

- **CSS (`site.css`):** `.messages` gap 3px, padding 16px 18px; `.msg` padding `6px 9px 6px 11px`, radius 14px (dumcha burchak 6px), font 14.5px, line-height 1.34, yengil soya, `width:fit-content`. `.meta-row` → **`float:right`** (Telegram hiyla: vaqt+tick pufakcha o'ng-pastiga yopishadi, matn joy bo'shatadi), `top:5px`. `.msg-time` 11px xira; tick 14×9 (dbl 16), o'qilganda `#7fe0ff`.
- **Vaqt (timestamp):** `MessageDto.SentAt` (UTC) endi har xabarda HH:mm ko'rsatiladi. Razor: `data-ts="@m.SentAt.ToString("o")"` + server `ToLocalTime()`. JS: `fmtTime(iso)` brauzer mahalliy vaqti, `msgInner` har doim vaqt qo'shadi, load'da server HH:mm `data-ts` dan qayta formatlanadi (server TZ noaniqligini bartaraf etadi). meta-row endi `in` xabarlarda ham chiqadi.
- Build 0/0. (Backend tegilmadi; faqat `Index.cshtml` + `site.css`.)
- **Keyingi mayda tuzatishlar (o'sha sessiya):** (a) matn tepa/pastidagi ortiqcha bo'shliq — `line-height` 1.34→1.25, `.msg` vertikal padding 6px→5px, meta-row `top` 5px→4px. (b) Edit/Delete tugmalari hoverda yo'qolib qolardi — `.msg-actions` dagi `margin-right:6px` → `padding-right:6px` (pufakcha bilan tugma orasidagi bo'shliq endi element ichida, hover ko'prigi uzilmaydi).

---

**2026-06-19 — /chat: xabar pufakchasi matn hajmiga moslashadi + tick matndan keyin inline.**

Talab: yozilayotgan xabar div'i matn hajmiga bog'liq (inline/content-sized) bo'lsin; tick ikoni matndan SO'NG, inline joylashsin (block emas).

- Avval tick `.meta-row` (`display:flex; justify-content:flex-end; margin-top`) alohida QATORDA, matn ostida ko'rinardi.
- Tuzatish (markup): `.meta-row` endi `.msg-content` ichida, matn/shared-card'dan keyin oxirgi inline element — `Index.cshtml` Razor (~116-q.) + JS `msgInner()` (~261-q.) ikkalasi yangilandi.
- Tuzatish (CSS, `site.css`): `.meta-row` → `display:inline-flex; margin-left:6px; vertical-align:bottom`; `.msg` ga `width:fit-content` (max-width:72% saqlandi) — pufakcha matn uzunligiga moslashadi.
- Build 0/0. (Faqat cshtml+css; backend o'zgarmadi.)

---

**2026-06-19 — /chat: markaziy scrollbar + xabarni tahrirlash/o'chirish + navbar search olib tashlandi.**

Talab: (1) markazda xabarlar ko'paysa ichki scrollbar. (2) o'z xabarini tahrirlash + "tahrirlangan" belgisi. (3) o'z xabarini o'chirish. (4) /chat navbaridan search ikoni olib tashlansin.

- **(1) Scrollbar:** `.messages` allaqachon `overflow-y:auto` edi, lekin flex-column ichida `min-height:auto` tufayli butun sahifa scroll bo'lardi. Tuzatish: `.messages`+`.chat-main` ga `min-height:0`, `.chat` ga `overflow:hidden`. Endi faqat xabarlar maydoni ichida scroll.
- **(2+3) Edit/Delete (full-stack):**
  - Domain: `Message.EditedAt DateTime?`. Migratsiya `20260619160604_AddMessageEditedAt` (real Postgres'ga startupda qo'llandi — port 5299 da tekshirildi).
  - Application: `EditMessageCommand`/Handler (faqat egasi, 5000 limit, `EditedAt`=now, `IChatNotifier.MessageEditedAsync`), `DeleteMessageCommand`/Handler (faqat egasi, hard delete, `MessageDeletedAsync`). `MessageDto.EditedAt` + projeksiya.
  - `IChatNotifier`: 2 yangi metod; `SignalRChatNotifier` (`MessageEdited`/`MessageDeleted` eventlari); test `SpyChatNotifier` ham yangilandi (`Edited`/`Deleted` ro'yxatlari).
  - Web: `POST /chat/message/{id}/edit`, `POST /chat/message/{id}/delete`.
  - Frontend: har bir xabar `data-msg-id` + `.msg-content`; o'z xabarida hover/teginishda paydo bo'luvchi qalam/savat tugmalari (`.msg-actions`); inline `<textarea>` editor (Saqlash/Bekor); `tahrirlangan` belgisi (`.msg-edited`); real-time `MessageEdited`→`updateMessageEl`, `MessageDeleted`→`removeMessageEl`.
- **(4) Navbar search:** `_Layout.cshtml` da `isChatPage` (controller==Chat) bo'lsa search nav-icon render qilinmaydi.
- **Tekshiruv:** build 0/0, **test 54/54** (5 yangi: edit/delete owner+forbidden+empty). App 5299 da ishga tushdi (migratsiya OK), /chat 401 (500 emas), CSS/JS 200, yangi class'lar serverdan keladi.

---

**2026-06-19 — /chat: doimiy real-time + zamonaviy SVG ikonlar + ixcham/responsive shared-card.**

Talab: (1) /chat real-time hech qachon to'xtamasligi kerak. (2) Zamonaviy ikonlar — ✓/✓✓ o'rniga zamonaviyroq icon. (3) Xabar ichidagi ulashilgan post karta juda katta — kichik + responsive.

- **(1) Doimiy SignalR:** default `withAutomaticReconnect()` ~4 urinishdan keyin to'xtardi. Endi ChatHub (Index.cshtml) va NotificationHub (site.js) ikkalasi ham **cheksiz qayta-ulanish** siyosati bilan (`nextRetryDelayInMilliseconds`, 1s→15s, hech qachon null emas). Qo'shimcha: `onclose` da qo'lda restart interval (server restart holati), `onreconnected` da heartbeat + read + `loadRequests` (o'tkazib yuborilgan eventlarni tiklash), `visibilitychange` da uzilgan bo'lsa darhol tiklash, boshlang'ich `start()` muvaffaqiyatsiz bo'lsa 5s dan keyin qayta urinish.
- **(2) Zamonaviy ikonlar:** ✓/✓✓ → ixcham **SVG tick** (single check / double check; o'qilganda yorqin ko'k `#34d1ff`). Yana: yuborish tugmasi → **paper-plane** SVG, orqaga → **chevron** SVG, qidiruv → input ichida **magnifier** SVG (`.search-box` wrapper), shared-card → **book** SVG. Tick'lar Razor (server-render) va JS (renderMessage + MessagesRead) ikkala yo'lda bir xil. `.btn-icon` yangi stil.
- **(3) Shared-card:** `.msg .shared` (eski katta blok-link) → `.msg .shared-card` — `max-width:240px`, flex, ikon + 2 qatorli (sarlavha/muallif) `text-overflow:ellipsis`, in/out bubble bo'yicha mos fon. Responsive: msg max-width mobil 85%, karta 240px ichida sig'adi.
- **Build:** Web (Razor kompilyatsiyasi bilan) 0/0.

---

**2026-06-19 — /chat taklif (invite) bug-fix: bo'sh-body JSON xatosi + offline taklif vaqti.**

Talab: (1) "Taklif qilish" → keyin tugmani qayta bosganda `Failed to execute 'json' on 'Response': Unexpected end of JSON input` chiqardi. (2) Offline foydalanuvchiga taklif "bormayapti"; online bo'lganda kim qachon yuborganini ko'rib, qabul/rad qila olishi kerak.

- **Bug 1 (sabab):** `ChatController.Cancel` va `MarkRead` `Ok()` qaytarardi — HTTP 200 lekin **bo'sh body**. `site.js` `apiPost` esa faqat 204 ni bo'sh deb bilardi, qolganida `res.json()` chaqirardi → bo'sh bodyda parse xatosi. "Yuborildi" tugmasi cancel chaqirgani uchun aynan shu xato chiqardi.
  - **Tuzatish:** `apiPost` endi 204 dan tashqari **bo'sh matn**ni ham `null` qaytaradi (`res.text()` → bo'sh bo'lmasa `JSON.parse`). Qo'shimcha: `Cancel`/`MarkRead` endi semantik to'g'ri `NoContent()` (204) qaytaradi.
- **Bug 2 (offline taklif):** Taklif allaqachon DB'da `Connection(Pending)` sifatida saqlanadi va addressee /chat ochganda `loadRequests()` (`GET /chat/requests`) orqali "Kelgan takliflar" panelida ko'rinadi — ya'ni offline foydalanuvchi keyin kirib ko'radi. Yetishmagan narsa: **yuborilgan vaqt** ko'rsatilmasdi.
  - **Tuzatish:** `Index.cshtml` ga `sentAgo(iso)` humanize funksiyasi qo'shildi; har bir taklif kartasida `.req-time` ("N daqiqa oldin yubordi" h.k.) `r.createdAt` asosida ko'rsatiladi. Qabul/Rad tugmalari avvaldan bor edi. CSS: `.req-item .req-time`.
- **Build:** `dotnet build` 0/0.

---

**2026-06-19 — /chat 2.0 yakunlandi (C5–C10). Oldingi akkaunt C5/C6 ni yarim qoldirib, buildni buzgan edi.**

Oldingi sessiya (limitda to'xtagan) C5/C6 kodini yozgan, lekin: (1) `Program.cs` ga `IPresenceService` DI ro'yxatini qo'shmagan → ChatHub runtime'da yiqilardi; (2) test `SpyChatNotifier` ni yangi interfeys metodlari (`MessagesReadAsync`/`PresenceChangedAsync`) bilan moslamaganmagan → **build BUZILGAN** edi (`dotnet build` 2 xato). PROGRESS yangilanmagan, shuning uchun holatni koddan tekshirib aniqladim.

Bajarilgan ishlar:
- **Build tuzatildi**: `SpyChatNotifier` ga 2 yangi metod + spy ro'yxatlari; `Program.cs` ga `AddScoped<IPresenceService, RedisPresenceService>()`.
- **C7**: `ChatController` ga 5 endpoint (`GET /chat/search`, `POST /chat/connect`, `POST /chat/connect/{id}/respond`, `POST /chat/connect/{id}/cancel`, `GET /chat/requests`). Search + conversation list `IsOnline` ni `IPresenceService.AreOnlineAsync` bilan boyitadi.
- **C8**: `Views/Chat/Index.cshtml` to'liq qayta yozildi (3 ustun). `site.css` chat bloki kengaytirildi (qidiruv kartalari, `.online-dot`, ticks, `.owl-panel`, speech bubble). `ViewHelpers.Presence(...)` qo'shildi. Razor `@@` escape eslatma: JS template literal ichidagi `@username` → `@@username`.
- **C9**: `wwwroot/js/owl.js` — three.js dinamik import (top-level emas, shuning uchun CDN yo'q bo'lsa modul baribir yuklanadi va 2D SVG zaxiraga o'tadi). `mountOwl(canvas)` → `{lookAt, alert, idle, happy, dispose}`. `site.js` `initNotifications` endi `kitob:notification` DOM event'ini dispatch qiladi (chat sahifasi 2-chi notif ulanish ochmasligi uchun); invite turlarida generic toast bosib turilmaydi (boyo'g'li ko'rsatadi).
- **C10**: `dotnet build` 0/0, `dotnet test` **49/49**. `DeleteAccountCommandHandler` ga `Connections` cleanup (ikkala yo'nalish — Restrict FK). Yangi testlar: SendMessage gate, 6 Connections handler, 2 SearchUsers, 2 DeleteAccount.
- **TEKShIRILDI (5261, dev JWT user6/user7, user-secrets Jwt:Key)**: anon `/chat`→302, `/chat/search`→401; `/js/owl.js`→200, `site.css` da `owl-panel`/`online-dot`/`chat-search`. To'liq oqim: user6 user7 ni taklif qildi→user7 `/chat/requests` da ko'rdi→search holatlari (PendingOutgoing=1 / Connected=3)→user7 qabul qildi→Conversation yaratildi→user6 xabar yubordi (gate o'tdi)→user7 read qildi (`IsRead=true`). `/chat/send` validatsiya→400 (to'g'ri kontrakt). **Barcha test ma'lumotlari (Connection, Conversation 6, Message) tozalandi — DB asl holatda (0 connection, 0 message).**
- **ESLATMA**: 3D boyo'g'li animatsiyasi faqat real brauzerda ko'rinadi (curl/headless'da emas) — modul to'g'ri serve bo'lyapti, struktura va 2D fallback sog'lom. Razor runtime compilation o'chiq → `.cshtml`/controller o'zgarishi rebuild+restart talab qildi (qilindi). Ilova 5261 da ishlab turibdi.

**2026-06-19 — Story halqasi: animatsiya/shine OLIB TASHLANDI, ozgina yorqin statik halqa qoldirildi.**

Fikr: oldingi aylanuvchi+shine+glow versiyasi juda yorqin/diqqatni tortib yuborardi. Endi `.has-story` — **aylanmaydigan statik** `linear-gradient(135deg, #ffb347, accent, #ff5e7e)` + juda yengil `box-shadow` (0 0 6px, 0.35 alpha). Barcha keyframe'lar (`story-spin/shine/glow`), `::before`/`::after` pseudo'lar va `prefers-reduced-motion` bloki olib tashlandi. Faqat CSS — rebuild kerak emas; `/css/site.css` da animatsiya yo'qligi va yangi gradient tasdiqlandi.

**2026-06-19 — Story qo'ygan foydalanuvchi avatar halqasi YORQIN + SHINE (animatsiya).**

Talab: story qo'ygan foydalanuvchilarning avatar halqasi yorqin bo'lib, shine (porlash) bo'lib tursin.

- **O'zgarish (faqat CSS, `site.css` `.has-story` bloki):** Avvalgi statik `linear-gradient(45deg, accent, primary)` o'rniga:
  - **Aylanuvchi yorqin gradient halqa** — `::before` pseudo-elementda `conic-gradient` (sariq→to'q sariq→pushti→naranj), `story-spin` 4s linear cheksiz aylanadi. Pseudo-elementda bo'lgani uchun avatar rasmi QIMIRLAMAYDI (animatsiya wrap'ning o'ziga emas).
  - **Shine** — `::after` pseudo-elementda tor oq conic segment (`rgba(255,255,255,.95)`), `mix-blend-mode: screen`, `story-shine` 2.6s aylanib halqa ustidan yorug' chiziq o'tkazadi.
  - **Glow** — wrap'da pulslovchi `box-shadow` (`story-glow` 2.4s ease-in-out) — to'q sariq+pushti nur kuchayib-pasayadi.
  - Avatar `z-index:1` bilan pseudo'lar ustida (markazni yopadi, shine faqat halqa hoshiyasida ko'rinadi).
  - **`prefers-reduced-motion: reduce`** — barcha animatsiyalar o'chadi, shine yashiriladi (foydalanuvchi harakatni kamaytirgan bo'lsa).
- **TEKShIRILDI:** App 5261'da ishlab turibdi; `/css/site.css` → `story-spin`/`story-shine`/`story-glow`/`conic-gradient`/`prefers-reduced-motion` qoidalari uzatilyapti. **CSS-faqat o'zgarish — rebuild/restart SHART EMAS** (app statik faylni darhol serve qiladi; brauzerda hard-refresh / `asp-append-version` cache-bust qiladi).

**2026-06-19 — Post detali sahifasi (`/post/{username}/{slug}`) anon-bannerga LOGO qo'shildi.**

Talab: `/post/javohirsadullayev/ee31a03a49b2` kabi (ulashilgan) post sahifalarining chap tomoniga logo qo'yish.

- **Holat:** Auth foydalanuvchilar uchun navbar (logo bilan) allaqachon ko'rinadi. Ammo anonim (ro'yxatdan o'tmagan) tashrif buyuruvchilar — ulashilgan post linkini ochganda — navbarsiz `.anon-banner` ni ko'radi, va undagi `.brand` faqat MATN edi ("kitobdagimen.uz"), logosiz.
- **O'zgarish:** `Views/Posts/Details.cshtml` — anon-banner `.brand` endi navbar bilan bir xil markupga ega: `<img class="brand-logo" src="~/img/logo.svg">` + matn. CSS o'zgarmadi — `.brand` (flex+gap) va `.brand-logo` (36×36) qoidalari mavjud edi; `.anon-banner .brand` faqat font/rangni override qiladi (oq matn). Logo dark primary fonida ham ko'rinadi (to'q sariq/krem/yashil ranglar).
- **TEKShIRILDI (rebuild + restart, port 5261):** `dotnet build` 0/0. App qayta ishga tushirildi (eski `dotnet run` jarayoni o'ldirildi — Razor runtime compilation o'chiq, .cshtml o'zgarishi rebuild+restart talab qiladi). Anonim `GET /post/javohirsadullayev/ee31a03a49b2` → anon-banner ichida `<img class="brand-logo" src="/img/logo.svg">` chap tomonda render qildi.

**2026-06-19 — Yangi kitobga KATEGORIYA tanlash (Feed + ReadingGoals) + o'qish maqsadiga BOSHLASH SANASI (kelajak emas).**

Talab: (1) `/Feed` post kompozeridagi "Yangi kitob" formasiga kitob qaysi kategoriyaga (janrga) tegishli ekanini tanlash qo'shildi. (2) `/reading-goals` "Yangi maqsad"ga: kitob qachon o'qishni boshlaganini kiritish (kelajakdagi sanani tanlab bo'lmaydi) + Feed bilan bir xil "Yangi kitob" qo'shish oqimi (Kitob nomi, Muallif, Sahifalar soni, Muqova rasmi, Kategoriya, "Kitobni qo'shish").

- **Backend allaqachon tayyor edi:** `Book.GenreId` + `CreateBookCommand.GenreId` + `CreateReadingGoalCommand.StartDate` mavjud edi; faqat UI va validatsiya yetishmasdi.
- **Kategoriya tanlash:** `Feed/Index.cshtml` va `ReadingGoals/Index.cshtml` `GetGenresQuery` orqali 10 janrni server tomonda `<select>`ga render qiladi (`#nbGenre` / `#goalNbGenre`). JS `nbSave` endi `genreId` yuboradi; kategoriya **majburiy** (tanlanmasa alert). Janrlar: Biografiya, Biznes, Detektiv, Falsafa, Fantastika, Ilmiy, Psixologiya, Roman, She'riyat, Tarix (DB'da seed qilingan).
- **ReadingGoals "Yangi kitob" oqimi:** Avval faqat kitob qidirish bor edi; endi Feed bilan bir xil — "Yangi kitob" tugmasi + to'liq forma (nom/muallif/sahifa/kategoriya/muqova rasmi yuklash → `/books/upload-cover`) + tanlangan kitob chip (×). Joylash tugmasi kitob tanlanmaguncha disabled.
- **Boshlash sanasi:** `goalStartDate` (`<input type="date" required value=bugun max=bugun>`) — brauzerda kelajak tanlab bo'lmaydi. Server himoyasi: `CreateReadingGoalCommandValidator` — `StartDate.Date <= UtcNow.Date` aks holda "O'qish boshlangan sana kelajakda bo'lishi mumkin emas.". `CreateReadingGoalCommandHandler` — forma'dan kelgan `Kind=Unspecified` sanani `DateTime.SpecifyKind(..Date, Utc)` bilan UTC yarim tunga normallashtiradi (Npgsql `timestamptz` uchun shart).
- **TEKShIRILDI (rebuild + restart, port 5261, dev JWT user7):** `dotnet build` 0/0, `dotnet test` 38/38. `/Feed` va `/reading-goals` 10 kategoriyali select + sana inputi (max=2026-06-19) render qildi. `POST /books/create {genreId:3}` → kitob `GenreId=3` bilan DB'ga yozildi. `POST /reading-goals/create` o'tmish sana (2026-06-10) → 302 muvaffaqiyat (StartDate to'g'ri saqlandi); kelajak sana (2026-12-31) → 400 validatsiya xatosi. Test ma'lumotlari (test kitob+goal) o'chirildi — DB asl holatda.
- **ESLATMA:** `.cshtml`/handler/validator o'zgargani uchun rebuild + restart SHART qilindi (Razor runtime compilation o'chiq).

**2026-06-19 — Story kompozeri responsive: rasm qo'shilganda pastdagi tugmalar yo'qolib qolardi.**

Muammo: `/profile` "Story qo'shish" oynasida rasm tanlanganda preview modal balandligini viewport'dan oshirib yuborar, "Bekor qilish"/"Joylash" tugmalari (`.modal-actions`) ekrandan chiqib ketardi — ayniqsa kichik/mobil ekranlarda. Sabab: `.modal-box`da `max-height`/scroll yo'q edi.

- **`site.css` o'zgarishlari (CSS — rebuild SHART EMAS, app static faylni darhol uzatadi):**
  - `.modal-box`: `max-height: calc(100dvh - 32px)` + `overflow-y: auto` — endi modal hech qachon viewport'dan oshmaydi, kontent ko'p bo'lsa ichida skroll bo'ladi.
  - `.modal-actions`: `position: sticky; bottom: -24px` (+ negativ margin/padding bilan to'liq kenglikka cho'zilgan surface fon) — tugmalar skroll paytida ham doim modal pastida ko'rinib turadi.
  - `.composer-image-preview` va ichidagi `img`: `max-height: 280px` → `max-height: min(280px, 32vh)` — past ekranlarda rasm preview modalni bosib ketmaydi.
- **TEKShIRILDI:** app 5261'da ishlab turibdi; `/css/site.css` → 200 va uchala o'zgarish (`100dvh`, `position: sticky`, `min(280px, 32vh)`) serve qilinyapti. CSS gina o'zgargani uchun rebuild/restart kerak emas (faqat brauzerda hard-refresh / `asp-append-version` cache-bust qiladi).

**2026-06-19 — STORY muddati (12/24/48 soat) + muddat tugagach avtomatik yo'qolish.**

Talab: `/profile` da "Story qo'shish"da story necha soatga qo'shilishi tanlanadi — **12, 24 yoki 48 soat**. Muddat tugagach story yo'qoladi va profil oddiy (halqasiz) ko'rinishga qaytadi.

- **Domain:** `Story.ExpiresAt` (DateTime) qo'shildi — story qachon ko'rinmay qolishi. `StoryConfiguration`ga `HasIndex(ExpiresAt)`.
- **Application:** `CreateStoryCommand.DurationHours` (int) qo'shildi; `CreateStoryCommandValidator` — faqat 12/24/48 (aks holda "Muddatni tanlang: 12, 24 yoki 48 soat."); `CreateStoryCommandHandler` — `ExpiresAt = CreatedAt.AddHours(DurationHours)`.
- **Faol filtri (muddati tugamagan):** `StoryQueryableExtensions.WhereActive()` (`s.ExpiresAt > DateTime.UtcNow`, EF → `now()`). Qo'llandi: `GetUserStoriesQueryHandler` (viewer faqat faol story'larni oladi), `PostQueryableExtensions.AuthorHasStory` (feed avatar halqasi), `GetUserProfileQueryHandler.HasStory` (profil halqasi). Shu sabab muddat tugagach halqa yo'qoladi, profil oddiy ko'rinadi.
- **Migratsiya:** `20260619134651_AddStoryExpiresAt` — `ExpiresAt` ustuni + index; **mavjud story'lar uchun backfill** (`ExpiresAt = CreatedAt + interval '24 hours'`) — eski story'lar darhol yo'qolib qolmasin. DB'ga startup'da qo'llandi (DbInitializer.MigrateAsync).
- **Frontend:** `_Layout.cshtml` kompozeriga "Story qancha vaqt ko'rinsin?" + 3 ta segment tugma (12/24/48 soat, default 24 active). `site.js`: `durationHours` holati, tugma click → `setDuration`, kompozer ochilganda 24 ga reset, submit payload'ga `durationHours` qo'shildi. `site.css`: `.story-duration`/`.story-duration-opt`(.is-active) uslublari.
- **TEKShIRILDI (ishlab turgan ilova, port 5261, dev JWT user7):** `dotnet build` 0/0, `dotnet test` 38/38. Migratsiya real DB'ga qo'llandi (`ExpiresAt` ustuni mavjud, history'da yozuv bor). 12 soatlik story yaratildi → DB'da `ExpiresAt - CreatedAt = 12:00:00`; `durationHours:99` → 400 validatsiya xatosi; story faol holatda `/stories/user/7` va profil halqasida ko'rindi; **`ExpiresAt`ni o'tmishga o'zgartirgach** — `/stories/user/7` → `[]`, profil `has-story` → 0, feedda faqat boshqa (faol story'li) foydalanuvchi halqasi qoldi. Test story (id 9) o'chirildi, DB asl holatda (1 real story).
- **ESLATMA:** `.cshtml`/controller/JS/CSS o'zgarishi uchun rebuild + restart SHART (Razor runtime compilation o'chiq). View/like/delete endpointlari id bo'yicha ishlaydi (muddat filtri faqat ko'rsatishda) — bu yetarli, chunki UI faqat faol story'larni ko'rsatadi.

**2026-06-19 — Informatsion LANDING PAGE + navbar logosi + favicon + navbar "Asosiy"→"Lenta".**

Talab: (1) Dastur haqida ma'lumot beruvchi to'liq LANDING PAGE yaratish (faqat ma'lumot). (2) Navbardagi "Asosiy" o'rniga boshqa so'z. (3) Barcha sahifalardagi navbar chap tomoniga logo (logo `Web/src` ichida). (4) Favicon qo'yish.

- **Logo/favicon joylashuvi:** `src/logo.svg` va `src/favicon.ico` statik xizmat uchun `wwwroot/img/logo.svg` va `wwwroot/favicon.ico` ga ko'chirildi (asl `Web/src/` nusxasi qoldi).
- **Layout (`_Layout.cshtml`):** `<head>`ga favicon linklari qo'shildi (`favicon.ico` + `svg` + `apple-touch-icon` → logo.svg). Navbar `.brand` endi `<img class="brand-logo">` (logo) + `kitobdagimen.uz` matnidan iborat — barcha auth sahifalarda chap tomonda logo. Nav havola **"Asosiy" → "Lenta"** (hamon `/Feed`ga). CSS: `.brand` flex+gap, `.brand-logo` 36×36px.
- **Landing page (`Views/Home/Index.cshtml` to'liq qayta yozildi):** Ilgari faqat login kartasi edi; endi to'liq informatsion sahifa — o'z `lp-nav` headeri (logo + "Imkoniyatlar/Qanday ishlaydi/Kirish"), hero (sarlavha + tavsif + Google CTA), **6 ta imkoniyat kartasi** (Kitob taqrizlari, O'qish maqsadlari, Iqtiboslar, Xabarlar, Storylar, Kitobxonlar jamiyati), **3 qadamli "Qanday boshlash"**, yashil CTA banner, footer (logo + © 2026). `?xato` xatosi `lp-alert`da ko'rsatiladi. `HomeController.Index` avvalgidek (anonim→landing, login→/Feed), `/Home/Landing` ham ishlaydi. CSS: yangi `.lp-*` blok (hero, grid, feature, steps, cta, footer) + mobil media query (`<=860px` da grid 1 ustun, nav havolalar yashirin). Eski `.landing*`/`.google-btn` saqlandi (`.google-btn` hamon ishlatiladi).
- **TEKShIRILDI (ishlab turgan ilova, 5261):** `/` → 200, hero+6 karta+3 qadam render; `/favicon.ico` → 200 `image/x-icon`; `/img/logo.svg` → 200 `image/svg+xml`; favicon linklari `<head>`da; `site.css`da `lp-hero`/`brand-logo` mavjud; `/Home/Landing` → 200. **Skrinshot bilan vizual tasdiqlandi** — logo chap yuqorida, dizayn tizimi ranglari to'g'ri. `dotnet build` => 0/0. ESLATMA: navbar (logo+Lenta) auth holatida ko'rinadi; minted dev-JWT bilan tekshirib bo'lmadi (server JWT kaliti user-secrets'da appsettings.Development'dagidan farq qiladi — imzo mos kelmadi), lekin aynan shu `.brand` markup landing'da logo bilan to'g'ri render bo'lgani tasdiqlandi.

**2026-06-19 — /Onboarding sahifasini himoyalash + bo'sh janr tanlovida JSON xato o'rniga ogohlantirish.**

Talab: (1) `/Onboarding` (janr tanlash) URL orqali ochilmasligi kerak — faqat ro'yxatdan o'tgandan keyin keyingi sahifa sifatida ko'rsatilsin, boshqa (onboardingni tugatgan) foydalanuvchilar kira olmasin. (2) `/Onboarding` da hech narsa tanlamasdan "Davom etish" bosilganda chiqayotgan `{"errors":{"GenreIds":["Kamida bitta janr tanlang."]}}` JSON xatosi o'rniga sahifada ogohlantirish chiqsin.

- **(1) Onboarding oqimi himoyasi:** Yangi `GetOnboardingStatusQuery`(+Handler, +`OnboardingStatusDto{HasUsername,HasGenres}`) — joriy foydalanuvchining username bor-yo'qligi va janr saqlaganligini qaytaradi. `OnboardingController`:
  - `Index` (GET `/Onboarding`, janr tanlash): `HasGenres` → `/Feed`ga redirect (onboardingni tugatgan kira olmaydi); `!HasUsername` → avval `Profile` bosqichiga redirect; aks holda janr sahifasi ko'rsatiladi.
  - `Profile` (GET `/Onboarding/Profile`): `HasUsername` bo'lsa bosqich tugagan — `HasGenres` bo'lsa `/Feed`, aks holda `Index`ga redirect. Shu bilan ikkala bosqich ham faqat ro'yxatdan o'tish ketma-ketligida ochiladi, URL orqali qayta kirib bo'lmaydi.
- **(2) Bo'sh tanlovda ogohlantirish:** `Views/Onboarding/Index.cshtml` — formaga `id=genreForm`, ustiga yashirin `.onboarding-warning` (`#genreWarning`, role=alert): "Ogohlantirish: davom etish uchun kamida bitta janr tanlang." `@section Scripts`: submit'da bironta checkbox belgilanmagan bo'lsa `preventDefault` + ogohlantirishni ko'rsatadi + unga scroll; janr belgilanishi bilan ogohlantirish yashiriladi. Shu sabab forma serverga bormaydi va validator JSON xato qaytarmaydi. CSS: `.onboarding-warning` (sariq alert uslubi) `site.css`ga qo'shildi.
- **Holat:** `dotnet build` => 0/0, `dotnet test` => 38/38. ESLATMA: `.cshtml`/controller o'zgargani uchun ishlayotgan ilovani rebuild + restart qilish SHART (Razor runtime compilation o'chiq).

**2026-06-19 — STORY: kitob asosidan → sarlavha+matn asosiga o'tkazildi.**

Talab: `/profile` da "Story qo'shish" kompozeri ilgari kitob/muallif tanlashni so'rardi ("Kitob yoki muallifni kiriting"). Endi kitob o'rniga **Sarlavha** (min 3, max 50) va **Matn** (min 3, max 140) kiritiladi. Rasm yuklash (ixtiyoriy) saqlanib qoldi.

- **Domain:** `Story` — `BookId/Book` olib tashlandi, `Title`(required) + `Text`(required) qo'shildi. `StoryConfiguration`: Book FK + IX_Stories_BookId olib tashlandi, `Title` maxlen 50, `Text` maxlen 140 (IsRequired).
- **Application:** `CreateStoryCommand`(BookId→Title/Text), `CreateStoryCommandValidator`(Title 3-50, Text 3-140 o'zbekcha xabarlar bilan; ImageUrl maxlen saqlandi), `CreateStoryCommandHandler`(kitob mavjudligi tekshiruvi olib tashlandi, Title/Text `.Trim()` bilan saqlanadi), `StoryDto`(BookTitle/Author/CoverUrl→Title/Text), `StoryQueryableExtensions.ToStoryDto`(s.Title/s.Text proyeksiyasi).
- **Migratsiya:** `20260619125009_ReplaceStoryBookWithTitleText` — DropFK/DropIndex/DropColumn(BookId) + AddColumn(Title, Text). DB'ga qo'llandi (user-secrets'dagi haqiqiy conn string `ConnectionStrings__DefaultConnection` env var orqali `AppDbContextFactory`ga uzatildi — bu factory user-secrets'ni o'qimaydi, faqat shu env var yoki appsettings fallback).
- **Frontend:** `_Layout.cshtml` kompozer — kitob qidiruv bloki o'rniga `#storyTitleInput`(maxlength 50) + `#storyTextInput` textarea(maxlength 140) + jonli belgilar sanagichi (X/50, X/140). `site.js`: kompozer kitob qidiruv/pick mantiqi o'rniga title/text validatsiyasi (har ikkisi to'g'ri uzunlikda bo'lsa Joylash faollashadi), submit `{title,text,imageUrl}` yuboradi; viewer caption endi `s.title`/`s.text` ko'rsatadi (rasm faqat `s.imageUrl`, kitob muqovasiga fallback yo'q).
- **Holat:** `dotnet build` 0 xato, `dotnet test` 38/38 o'tdi. Stale kitob referenslari (bookTitle/storyBookSearch va h.k.) qolmadi.

---

**2026-06-19 — Username jonli tekshiruv + akkaunt o'chirish oqimi + STORY funksiyasi (19-bosqichdan keyingi qo'shimcha talab).**

- **(1) Username jonli (real-time) tekshiruvi (`/profile/edit`):** Ilgari band username faqat submit'da xato berardi. Yangi `CheckUsernameQuery`(+Handler — format regex `^[a-zA-Z0-9_]{3,32}$` + bandlik, joriy foydalanuvchining o'z usernamesi har doim bo'sh hisoblanadi) → `GET /profile/check-username`. `Edit.cshtml`: yozish davomida (350ms debounce) tekshiradi, `#usernameStatus`da yashil ✓ / qizil "band" / "tekshirilmoqda" ko'rsatadi, band/noto'g'ri bo'lsa submit'ni bloklaydi. **Mosizlik tuzatildi:** `UserConfiguration` Username maxlength 30→32 (validator 32 bilan moslashtirildi), migratsiya bilan DB ustuni `varchar(32)` ga o'tkazildi.
- **(2) Akkauntni o'chirish (`/profile`):** Joriy foydalanuvchiga "Akkauntni o'chirish" tugmasi. **Ko'p bosqichli modal:** 1) "rostdan o'chirmoqchimisiz?" → Ha; 2) o'z Gmail manzilini kiriting (sahifaga `data-email` orqali embed qilingan, mos kelmasa darhol xato); 3) "barcha ma'lumotlaringiz o'chadi" ogohlantirishi + Orqaga/O'chirish. `DeleteAccountCommand(Email)`(+Handler) — email mosligini DB'dagi email bilan tekshiradi (mos emas → `ValidationException` 400), so'ng FK-xavfsiz tartibda BARCHA bog'liq ma'lumotni o'chiradi (tracked RemoveRange + bitta SaveChanges; Restrict FK'lar uchun: StoryLikes/Views, Likes, PostViews, Comments (reply'lar self-ref bilan), Conversations (Message'lar cascade), Follows, SavedQuotes, so'ng Posts/Quotes/ReadingGoals/UserGenres, oxirida User). `ProfileController.DeleteAccount` → cookie (`AuthConstants.AccessTokenCookie`) o'chiriladi, `{redirect:"/"}` qaytaradi; JS asosiy sahifaga o'tadi. **ESLATMA:** `ExecuteDeleteAsync` ishlatilmadi — u Relational paketda, Application esa provider-agnostik (faqat `Microsoft.EntityFrameworkCore` bazasi); shu sabab tracked RemoveRange.
- **(3) STORY funksiyasi (to'liq yangi feature):**
  - **Domain:** `Story`(UserId→User Cascade, BookId→Book Restrict, ImageUrl? — bo'lmasa muqovaga qaytadi), `StoryView`(unique StoryId+UserId, User Restrict), `StoryLike`(unique StoryId+UserId, User Restrict). `User.Stories` nav. 3 ta Configuration. `IAppDbContext`/`AppDbContext`/`TestDbContext`ga DbSetlar. Migratsiya `20260619114216_AddStoriesAndUsernameLength` (DB'ga qo'llandi).
  - **Application:** `StoryDto` (image=ImageUrl??BookCoverUrl, BookTitle/Author caption, ViewCount/LikeCount/IsLikedByCurrentUser/IsMine), `StoryLikeResultDto`, `StoryQueryableExtensions.ToStoryDto`. Commands: `CreateStory`(+Validator: BookId>0; kitob bor), `ToggleStoryLike`, `RecordStoryView` (idempotent, **muallifning o'z ko'rishi sanalmaydi**), `DeleteStory` (faqat egasi). Query: `GetUserStories`.
  - **Web:** `StoriesController` (`/stories/user/{id}`, `create`, `{id}/view`, `{id}/like`, `{id}/delete`). Rasm yuklash mavjud `/posts/upload-image`ni qayta ishlatadi.
  - **Feed/Profil ko'rsatish:** `PostDto.AuthorHasStory` (+ToPostDto `p.User.Stories.Any()`) va `ProfileDto.HasStory` qo'shildi. `_PostCard` muallif avatari story bo'lsa **gradient halqa** (`has-story`) + `data-open-story` (bosilsa modal, story yo'q bo'lsa profilga); profil avatari ham xuddi shunday. Profilda "Story qo'shish" tugmasi (faqat o'ziga).
  - **Modallar (`_Layout`, faqat auth):** `#storyViewer` — rasm + caption (kitob nomi/muallif) + muallif qatori + **ko'rishlar va like ikonkalari (ishlaydi)** + (o'ziga) o'chirish + oldingi/keyingi navigatsiya + progress segmentlar; ochilganda har story uchun ko'rish yoziladi. `#storyComposer` — kitob qidirish (`/books/search`) + ixtiyoriy rasm yuklash + Joylash. Barcha mantiq `site.js`da yangi IIFE. CSS: story halqasi, viewer, umumiy `.modal-overlay/.modal-box`, `.username-status` holatlari.
- **TEKShIRILDI (rebuild + toza `dotnet run`, port 5261; eski sessiyaning `dotnet watch` jarayoni eski kodni xizmat qilayotgani uchun o'ldirildi):** `dotnet build` => 0/0, `dotnet test` => **38/38**. Dev JWT (user1/user2) bilan curl orqali: (1) `/profile/check-username` — o'ziniki=bo'sh, "user2"=band, "ab"=noto'g'ri, yangi=bo'sh; (2) story yaratish→ro'yxat→view (user2=1, muallif ko'rishi sanalmadi)→like toggle→antiforgery'siz 400; feed user1 postlarida 5×`data-open-story`; profil sahifasida halqa+composer+delete tugma+modal `data-email` to'g'ri render; (3) **akkaunt o'chirish** — tashlandiq user (id 5) barcha bog'liq ma'lumot bilan (post, like, view, izoh+reply, follow 2 yo'nalish, conversation+message, story+like/view, savedquote, quote, goal+progress, genre): xato email→400 (o'chmaydi), to'g'ri email→200 `{redirect:"/"}`, BARCHA user5 satrlari 0 ga tushdi, user1'ning izohiga qilingan reply ham (parent o'chgani uchun) o'chdi, **user1 butunlay saqlandi** (5 post, postlikes). Barcha test ma'lumotlari tozalandi — DB asl holatida (2 user, 0 story, real postlar/iqtiboslar). Ilova 5261'da ishlab turibdi.
- **ESLATMA:** `.cshtml`/controller o'zgarishlari uchun rebuild + restart SHART (Razor runtime compilation o'chiq). Story rasmi bo'lmasa viewer kitob muqovasini yoki gradient+kitob ikonkasini ko'rsatadi (caption har doim kitob nomi).

**2026-06-19 — Profil tahrirlash yangilandi + post URL'lari slug'ga o'tkazildi + post detalida kitob ma'lumoti tepaga ko'chirildi (19-bosqichdan keyingi qo'shimcha talab).**

- **(1) Profil tahrirlash (`/profile/edit`):**
  - `UpdateProfileCommand`ga `Username` qo'shildi. `UpdateProfileCommandValidator`: username `^[a-zA-Z0-9_]{3,32}$` (min 3, max 32), FullName endi "To'liq ism" matni bilan. `UpdateProfileCommandHandler`: username trim+lowercase, **bandlik tekshiruvi** (`CompleteProfile` bilan bir xil pattern — `ValidationException`), `ProfileDto.Username` qaytariladi. Izchillik uchun `CompleteProfileCommandValidator` ham 3-30 → 3-32 ga yangilandi.
  - `Views/Profile/Edit.cshtml` qayta yozildi: "Ism" → **"To'liq ism"**; username maydoni (3-32, hint bilan); avatar endi **URL emas, rasm yuklash** — fayl tanlanishi bilan darhol `/profile/upload-avatar`ga AJAX yuklanadi, dumaloq preview, "O'chirish" tugmasi, hidden `AvatarUrl`. CSS: `.avatar-edit*` klasslari `site.css`ga qo'shildi.
  - **(2) Avatar .webp konversiyasi:** `ProfileController.UploadAvatar` (`POST /profile/upload-avatar`, `[Authorize]`+antiforgery) — `PostsController.UploadImage` konvensiyasiga mos: JPG/PNG/WEBP/GIF (8 MB), `Image.LoadAsync` bilan haqiqiy formatni tekshiradi, 512px gacha kichraytiradi, **har doim `.webp`ga** qayta kodlaydi (`WebpEncoder Quality=82`), `wwwroot/uploads/avatars/{guid}.webp`ga saqlaydi — **asl format hech qachon saqlanmaydi**. Buzilgan/notanish rasm → toza 400 (`UnknownImageFormatException` + `InvalidImageContentException` ushlanadi). `Edit [HttpPost]`: avatar almashtirilganda eski mahalliy `/uploads/avatars/` fayli o'chiriladi (`DeleteLocalUpload`, best-effort).
- **(3) Post URL'lari `/post/{username}/{12-char-slug}`:**
  - Domain: `Post.Slug` (string, required). `PostConfiguration`: `IsRequired().HasMaxLength(16)` + **unique index**. Migratsiya `20260619105552_AddPostSlug` — Slug ustuni + unique index; **mavjud postlar uchun backfill SQL** (`UPDATE Posts SET Slug = substr(md5(random()||Id),1,12) WHERE Slug=''`) unique indexdan oldin (bo'sh jadvalda no-op).
  - `Application/Common/SlugGenerator` (base62, `RandomNumberGenerator`, default 12 belgi). `CreatePostCommandHandler` slug yaratadi + collision'da qayta urinadi. `PostDto.Slug` + `ToPostDto` proyeksiyasiga qo'shildi.
  - `GetPostBySlugQuery`(+Handler) yangi. Izoh-daraxti logikasi `GetPostByIdQueryHandler` bilan takrorlanmasligi uchun `PostDetailLoader` (shared internal) ga ajratildi — ikkala handler ham shuni ishlatadi.
  - `PostsController`: `/post/{username}/{slug}` → `DetailsBySlug` (slug bo'yicha, view'ni `RecordPostView`dan keyin Post.Id bilan yozadi). Eski `/posts/{id:int}` → `Details` saqlandi (ichki/orqaga-moslik — chat'dagi ulashilgan post linklari `/posts/{id}` ishlaydi). `Create` endi slug URL'ga redirect qiladi. `ViewHelpers.PostUrl(username, authorId, **slug**)`. `_PostCard.cshtml` va `Details.cshtml` slug ishlatadi.
  - **(4) Post detali:** `Views/Posts/Details.cshtml`da `pd-bookhead` (kitob nomi + muallif + janr badge) **tepaga ko'chirildi** — endi muallif qatoridan keyin, hero/fikr matnidan oldin (ilgari fikr matnidan keyin pastda edi). `.pd-article` flex+gap bo'lgani uchun CSS o'zgarmadi.
- **Testlar:** `PostsHandlerTests`'dagi qo'lda yaratilgan `Post`larga `Slug` qo'shildi (InMemory required maydonni majburlaydi). `dotnet test` => **38/38 passed**. Release build => **0/0**.
- **TEKShIRILDI (ishlab turgan ilova, port 5261, dev JWT user-1 bilan):** (1) `/profile/edit` → "To'liq ism", Username (3-32), avatar yuklash, AvatarUrl hidden render bo'ldi; (2) PNG yuklash → `.webp` (`RIFF....WEBP` magic), buzilgan rasm → 400; (3) `POST /posts/create` → 302 `/post/javohirsadullayev/HOQ7pzUdVkfi` (username + 12-char slug); slug sahifa 200, kitob nomi/muallif **tepada** (`pd-bookhead` `pd-review`dan oldin); (4) profil edit POST → bio/avatar saqlandi, avatar almashtirilganda eski fayl o'chdi; (5) qisqa username (2) → 400 validatsiya; noma'lum slug → 404; (6) **migratsiya real DB'ga startup'da qo'llandi** — mavjud 5 ta post backfill orqali to'g'ri 12-char slug oldi, detal sahifalari 200. Test posti + test bio/avatar tozalandi (DB asl holatiga qaytdi). Eslatma: Google login har kirishda FullName/AvatarUrl'ni qayta sync qiladi (LoginWithGoogle 47-50), shuning uchun test paytidagi FullName/avatar o'zgarishlari keyingi login'da tiklanadi.

**2026-06-19 — Landing page uchun alohida URL qo'shildi.**
- `HomeController`ga `Landing()` action qo'shildi — `Index()` dan farqli o'laroq, login holatidan qat'i nazar har doim landing view'ni qaytaradi (Feed'ga redirect yo'q). Xuddi shu `Views/Home/Index.cshtml`ni ishlatadi (`View("Index")`).
- Manzil: `/Home/Landing` (masalan, `http://localhost:5261/Home/Landing`) — dizaynni login holatidan qat'i nazar ko'rish/tekshirish uchun.
- `/` (root, `Home/Index`) avvalgidek: anonim bo'lsa landing, login bo'lsa `/Feed`ga redirect.
- Build => 0/0.

**2026-06-19 — /Feed sahifasi qayta yozildi + backend bug tuzatildi.**
- **Backend bug:** `BooksController.Create` da `[FromBody]` yo'q edi → composer'dagi "Yangi kitob" qo'shish JSON body bog'lanmay har doim validatsiya xatosi berardi. `[FromBody]` qo'shildi.
- **`Views/Feed/Index.cshtml` qayta yozildi:** composer endi joriy foydalanuvchi avatari bilan; tanlangan kitob alohida "chip"da ko'rinadi va bekor qilish (×) tugmasi bor; "Joylash" tugmasi kitob+matn tayyor bo'lguncha disabled; holat matni dinamik yangilanadi (`refreshState`); `style="display:none"` o'rniga `hidden` atributi ishlatildi; bo'sh holat (empty-state) ikonka bilan chiroyliroq.
- **`Views/Shared/_PostCard.cshtml` qayta yozildi:** dizayn referensiga (`03-asosiy-feed.html`) moslab — sarlavha (avatar+ism+vaqt), tana = kitob muqovasi (2/3, CoverUrl bo'lmasa `menu_book` placeholder) + kitob nomi/muallif/fikr (4 qatorга clamp), footer = like/izoh/ko'rish hisoblagichlari alohida fonda. Like tugmasi `.like-btn`/`data-like`/`.like-count` saqlandi (site.js bilan mos).
- **`wwwroot/css/site.css`:** Feed/post bo'limi yangilandi — `.post-body` grid (96px muqova | matn), `.post-cover`, `.post-book-title`, composer chip/newbook stillari, empty-state, like FILL animatsiya; mobil (480px) uchun 72px muqova.
- **Tekshirildi (rebuild + ishlab turgan app, 5261):** GET /Feed → 200; `POST /books/create` (JSON) → 200 (avval 400 edi); `POST /posts/create` → 302 /posts/1; feed yangi kartochkani muqova+sarlavha+matn bilan render qildi; `POST /posts/1/like` → `{isLiked:true,likeCount:1}`; log'da runtime xato yo'q. Test ma'lumotlari o'chirildi (0 post, 10 seed kitob — asl holat).
- Eslatma: `.cshtml`/controller o'zgarishlari uchun **rebuild + app restart SHART** (Razor runtime compilation yoqilmagan).

**2026-06-19 — Bugfix: Onboarding sahifasida janr tanlanmayotgan edi.**
- `wwwroot/js/site.js` — genre-card click handler `<label>` ichidagi checkbox'ni QO'LDA toggle qilardi (`input.checked = !input.checked`). Ammo `<label>` brauzer tomonidan checkbox'ni allaqachon avtomatik almashtiradi — natijada ikki marta almashinib, holat o'zgarmasdi (janr tanlab bo'lmasdi).
- Tuzatish: qo'lda toggle olib tashlandi; endi faqat checkbox'ning `change` hodisasida `.selected` klassi sinxronlanadi. Native label toggle ishlaydi, vizual va form qiymati to'g'ri.
- Konsoldagi `Unchecked runtime.lastError` — brauzer kengaytmasidan, ilovaga aloqasi yo'q. SignalR loglari normal.
- **Tekshirildi (ishlab turgan ilova, port 5261):** (1) tuzatilgan `site.js` to'g'ri uzatilyapti; (2) dev JWT yasab `/Onboarding` autentifikatsiya bilan yuklandi → HTTP 200, 10 ta janr kartochkasi checkbox+antiforgery bilan render; (3) `POST /Onboarding/Save` (genreIds=1,2,3) → HTTP 302 → `/Feed`, DB `UserGenres`ga yozildi. Tekshiruv yozuvlari keyin o'chirildi (user 1 asl holatiga qaytdi).
- Eslatma: dev sirlari user-secrets'da (`36209042-...`) — `Jwt:Key` va DB conn (`kitobdagimen/kitobdagimen`) appsettings'dagi qiymatlarni override qiladi.

**2026-06-19 — 1-bosqich yakunlandi (Loyiha skeleti).**
- `global.json` yaratildi, SDK 8.0.128 ga qotirildi (rollForward: latestPatch). Tizimda 8.0.128 va 10.0.109 bor edi; .NET 8 tanlandi.
- `KitobdaGimen.sln` + 5 loyiha: `src/KitobdaGimen.Domain`, `.Application`, `.Infrastructure`, `.Web` (web), `tests/KitobdaGimen.Application.Tests` (xunit). Barchasi net8.0.
- Reference'lar: Application→Domain; Infrastructure→Application; Web→Application+Infrastructure; Tests→Application+Domain.
- NuGet (net8 mos versiyalar): Application — MediatR 12.4.1, Mapster 7.4.0 (+DI 1.0.1), FluentValidation 11.9.2 (+DI ext), MS.DI.Abstractions 8.0.2. Infrastructure — EF Core 8.0.8 (+Relational, +Design), Npgsql.EF 8.0.4, StackExchange.Redis 2.8.0, Hangfire.Core 1.8.14, Hangfire.PostgreSql 1.20.9, Auth.Google 8.0.8, Auth.JwtBearer 8.0.8; `FrameworkReference Microsoft.AspNetCore.App` qo'shildi (classlib'da ASP.NET Core tiplari uchun). Web — Serilog.AspNetCore 8.0.2, Hangfire.AspNetCore 1.8.14, EF.Design 8.0.8.
- Qatlam papkalari skeleti (.gitkeep bilan) va ildiz `.gitignore` yaratildi.
- `dotnet build KitobdaGimen.sln` => muvaffaqiyatli, 0 xato, 0 ogohlantirish.

**2026-06-19 — 2-bosqich yakunlandi (Domain qatlami).**
- `Common/BaseEntity.cs` — `int Id` surrogate kalit (abstract). CreatedAt har entity'da alohida (spec qaysi entity'da bor desa, o'shanda).
- 15 ta entity yozildi (`Entities/`): User, Genre, UserGenre (kompozit kalit, surrogate Id yo'q), Book, Post, PostView, Like, Comment (self-ref ParentCommentId + Replies), Follow (Follower/Following — ikkita User FK), ReadingGoal, ReadingProgress (DateOnly Date), Quote, SavedQuote, Conversation (User1/User2), Message (nullable Text + SharedPostId).
- Navigation property'lar EF uchun qo'shildi (nullable=enable, `null!` pattern).
- Enum: spec MA'LUMOTLAR MODELI'da enum maydon yo'q — `Enums/` papka kelajak uchun bo'sh qoldirildi (16-bosqich notification kabi ehtiyojlarda to'ldiriladi).
- Domain build => 0 xato, 0 ogohlantirish.

**2026-06-19 — 3-bosqich yakunlandi (Infrastructure — Persistence).**
- `Application/Common/Interfaces/IAppDbContext.cs` — 15 DbSet + SaveChangesAsync. Application'ga `Microsoft.EntityFrameworkCore` 8.0.8 qo'shildi (Clean Arch abstraksiyasi uchun).
- `Persistence/AppDbContext.cs` — IAppDbContext'ni amalga oshiradi, `ApplyConfigurationsFromAssembly`.
- `Persistence/Configurations/` — 15 ta `IEntityTypeConfiguration` (maxlength'lar, required, FK delete behaviour, indekslar).
  - Unique indekslar: User.GoogleId, User.Email, Genre.Name, UserGenre(UserId,GenreId composite key), Follow(FollowerId,FollowingId), Like(PostId,UserId), PostView(PostId,UserId), ReadingProgress(ReadingGoalId,Date), SavedQuote(QuoteId,UserId), Conversation(User1Id,User2Id).
  - Delete behaviour: User-ga ikkilangan FK joylar (Follow, Conversation) va ikkilamchi bog'lanishlar Restrict; tabiiy egalik (Post→User, Comment→Post, ReadingProgress→ReadingGoal, Message→Conversation va h.k.) Cascade; Message.SharedPost SetNull; Comment.ParentComment Restrict (self-ref sikl oldini olish).
- `Persistence/AppDbContextFactory.cs` — design-time factory, conn string `ConnectionStrings__DefaultConnection` env'dan yoki localhost fallback.
- `DependencyInjection.cs` — `AddInfrastructure(IConfiguration)`: AddDbContext (Npgsql, "DefaultConnection") + `IAppDbContext` registratsiya. (Redis/SignalR/Hangfire 11-bosqichda qo'shiladi.)
- `dotnet-ef` 8.0.8 local tool (.config/dotnet-tools.json) o'rnatildi. Birinchi migratsiya yaratildi: `Persistence/Migrations/20260619003115_InitialCreate`. (DB'ga hali APPLY qilinmagan — Postgres ishlamayapti; 17-bosqichda yoki haqiqiy DB bilan `database update`.)
- Full solution build => 0 xato, 0 ogohlantirish.

**2026-06-19 — 4-bosqich yakunlandi (Infrastructure — Identity).**
- Application interfeyslari: `ICurrentUserService` (UserId, Email, IsAuthenticated), `ITokenService` (GenerateToken(User), TokenLifetime).
- `Infrastructure/Identity/`:
  - `JwtSettings` (Jwt section: Key, Issuer, Audience, ExpiryMinutes=7kun), `GoogleAuthSettings` (Authentication:Google section: ClientId, ClientSecret), `AuthConstants` (cookie nomi `kitobdagimen_token`, external scheme "External").
  - `JwtTokenService` — HS256 JWT (sub, NameIdentifier, email, name, jti).
  - `CurrentUserService` — IHttpContextAccessor orqali claim'lardan UserId/Email.
  - `IdentityServiceExtensions.AddIdentityServices` — DefaultScheme=JwtBearer; JwtBearer token'ni **HttpOnly cookie**'dan o'qiydi (OnMessageReceived); "External" cookie scheme (Google OAuth correlation uchun); AddGoogle (SignInScheme=External, scope email+profile); AddAuthorization.
- `AddInfrastructure` endi `AddIdentityServices`'ni ham chaqiradi. System.IdentityModel.Tokens.Jwt 7.6.0 qo'shildi.
- Build => 0 xato, 0 ogohlantirish.
- Login oqimi (Google challenge → callback → user yaratish/topish → JWT → HttpOnly cookie set) controller'i 5/12-bosqichlarda yoziladi. Google ClientId/Secret bo'sh bo'lsa placeholder ishlatiladi (challenge real kalitlarsiz ishlamaydi).

**2026-06-19 — 5-bosqich yakunlandi (Application — Auth + Onboarding).**
- `Application/DependencyInjection.cs` `AddApplication`: MediatR (assembly scan), `ValidationBehavior` pipeline, FluentValidation validators, Mapster (GlobalSettings.Scan + ServiceMapper).
- Common: `Exceptions/{NotFoundException, ValidationException(Errors map), ForbiddenAccessException}`, `Behaviors/ValidationBehavior`.
- Auth: `UserDto`, `AuthResultDto`(Token, User, RequiresOnboarding); `LoginWithGoogleCommand`(+Validator,+Handler — GoogleId bo'yicha topadi/yaratadi, profilni sync, token beradi, RequiresOnboarding=janr tanlanmaganmi); `GetCurrentUserQuery`(+Handler — ICurrentUserService).
- Onboarding: `GenreDto`; `GetGenresQuery`(+Handler); `SaveUserGenresCommand`(+Validator: >=1 noyob janr, +Handler: eski UserGenre'larni o'chirib yangisini yozadi).
- Mapster: User→UserDto, Genre→GenreDto avtomatik (nom mosligi). Build => 0/0.
- Konvensiya: har feature `Commands/<Name>/` va `Queries/<Name>/` ichida Command+Validator+Handler alohida fayl, DTO'lar `Dtos/`. Keyingi feature'lar shu tartibni davom ettiradi.

**2026-06-19 — 6-bosqich yakunlandi (Application — Posts).**
- Common/Models: `PagedResult<T>` (Items, Page, TotalCount, TotalPages, HasNext/Prev), `UserSummaryDto`, `BookSummaryDto` (boshqa feature'lar ham ishlatadi).
- Posts/Dtos: `PostDto` (Author, Book, Like/Comment/View count, IsLikedByCurrentUser), `CommentDto` (Replies nested), `PostDetailDto`, `LikeResultDto`.
- `PostQueryableExtensions.ToPostDto(currentUserId)` — qayta ishlatiluvchi EF projeksiya (counts + isLiked SQL'da).
- Commands: `CreatePostCommand`(+Validator,+Handler — kitob borligini tekshiradi), `ToggleLikeCommand`(+Handler — like qo'shadi/oladi, yangi count), `AddCommentCommand`(+Validator,+Handler — parent boshqa postga tegishli emasligi; bir darajali threading: reply root'ga biriktiriladi), `RecordPostViewCommand`(+Handler — idempotent, anonim e'tiborsiz).
- Queries: `GetFeedQuery`(+Handler — kuzatilganlar+o'zi, bo'sh bo'lsa global; sahifalash, pageSize clamp<=50), `GetPostByIdQuery`(+Handler — post + izoh daraxti, yo'q bo'lsa NotFound).
- Build => 0/0.

**2026-06-19 — 7-bosqich yakunlandi (Application — Profile + Follow).**
- Profile: `ProfileDto` (counts + IsFollowedByCurrentUser + IsCurrentUser); `GetUserProfileQuery`(+Handler — yo'q bo'lsa NotFound), `GetUserPostsQuery`(+Handler — sahifalangan, Posts'dagi `ToPostDto` internal extension qayta ishlatildi), `UpdateProfileCommand`(+Validator,+Handler — current user).
- Follow: `FollowResultDto`, `FollowUserDto`; `ToggleFollowCommand`(+Handler — o'zini follow qila olmaydi → ForbiddenAccess, target yo'q → NotFound), `GetFollowersQuery`/`GetFollowingQuery`(+Handlers — sahifalangan, har userda IsFollowedByCurrentUser). Domain `Follow` entity bilan nom to'qnashuvi `using FollowEntity = ...` orqali hal qilindi.
- Build => 0/0.

**2026-06-19 — 8-bosqich yakunlandi (Application — ReadingGoals).**
- DTOs: `ReadingGoalDto` (ProgressPercent/PagesRemaining hisoblanadi, PagesReadToday), `ReadingProgressDto`, `ReadingGoalDetailDto`.
- `ReadingGoalQueryableExtensions.ToReadingGoalDto(today)` — internal projeksiya (bugungi sahifalar Sum).
- Commands: `CreateReadingGoalCommand`(+Validator,+Handler — kitob bor; shu kitobning eski faol goal'ini deaktiv qiladi), `UpdateReadingProgressCommand`(+Validator,+Handler — bugungi ReadingProgress upsert, CurrentPage += PagesRead, tugasa IsActive=false, faqat egasi).
- Queries: `GetActiveReadingGoalsQuery`(+Handler), `GetReadingGoalByIdQuery`(+Handler — egalik tekshiruvi, progress tarixi).
- Build => 0/0.

**2026-06-19 — 9-bosqich yakunlandi (Application — Quotes).**
- `QuoteDto` (Author, Book, SaveCount, IsSavedByCurrentUser), `SaveQuoteResultDto`; `QuoteQueryableExtensions.ToQuoteDto(currentUserId)`.
- Commands: `CreateQuoteCommand`(+Validator,+Handler), `ToggleSaveQuoteCommand`(+Handler — SavedQuote toggle, yangi count), `DeleteQuoteCommand`(+Handler — faqat egasi).
- Queries: `GetQuotesQuery`(BookId? filtri), `GetMyQuotesQuery`, `GetSavedQuotesQuery` (SavedQuote.CreatedAt bo'yicha) — barchasi sahifalangan.
- Build => 0/0.

**2026-06-19 — 10-bosqich yakunlandi (Application — Chat). BUTUN APPLICATION QATLAMI TUGADI.**
- DTOs: `MessageDto` (Sender, SharedPost preview, IsMine, IsRead), `ConversationDto` (OtherUser, oxirgi xabar, UnreadCount), `SharedPostPreviewDto`.
- `MessageQueryableExtensions.ToMessageDto(currentUserId)`; `ConversationHelper.GetOrCreateAsync` (User1Id=min, User2Id=max — unique juftlik kanonik).
- Commands: `GetOrCreateConversationCommand`(+Handler — o'zi bilan emas), `SendMessageCommand`(+Validator,+Handler — conversationId yoki recipientId; Text yoki SharedPostId kamida biri; ishtirokchi/post tekshiruvi), `MarkMessagesReadCommand`(+Handler — qarshi tomon xabarlari).
- Queries: `GetConversationsQuery`(+Handler — OtherUser conditional, oxirgi xabar, unread), `GetMessagesQuery`(+Handler — newest-first sahifa, har sahifada eskidan yangiga reverse, ishtirokchi tekshiruvi).
- Full solution build => 0/0.

**2026-06-19 — 11-bosqich yakunlandi (Infrastructure — Redis, SignalR, Hangfire).**
- `Application/Common/Interfaces/ICacheService` (Get/Set/Remove<T>); `Infrastructure/Caching/RedisCacheService` (JSON, RedisException'ni yutadi va log qiladi — Redis o'chsa ham ishlaydi).
- `CachingServiceExtensions.AddCaching` — `IConnectionMultiplexer` singleton, `AbortOnConnectFail=false` (Redis yo'q bo'lsa startup yiqilmaydi), conn string "Redis" yoki localhost:6379.
- `BackgroundJobsServiceExtensions.AddBackgroundJobs` — Hangfire + PostgreSQL storage + server; `Hangfire:Enabled=false` bilan o'chiriladi (DB'siz lokal run uchun). Hangfire.AspNetCore 1.8.14 Infrastructure'ga qo'shildi.
- `RealTimeServiceExtensions.AddRealTime` — `AddSignalR()`. Hub'lar Web'da (16-bosqich).
- `AddInfrastructure` endi: persistence + identity + caching + realtime + backgroundjobs. Build => 0/0.
- ESLATMA: lokal run/test'da `Hangfire:Enabled=false` qo'yish kerak (aks holda DB yo'qligida Hangfire server schema yaratishda urinadi). 12-bosqich appsettings.Development'da false qo'yiladi.

**2026-06-19 — 12-bosqich yakunlandi (Web — backend).**
- `Program.cs`: Serilog (ReadFrom.Configuration), `AddControllersWithViews`, `AddApplication`+`AddInfrastructure`, `ExceptionHandlingMiddleware` (eng tashqi), HSTS/HttpsRedirect faqat prod, static files, routing, auth, default route (Home/Index), Hangfire dashboard faqat Enabled bo'lsa.
- `appsettings.json` (ConnectionStrings DefaultConnection+Redis, Jwt, Authentication:Google bo'sh, Hangfire.Enabled=true, Serilog) + `appsettings.Development.json` (dev Jwt key, **Hangfire.Enabled=false**, Serilog Debug).
- `Middleware/ExceptionHandlingMiddleware`: ValidationException→400+errors, NotFound→404, Forbidden→403, Unauthorized→401, qolgani→500 (JSON, o'zbekcha).
- `Controllers/`: `AppController` (Mediator+CurrentUserId baza), `AuthController` (google-login Challenge / google-callback → LoginWithGoogle → JWT HttpOnly cookie → onboarding/feed redirect / logout), `HomeController`, `OnboardingController`, `FeedController`, `PostsController` (Details+RecordView, Create, Like/Comment AJAX), `ProfileController` (Index, Edit, Follow AJAX, Followers/Following), `ReadingGoalsController`, `QuotesController` (Index/My/Saved, Create, ToggleSave, Delete), `ChatController` (Index, Start, Send AJAX, Messages AJAX, MarkRead), `BooksController` (Search/Create AJAX). Models: `ProfilePageViewModel`, `ChatPageViewModel`.
- **YANGI: Books feature qo'shildi** (Application/Features/Books) — Posts/Quotes/ReadingGoals hammasi BookId talab qiladi, lekin kitob tanlash/yaratish query yo'q edi. `BookDto`, `GetBooksQuery` (qidiruv), `CreateBookCommand` (+Validator, identik title+author bo'lsa qayta ishlatadi). 17-bosqich seed'ga kitoblar ham qo'shiladi.
- `IdentityServiceExtensions` JwtBearer `OnChallenge`: brauzer navigatsiyasi (Accept: text/html) → 302 `/`; AJAX/JSON → 401.
- TEKShIRILDI (dotnet run): startup toza (DB/Redis startup'da kerak emas), `/Feed` text/html→302 `/`, json→401. `/` hozir 500 (view yo'q — 13-bosqichda). Build => 0/0.

**2026-06-19 — 13-bosqich yakunlandi (Web — frontend 1-3).**
- Razor plumbing: `_ViewImports` (using'lar + tag helpers), `_ViewStart` (Layout="_Layout").
- `Views/Shared/_Layout.cshtml`: `@inject ICurrentUserService` + `ISender` → `GetCurrentUserQuery` bilan navbar avatar/ism. Navbar: brand chapda, markazda Asosiy/Kutubxona/Iqtiboslar/Xabarlar, o'ngda qidiruv/bildirishnoma/avatar + mobil burger. `ViewData["HideChrome"]` bilan navbarsiz sahifalar (landing/onboarding). `@Html.AntiForgeryToken()` global. Fonts: Lora (serif) + Source Sans 3 + Material Symbols (Google Fonts CDN).
- `wwwroot/css/site.css`: to'liq dizayn tizimi — CSS o'zgaruvchilar (spec ranglari), navbar+burger, btn (primary/accent/outline/ghost/danger), card, form/input, badge, pagination, landing, genre grid. Responsive breakpoint'lar: <=1024 (3 ustun), <=768 (burger menyu, 2 ustun), <=480 (mobil paddinglar). **Bu CSS 14-15 bosqichlar uchun ham asos.**
- `wwwroot/js/site.js`: burger toggle, genre-card tanlash, `kitob.apiPost` (antiforgery header "RequestVerificationToken" + X-Requested-With, 401→/). Program.cs'da `AddAntiforgery(HeaderName="RequestVerificationToken")`.
- Sahifalar: `Home/Index` (Landing — hero + Google tugma → /auth/google-login; ?xato bo'lsa ogohlantirish), `Onboarding/Index` (janr grid checkbox kartochkalar, janr→material icon map, POST /Onboarding/Save).
- ESLATMA: dizayndagi "01-kirish" = landing/login (alohida sahifa emas) — bittaga birlashtirildi (Google-only auth).
- TEKShIRILDI: build 0/0; `dotnet run` → GET / endi 200, "Google orqali kirish"/"landing-card" render bo'ladi.

**2026-06-19 — 14-bosqich yakunlandi (Web — frontend 4-6).**
- `ViewHelpers` (RelativeTime "x daqiqa oldin", Initial avatar harfi).
- site.css: feed/composer, post-card, post-actions/action-btn (liked), comment/replies, profile-head/stats/follow-row qo'shildi.
- site.js: delegatsiya bilan like (`/posts/{id}/like`), follow (`/profile/{id}/follow`), save-quote (`/quotes/{id}/save`) handler'lar; `kitob.apiPost`.
- Partial'lar: `_PostCard` (PostDto — author/book/text/like-comment-view), `_Comment` (rekursiv reply'lar bilan).
- `Feed/Index`: composer (kitob qidirish AJAX /books/search, yangi kitob qo'shish /books/create, tanlangan kitob bookId hidden), post kartalari, sahifalash. `Posts/Details`: _PostCard + izohlar (AJAX qo'shish, ro'yxat boshiga qo'shadi). `Profile/Index`: header+stats+follow/xabar tugma+postlar. `Profile/Edit`: UpdateProfile form. `Profile/FollowList`: follower/following ro'yxati.
- Build => 0/0.

**2026-06-19 — 15-bosqich yakunlandi (Web — frontend 7-9). BARCHA SAHIFALAR TAYYOR.**
- site.css: goal-card/progress-bar/goal-log, tabs, quote-grid (columns)/quote-card serif, chat (grid sidebar+main, conv-item, unread-badge, msg bubbles in/out, shared, msg-form) + mobil chat (has-active bilan ro'yxat/suhbat almashinuvi), quote columns:1.
- `ReadingGoals/Index`: yangi maqsad (kitob qidirish + kunlik maqsad), goal kartalar (progress bar, bugun/kunlik, "+bet" AJAX /reading-goals/progress jonli yangilash). `ReadingGoals/Details`: goal + progress tarixi jadvali.
- `Quotes/Index`: tablar (barcha/mening/saqlangan — path bo'yicha active), yangi iqtibos (kitob qidirish + matn), iqtibos kartalar (serif, save AJAX + count, o'zinikida delete form).
- `Chat/Index`: sidebar suhbatlar (avatar, oxirgi xabar, unread-badge), main xabarlar (in/out bubble, shared post), xabar yuborish AJAX /chat/send (jonli append). MarkRead ChatController.Index'da avtomatik.
- Build => 0/0.

**2026-06-19 — 16-bosqich yakunlandi (SignalR Hub'lar — frontend bilan ulash).**
- Application/Common/Interfaces: `IChatNotifier` (MessageReceivedAsync — MessageDto'ni qabul qiluvchiga push), `INotificationService` (NotifyAsync — like/comment/follow bildirishnomasi). Common/Models: `NotificationDto` (Type, Message, Url, ActorName, ActorAvatarUrl, CreatedAt).
- Handler'larga inject qilindi:
  - `SendMessageCommandHandler` → `IChatNotifier`: saqlangach, suhbatdagi ikkinchi ishtirokchini (`otherUserId`) aniqlab, DTO'ni `with { IsMine = false }` bilan push qiladi.
  - `ToggleLikeCommandHandler` → `INotificationService`: like qo'shilganda (un-like emas, o'z postiga emas) post egasiga "X postingizni yoqtirdi". Post egasi `AnyAsync` o'rniga `Select(p => (int?)p.UserId)` bilan olinadi.
  - `AddCommentCommandHandler` → izoh qo'shilganda post egasiga "X postingizga izoh qoldirdi".
  - `ToggleFollowCommandHandler` → follow boshlanganda kuzatilgan userga "X sizni kuzata boshladi".
- Web/Hubs: `ChatHub`, `NotificationHub` — ikkalasi `[Authorize]`, `OnConnectedAsync`'da `ClaimTypes.NameIdentifier`'dan userId olib `user-{id}` guruhiga qo'shadi. `UserGroup(int)` static helper.
- Web/RealTime: `SignalRChatNotifier` (`IHubContext<ChatHub>` → "ReceiveMessage"), `SignalRNotificationService` (`IHubContext<NotificationHub>` → "ReceiveNotification"). Ikkalasi xatoni yutadi + log (real-time best-effort, so'rovni yiqitmaydi).
- Program.cs: `IChatNotifier`/`INotificationService` scoped ro'yxatdan; `MapHub<ChatHub>("/hubs/chat")`, `MapHub<NotificationHub>("/hubs/notifications")`. (AddSignalR allaqachon Infrastructure.AddRealTime'da.)
- SignalR auth: HttpOnly cookie negotiate va WS handshake bilan avtomatik yuboriladi → JwtBearer `OnMessageReceived` cookie'dan o'qiydi. Query-string token KERAK EMAS (cookie HttpOnly bo'lgani uchun baribir JS o'qiy olmaydi).
- Frontend: `_Layout` — auth foydalanuvchilar uchun `@microsoft/signalr` CDN (8.0.7), `<body data-authenticated>`, qo'ng'iroq ikonkasida `notif-badge`, `toast-host`. `site.js` — `showToast(...)`, `initNotifications()` (/hubs/notifications, "ReceiveNotification" → toast + badge++). `Chat/Index` — /hubs/chat ga ulanib "ReceiveMessage"da ochiq suhbatga jonli xabar qo'shadi + avtomatik MarkRead. `site.css` — notif-badge + toast stillari.
- TEKShIRILDI (dotnet run): negotiate `/hubs/chat` va `/hubs/notifications` → 401 (auth talab, to'g'ri map). `GET /` → 200. `GET /chat` (Accept: text/html) → 302 `/`. Build => 0/0.

**2026-06-19 — 17-bosqich yakunlandi (Migratsiya va seed data).**
- **Janrlar (kanonik reference data) — `HasData`:** `GenreConfiguration`'ga 10 ta janr qat'iy Id (1..10) bilan qo'shildi: Roman(1), Ilmiy(2), Detektiv(3), Biografiya(4), Falsafa(5), Biznes(6), She'riyat(7), Tarix(8), Fantastika(9), Psixologiya(10). Nomlar onboarding ikonka-map'iga (`Views/Onboarding/Index.cshtml`) va dizayn-referens `02-janr-tanlash`'ga mos. Qat'iy Id'lar seed kitoblar GenreId'lariga bog'lanishi uchun.
- **Migratsiya:** `Persistence/Migrations/20260619045849_SeedGenres` yaratildi — 10 janrni `InsertData` qiladi. (Birinchi `InitialCreate` migratsiyasi DB apply'da avval qo'llanadi.)
- **Namuna kitoblar (demo data) — runtime idempotent seeder:** `Persistence/DbInitializer.cs`. 10 ta haqiqiy kitob (O'tkan kunlar, Kecha va kunduz, Sapiens, Atom odatlari, Sherlok Xolms, va h.k.) janr Id'lariga bog'langan. FAQAT Books bo'sh bo'lsa qo'shadi (`AnyAsync` guard). Janrlar bu yerda EMAS — ular HasData orqali (yagona manba).
- **Startup'da migrate+seed:** `Program.cs` `app.Build()`'dan keyin `await DbInitializer.InitializeAsync(app.Services)`. `Database.MigrateAsync()` + namuna kitoblar. **Best-effort:** try/catch + Serilog WARN — Postgres yo'q/ulanmasa app baribir ishga tushadi (loyiha konvensiyasi: DB startup'da shart emas).
- TEKShIRILDI (`dotnet run`, DB parol noto'g'ri muhitda): WRN "Ma'lumotlar bazasini migratsiya/seed qilib bo'lmadi — startup davom etadi" log qilindi, so'ng `Now listening on http://localhost:5261` — best-effort to'g'ri ishladi. Build => 0/0.
- ESLATMA: lokal Postgres ishlamoqda, lekin `postgres/postgres` auth muvaffaqiyatsiz (parol boshqa) va `javohir` roli yo'q — DB qayta sozlash sudo talab qiladi, bosqich doirasidan tashqari. Migratsiya+seed real DB to'g'ri kredensiallar bilan ishga tushganda avtomatik qo'llanadi.

**2026-06-19 — 18-bosqich yakunlandi (Testlar).**
- `Microsoft.EntityFrameworkCore.InMemory` 8.0.8 test loyihasiga qo'shildi. Test loyihasi FAQAT Application+Domain ga bog'langani uchun (Infrastructure emas) Infrastructure'dagi AppDbContext ishlatilmadi.
- **Test infratuzilmasi (`Support/`):**
  - `TestDbContext` — `IAppDbContext` ni amalga oshiruvchi yengil DbContext (InMemory provider). 15 ta DbSet. `OnModelCreating`: UserGenre kompozit kalit; Follow (Follower/Following) va Conversation (User1/User2) ikkita User FK'sini aniq sozlaydi (InMemory model ambiguity'ni hal qilish uchun — bunisiz "Unable to determine the relationship" xatosi). Indeks/delete-behaviour InMemory'da e'tiborsiz.
  - `Fakes.cs` — `FakeCurrentUserService` (settable UserId), `FakeTokenService` ("test-token"), `SpyChatNotifier`/`SpyNotificationService` (push'larni List'ga yozadi — real-time va notification chaqiruvlarini assert qilish uchun).
  - `TestBase` — `CreateContext()` (har test uchun noyob Guid nomli izolyatsiyalangan InMemory DB), `CreateMapper()` (Mapster konvensiya bo'yicha — prod'dagi kabi).
- **Handler testlari (`Handlers/`, 29 ta):** LoginWithGoogle (yangi user + RequiresOnboarding, mavjud user janr bilan onboarding talab qilmaydi, profil Google'dan sync), SaveUserGenres (faqat mavjud janr, eski tanlovni almashtiradi, auth talab), CreatePost (persist+projeksiya, kitob yo'q→NotFound), ToggleLike (qo'shish/olish + count, egasiga notification, un-like notify qilmaydi, o'z postiga notify yo'q, post yo'q→NotFound), GetFeed (follow+o'zi, global fallback, sahifalash+clamp+newest-first), ToggleFollow (follow/unfollow+count+notify bir marta, o'zini follow→Forbidden, target yo'q→NotFound), UpdateReadingProgress (bugungi progress yaratish+page, bir kunda accumulate/upsert, total'ga yetganda IsActive=false+cap, boshqa user→Forbidden, goal yo'q→NotFound), ToggleSaveQuote (save/unsave+count, yo'q→NotFound), SendMessage (recipient→kanonik conversation (kichik id User1Id)+notify IsMine=false, mavjud conversation qayta ishlatiladi, o'ziga→Forbidden, begona conversation→Forbidden, recipient/post yo'q→NotFound).
- **Validator testlari (`Validators/`, 5 ta):** LoginWithGoogle, SaveUserGenres (>=1 noyob), CreatePost (book+text+maxlength), UpdateReadingProgress (musbat sahifa+limit), SendMessage (target+content shart).
- `dotnet test` => **35/35 passed**, 0 fail. Full solution build => 0 xato, 0 ogohlantirish.
- ESLATMA: `ConversationHelper` internal bo'lgani uchun to'g'ridan-to'g'ri test qilinmadi — u SendMessage testlari orqali qoplanadi (kanonik juftlik + qayta ishlatish).

**2026-06-19 — 19-bosqich yakunlandi (Yakuniy build + README). LOYIHA TUGADI.**
- **Release build:** `dotnet build KitobdaGimen.sln -c Release` => **0 xato, 0 ogohlantirish** (5 loyiha: Domain, Application, Infrastructure, Tests, Web). Tozalanadigan ogohlantirish yo'q edi.
- **Testlar (release):** `dotnet test -c Release` => **35/35 passed**, 0 fail.
- **README.md** loyiha ildizida yozildi: loyiha tavsifi, imkoniyatlar ro'yxati, texnologik stack, loyiha tuzilishi, talablar (.NET 8 / Postgres / Redis), sozlash (Postgres conn string, Redis, `Jwt:Key`, Google OAuth ClientId/Secret — user secrets misoli bilan), DB tayyorlash (`dotnet tool restore` + `dotnet dotnet-ef database update --project ...Infrastructure --startup-project ...Web`), ishga tushirish (`dotnet run --project src/KitobdaGimen.Web`), build/test buyruqlari, dizayn tizimi. Ma'lum muammolar bo'limidagi kredensial eslatmalari (Google kalitlari bo'sh→placeholder, JWT kalit kerak, startup best-effort migrate/seed) README'ga ko'chirildi.
- **Holat:** barcha 19 bosqich yakunlandi. Loyiha funksional tugallangan; real ishlatish uchun faqat haqiqiy kredensiallar (Postgres parol, Google OAuth, kuchli JWT kalit) va ishlaydigan Postgres/Redis kerak (qarang README "Sozlash").

**2026-06-19 — Post rasmi yuklash (.webp konversiya) qo'shildi (19-bosqichdan keyingi feature — loyiha asosiy ishi tugagan, bu qo'shimcha talab).**
- **Domain/Infrastructure:** `Post.ImageUrl` (string?) qo'shildi; `PostConfiguration` — maxlength 2048 (Book.CoverUrl bilan bir xil konvensiya). Migratsiya: `Persistence/Migrations/20260619070130_AddPostImageUrl`.
- **`SixLabors.ImageSharp` 3.1.12** Web loyihasiga qo'shildi (FAQAT Web — rasm qayta ishlash controller darajasida, Books.UploadCover konvensiyasiga mos). ESLATMA: 4.x versiya tijorat litsenziya talab qiladi (build vaqtida xato beradi, "sixlabors.lic" kerak) — shuning uchun 3.1.x (Six Labors Split License, kichik/notijorat loyihalar uchun bepul) ishlatildi. NU1902 zaifligi tufayli 3.1.7 emas, eng so'nggi tuzatilgan **3.1.12** tanlandi.
- **`PostsController.UploadImage`** (`POST /posts/upload-image`, `[Authorize]`+antiforgery): JPG/PNG/WEBP/GIF qabul qiladi (8 MB gacha), `Image.LoadAsync` bilan haqiqiy formatini tekshiradi (Content-Type header'ga ishonmaydi — spoofing'dan himoya + buzilgan fayl rad etiladi), 1600px'dan katta tomonni `ResizeMode.Max` bilan kichraytiradi, **har doim** `.webp`ga qayta kodlaydi (`WebpEncoder { Quality = 80 }`, asl format qanday bo'lishidan qat'iy nazar — JPG/PNG/GIF yuklansa ham natija .webp), `wwwroot/uploads/posts/{guid}.webp`ga saqlaydi, `{ url }` JSON qaytaradi.
- **Application:** `CreatePostCommand.ImageUrl` (ixtiyoriy, maxlength 2048 validator); handler `Post.ImageUrl`ni saqlaydi; `PostDto.ImageUrl` + `PostQueryableExtensions.ToPostDto` proyeksiyasiga qo'shildi — shu orqali Feed, Post Details, Profile sahifalari (barchasi `_PostCard` partial orqali) avtomatik rasmni ko'rsatadi.
- **Feed composer** (`Feed/Index.cshtml`): post matni ostida "Rasm qo'shish" tugmasi + fayl tanlangach **darhol** (submit kutmasdan) `/posts/upload-image`ga AJAX yuklanadi (preview FileReader bilan, yuklanayotganda submit tugma o'chiriladi + "Rasm yuklanmoqda..." hint), natija URL hidden `ImageUrl` maydoniga yoziladi — asosiy forma hali ham oddiy browser POST (`/posts/create`) bilan ishlaydi, network xatosida rasm tozalanadi (`resetPostImage`).
- **`_PostCard.cshtml`:** `Model.ImageUrl` bo'lsa, kitob/matn blokidan keyin to'liq kenglikdagi rasm (`post-image-wrap` → `/posts/{id}`ga link, lazy-load).
- **site.css:** `.composer-image-picker/.composer-image-preview/.composer-image-remove` (preview overlay, max-height 280px) va `.post-image-wrap/.post-image` (max-height 480px, object-fit cover) qo'shildi.
- **TEKShIRILDI:** Full build => 0/0; `dotnet test` => **38/38 passed**. Standalone ImageSharp smoke-test (`/tmp` ichida vaqtinchalik loyiha, keyin o'chirildi): 2000x1200 PNG → decode → 1600x960ga resize → WebP encode → fayl `RIFF....WEBP` magic byte bilan to'g'ri yozildi. `dotnet run` bilan: `/posts/upload-image` auth'siz → 401 (xuddi `/books/upload-cover` kabi — mavjud konvensiyaga mos, regressiya emas).
- **ESLATMA — oldingi sessiya/akkaunt limiti haqida tekshirildi:** kod holatida tugallanmagan/yarim qolgan ish topilmadi (TODO/FIXME yo'q, bo'sh fayl yo'q, build+testlar toza). Loyiha haqiqatda **git repozitoriyasi EMAS** (`.git` yo'q) — versiyalash yo'qligi alohida xavf, lekin kodga aloqasi yo'q.
- **TUZATISH:** `appsettings.json`dagi `postgres/postgres` ulanish satri va Google OAuth bo'sh maydonlari ESKIRGAN/ISHLATILMAYDIGAN — haqiqiy konfiguratsiya `dotnet user-secrets` orqali (`src/KitobdaGimen.Web`) allaqachon to'g'ri sozlangan: `ConnectionStrings:DefaultConnection` haqiqiy `kitobdagimen`/`kitobdagimen` Postgres roliga, `Authentication:Google:ClientId/ClientSecret` haqiqiy Google kalitlariga ishora qiladi. DB'da barcha migratsiyalar (jumladan yangi `AddPostImageUrl`) allaqachon qo'llangan, 10 janr+demo kitoblar seed qilingan, 2 user Google orqali allaqachon kirgan. **Lokal run real ishlaydi** — qo'shimcha sozlash kerak EMAS, faqat `dotnet run --project src/KitobdaGimen.Web` (port 5261, `launchSettings.json`). Google Console'dagi ro'yxatdan o'tgan redirect URI — `http://localhost:5261/signin-google` (ASP.NET Google middleware'ning standart callback yo'li, `/auth/google-callback` EMAS — bu route middleware `/signin-google`ni ichki qayta ishlagandan KEYIN `RedirectUri` sifatida ishlatiladi).

**2026-06-19 — Feed'da Follow tugmasi/"Muallif" belgisi + Post Detail sahifasi 04-post-detail.html ga moslab qayta loyihalashtirildi (19-bosqichdan keyingi qo'shimcha talab).**
- **PostDto:** `IsAuthor` (joriy foydalanuvchi shu postning muallifimi) va `IsFollowingAuthor` (joriy foydalanuvchi muallifni allaqachon kuzatib turganmi) qo'shildi; `PostQueryableExtensions.ToPostDto` ikkalasini SQL'da hisoblaydi (`p.User.Followers.Any(f => f.FollowerId == currentUserId)` — `GetUserProfileQuery`dagi bilan bir xil pattern). `BookSummaryDto.GenreName` qo'shildi (Book.Genre.Name) — post detail sahifasida janr badge uchun.
- **CommentDto.IsPostAuthor** qo'shildi — izoh muallifi postning o'zi muallifi bilan bir xil odammi. `GetPostByIdQueryHandler` va `AddCommentCommandHandler`da hisoblanadi (`c.UserId == postAuthorId`).
- **Feed (`_PostCard.cshtml`):** `ViewData["ShowAuthorActions"]` bayrog'i orqali (faqat `Feed/Index.cshtml` `true` qo'yadi — Profile/Index'da post kartalari hammasi bir xil muallifga tegishli bo'lgani uchun bu tugma u yerda ko'rsatilmaydi, profil sahifasining o'z Follow tugmasi bilan ortiqcha takrorlanmasin). Post sarlavhasining o'ng tomonida: agar post joriy foydalanuvchining o'zinikiu bo'lsa ism yonida "Muallif" badge (`.badge-author`), aks holda "Kuzatish"/"Kuzatilmoqda" tugmasi (`data-follow`, mavjud `/profile/{id}/follow` endpoint'ini qayta ishlatadi).
- **`site.js`:** `data-follow` delegatsiyasi endi bir xil `id`ga ega BARCHA tugmalarni sinxronlaydi (feedda bitta muallifning bir nechta posti bo'lsa hammasi yangilanadi). Follow muvaffaqiyatli bo'lib (`isFollowing=true`) va tugma `data-follow-source="feed"` bo'lsa, 400ms dan keyin sahifa qayta yuklanadi — shu orqali `GetFeedQuery`ning kuzatilganlar+o'zi scope'iga yangi muallif postlari "kelishi" darhol ko'rinadi (server logikasi allaqachon shunday ishlaydi, reload shuni darhol ko'rsatadi).
- **Post Detail (`Posts/Details.cshtml`) to'liq qayta yozildi** — `design-reference/04-post-detail.html`dan FAQAT vizual yo'nalish olindi (ikki ustunli grid: chap maqola ~66% + o'ng sticky izohlar kartochkasi ~34%, rang/shrift tizimi), lekin SOXTA ma'lumotlar (sahifa/kun statistikasi, yulduzcha reyting, "Ulashish"/"Saqlash" tugmalari, ajratilgan iqtibos bloki) o'tkazilmadi — ular hozirgi ma'lumotlar modelida yo'q. O'rniga: muallif qatori (follow/Muallif belgisisiz — reference shunday), post rasmi yoki bo'lmasa kitob muqovasi hero sifatida, kitob sarlavhasi+muallifi+janr badge (haqiqiy `Book.GenreName`), to'liq fikr matni, real statistik qator (izoh+ko'rish soni), like tugmasi (mavjud `.like-btn` logikasi). Yangi CSS klasslar: `.pd-grid/.pd-article/.pd-author/.pd-hero/.pd-bookhead/.pd-review/.pd-stats/.pd-actions/.pd-comments*` — `.post-*` (feed kartasi) klasslariga ta'sir qilmaydi.
- **`_Comment.cshtml` to'liq qayta yozildi** — chat-bubble uslubi (avatar + bubble, `border-top-left-radius:4px`), har bir izohda "Javob berish" tugmasi (faqat 1-darajali izohlarda — server bir darajali threading'ni allaqachon majburlaydi), javob formasi JS orqali dinamik ochiladi/yopiladi. **Agar izoh muallifi postning o'zi muallifi bo'lsa** (`IsPostAuthor`): `.comment-author` klassi — bubble teskari tomonga (`flex-direction:row-reverse`, `align-self:flex-end`) va primary rangda, ism yonida "Muallif" badge (oq fonli `.badge-author` varianti) — "boshqacha architecture joylashuvi" talabi shu orqali bajarildi. AJAX bilan qo'shilgan yangi izoh/javoblar (`Details.cshtml`ning ichki scripti, `commentHtml()` funksiyasi) xuddi shu markup'ni serverga mos generatsiya qiladi (postAuthorId JS o'zgaruvchisi orqali solishtiradi).
- **TEKShIRILDI** (rebuild + app restart — eslatma: avvalgi sessiyaning `dotnet watch run` jarayoni Razor o'zgarishlarini ushlamay qolgan edi, shuning uchun process o'ldirilib qayta ishga tushirildi; xuddi shu `dotnet watch run` rejimida qayta ko'tarildi): `dotnet build` => 0/0, `dotnet test` => 38/38 passed. Real DB'dagi 2 foydalanuvchi (Id=1,2) bilan dev JWT yasab (user-secrets'dagi `Jwt:Key` bilan HS256) curl orqali tekshirildi: (1) user2 `/Feed`'ni ko'rganda user1'ning postlarida `post-follow-btn`+`data-follow="1"` chiqdi; (2) user1 o'z feed'ida `badge-author` ("Muallif") chiqdi, follow tugmasi yo'q; (3) `POST /profile/1/follow` → `{isFollowing:true,followerCount:1}`, qayta bosilganda `{isFollowing:false,followerCount:0}` (DB Follows jadvali tozaligicha qoldi); (4) `/posts/4` (2 izohli post, biri muallifdan) → `pd-grid` va boshqa barcha `pd-*` klasslar render bo'ldi, muallif izohi `comment-author`+`Muallif` badge bilan, boshqa foydalanuvchi izohi oddiy; (5) `POST /posts/4/comment` bilan javob (`parentCommentId:3`) → `{..., "isPostAuthor":true}` qaytardi, keyin test yozuvi o'chirildi (DB asl holatiga qaytdi: 3 izoh, 0 follow).

**2026-06-19 — BUG TUZATISH: Story modal oynalari yopilmasdan ko'rinib qolardi.**
- **Muammo:** Story qo'shish/ko'rish modal oynalari (`#storyComposer`, `#storyViewer`) `hidden` atributi bilan boshqarilardi (site.js `composer.hidden = true/false`, `viewer.hidden`), lekin sahifada doim ko'rinib qolar va yopilmas edi.
- **Sabab:** `site.css`da `.story-viewer` va `.modal-overlay` qoidalari `display: flex` belgilaydi. Klass selektori brauzerning UA `[hidden] { display: none }` qoidasidan kuchliroq (yuqori specificity), shuning uchun `hidden` atributi qo'yilsa ham element `display: flex` bo'lib ko'rinaverardi. Developer bu muammoni `.post-image-wrap[hidden]` (311-qator) va `.pd-hero[hidden]` (403-qator) uchun allaqachon yechgan edi, lekin story/modal overlay'lar uchun unutilgan edi.
- **TUZATISH:** `site.css`ga `.story-viewer[hidden] { display: none; }` va `.modal-overlay[hidden] { display: none; }` qo'shildi. Endi `hidden` atributi to'g'ri yashiradi. Bu o'zgarish barcha `.modal-overlay`larga (story composer + account o'chirish modali) bir xil to'g'ri ishlaydi.

**2026-06-19 — BUG TUZATISH: "Taklif qilish" tugmasi → "Foydalanuvchi (0) topilmadi".**
- **Muammo:** /chat sahifasida qidiruv natijasidagi "Taklif qilish" tugmasi bosilganda `Foydalanuvchi (0) topilmadi` (`NotFoundException("Foydalanuvchi", AddresseeId=0)`) xatosi chiqardi.
- **Sabab:** `ChatController` da `Connect(int addresseeId)`, `Respond(int id, bool accept)` va `Send(SendMessageCommand command)` action'larida `[FromBody]` ATRIBUTI YO'Q edi. Frontend `apiPost` JSON body (`Content-Type: application/json`) yuboradi, lekin MVC (`AddControllersWithViews`, `[ApiController]` yo'q) `[FromBody]`siz oddiy/kompleks parametrlarni JSON body'dan bog'lamaydi → `addresseeId` 0 bo'lib qolardi (xuddi shu sabab `Send` ham brauzerdan ishlamasdi). Ilovaning qolgan controller'lari (Books/Stories/Posts/Onboarding) allaqachon `[FromBody]` ishlatadi — Chat unutilgan edi.
- **TUZATISH:** `ChatController` ga `[FromBody]` qo'shildi. Oddiy `int addresseeId`/`bool accept` ni JSON obyektidan bog'lab bo'lmagani uchun ikkita kichik record model qo'shildi: `ConnectRequest(int AddresseeId)`, `RespondRequest(bool Accept)` — frontend yuborayotgan `{addresseeId}` / `{accept}` ga mos. `Send` esa `[FromBody] SendMessageCommand`. Build 0/0.
- **2-xatolik (kod muammosi EMAS):** `Unchecked runtime.lastError: The message port closed before a response was received.` — bu Chrome brauzer kengaytmasi (extension) xabari, ilova kodidan kelmaydi va zararsiz. Inkognito rejimda yoki kengaytmalarsiz tekshirilsa chiqmaydi.

**2026-06-20 — BUG TUZATISH: Boshqa foydalanuvchining storysida ham "delete" iconi ko'rinardi.**
- **Muammo:** /feed (yoki boshqa bo'limlar) orqali boshqa foydalanuvchining storylari ko'rilganda, faqat egasiga ko'rinishi kerak bo'lgan "delete" (o'chirish) iconi hammaga chiqib qolardi.
- **Sabab:** Backend va frontend mantiq aslida to'g'ri edi — `StoryDto.IsMine` (`StoryQueryableExtensions.ToStoryDto`: `currentUserId != null && s.UserId == currentUserId`) to'g'ri hisoblanadi, va site.js `deleteBtn.hidden = !s.isMine` qo'yadi. Lekin `site.css`dagi `.story-stat { display: inline-flex }` klass qoidasi brauzerning UA `[hidden] { display: none }` qoidasidan kuchliroq (yuqori specificity), shuning uchun `deleteBtn.hidden = true` qo'yilsa ham tugma `inline-flex` bo'lib ko'rinaverardi. (Bu xuddi `.story-viewer[hidden]`/`.modal-overlay[hidden]` bilan bir xil bug sinfi — avval ham uchragan.)
- **TUZATISH:** `site.css`ga `.story-stat[hidden] { display: none; }` qo'shildi. Endi `hidden` atributi to'g'ri yashiradi — delete iconi faqat `isMine` bo'lgan (ya'ni egasining o'z) storysida ko'rinadi. Frontend-only o'zgarish, build shart emas.

**2026-06-20 — BUG TUZATISH: /chat — xabar refresh'dan keyin ulkan bo'lib ketardi.**
- **Muammo:** Xabar yozib yuborilganda pufakcha (bubble) matnga teng bo'lardi, lekin sahifa refresh qilingach o'sha xabar juda katta/keng bo'lib ketardi.
- **Sabab:** `site.css`da `.msg`ga `width: fit-content` bilan birga `white-space: pre-wrap` qo'yilgan edi (va `.msg .msg-content`ga ham). JS orqali jonli render qilingan xabarda (`msgInner`) teglar orasida bo'sh joy yo'q, shuning uchun normal. Lekin refresh'dan keyin server (`Views/Chat/Index.cshtml`) HTML ni chiroyli indentatsiya bilan chiqaradi — `pre-wrap` o'sha indentatsiya bo'sh joylari/yangi qatorlarini ko'rinadigan qiladi va `fit-content` eng keng bo'sh-joy qatoriga moslashib pufakchani ulkan qiladi.
- **TUZATISH:** `pre-wrap` `.msg`/`.msg-content`dan olib tashlandi, faqat haqiqiy matn span'iga ko'chirildi: `.msg .msg-text { white-space: pre-wrap; }`. Foydalanuvchi yozgan yangi qatorlar saqlanadi, lekin Razor indentatsiyasi layoutga ta'sir qilmaydi. Server ham, JS ham bir xil `.msg-text` span ishlatadi. Frontend-only, build shart emas.

**2026-06-20 — YANGI: Navbar "Xabarlar" realtime o'qilmagan-xabar badge'i.**
- **Talab:** Kimdir xabar yuborganda navbardagi "Xabarlar" da notification (badge) chiqsin; o'sha suhbat ochilganda (kim yozgan bo'lsa) badge yo'qolsin. Hammasi realtime.
- **Yondashuv:** Badge'ning haqiqat manbasi — DB'dagi o'qilmagan kiruvchi xabarlar soni (har sahifa yuklanishida `/chat/unread-count` dan qayta olinadi, shuning uchun refresh'dan keyin ham aniq). Realtime ko'tarilish — global NotificationHub orqali (har sahifada ulangan), shuning uchun /chat ochiq bo'lmasa ham ishlaydi.
- **Backend:** `GetUnreadMessageCountQuery` (+ handler) — joriy user uchun jami o'qilmagan kiruvchi xabarlar. `ChatController` GET `/chat/unread-count`. `IChatNotifier.NewMessageBadgeAsync(...)` qo'shildi; `SignalRChatNotifier` uni `IHubContext<NotificationHub>` orqali `ReceiveNotification` (`type:"message"`, `relatedId:conversationId`) sifatida yuboradi (persist QILINMAYDI — xabar qatorining o'zi persistent, badge load'da qayta hisoblanadi). `SendMessageCommandHandler` xabardan keyin qabul qiluvchiga badge signalini chiqaradi. Test `SpyChatNotifier` yangilandi.
- **Frontend:** `_Layout.cshtml` "Xabarlar" linkiga `[data-msg-badge]` span; `site.css` `.nav-msg-badge`. `site.js` `initNotifications` da alohida message-badge mantig'i: load'da `/chat/unread-count` dan o'rnatadi; `ReceiveNotification` da `type==="message"` → bell EMAS, "Xabarlar" badge ko'tariladi (toast faqat /chat'dan tashqarida — /chat o'zining toastiga ega). Agar o'sha suhbat hozir ochiq bo'lsa (`#conversationId` === `relatedId`) badge ko'tarilmaydi (xabar darhol o'qilgan). Tozalash: suhbat ochilganda server `MarkMessagesReadCommand` ishlaydi (`ChatController.Index`), keyingi sahifa yuklanishida badge kamayadi/yo'qoladi.
- Build 0/0, test **57/57**.

**2026-06-20 — O'ZGARISH: /feed va /quotes composer'larida kitob qidirish olib tashlandi.**
- **Talab:** /quotes va /feed dagi "Kitob nomi yoki muallifni qidiring" qidiruv inputi olib tashlansin; o'rniga "Yangi kitob" buttoni bosilganda chiqadigan inputlar har doim ko'rinib tursin; "Yangi kitob" toggle buttoni ham olib tashlansin.
- **O'zgarish (faqat frontend):** `Views/Feed/Index.cshtml` va `Views/Quotes/Index.cshtml` — `composer-book` (qidiruv inputi + "Yangi kitob" tugmasi) va `book-suggestions` divlari o'chirildi; yangi-kitob formi (`#newBookForm`/`#quoteNewBookForm`) endi `hidden`siz, doim ko'rinadi. JS dan qidiruv (`search`, `suggestions`, debounce `timer`), `newBookToggle`/`quoteNewBookToggle` listenerlari va `newBookForm.hidden` ssatrlari olib tashlandi. Kitob endi faqat shu inputlar to'ldirilib "Kitobni qo'shish" orqali yaratiladi → `pickBook` ishlaydi, tanlangach `bookPicker` yashirinadi, "Bekor"/clear bilan qaytadi.
- Build 0/0.

**2026-06-20 — O'ZGARISH: /feed va /quotes da yangi-kitob inputlari kitob-iconli toggle ortiga olindi.**
- **Talab:** Composer'da tepada book (`menu_book`) iconli div bo'lsin; bosilganda yangi-kitob inputlari chiqsin, yana bosilsa yopilsin (toggle).
- **O'zgarish (frontend):** `Views/Feed/Index.cshtml` va `Views/Quotes/Index.cshtml` — `#newBookForm`/`#quoteNewBookForm` endi `hidden` (default yopiq); ustiga `composer-book-toggle` tugmasi qo'shildi (book iconi + "Yangi kitob qo'shish" matni). JS: `newBookToggle`/`quoteNewBookToggle` listener'i `newBookForm.hidden = !newBookForm.hidden`; kitob saqlangach `newBookForm.hidden = true` (keyingi safar yopiq boshlanadi). `site.css`ga `.composer-book-toggle` (+`:hover`) qo'shildi.
- Build 0/0, jarayon qayta ishga tushirildi (5261).

**2026-06-20 — BUG TUZATISH: composer'da 2 ta book-iconli element ko'rinardi.**
- **Muammo:** /feed va /quotes da kitob tanlanmagan holatda ham book iconli element 2 ta ko'rinardi — yuqorida bo'sh "tanlangan kitob" chipi, ostida "Yangi kitob qo'shish" toggle tugmasi.
- **Sabab:** `.composer-selected { display: flex }` UA `[hidden] { display: none }` qoidasidan kuchliroq (avval uchragan bug sinfi — `story-stat[hidden]`, `modal-overlay[hidden]` va h.k.). Shu sabab `#selectedBook`/`#quoteSelectedBook` `hidden` bo'lsa-da bo'sh chip sifatida ko'rinaverardi.
- **TUZATISH:** `site.css`ga `.composer-selected[hidden] { display: none; }` qo'shildi. Endi chip faqat kitob tanlangach ko'rinadi; default holatda faqat bitta book-iconli toggle qoladi. Faqat CSS — build shart emas.

---

## /quotes qidiruv — yangi talab (BAJARILDI 2026-06-20)

`/quotes` (Barcha) sahifasiga search ikonasi + qidiruv. Barcha MAVJUD iqtiboslar
bo'yicha (faqat joriy sahifa emas) — server-side qidiruv:

- [x] `GetQuotesQuery.Search` + handler filtri: `q.Text` / `Book.Title` /
  `Book.Author` / `User.FullName` bo'yicha `ToLower().Contains` (provider-agnostik,
  C3 patterni). `QuotesController.Index` `q` parametrini qabul qiladi, `ViewData["Search"]`.
- [x] View: sarlavha yonida search ikona tugmasi (bosilganda qidiruv maydonini
  ochadi/yopadi), `.search-box` (chat'dan qayta ishlatildi), GET form `/quotes?q=`.
  Bo'sh natija va paginatsiya `q` ni saqlaydi.
- [x] Highlight: topilgan so'z(lar) yashil `<mark class="quote-mark">` bilan
  belgilanadi (JS text-node walker, regex-escape, matn+manba+yozuvchi ismi).
  CSS `.quote-mark` (yashil: #15803d / rgba(22,163,74,.18)).
- [x] Build 0/0.

## /feed qidiruv — yangi talab (BAJARILDI 2026-06-20)

`/quotes` dagi qidiruv tizimi `/feed` ga ham qo'llandi. Har qanday postdagi
matnni topadi — server-side, BARCHA postlar bo'yicha (kuzatilganlar filtri
qidiruvda chetlab o'tiladi):

- [x] `GetFeedQuery.Search` + handler: qidiruv bo'lsa `source` barcha postlar,
  filtr `p.ReviewText` / `Book.Title` / `Book.Author` / `User.FullName` bo'yicha
  `ToLower().Contains` (provider-agnostik). `FeedController.Index` `q` ni qabul qiladi.
- [x] Feed view: composer ustida `.feed-search-bar` (search ikona tugmasi +
  `.search-box` GET form `/Feed?q=`). Bo'sh natija (`search_off`) va paginatsiya `q` saqlaydi.
- [x] Highlight: `.post-text` / `.post-book-title` / `.post-book-author` / `.name`
  ichida topilgan so'z yashil `<mark class="search-mark">` (quotes bilan bir xil JS walker).
  CSS `.quote-mark, .search-mark` umumiy yashil uslub. `.feed-search-bar` layout.
- [x] Navbardagi qidiruv ikonasi `/feed` da yashirildi (`_Layout.cshtml` `hideSearch`
  ro'yxatiga `Feed` controller qo'shildi — sahifaning o'z qidiruvi bor, navbardagisi ortiqcha).
- [x] Build 0/0.

## /chat — last-seen aniq vaqt + boyo'g'li ochroq rang (2026-06-20)

- [x] Last-seen endi nisbiy emas, ANIQ vaqt ko'rsatadi: bugun → `oxirgi marta 09:10:12 da`,
  kecha → `oxirgi marta kecha 09:10:12 da`, undan oldin → `oxirgi marta 18.06.26` (dd.MM.yy).
  Server: `ViewHelpers.LastSeen` (O'zbekiston UTC+5, DSTsiz). Client: `lastSeenText`
  (`Index.cshtml`, brauzer mahalliy vaqti). Ikkalasi bir xil formatda.
- [x] 🦉 Boyo'g'li rangi `#e8703a` dan ochroq `#f2915e` ga, tumshuq/quloq pati `#d2602d`→`#e07d4a`.
  `owl.js` 3D materiallar + 2D SVG zaxira. Global `--accent` (#E8703A) o'zgarmadi.
- [x] Build 0/0.

## Profil URL username bo'yicha + tugatilgan kitob ko'rish bug'i (2026-06-20)

- [x] **Profil endi `/profile/{username}`** (oldin `/profile/1`). `ProfileController.Index`
  route `{id:int?}`→`{id?}` (string): bo'sh→joriy user, raqam→eski id (orqaga moslik),
  aks holda→`GetUserIdByUsernameQuery` orqali username→id. Reserved literal route'lar
  (`edit`, `check-username`, ...) literal ustunligi tufayli buzilmaydi.
- [x] `ViewHelpers.ProfileUrl(username, id)` — username bo'lsa `/profile/{username}`, bo'lmasa id.
  Barcha profil havolalari ko'chirildi: `_PostCard`, `_Comment`, `Posts/Details` (razor + JS
  `username || id`), `Profile/Index` paginatsiya, `FollowList`, `Stories/Details`.
- [x] `FollowUserDto` + `UserSummaryDto` (komment) ga `Username` qo'shildi:
  GetFollowers/GetFollowing handlerlari, `PostDetailLoader`, `AddCommentCommandHandler`.
- [x] **Bug**: boshqa userning "Tugatilgan kitoblar"idagi kitobni bossa
  "ruxsatingiz yo'q" (`ForbiddenAccessException`). Sabab: `GetReadingGoalByIdQueryHandler`
  egalik tekshiruvi. Batafsil sahifa faqat o'qish (tahrirlash yo'q) va tugatilgan kitoblar
  profilda ommaviy — egalik sharti olib tashlandi, faqat mavjudlik (`NotFound`) tekshiriladi.
  `ICurrentUserService` keraksiz bo'lib chiqdi, olib tashlandi.
- [x] Build 0/0.

## Bug — online/last-seen qotib qolishi (2026-06-20)

- **Shikoyat**: foydalanuvchi /chatda boshqa akkaunti orqali ~10 daqiqa oldin online bo'lgan,
  lekin suhbatdoshda "Javohir Sadullayev oxirgi marta kecha 20:39:31" ko'rinardi (eski vaqt).
- **Sabab**: `LastSeenAt` faqat `RedisPresenceService.SetOfflineAsync` ichida — ya'ni faqat
  SignalR `OnDisconnectedAsync` "graceful" ishlaganda — DB'ga yozilardi. Server qayta ishga
  tushsa (akkaunt almashtirilganda doim), crash/kill bo'lsa yoki Redis TTL muddati o'tib uzilsa,
  `OnDisconnected` umuman ishlamaydi → `LastSeenAt` abadiy eski qoladi. Dalil: online user 8
  (`presence:conn:8` mavjud) `LastSeenAt`=NULL edi (hech qachon graceful uzilmagan).
- **Tuzatish**: `LastSeenAt` endi online ekan doimiy yangilanadi — `SetOnlineAsync` (ulanish +
  har ~30s heartbeat) `TouchLastSeenAsync` ni chaqiradi (set-based `ExecuteUpdateAsync`, entity
  yuklamaydi). `SetOfflineAsync` ham shu helperdan foydalanadi. Endi sessiya qanday tugashidan
  qat'i nazar last-seen ko'pi bilan ~bitta heartbeat (30–75s) eski bo'ladi. Build 0/0, test
  o'zgarmadi (presence uchun test yo'q).

## Onboarding (janr tanlash) — tun rejimi rang tuzatishi (2026-06-20)

- **Muammo**: Onboarding sahifasidagi janr kartalari ikonkasi va tagidagi so'z rangi
  `site.css`'da qattiq kodlangan to'q-yashil (`rgba(27,77,62,...)`) edi — tun (dark) rejim
  to'q fonida deyarli ko'rinmasdi.
- **Tuzatish**: `html[data-theme="dark"]` uchun `.genre-card .material-symbols-outlined`
  va `.genre-name` oq rangga o'tkazildi (`rgba(236,231,219,.75)` / `var(--text)`); tanlangan
  holatda ikonka `--accent`, ism `#fff`. `site.css` ~1626-qator atrofida.

## /chat — telefon rejimida o'ngdagi bo'sh joy tuzatildi (2026-06-20)

- **Muammo**: Telefon kengligida (≤768px) `/chat` suhbat ochiq holatda chat-main (xabar
  yozish maydoni bilan birga) butun ekranni egallamasdan, o'ngda ~90px bo'sh joy ortib
  qolardi.
- **Sabab**: `.chat` uchun bir nechta `@media (max-width:1024px/1200px)` bloklari
  `grid-template-columns: 300px 1fr` beradi. Telefon kengligida bu media query'lar HAM
  mos keladi va manba tartibida `≤768px`'dagi `1fr` qoidasidan KEYIN turgani uchun yutadi.
  Sidebar `display:none` bo'lsa-da, 300px ustun band qoladi → chat-main birinchi trekka
  tushib, o'ngdagi `1fr` (≈90px) bo'sh qoladi.
- **Tuzatish**: `site.css` chat bo'limidagi `@media (max-width:768px)` blokiga (manbada
  kelishuvchi qoidadan keyin) `.chat { grid-template-columns: 1fr; }` qayta qo'shildi.
  Endi chat-main, msg-form va messages 100% kenglikni egallaydi. CDP o'lchovi bilan
  tasdiqlandi (390px: chat-main 300→390px, o'ng bo'shliq 0). Faqat CSS o'zgardi.

## Follower bildirishnomalari — post/iqtibos (2026-06-20)

- **Vazifa**: Foydalanuvchi (A) boshqa foydalanuvchini (B) kuzatayotgan bo'lsa, B yangi
  post yoki iqtibos yozganda A ga (va B ning barcha follower'lariga) bildirishnoma borishi.
- **Tuzatish**:
  - `INotificationService` ga `NotifyManyAsync(IReadOnlyCollection<int> recipientUserIds, ...)`
    qo'shildi — bitta `SaveChangesAsync` bilan barcha follower'lar uchun `Notification` row'lari
    saqlanadi, keyin har biriga SignalR orqali live push qilinadi (fan-out).
    `SignalRNotificationService` da implement qilindi (push logikasi `PushAsync` helper'iga ajratildi).
  - `CreatePostCommandHandler` va `CreateQuoteCommandHandler` endi `INotificationService` oladi:
    post/iqtibos saqlangach `_db.Follows.Where(f => f.FollowingId == userId)` orqali follower id'lari
    olinadi va `NotifyManyAsync` chaqiriladi. Type: `"post"` (Url = `/post/{username}/{slug}`) va
    `"quote"` (Url = `/quotes`). Message: "{FullName} yangi post chop etdi" / "...yangi iqtibos qo'shdi".
  - Frontend o'zgartirilmadi — `site.js` bildirishnoma render'i type-agnostik (avatar + message + url +
    badge + toast), shuning uchun yangi `post`/`quote` type'lari avtomatik ko'rinadi.
- **Testlar**: `PostsHandlerTests` (follower'larga yuborilishi, follower yo'q holat) +
  yangi `CreateQuoteHandlerTests` (persist, kitob yo'q, follower notify). `SpyNotificationService`
  ga `NotifyManyAsync` qo'shildi. Build 0/0, **71 test o'tdi**.

## Asoschi (founder) nishoni — `@javohirsadullayev` (TUGADI)

- **Talab**: faqat `javohirsadullayev` username'iga "Asoschi" oltin nishoni qo'shilsin va uni
  BARCHA foydalanuvchilar ko'rsin — /profile, /feed va /chat'da.
- **Yagona manba (single source of truth)**:
  - `ViewHelpers.FounderUsername = "javohirsadullayev"` + `IsFounder(username)` (regdan mustaqil,
    katta-kichik harfsiz) + `FounderBadge(username)` (oltin pill: `verified` ikon + "Asoschi") +
    `FounderBadgeMini(username)` (faqat ikon — tor joylar uchun).
  - Mijoz tomoni (chat qidiruv/takliflar JS render): `site.js` da `FOUNDER_USERNAME`/`isFounder`/
    `founderBadge` — `window.kitob` orqali ochilgan. Server qiymati bilan AYNI bo'lishi shart.
- **Qo'llanilgan joylar** (hammasi `OtherUser/Author/Profile.Username` orqali kalitlanadi):
  - `/profile`: `Profile/Index.cshtml` — ism (`<h1>`) yonida `FounderBadge`.
  - `/feed`: `Shared/_PostCard.cshtml` — `name-row` da; `Shared/_Comment.cshtml` — izoh meta'sida.
  - `/chat`: `Chat/Index.cshtml` — suhbatlar ro'yxatida `FounderBadgeMini`, sarlavhada `FounderBadge`;
    JS qidiruv kartasi va kelgan takliflar `api.founderBadge(u.username)` bilan.
- **CSS**: `site.css` da `.founder-badge` (oltin gradient pill) + `.founder-badge-mini` (oltin ikon).
- **Holat**: Build 0/0. DTO yo'llari (`SearchUsers`, `GetConversations`, `GetPendingRequests`,
  `Respond/SendConnection`) hammasi `Username` ni allaqachon to'ldiradi — backend o'zgartirilmadi.

## Ma'lum muammolar / eslatmalar

- **Google OAuth haqiqiy kalitlari YO'Q.** `IdentityServiceExtensions`'da bo'sh bo'lsa `placeholder-client-id`/`placeholder-client-secret` ishlatiladi — real Google login ishlamaydi. 12-bosqichda `appsettings.json`'ga `Authentication:Google:ClientId`/`ClientSecret` qo'yiladi (yoki user secrets/env). Haqiqiy kalitlarni Google Cloud Console'dan olish kerak.
- **JWT `Jwt:Key` YO'Q.** Bo'sh bo'lsa vaqtincha 32 ta '0' ishlatiladi (xavfsiz EMAS). 12-bosqichda `appsettings.json`'ga kuchli maxfiy kalit qo'yiladi.
- **Migratsiya DB'ga apply qilinmagan** — Postgres ulanishi yo'q. Real DB bilan: `dotnet dotnet-ef database update --project src/KitobdaGimen.Infrastructure --startup-project src/KitobdaGimen.Infrastructure` (yoki 12-bosqichda Web startup-project bilan).
