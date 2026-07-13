# kitobdagimen.uz — Biznes reja va rivojlantirish plani

> Ushbu hujjat loyihaning keyingi bosqichini — **kitob do'konini, pullik obunani,
> sovg'a/chegirma tizimlarini va oylik challenge'ni firibsiz va daromadli
> qilish** rejasini bayon etadi. Reja texnik (mavjud .NET 8 / Clean Architecture
> kod bazasiga asoslangan) va biznes (daromad modeli, KPI) qismlarni o'z ichiga
> oladi.
>
> Eng muhim muammo — **foydalanuvchi o'qigan betini o'zi kiritib, yutib
> yuborishi** (masalan 10 bet o'qib 200 deb yozish) — 3-bo'limda batafsil
> yechim berilgan.

---

## 1. Umumiy g'oya (Executive Summary)

`kitobdagimen.uz` — o'zbek kitobxonlari uchun ijtimoiy tarmoq. Hozirgi holatda:
kirish (Google OAuth), feed, postlar, kuzatish, o'qish maqsadlari, chat,
iqtiboslar, oylik challenge va g'oliblar taxtasi mavjud. Asaxiy.uz'dan kitob
ma'lumotlari olinadi.

**Yangi bosqich maqsadi:** saytni shunchaki ijtimoiy tarmoqdan **"o'qish
ekotizimiga"** aylantirish:

1. **/store** — kitob do'kon (mashhur do'kon bazasiga ulangan, kitob sotib olish).
2. **Formatlar** — online o'qish, PDF, ovozli (audio) kitoblar.
3. **Pullik obuna** — bepul / Plus / Premium darajalari.
4. **O'sish ilkalari** — kuzatuvchilar ko'payganda sovg'a, referallar, yutuqlar.
5. **Hamkorlik (B2B)** — kitob do'konlari bilan shartnomalar, chegirmalar,
   challenge g'oliblariga haqiqiy kitob sovg'a qilish.
6. **Sotuv mexanikalari** — sovg'a qilish, chegirma, bayramiy sotuvlar,
   Top 10, "Eng yaxshi kitoblar".
7. **Oylik challenge** — g'oliblar kitob do'konidan haqiqiy kitob oladi, ammo
   **o'qish haqiqiytan tekshiriladi** (3-bo'lim). Faqat online/PDF/audio emas —
   **qog'ozli (bosma) kitoblar ham** challenge'ga hisoblanadi (3.5-band).
8. **Kitob almashish + joylashuv** — foydalanuvchi o'z kitobini ro'yxatga
   qo'shadi va joylashuvini belgilaydi; boshqa foydalanuvchi "o'qimochiman"
   deb belgilagach, manzil ko'rsatiladi (2.7-band). *Mavjud "almashish"
   bo'limi asosida kengaytiriladi.*
9. **O'zbekiston xaritasi** — barcha foydalanuvchilar joylashtirgan kitoblar
   xaritada nuqtalar bilan ko'rsatiladi (2.8-band). Yangi xarita YARATILMAYDI —
   ochiq kodli kutubxona ishlatiladi.

---

## 2. Asosiy yangi bo'limlar

### 2.1. `/store` — Kitob do'koni

Mavjud `AsaxiyBookService` (asaxiy.uz integratsiyasi, 4 ta zaxira yo'l) asosida
quriladi. Do'kon ikki rejimda ishlaydi:

| Rejim | Nima | Foyda |
|---|---|---|
| **Affiliate (havola)** | Foydalanuvchi kitobni ko'radi, "Sotib olish" bosganda asaxiy.uz (yoki boshqa hamkor) sahifasiga o'tadi | Hamdordan komissiya (haridning % qismi) |
| **To'g'ridan-to'g'ri** | Platforma o'zi sotadi (online/PDF/audio ruxsatnomasi bilan) | To'liq daromad + o'qishni ichkarida tekshirish imkoniyati |

`/store` sahifasi bo'limlari:
- Kategoriyalar va janrlar bo'yicha filtr.
- Qidiruv (muallif, nom, ISBN).
- Narx diapazoni, chegirma belgisi, "yangi", "mashhur" yorliqlari.
- Kitob sahifasi: tavsif, muallif, formatlar (online/PDF/audio), narx,
  "Sovg'a qilish", "Istaklar ro'yxatiga qo'shish", reyting.

### 2.2. Kitob formatlari: Online, PDF, Audio

Har bir kitob bir nechta formatda bo'lishi mumkin. Buning uchun `BookFormat`
entity'si (yoki `Book.Formats` ro'yxati) qo'shiladi:

- **Online o'qish** — serverda paginalangan matn; brauzerda o'quvchi (reader).
  Eng qimmatli format, chunki **o'qish joyi serverda kuzatiladi** (3-bo'lim).
- **PDF** — brauzerda PDF ko'ruvchi; qaysi sahifalar haqiqatan ochilgani
  serverga xabar qilinadi.
- **Audio (ovozli)** — audio pleyer; eshitish vaqti va pozitsiyasi serverda.

### 2.3. Pullik obuna (Subscription)

Daraja | Narx (taxminiy) | Imkoniyatlar
---|---|---
**Bepul (Free)** | 0 | Feed, post, kuzatish, cheklangan online o'qish (kuniga N bet), reklama bor, asosiy statistika
**Plus** | oyiga ~29 000 so'm | Cheksiz online o'qish, PDF, reklama yo'q, offline rejim, kengaytirilgan statistika
**Premium** | oyiga ~59 000 so'm | Plus + ovozli kitoblar, oilaviy rejim (3 kishi), eksklyuziv kitoblar, ustuvor yordam

