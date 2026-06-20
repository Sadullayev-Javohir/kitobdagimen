# kitobdagimen.uz — to'liq texnik spetsifikatsiya

Bu fayl o'zgarmaydi (faqat sen qo'lda tahrirlasang o'zgaradi).
Claude Code har sessiyada shu faylni o'qib, loyiha talablarini eslab oladi.

## LOYIHA HAQIDA

**Nomi:** kitobdagimen.uz
**Tavsifi:** O'zbek tilidagi kitobxonlar uchun ijtimoiy veb-platforma. Foydalanuvchilar kitob o'qish jarayonini kuzatadi, kitob postlari ulashadi, bir-birini kuzatadi (follow), iqtiboslar saqlaydi va chatda yozishadi.
**Til:** Butun foydalanuvchi interfeysi (UI matnlari, xabarlar, validatsiya xabarlari) FAQAT O'ZBEK TILIDA. Kod, comment'lar, class/method nomlari — inglizcha.
**Platforma:** Veb-sayt, to'liq responsive — Desktop, Tablet va Telefon (mobile) ekranlarining barchasida to'liq, sifatli ishlashi shart. Har bir sahifa shu uch breakpoint uchun alohida sinovdan o'tkazilgan holatda hisoblanadi (masalan: Desktop 1440px+, Tablet ~768-1024px, Telefon ~375-480px). Mobil holatda navbar gorizontal menyu o'rniga "burger" menyuga aylanishi, ko'p ustunli grid'lar bitta ustunga tushishi, kartochkalar va matnlar o'qilishi oson o'lchamda qolishi kerak.

## TEXNOLOGIK STACK

- .NET 8 (global.json bilan SDK versiyasini qotirib qo'y)
- Clean Architecture: Domain → Application → Infrastructure → Web
- CQRS + MediatR
- Entity Framework Core 8 + PostgreSQL (Npgsql)
- Redis (StackExchange.Redis)
- SignalR (real-time chat va bildirishnomalar)
- Hangfire (fon vazifalari)
- Mapster (AutoMapper o'rniga)
- FluentValidation
- Google OAuth 2.0 + JWT (HttpOnly cookie), parol bilan ro'yxatdan o'tish YO'Q
- Serilog
- xUnit

## LOYIHA TUZILISHI

```
KitobdaGimen/
├── global.json
├── KitobdaGimen.sln
├── src/
│   ├── KitobdaGimen.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── ValueObjects/
│   │   └── Common/
│   ├── KitobdaGimen.Application/
│   │   ├── Common/ (Interfaces, Behaviors)
│   │   ├── Features/ (Auth, Onboarding, Posts, Profile, Follow, ReadingGoals, Quotes, Chat)
│   │   └── DependencyInjection.cs
│   ├── KitobdaGimen.Infrastructure/
│   │   ├── Persistence/
│   │   ├── Identity/
│   │   ├── Caching/
│   │   ├── RealTime/
│   │   ├── BackgroundJobs/
│   │   └── DependencyInjection.cs
│   └── KitobdaGimen.Web/
│       ├── Controllers/ yoki Endpoints/
│       ├── Pages/ yoki Views/
│       ├── wwwroot/
│       ├── Hubs/
│       ├── appsettings.json
│       └── Program.cs
└── tests/
    └── KitobdaGimen.Application.Tests/
```

## DIZAYN TIZIMI

`design-reference/` papkasida Stitch (Google AI dizayn generatori) orqali
yaratilgan 9 ta sahifa bor:
1. Landing page, 2. Kirish sahifasi, 3. Janr tanlash, 4. Asosiy feed,
5. Post batafsil ko'rinishi, 6. Foydalanuvchi profili, 7. O'qish maqsadlari,
8. Kitob iqtiboslari, 9. Chat / Xabarlar

Ranglar: Primary `#1B4D3E`, Accent `#E8703A`, Fon `#FAF6EE`, Surface `#FFFDF8`,
Text primary `#1F2A24`, Text secondary `#6B7568`, Border `#E5DFD0`.
Sarlavhalar — serif (Georgia/Lora), tana matni — sans-serif.
Kartochkalar: 20px border-radius, yengil soya.
Navbar barcha ichki sahifalarda bir xil: chap logotip, markazda Asosiy/Kutubxona/Iqtiboslar/Xabarlar, o'ng tomonda qidiruv/bildirishnoma/avatar.

### MUHIM — Stitch kodi xom material, tayyor kod EMAS

`design-reference/` papkasidagi HTML fayllar Stitch AI tomonidan avtomatik
generatsiya qilingan va ularda quyidagi muammolar BOR DEB FARAZ QIL:

- Sahifalar orasida navbar yoki ranglar bir xil bo'lmasligi mumkin
  (Stitch ko'p ekranli izchillikni saqlay olmaydi) — shunday holatda
  shu fayldagi rang kodlarini va navbar tavsifini YAKKA HAQIQAT sifatida
  ol, design-reference fayllaridagi nomuvofiqlikni e'tiborsiz qoldir va
  to'g'irlab yoz.
- Ba'zi sahifalarda noto'g'ri/buzilgan markup, inline style "axlat"i,
  ishlamaydigan yoki ma'nosiz CSS class nomlari, qattiq kodlangan
  o'lchamlar bo'lishi mumkin.
- Ba'zi joylarda xato grafika, path matni yoki tasodifiy SVG kodi
  matn sifatida qolib ketgan bo'lishi mumkin — bularni butunlay olib tashla.
- Matnlar joy-to'ldiruvchi bo'lishi mumkin — kerak bo'lsa shu fayldagi
  ma'lumotlar modeliga mos, real ko'rinadigan o'zbekcha namuna matnlarga
  almashtir.
- Responsive emas, faqat bitta fixed o'lchamga mo'ljallangan bo'lishi
  mumkin — semantik, responsive Razor sifatida QAYTA QUR, shunchaki
  nusxa olma.

QOIDA: design-reference fayllaridan FAQAT vizual yo'nalishni (umumiy
joylashuv, qaysi elementlar qayerda, qaysi rang qayerda ishlatilgan) ol.
Kodning o'zini (HTML struktura, class nomlari, inline style'lar) so'zma-so'z
ko'chirma — har bir sahifani toza, semantik Razor view sifatida, shu
fayldagi dizayn tizimiga rioya qilgan holda QAYTADAN yoz. Agar bitta
sahifada aniq nomuvofiqlik yoki xato ko'rsang (masalan boshqa rang, boshqa
navbar tartibi), buni e'tiborsiz qoldirib, shu fayldagi standartni qo'lla
— sababini PROGRESS.md'ga bir qatorda yoz (masalan: "5-sahifa faylida
navbar tartibi boshqacha edi, standart navbarga moslashtirildi").

