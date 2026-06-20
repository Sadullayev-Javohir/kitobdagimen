# kitobdagimen.uz

O'zbek tilidagi kitobxonlar uchun ijtimoiy veb-platforma. Foydalanuvchilar kitob
o'qish jarayonini kuzatadi, kitob postlari (sharhlar) ulashadi, bir-birini kuzatadi
(follow), iqtiboslar saqlaydi va real vaqtda chatda yozishadi.

Butun foydalanuvchi interfeysi **o'zbek tilida**. Sayt to'liq responsive — Desktop,
Tablet va Telefon ekranlarida ishlaydi.

## Imkoniyatlar

- **Google OAuth orqali kirish** (parolli ro'yxatdan o'tish yo'q), JWT HttpOnly cookie sessiyasi.
- **Onboarding** — qiziqish janrlarini tanlash.
- **Feed** — kuzatilganlar va o'zining postlari (bo'sh bo'lsa global), sahifalash.
- **Postlar** — kitob sharhi yaratish, like, izoh (bir darajali threading), ko'rishlar sanog'i.
- **Profil va Follow** — profilni tahrirlash, kuzatish/bekor qilish, kuzatuvchilar ro'yxati.
- **O'qish maqsadlari** — kunlik bet maqsadi, progress bar, kunlik bet kiritish va tarix.
- **Iqtiboslar** — iqtibos yaratish, saqlash (toggle), o'zinikini o'chirish; barcha/mening/saqlangan tablari.
- **Chat** — real vaqtda xabar almashinuvi (SignalR), o'qildi belgilash, post ulashish.
- **Bildirishnomalar** — like / izoh / follow uchun real vaqtda toast (SignalR).

## Texnologik stack

- **.NET 8** (SDK versiyasi `global.json` da `8.0.128` ga qotirilgan)
- **Clean Architecture**: Domain → Application → Infrastructure → Web
- **CQRS + MediatR**, **FluentValidation** (ValidationBehavior pipeline), **Mapster**
- **Entity Framework Core 8 + PostgreSQL** (Npgsql)
- **Redis** (StackExchange.Redis) — keshlash
- **SignalR** — real-time chat va bildirishnomalar
- **Hangfire + PostgreSQL** — fon vazifalari
- **Google OAuth 2.0 + JWT** (HttpOnly cookie)
- **Serilog** — loglar
- **xUnit + EF Core InMemory** — testlar
- Frontend: Razor Views + vanilla JS + custom CSS dizayn tizimi

## Loyiha tuzilishi

```
KitobdaGimen/
├── global.json
├── KitobdaGimen.sln
├── src/
│   ├── KitobdaGimen.Domain/          # Entity'lar, BaseEntity, enum'lar
│   ├── KitobdaGimen.Application/     # CQRS feature'lar, DTO, interfeyslar, behaviors
│   │   └── Features/                 # Auth, Onboarding, Posts, Profile, Follow,
│   │                                 #   ReadingGoals, Quotes, Chat, Books
│   ├── KitobdaGimen.Infrastructure/  # Persistence, Identity, Caching, RealTime, BackgroundJobs
│   └── KitobdaGimen.Web/             # Controllers, Views, wwwroot, Hubs, Program.cs
└── tests/
    └── KitobdaGimen.Application.Tests/   # Handler va validator testlari (35 ta)
```

## Talablar

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (8.0.128 yoki mosroq patch)
- [PostgreSQL](https://www.postgresql.org/) 14+
- [Redis](https://redis.io/) (ixtiyoriy — o'chiq bo'lsa ham sayt ishlaydi, keshlashsiz)

> Eslatma: Redis yoki Postgres ulanmasa ham startup yiqilmaydi (best-effort) —
> keshlash va DB amallarisiz sayt ko'tariladi. To'liq funksional uchun ikkalasi kerak.

## Sozlash

Sozlamalar `src/KitobdaGimen.Web/appsettings.json` da. Maxfiy qiymatlarni repozitoriyga
kommit qilmaslik uchun **user secrets** yoki **environment variable**'lardan foydalaning.

### 1. PostgreSQL ulanish satri

```
ConnectionStrings:DefaultConnection = Host=localhost;Port=5432;Database=kitobdagimen;Username=postgres;Password=<parol>
```

### 2. Redis (ixtiyoriy)

```
ConnectionStrings:Redis = localhost:6379
```

### 3. JWT maxfiy kaliti

Kamida 32 belgili kuchli maxfiy kalit qo'ying (HS256):

```
Jwt:Key = <kamida 32 belgili maxfiy satr>
```

### 4. Google OAuth kalitlari

[Google Cloud Console](https://console.cloud.google.com/) da OAuth 2.0 Client ID
yarating, ruxsat etilgan redirect URI sifatida `https://localhost:<port>/auth/google-callback`
ni qo'shing, so'ng:

```
Authentication:Google:ClientId     = <client-id>
Authentication:Google:ClientSecret = <client-secret>
```

> Kalitlar bo'sh bo'lsa placeholder ishlatiladi va real Google login ishlamaydi
> (qolgan sayt ko'rinadi).

### Misol — user secrets bilan (tavsiya etiladi)

```bash
cd src/KitobdaGimen.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=kitobdagimen;Username=postgres;Password=parol"
dotnet user-secrets set "Jwt:Key" "kamida-32-belgili-juda-maxfiy-kalit-1234567890"
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>"
```

## Ma'lumotlar bazasini tayyorlash

EF Core local tool (`dotnet-ef` 8.0.8) `.config/dotnet-tools.json` da. Avval tiklang:

```bash
dotnet tool restore
```

Migratsiyalarni qo'llang:

```bash
dotnet dotnet-ef database update \
  --project src/KitobdaGimen.Infrastructure \
  --startup-project src/KitobdaGimen.Web
```

> Sayt startup'ida ham migratsiya + seed avtomatik bajariladi
> (`DbInitializer`): janrlar `HasData` orqali, namuna kitoblar esa runtime'da
> (faqat `Books` jadvali bo'sh bo'lsa). Ulanish bo'lmasa startup ogohlantirish
> bilan davom etadi.

## Ishga tushirish

```bash
dotnet run --project src/KitobdaGimen.Web
```

So'ng brauzerda chiqqan manzilni oching (masalan `http://localhost:5261`).

Hangfire dashboard `appsettings`'da `Hangfire:Enabled=true` bo'lsa `/hangfire`
manzilida ochiladi. Development profilida Hangfire o'chiq (`appsettings.Development.json`).

## Build va test

```bash
# To'liq solution build (release)
dotnet build KitobdaGimen.sln -c Release

# Testlar (35 ta — handler va validator)
dotnet test KitobdaGimen.sln
```

## Dizayn tizimi

- Ranglar: Primary `#1B4D3E`, Accent `#E8703A`, Fon `#FAF6EE`, Surface `#FFFDF8`.
- Sarlavhalar — serif (Lora), tana matni — sans-serif (Source Sans 3).
- Kartochkalar 20px radius, yengil soya. Barcha ichki sahifalarda yagona navbar
  (mobil holatda burger menyu).

`design-reference/` papkasidagi Stitch fayllari faqat vizual yo'nalish uchun
ishlatilgan — kod so'zma-so'z ko'chirilmagan, har sahifa toza Razor sifatida qayta yozilgan.

## Litsenziya

Ichki loyiha — litsenziya hali belgilanmagan.