To'lov darvozalari (O'zbekiston): **Click, Payme, Uzum Bank, Oson**;
xalqaro uchun **Stripe**. *Eslatma: O'zbekistonda obunali (recurring) to'lov
qo'llab-quvvatlash'i provayderga qarab farq qiladi — 8.2-riskda.*

### 2.4. Kuzatuvchilar mukofotlari (Follower Rewards)

Foydalanuvchining kuzatuvchilari soni chegaralarni kesib o'tganda avtomatik
sovg'a:

Chegara | Sovg'a
---|---
100 kuzatuvchi | 1 ta bepul online kitob yoki 7 kun Premium
500 | 1 oy Premium yoki 30 000 so'mlik chegirma kodi
1 000 | Hamkor do'kondan 1 ta haqiqiy (qog'oz) kitob
5 000 | Yillik Premium + "Tanishtiruvchi" (Featured) belgisi
10 000 | Homiylik shartnomasi imkoniyati (o'z kitobini targ'ib qilish)

Mexanizm: `Follow` qo'shilganda `FollowerMilestoneReward` tekshiruvi ishga
tushadi (SignalR orqali "tabriklaymiz" bildirishnomasi).

### 2.5. Kitob do'kon bilan shartnomalar (B2B Partnership)

`PartnerContract` entity'si:

- Hamkor nomi, API kaliti, komissiya foizi.
- Sovg'a kvotasi (oyiga nechta kitob g'olibga beriladi).
- Chegirma kodlari (masalan `KITOBDAGIMEN10` — 10% chegirma).
- Moliyaviy hisob-kitob (har oy hisobot).

Foyda: hamkor saytga real savdo oladi; biz esa sovg'a kitoblar va chegirma
kodlari orqali foydalanuvchini platformaga bog'laymiz.

### 2.6. Sovg'a qilish, chegirma, bayramlar, Top 10, Eng yaxshilar