## MA'LUMOTLAR MODELI

- User: Id, GoogleId, Email, FullName, AvatarUrl, Bio, CreatedAt
- Genre: Id, Name
- UserGenre: UserId, GenreId
- Book: Id, Title, Author, CoverUrl, TotalPages, GenreId
- Post: Id, UserId, BookId, ReviewText, CreatedAt
- PostView: Id, PostId, UserId, ViewedAt
- Like: Id, PostId, UserId, CreatedAt
- Comment: Id, PostId, UserId, Text, CreatedAt, ParentCommentId
- Follow: Id, FollowerId, FollowingId, CreatedAt
- ReadingGoal: Id, UserId, BookId, DailyPageGoal, StartDate, CurrentPage, IsActive
- ReadingProgress: Id, ReadingGoalId, Date, PagesReadToday
- Quote: Id, UserId, BookId, Text, CreatedAt
- SavedQuote: Id, QuoteId, UserId
- Conversation: Id, User1Id, User2Id, CreatedAt
- Message: Id, ConversationId, SenderId, Text, SharedPostId, SentAt, IsRead

## BOSQICHLAR (PROGRESS.md bilan bog'liq)

1. Loyiha skeleti
2. Domain qatlami
3. Infrastructure — Persistence
4. Infrastructure — Identity (Google OAuth, JWT)
5. Application — Auth va Onboarding
6. Application — Posts
7. Application — Profile va Follow
8. Application — ReadingGoals
9. Application — Quotes
10. Application — Chat
11. Infrastructure — Redis, SignalR, Hangfire
12. Web — backend (Program.cs, DI, middleware)
13. Web — frontend sahifa 1-3 (Landing, Kirish, Janr tanlash) — Desktop/Tablet/Telefon uchun responsive
14. Web — frontend sahifa 4-6 (Asosiy feed, Post detail, Profil) — Desktop/Tablet/Telefon uchun responsive
15. Web — frontend sahifa 7-9 (O'qish maqsadlari, Iqtiboslar, Chat) — Desktop/Tablet/Telefon uchun responsive
16. SignalR Hub'lar — frontend bilan ulash
17. Migratsiya va seed data
18. Testlar
19. Yakuniy build va README.md

Har bir bosqichning aniq tafsilotlari `docs/PROGRESS.md` faylidagi izohlarda
va Claude Code'ga berilgan qadam promptlarida.