- **Sovg'a qilish (Gift):** foydalanuvchi boshqasiga kitob yoki obuna sovg'a
  qiladi (to'lovdan keyin qabul qiluvchiga "Sizga kitob sovg'a qilindi" xabari).
- **Chegirma:** vaqtinchalik kodlar, hamkor kodlari, "birinchi xarid" chegirmasi.
- **Bayramiy sotuvlar:** Navro'z, Yangi yil, Hayit — maxsus sahifa va banner.
- **Top 10:** haftalik/oylik eng ko'p sotilgan va eng ko'p o'qilgan 10 ta kitob.
- **Eng yaxshi kitoblar:** foydalanuvchi reytingi (5 yulduz) bo'yicha saralangan.

### 2.7. Kitob almashish (Exchange) + joylashuv belgilash

Mavjud "almashish" (Exchange) bo'limi kengaytiriladi. Foydalanuvchi **o'zida
mavjud bo'lgan** kitoblarni boshqa kitobxonlar bilan almashish yoki berish
uchun ro'yxatga qo'sha oladi. Asosiy farq va xavfsizlik nuqtasi — **manzil
faqat so'rovdan keyin ochiladi**.

**Oqim (flow):**
1. Egasi kitobni ro'yxatga qo'shadi va **kitob rasmini yuklaydi** (majburiy —
   xaritada shu rasm ko'rinadi, quyida 2.8). Rasm `ImageSharp` orqali WebP'ga
   o'girilib kichraytiriladi (mavjud mexanizm qayta ishlatiladi).
2. Egasi **joylashuvini belgilaydi** — ikki usuldan biri:
   - **a) Hozirgi joylashuv** — "Hozirgi joylashuvimni ishlatish" tugmasi
     bosilganda brauzerning **Geolocation API** chaqiriladi (`navigator
     .geolocation.getCurrentPosition`). Foydalanuvchi ruxat berganida `lat`/`lng`
     olinadi va xaritada avtomatik nuqta qo'yiladi.
   - **b) Xaritada o'zi belgilash** — foydalanuvchi Leaflet xaritasida bosib,
     **nuqta (pin) qo'yadi**; shu nuqtaning `lat`/`lng` koordinatasi saqlanadi.
     (Ikkala usulda ham fallback sifatida shahar/viloyat ro'yxatidan tanlash
     bo'ladi — geolocation rad etilsa yoki xarita ishlamasa.)
   - Koordinatalar (`lat`, `lng`) saqlanadi, lekin **boshqalarga ko'rsatilmaydi**
     — xaritada faqat umumiy nuqta + rasm ko'rinadi (aniq uy manzili emas).
3. Egasi nuqtani tasdiqlab, **kitobni shu joylashuvga joylaydi** (published).
   Endi kitob `/map` xaritasida o'z rasmini ko'rsatadi (2.8).
4. Boshqa foydalanuvchi kitobni ko'rib, **"Men o'qimochiman"** (yoki "almashmoqchiman")
   tugmasini bosadi → bu **so'rov (request)** yaratiladi.
5. **Egasi so'rovni qabul qilgungacha manzil yashirin.** So'rov qabul qilingan
   (va ikkala tomon kelishgandan) keyingina so'rovchi **egasining manzilini**
   (nuqta + matn) ko'radi.
6. Egasi rad etsa yoki vaqt o'tsa — manzil ochilmaydi.
7. So'rov qabul qilingandan keyin ikki tomon **bog'lanish usulini tanlaydi**
   (2.9-band): egasining ko'rsatgan **telefon raqami** orqali yoki **sayt
   ichidagi chat** orqali.

**Nega shunday:** xavfsizlik va spam'ni kamaytirish. Bevosita manzilni hamma
ko'rsa, firibgarlik/stalking xavfi ortadi. "Ko'rish → so'rov → ruxsat"
modeli xavfsiz va shaxsiylikni saqlaydi.

**Qo'shimcha qatlamlar:**
- Faqat autentifikatsiyalangan foydalanuvchi so'rov qila oladi.
- So'rov tarixi va holati (kutilmoqda / qabul qilindi / rad etildi / yakunlangan).
- Ikkala tomon baho berishi (trust/reyting) — ishonchli almashuvchilar ko'rinadi.
- SignalR orqali egasiga "Sizning kitobingizga so'rov keldi" bildirishnomasi.

### 2.8. O'zbekiston xaritasi — barcha kitoblar joylashuvi

Foydalanuvchilar joylashtirgan (almashish uchun qo'ygan) kitoblar **O'zbekiston
xaritasi**da nuqtalar bilan ko'rsatiladi. Maqsad — yaqin atrofdagi kitoblarni
topish va jamoaviy xaritani ko'rish.

**Yangi xarita YARATISh KERAKMI? — YO'Q.**
Xaritani noldan yozishga hojat yo'q. Ochiq kodli, bepul **Leaflet** +
**OpenStreetMap** (yoki O'zbekiston uchun mahalliy GeoJSON) ishlatiladi:

- **Leaflet.js** — eng keng tarqalgan bepul xarita kutubxonasi; kichik hajmdagi
  JS, server-rendered saytlarga mos.
- **OpenStreetMap** (OSM) yoki **OSM Uzbekistan** plitkalari — xarita fon uchun
  (API kaliti talab qilinmaydi, lekin chegaralarni hurmat qilish kerak).
- **O'zbekiston chegarasi GeoJSON** — faqat mamlakat hududini ko'rsatish va
  xaritani markazlashtirish uchun. Bu kichik fayl (github'dan ochiq).
- **Markerlar** — har bir ro'yxatga qo'shilgan kitob uchun nuqta; **nuqta ustida
  aynan shu kitobning rasmi (thumbnail) ko'rinadi** (2.7'dagi yuklangan rasm).
  Nuqta bosilganda esa popup ochilib, kitob nomi, janri, egasi (link), rasm
  va "Men o'qimochiman" tugmasi ko'rsatiladi.

**Qayerdan ko'rinadi:**
- Alohida **`/map`** sahifasi (asosiy kirish).
- Shuningdek "almashish" ro'yxatining ustida kichik xarita (preview).
- Filtr: shahar/viloyat, janr, "faqat yaqinimdagi" (foydalanuvchi joylashuviga
  nisbatan radius bo'yicha).

**Texnik:**
- `ExchangeListing` entity'si `Location` (lat, lng, shahar) va `CoverImageUrl`
  (kitob rasmi) maydonlariga ega.
- `/map` uchun后端 query: faqat joylashuvi va rasmi bor va "ko'rsatishga ruxsat"
  bo'lgan listinglarni qaytaradi. Rasm kichik hajmdagi WebP sifatida keshlanadi
  (Redis yoki brauzer), chunki xaritada ko'p nuqta bo'lganda yuklash tezligi muhim.
- Marker ikonasi sifatida `L.divIcon` ishlatiladi va ichiga `<img>` (kitob rasmi)
  joylashtiriladi — shunda xaritada har bir kitobning rasmi ko'rinadi.
- Ko'p nuqta bo'lsa — marker clustering (guruhlash) kerak bo'ladi (Leaflet
  markercluster plagin); cluster ochilganda ichidagi kitob rasmlari ko'rinadi.
- Xavfsizlik: xaritada **faqat nuqta + rasm** ko'rinadi, aniq uy manzili emas;
  aniq manzil faqat 2.7 oqimidagi so'rovdan keyin ochiladi.

### 2.9. Bog'lanish usullari (telefon / chat) va umumiy guruh

Exchange so'rovi qabul qilingandan keyin ikki tomon o'rtasida aloqa ochiladi.
Bundan tashqari barcha kitobxonlar uchun **umumiy guruh** mavjud bo'lib, unda
yozishishadi va guruh ichida o'zaro challenge o'tkazishadi.

**A) Ikki tomonlama bog'lanish (contact):**
- **Telefon orqali** — egasi ixtiyoriy ravishda **telefon raqamini ko'rsatishga
  rozilik** bergan bo'lsa, so'rov qabul qilingandan keyin raqam so'rovchiga
  ko'rsatiladi. (Raqam **faqat rozilik bilan** va faqat so'rovdan keyin ochiladi —
  xavfsizlik uchun. Rozi bo'lmasa faqat chat.)
- **Chat orqali** — sayt ichidagi mavjud **SignalR chat** orqali suhbat
  ochiladi (mavjud `Conversation`/`ChatHub` qayta ishlatiladi). Egasi va
  so'rovchi o'rtasida shaxsiy suhbat, kitobni qachon/uyaqa o'tkazish haqida
  gaplashishadi.

**B) Umumiy guruh (group chat):**
- Alohida **Exchange umumiy guruhi** (yoki shahar/viloyat bo'yicha bir nechta
  guruh) — barcha kitobxonlar a'zo bo'lishi mumkin.
- Mavjud `ChatHub`/`Conversation` kengaytiriladi: **guruh suhbati** (group
  conversation) qo'shiladi. SignalR orqali real vaqtda xabarlar.
- Guruhda foydalanuvchilar kitoblar, uchrashuvlar, almashish haqida yozishadi.

**C) Guruhda o'qish xabari (reading activity feed):**
- Guruh a'zosi kuni "10 bet o'qidim" deb kiritsa (yoki server tekshirilgan
  o'qishni aniqlasa), guruh chatiga **avtomatik xabar** tushadi:
  > "**{Foydalanuvchi}** bugun **{Kitob nomi}** ({Muallif}) kitobidan
  > **{N} bet** o'qidi (kitob jami {Jami bet} bet)."
- Bu xabar `ReadingSession`/`ChallengeVerification` (3-bo'lim) ma'lumotidan
  olinadi va **faqat tasdiqlangan o'qish** uchun yuboriladi (firibgarlikni
  oldini olish — 10 bet o'qib 200 kiritilsa guruhga 200 yozilmaydi, chunki
  guruh xabari serverdagi haqiqiy progress'dan olinadi).
- Xabar SignalR orqali guruhga real vaqtda; guruh a'zolari boshqalarni
  rag'batlantirishi uchun "like"/reaktsiya qo'yishi mumkin.
- Sozlama: foydalanuvchi o'z o'qishini guruhga chiqarishni o'chirib qo'yishi
  mumkin (maxfiylik).

**D) Guruh ichi challenge (group challenge):**
- Umumiy guruh (yoki alohida guruh) ichida **guruh a'zolari o'rtasida
  challenge** o'tkaziladi — masalan "shu hafta eng ko'p bet o'qigan".
- Mavjud `Challenge` feature'ga **guruh darajasi** (scope) qo'shiladi:
  umumiy (global) + guruh (group) turlari. Guruh challenge'ida faqat o'sha
  guruh a'zolari ishtirok etadi va reyting faqat ular orasida hisoblanadi.
- Guruh challenge'ining g'olibi ham sovg'a (kitob do'konidan) yoki medal oladi
  (3-bo'limdagi firibgarlikka qarshi himoya guruh challenge'iga ham qo'llaniladi).
- Guruh adminlari (moderatorlar) challenge'ni e'lon qilishi va yakunlashi mumkin.

**Qo'shimcha xavfsizlik:**
- Telefon raqami faqat egasi rozilik bergan va so'rov qabul qilingan holatda
  ko'rinadi; boshqalarga tarqatilmaydi.
- Guruhda spam/abuse uchun moderatsiya va xabarlarni xabar qilish (report).
- Guruh xabari faqat tekshirilgan o'qishdan olinadi — firibgarlik guruhga
  aks ettirilmaydi.

---

## 3. ENG MUHIM MUAMMO: o'qish progressini tekshirish (firibgarlikka qarshi)

### 3.1. Muammo

Hozirgi `UpdateReadingProgress` (ReadingGoals) foydalanuvchining **o'zi
kiritgan** bet sonini qabul qiladi. Challenge'ga g'olib bo'lish uchun "200 bet"
deb yozsa, tizim buni ko'r-ko'rona qabul qiladi — garchi haqiqatda 10 bet
o'qigan bo'lsa ham. Bu challenge adolatsiz va tizimga ishonchni yo'qotadi.

**Asosiy qoida:** *O'z-o'zidan kiritilgan son hech qachon challenge hisobiga
ketmasligi kerak.* Raqamli formatlar (online/PDF/audio) uchun challenge faqat
**serverda tekshirilgan** o'qishni hisoblaydi (3.3). **Qog'ozli (bosma) kitoblar**
uchun esa server o'qishni ko'ra olmaydi — ular uchun **dalil-asosli tekshiruv**
(qo'lda tasdiqlash) yo'li qo'llaniladi (3.5-band).

### 3.2. Yechim — "Platforma o'qishning manbai" (source of truth)

Eng ishonchli yechim: kitobni **platformaning o'z o'quvchisi** (online/PDF/audio)
orqali o'qishni talab qilish. Shunda server har bir betni/sahifani real vaqtda
ko'radi va "da'vo qilingan" son emas, **"ko'rilgan" son**ni hisoblaydi.

```
Foydalanuvchi o'qiydi (reader)  →  brauzer har 15-30s da heart-beat yuboradi
   { bookId, position, vaqt }   →  server ReadingSession yozadi
                                    ↓
              ChallengeProgress = serverda yozilgan real pozitsiya (o'zi emas!)
```

### 3.3. Qatlamli himoya (5 ta qatlam)

**1-qatlam — Serverda kuzatilgan pozitsiya (asosiy).**
- Online reader: har sahifa ochilganda serverga xabar; `ReadingSession` da
  `{startPage, endPage, startedAt, endedAt}` saqlanadi.
- PDF: faqat sahifa **ko'rinib o'tganini** (viewport da render bo'lganini)
  hisoblash, scrolldan o'tib ketganni emas.
- Audio: eshitish vaqti va pozitsiya; tezlik (1.5x) hisobga olinadi — lekin
  real vaqt bo'yicha.
- Challenge faqat shu sessiyalardan yig'ilgan betni oladi. 10 bet o'qisa —
  200 ni hech qanday yo'l bilan kirita olmaydi, chunki ikkinchi pozitsiya
  serverda yo'q.

**2-qatlam — Vaqt chegarasi (reading-speed envelope).**
- Inson mantiqiy tezlikda o'qiydi: matn uchun taxminan 1.5–3 bet/daqiqa.
- Agar foydalanuvchi 3 daqiqada "200 bet" da'vo qilsa → tizim rad etadi yoki
  vaqtga qarab cheklaydi (masalan maks 9 bet).
- Har bir bet uchun minimal "turish vaqti" (dwell time), masalan ≥ 8–15 soniya.
  Tez o'tib ketgan betlar hisobga olinmaydi.

**3-qatlam — Heart-beat va sessiya yaxlitligi.**
- O'quvchi har 15–30 soniyada `{bookId, position, nonce}` yuboradi.
- Server: pozitsiya faqat oldingisidan katta bo'lishi kerak; katta sakrash
  (10→200 bir zumda, oraliq beatingiz) → shubhali, farq cheklanadi.
- Soat/vaqt mantiqsizligi (kelajak vaqt) aniqlansa — sessiya bekor.
- Bitta kitobda bitta faol sessiya (ko'p qurilmada bir vaqtda o'qish — shubha).

**4-qatlam — Tasodifiy tekshiruv punktlari (proof-of-reading).**
- Har N betda yoki tasodifiy sahifada o'quvchi to'xtatilib, bitta oddiy savol
  yoki "ushbu sahifadan bir jumla ajrating" vazifasi beriladi.
- Punktni o'tmagan betlar challenge'ga **hisoblanmaydi**.
- Oddiy (ko'p tanlovli), shovqinsiz; challenge bo'lmagan o'qishda ixtiyoriy.

**5-qatlam — Egaga bog'liqlik va reputatsiya.**
- Challenge uchun kitob **egasida bo'lishi** kerak (sotib olingan yoki bepul
  kutubxona). Egasi bo'lmagan kitob o'qilsa — hisobga olinmaydi.
- `TrustScore` (ishonch balli): doimiy tekshirilgan o'qish ballni oshiradi,
  g'ayritabiiy tezlik tushiradi.
- Hangfire kunlik ishi: "kuni 300 betlik kitobda 500 bet o'qidi" kabi
  imkonsiz holatlarni avtomatik skan qilib, belgilaydi.
- Takroriy firibgarlik → challenge qatnashish taqiqlanadi, reytingdan o'chiriladi.
- Kuzatuvchilar shubhali natijani xabar berishi mumkin (moderatsiya).

### 3.4. Mualliflik huquqi (OHIRQ muhim ogohlantirish)

Platforma to'liq kitob matni/audio saqlashi uchun **noshirlik huquqi** kerak.
Huquqsiz to'liq kitob joylash — qonunbuzarlik. Shuning uchun tavsiya etilgan
**aralash modeli**:

- **A** — Faqat affiliate: foydalanuvchi hamkor sayt'da sotib oladi;
  kitobdagimen'da namuna (sample) o'qiladi + sotib olish cheki (receipt)
  tekshiriladi + namunaga comprehension savollari. Tekshirish zaifroq, lekin
  qonuniy.
- **B** — Litsenziyalash: to'liq huquqli kontent (startup uchun qiyin).
- **C** — Ommaviy mulk (public domain) + Creative Commons: ko'p o'zbek
  klassikalari (Abdulla Qodiriy, Oybek va b.) omma mulkida — ularni to'liq
  joylash qonuniy va challenge uchun eng yaxshi manba.
- **D** — O'z audio-nashrimiz: faqat ruxsat olingan kitoblar uchun.

**Tavsiya:** C (ommaviy mulk) + A (affiliate bilan receipt tekshiruvi)
aralashmasi bilan boshlash; keyinroq B/D. Bu challenge'ni ham qonuniy, ham
firibsiz qiladi.

### 3.5. Qog'ozli (bosma) kitoblarni challenge'ga hisoblash

Foydalanuvchilar online/PDF/audio bilan birga **qog'ozli kitob** ham o'qiydilar.
Server qog'ozli kitobni o'qiyotganini ko'ra olmaydi — shuning uchun "serverda
kuzatilgan pozitsiya" (3.3) shu formatga taalluqli emas. Qog'ozli kitobni ham
challenge'ga qo'shish uchun **dalil-asosli (evidence-based) tekshiruv** tizimi
kerak. Maqsad — firibgarlikni iloji boricha kamaytirish, lekin oddiy foydalanuvchi
uchun og'ir qilmaslik.

**Qoida:** qog'ozli kitob uchun challenge balli **avtomatik emas**, balki
**tasdiqlanganidan keyin** yoziladi. 10 bet o'qib "200 bet" yozishning oldi
bir necha himoya bilan olinadi.

**Qatlamli tekshiruv (paper-track):**

**1) Kitob identifikatsiyasi (egaga bog'liqlik).**
- Foydalanuvchi qog'ozli kitobni challenge'ga qo'shganda, kitob bazamizdagi
  (yoki asaxiy.uz'dan olingan) kitobga bog'lanadi va uning **umumiy bet
  soni** ma'lum bo'ladi.
- Da'vo qilinayotgan bet soni kitobning umumiy betidan osha olmaydi
  (masalan 300 betlik kitobda 500 bet o'qidi — avtomatik rad).
- Kitob "mening kutubxonam" ga qo'shilgan bo'lishi kerak (egaga bog'liqlik,
  3.3 5-qatlam).

**2) Dalil yuklash (proof of reading).**
Har bir "o'qidim" da'vosi uchun quyidagilardan **kamida bittasi** talab qilinadi:
- Kitobning **hozirgi sahifasining rasmi** (foydalanuvchi qo'lida ushlab turgan
  holatda) — `ImageSharp` orqali WebP'ga o'girilib, EXIF/metadata olib tashlanadi
  (mavjud mexanizm qayta ishlatiladi).
- Yoki "saralash" rejimi: huddi 3.3 4-qatlamdagi kabi, serverda mavjud
  **namuna matndan** tasodifiy savol (ko'p tanlovli). *Bu uchun kitobdan
  namuna (sample) matn bazada bo'lishi kerak* — ommaviy mulk kitoblari uchun
  to'liq, boshqalari uchun bir necha sahifa.*

**3) Vaqt va miqdor chegarasi (rate limit).**
- Bir kunda da'vo qilinishi mumkin bo'lgan bet soni chegaralanadi (masalan
  kuniga maks 100 bet) — tizimli "bir kechada 500 bet" hujumini oldini oladi.
- Ikki da'vo orasidagi minimal vaqt (masalan 30 daqiqa) — juda tez
  ketma-ket da'volar shubhali.
- Insoniy o'qish tezligi (1.5–3 bet/daqiqa) chegarasi qo'llaniladi: 5 daqiqada
  50 bet da'vo qilinsa — rad yoki kutishga qo'yiladi.

**4) Samarali tasdiqlash (approval flow).**
- **Avtomatik:** rasm aniq, bet soni mantiqli, tezlik normal → darhol tasdiq,
  challenge'ga qo'shiladi.
- **Yarim-avtomatik:** AI/shablon orqali rasm haqiqiyligi tekshiriladi
  (bir xil rasm qayta yuklanmaganmi, sahifa raqami o'qilishi, oldingi
  sahifadan katta). Shubhali bo'lsa kutishga qo'yiladi.
- **Qo'lda (moderatsiya):** shubhali da'volar admin yoki ishonchli
  foydalanuvchilar (TrustScore yuqori) tomonidan ko'rib chiqiladi.
- **Hamjamoa tasdiqlash:** yuqori TrustScore'ga ega foydalanuvchilar boshqalar
  da'volarini tasdiqlashi mumkin (kam miqdorda, firibgarlikni tarqatish uchun).

**5) Reputatsiya va qayta tekshiruv (3.3 5-qatlam bilan bir xil).**
- `TrustScore`: tasdiqlangan qog'ozli o'qish ballni oshiradi, rad etilgan
  da'vo tushiradi. Past ball → keyingi da'volar faqat qo'lda tasdiqlashdan o'tadi.
- Hangfire kunlik ishi: "300 betlik kitobda bir kunda 250 bet da'vo qildi"
  kabi imkonsiz holatlarni skan qiladi, shubhalilarni belgilaydi.
- Takroriy firibgarlik → challenge'dan chetlashtirish.

**6) Fotosurat bilan firibgarlikka qarshi.**
- Bir xil rasm bir necha marta yuklanishining oldi olinadi (hash/perceptual
  hash).
- Sahifa raqami oldingisidan kichik yoki bir xil bo'lsa — rad.
- "Men o'qidim" tasdiqlangandan keyin keyingi da'vo faqat undan katta bet uchun
  qabul qilinadi (orqaga qaytib firibgarlik qilishning oldi).

**Qog'ozli vs raqamli — qanday hisoblanadi:**
- Raqamli (online/PDF/audio): server real vaqtda hisoblaydi → darhol challenge.
- Qog'ozli: dalil yuklanadi → tasdiqlangandan keyin challenge'ga qo'shiladi.
- Natija: challenge reytingida ikkalasi ham bir xil "bet" bilan hisoblanadi,
  farq faqat tasdiqlash usulida.

> Muhim: qog'ozli kitob uchun 100% firibsizlik qo'lga kiritib bo'lmaydi
> (server ko'ra olmaydi), lekin dalil + tezlik chegarasi + reputatsiya
> aralashmasi firibgarlikni **iqtisodiy jihatdan foydasiz** darajaga tushiradi
> — ya'ni firibgarlikka sarflanadigan vaqt/kuch haqiqiy o'qishdan ko'ra ko'p.

---

## 4. Daromad modeli (Revenue)

Manba | Qisqacha
---|---
**Obuna** | Plus/Premium oylik to'lovlari (asosiy takrorlanuvchi daromad)
**Affiliate komissiya** | `/store` orqali boshqa do'kondan sotuvdan %
**To'g'ridan-to'g'ri sotuv** | Online/PDF/audio ruxsatnomasi
**Sovg'a va chegirma kodlari** | Sovg'a qilingan kitoblardan komissiya
**Homiylik challenge** | Brend oylik challenge'ni homiylik qiladi, g'olibga ularning kitobi
**Reklama** | Faqat Free darajada, bezovta qilmaydigan (Premium'da yo'q)
**Noshir/muallif promo** | Kitobni "Tavsiya etilgan" qilib ko'rsatish (pullik)

---

## 5. Qo'shimcha funksiyalar (rejani to'ldirish uchun)

Mavjud imkoniyatlarga qo'shimcha sifatida taklif qilinadigan funksiyalar:

1. **O'qish zanjiri (Streak)** — ketma-ket kunlar o'qish; "5 kun o'qidingiz!"
   badges. Hangfire eslatmasi bilan.
2. **Yutuqlar va medallar (Achievements)** — "100 kitob", "Marafonchi",
   "Audio muxlisi" va h.k. Profilda ko'rsatiladi.
3. **Kitob klublari (Book Clubs)** — guruhlar, guruh ichi challenge,
   umumiy reyting.
4. **Muallif sahifalari** — muallif profili, uning barcha kitoblari, follow.
5. **Referal tizimi** — do'st taklif qil, ikkingizga ham sovg'a/chegirma.
6. **Istaklar ro'yxati (Wishlist) + narx signali** — narx tushganda xabar.
7. **Sharhlar va reyting (Reviews)** — 5 yulduz + matnli sharh, foydali deb
   belgilash.
8. **O'qish kalendari** — GitHub kabi issiqlik xaritasi (har kun qancha o'qigan).
9. **Janr bo'yicha reytinglar** — "Eng ko'p o'qigan detektiv" kabi.
10. **Offline rejim** — kitobni yuklab olish (PWA/TWA), internetsiz o'qish;
    progress sinxronlanadi.
11. **Oilaviy obuna** — 3–5 kishi bitta Premium.
12. **Sovg'a kartalari / promo kodlari** — do'stlarga kod berish.
13. **Audio qulayliklari** — tezlik, xom-uqubat (sleep timer), bookmark.
14. **AI tavsiya** — o'qish tarixiga asoslangan "Sizga yoqishi mumkin".
15. **Korporativ / ta'lim obunasi** — maktab, universitet, kompaniya paketlari.
16. **Challenge turlari** — kunlik, haftalik, jamoa, janr bo'yicha.
17. **Maxsus sanalar bildirishnomasi** — yangi kitob, chegirma, do'st g'olib
    bo'ldi (SignalR push).
18. **Kirish imkoniyati (Accessibility)** — ko'zi zaiflar uchun katta shrift va
    ovozli rejim.
19. **Kitob almashish** — *mavjud* ("almashish" bo'limi) challenge'ga ulanishi.
20. **Yillik yakun kengaytirilgan** — eng ko'p o'qilgan, eng uzoq sessiya va
    boshqalar (mavjud YearReview asosida).

---

## 6. Texnik amalga oshirish rejasi (mavjud arxitektura asosida)

Loyiha **Clean Architecture + CQRS + MediatR** qolipida. Yangi bo'limlar
xuddi shu naqshda `Features/` papkasiga qo'shiladi.

### 6.1. Yangi Entity'lar (Domain)

- `BookFormat` (Online/PDF/Audio), `StoreProduct`, `PartnerContract`
- `Subscription`, `SubscriptionPlan`, `PaymentTransaction`
- `Gift`, `DiscountCoupon`
- `ReadingSession` (serverda kuzatilgan o'qish), `ReadingCheckpoint`
- `ChallengeVerification` (faqat tekshirilgan progress), `TrustScore`
- `Achievement`, `BookClub`, `WishlistItem`, `Review`, `FollowerMilestoneReward`
- `ExchangeListing` (kitob almashish ro'yxati) + `Location` (lat/lng, shahar)
- `ExchangeRequest` (so'rov: kutilmoqda/qabul/rad), `ExchangeProof` (qog'ozli
  kitob uchun rasm/dalil), `MapPin` (xarita uchun nuqta — `ExchangeListing`
  dan kelib chiqadi)
- `GroupConversation` (umumiy guruh suhbati), `GroupMember` (guruh a'zolari)
- `GroupReadingActivity` (guruhga tushadigan o'qish xabari), `GroupChallenge`
  (guruh ichi challenge, scope = group)

### 6.2. Yangi Feature'lar (Application/Features)

- `Store` — katalog, qidiruv, formatlar, buyurtma (mavjud `Books` ustiga).
- `Subscriptions` — reja tanlash, to'lov, yangilash/bekor qilish.
- `Payments` — Click/Payme/Stripe integratsiyasi, webhook.
- `Gifts` — sovg'a yaratish/qabul qilish.
- `Discounts` — kod yaratish/tegishli, bayramiy aksiyalar.
- `Partnerships` — shartnoma, komissiya hisoboti, sovg'a kvotasi.
- `ReadingVerification` — `ReadingSession` yozish, heart-beat tekshirish,
  checkpoint baholash, tezlik chegarasi (3-bo'lim yadrosi).
- `Achievements`, `BookClubs`, `Reviews`, `Recommendations`, `Wishlist`.
- `Exchange` — kitob ro'yxati, joylashuv belgilash, so'rov yaratish/qabul qilish,
  telefon/chat orqali bog'lanish, faqat so'rovdan keyin manzil ochilishi (2.7, 2.9).
- `Map` — O'zbekiston xaritasi (Leaflet + OSM), barcha listing nuqtalari,
  filtr va marker clustering (2.8).
- `GroupChat` — umumiy guruh suhbati, a'zolik, guruhga o'qish xabari
  (ReadingSession'dan), guruh ichi challenge (2.9).

### 6.3. Qayta ishlatiladigan mexanizmlar

- `AsaxiyBookService` → `/store` uchun manba.
- `Challenge` feature → `ChallengeVerification` bilan bog'lanadi (g'olib
  faqat tekshirilgan progress bilan).
- Hangfire → kunlik anomaly skan + eslatma + obuna yangilanish.
- SignalR → sovg'a/chegirma/yutuq bildirishnomalari.
- Redis → reyting, Top 10, narx signali keshlash.

### 6.4. Bosqichma-bosqich yo'l xaritasi (Roadmap)

| Faza | Nima qilinadi | Asosiy natija |
|---|---|---|
| **1** | `/store` + formatlar (online/PDF/audio) + `ReadingSession` (server kuzatuvi) | O'qish kuzatiladi, asos yaratildi |
| **2** | Obuna + to'lov darvozalari | Takrorlanuvchi daromad |
| **3** | Sovg'a, chegirma, bayram, Top 10, Eng yaxshilar | Sotuv mexanikalari |
| **4** | Kuzatuvchi mukofotlari + `PartnerContract` (B2B) | O'sish va hamkorlik |
| **5** | `ReadingVerification`: checkpoint, tezlik chegara, reputatsiya, anomaly job | Challenge firibsiz (raqamli + qog'ozli) |
| **6** | Kitob almashish + joylashuv + `/map` (Leaflet/OSM) xarita + telefon/chat bog'lanish | Jamoadan foydalanish |
| **7** | Umumiy guruh suhbati + guruhga o'qish xabari + guruh ichi challenge | Jamoaviy faollik |
| **8** | Klublar, yutuqlar, referallar, AI tavsiya, offline | Mahkamlash va kengaytirish |

---

## 7. Muvaffaqiyat ko'rsatkichlari (KPI)

- **MAU** (oylik faol foydalanuvchi) va stickiness (qaytish chastotasi).
- **Obuna konversiyasi** — bepul → Pullik foizi (maqsad ≥ 5%).
- **Challenge ishtiroki** va **tekshirilgan progress ulushi** (firibgarlik ↓).
- **Sovg'a/qayta sotib olish** (redemption) soni.
- **Hamkor sotuv komissiyasi** va to'g'ridan-to'g'ri sotuv daromadi.
- **Kuzatuvchi o'sishi** (follower reward loop samaradorligi).

---

## 8. Xavflar va kamchiliklar

| Xavf | Ta'siri | Yumshatish |
|---|---|---|
| **Mualliflik huquqi** | To'liq kitob joylash qonunbuzarlik | Ommaviy mulk + affiliate + receipt tekshiruvi (3.4) |
| **Recurring to'lov (UZ)** | Obuna avtomatik yangilanmasligi | Click/Payme/Uzum tekshiruvi, qo'lda yangilash yo'li |
| **Hamkor API ishonchliligi** | Do'kon ma'lumoti uzilishi | Mavjud 4 ta zaxira yo'l + kesh |
| **Firibgarlik murakkabligi** | Yangi usullar paydo bo'lishi | Qatlamli himoya + reputatsiya + anomaly job |
| **Kontent yetishmasligi** | Kam kitob → kam qiziqish | Ommaviy mulk klassikalari + hamkor katalog |
| **To'lov qaytarish (chargeback)** | Daromad yo'qotish | Aniq qaytarish siyosati, gift=final |
| **Joylashuv shaxsiyligi / stalking** | Manzil oshkor bo'lishi | Manzil faqat so'rovdan keyin ochiladi (2.7); xaritada faqat nuqta, aniq uy yo'q (2.8) |
| **Qog'ozli kitob firibgarligi** | Rasm/savol orqali aldash | Dalil + tezlik chegara + reputatsiya + qo'lda tasdiqlash (3.5) |
| **Xarita plitalari chegarasi** | OSM limit / blok** | Keshlash + foydalanish qoidalariga rioya; kerak bo'lsa maxsus plitalar |

---

## 9. Xulosa

Reja `kitobdagimen.uz` ni ijtimoiy tarmoqdan to'liq **o'qish ekotizimiga**
aylantiradi: do'kon (`/store`), uch format (online/PDF/audio) va qo'shimcha
**qog'ozli kitoblar**, pullik obuna, o'sish ilkalari (kuzatuvchi sovg'alari,
referallar, yutuqlar), hamkorlik shartnomalari, sotuv mexanikalari, kitob
almashish (joylashuv bilan) va **O'zbekiston xaritasi**. Eng muhimi — **oylik
challenge firibgarlikka qarshi himoyalangan** va barcha formatlarni qamrab oladi:
- Raqamli (online/PDF/audio) o'qish faqat platforma o'quvchisi orqali serverda
  tezlik, sessiya yaxlitligi, tasodifiy tekshiruv punktlari va reputatsiya orqali
  tekshiriladi.
- Qog'ozli kitob esa dalil (sahifa rasmi / namuna savoli) + tezlik chegarasi +
  reputatsiya + qo'lda tasdiqlash orqali hisoblanadi (3.5).

Shubhali "10 bet o'qib 200 kiritish" imkoni yo'q: raqamli formatda ikkinchi
pozitsiya serverda yo'q, qog'ozli formatda esa dalil va tezlik chegarasi
firibgarlikni iqtisodiy jihatdan foydasiz qiladi.

> Keyingi qadam: Faza 1 uchun `ReadingSession` entity va `Store` feature
> sketch'ini yozish; Paza 6 uchun `ExchangeListing`/`Location`/`ExchangeRequest`
> va `/map` (Leaflet+OSM) sketch'ini tayyorlash; hamda mualliflik huquqi
> modelini (3.4) yakuniy tasdiqlash.
