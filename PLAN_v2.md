# kitobdagimen.uz — Rivojlantirish rejasi (v2.0)

> Ushbu hujjat v1 (PLAN.md) asosida qayta ishlangan: **til/mantiq xatolari
> tuzatildi, xavfsizlik kamchiliklari yopildi va yangi funksiyalar qo'shildi**.
> Reja texnik (.NET 8 / Clean Architecture / CQRS + MediatR kod bazasiga
> asoslangan) va mahsulot qismlarini qamraydi. Daromad, marketing va moliya
> alohida hujjatda — **BIZNES_REJA.md**.

---

## 0. v1 dan v2 ga: nimalar tuzatildi va qo'shildi

### 0.1. Til va matn xatolari (tuzatildi)

| # | v1 dagi xato | v2 dagi to'g'ri varianti |
|---|---|---|
| 1 | «haqiqiytan tekshiriladi» | «haqiqatan tekshiriladi» |
| 2 | «O'sish ilkalari» (2 joyda) | «O'sish mexanizmlari (growth loops)» |
| 3 | «Hamdordan komissiya» | «Hamkordan komissiya» |
| 4 | «ruxat berganida» | «ruxsat berganida» |
| 5 | «后端 query» (xitoycha belgilar qolib ketgan) | «backend so'rovi (query)» |
| 6 | «o'qimochiman» (3 joyda) | «o'qimoqchiman» |
| 7 | «qachon/uyaqa o'tkazish» | «qachon va qayerda uchrashish» |
| 8 | «OHIRQ muhim ogohlantirish» | «O'TA muhim ogohlantirish» |
| 9 | «xom-uqubat (sleep timer)» | «uyqu taymeri (sleep timer)» |
| 10 | «Paza 6» | «Faza 6» |
| 11 | «kitob do'kon», «Kitob do'kon bilan» | «kitob do'koni», «Kitob do'konlari bilan» |
| 12 | «Tanishtiruvchi (Featured) belgisi» | «Ko'zga ko'ringan (Featured) belgisi» |
| 13 | 8-bo'lim jadvalida ortiqcha «**» belgilar | olib tashlandi |
| 14 | «yutib yuborishi» (ma'no noaniq) | «g'irromlik bilan yutib olishi» |

### 0.2. Mantiqiy/xavfsizlik xatolari (tuzatildi)

| # | v1 dagi muammo | v2 dagi yechim |
|---|---|---|
| 1 | **Xaritadagi nuqta = uy manzili.** v1 da «aniq manzil ko'rinmaydi, faqat nuqta» deyilgan, lekin aniq `lat/lng` bilan qo'yilgan nuqtaning o'zi uy joylashuvini fosh qiladi | Ommaviy xaritada koordinata **300–500 m radiusda xiralashtiriladi (jitter/grid-snap)**; aniq nuqta faqat so'rov tasdiqlangach ochiladi (3.7, 3.8) |
| 2 | **Kuzatuvchi mukofotlari bot-hujumga ochiq.** Soxta akkauntlar bilan 1 000 obunachi yig'ib, haqiqiy kitob yutish mumkin edi | Mukofot hisobiga faqat **tasdiqlangan va faol** obunachilar kiradi; anti-bot qatlam qo'shildi (3.4) |
| 3 | **Referal tizimida himoya yo'q edi** | Mukofot faqat referal telefonni tasdiqlab, tekshirilgan o'qish qilgandan keyin (5.15) |
| 4 | 3.5(4)-bandda «hamjamoa tasdiqlash ... firibgarlikni **tarqatish** uchun» — ma'no teskari yozilgan | «moderatsiya yukini **taqsimlash** va firibgarlikni kamaytirish uchun» (4.5) |
| 5 | «Xalqaro uchun Stripe» — **Stripe O'zbekistonda ro'yxatdan o'tgan yuridik shaxslar bilan ishlamaydi** | Muqobillar ko'rsatildi: xorijiy yuridik shaxs yoki Merchant-of-Record (3.3, 8-bo'lim) |
| 6 | **Shaxsiy ma'lumotlarni lokalizatsiya qilish qonuni** umuman hisobga olinmagan (telefon, joylashuv, chat yig'ilyapti!) | Alohida huquqiy bo'lim qo'shildi (8-bo'lim) |
| 7 | Audio tezlik qoidasi noaniq («tezlik hisobga olinadi — lekin real vaqt bo'yicha») | Aniq formula berildi (4.3, 1-qatlam) |
| 8 | Faqat Google OAuth — telefon-birinchi auditoriya (O'zbekistonning katta qismi) chetda qolgan | SMS-OTP va Telegram login qo'shildi (5.1, 5.2) |
| 9 | KPI da retention (D7/D30) va viral koeffitsient yo'q edi | Qo'shildi (9-bo'lim) |

### 0.3. Yangi qo'shilgan yirik funksiyalar (v2)

Telegram ekotizimi (bot + Mini App + login), SMS orqali kirish, mualliflar
uchun self-publishing, jamoaviy audio ovozlashtirish, ichki ball tizimi
(«Kitob tanga»), lotin↔kirill transliteratsiya, AI yordamchi, highlight/izohlar,
iqtibos kartochkalari va «Yil yakuni» ulashish, maktab kabineti, bolalar rejimi,
muallifga donat, SEO strategiyasi, fokus taymeri, anti-bot infratuzilma —
barchasi 5-bo'limda batafsil.

---

## 1. Umumiy g'oya (Executive Summary)

`kitobdagimen.uz` — o'zbek kitobxonlari uchun ijtimoiy tarmoq. Hozirgi holat:
kirish (Google OAuth), feed, postlar, kuzatish, o'qish maqsadlari, chat,
iqtiboslar, oylik challenge va g'oliblar taxtasi. Kitob ma'lumotlari
asaxiy.uz'dan olinadi.

**Yangi bosqich maqsadi:** saytni ijtimoiy tarmoqdan to'liq **«o'qish
ekotizimiga»** aylantirish:

1. **/store** — kitob do'koni (hamkor do'kon bazasiga ulangan, kitob sotib olish).
2. **Formatlar** — online o'qish, PDF, ovozli (audio) va **qog'ozli** kitoblar.
3. **Pullik obuna** — Bepul / Plus / Premium darajalari.
4. **O'sish mexanizmlari** — kuzatuvchi mukofotlari, referallar, yutuqlar,
   ulashiladigan kartochkalar.
5. **Hamkorlik (B2B)** — kitob do'konlari bilan shartnomalar, chegirmalar,
   challenge g'oliblariga haqiqiy kitob.
6. **Sotuv mexanikalari** — sovg'a qilish, chegirma, bayramiy sotuvlar, Top 10.
7. **Oylik challenge** — g'oliblar haqiqiy kitob oladi, ammo **o'qish haqiqatan
   tekshiriladi** (4-bo'lim); qog'ozli kitoblar ham hisoblanadi (4.5).
8. **Kitob almashish + joylashuv** — «ko'rish → so'rov → ruxsat» modeli (3.7).
9. **O'zbekiston xaritasi** — almashish kitoblari xarita nuqtalarida (3.8);
   tayyor ochiq kodli kutubxona (Leaflet + OSM) ishlatiladi.
10. **Telegram ekotizimi** — bot, Mini App, login, kanal (5.1) — O'zbekiston
    uchun eng muhim jalb kanali.
11. **Mualliflar platformasi** — mahalliy mualliflar o'z kitobini yuklaydi,
    daromad ulushi oladi (5.3) — kontent VA mualliflik huquqi muammosini
    bir yo'la yechadi.
12. **Yagona ball ekonomikasi («Kitob tanga»)** — barcha mukofot, referal va
    faollik bitta valyutada (5.5).

---

## 2. Mavjud holat (qisqacha)

- Backend: .NET 8, Clean Architecture, CQRS + MediatR, Hangfire, SignalR, Redis.
- Auth: Google OAuth (v2 da SMS-OTP va Telegram qo'shiladi).
- Kitob manbasi: `AsaxiyBookService` (4 ta zaxira yo'l bilan).
- Mavjud feature'lar: Feed, Posts, Follow, ReadingGoals, Chat, Quotes,
  Challenge + Leaderboard, YearReview, Exchange (bazaviy).

---

## 3. Asosiy bo'limlar

### 3.1. `/store` — Kitob do'koni

Mavjud `AsaxiyBookService` asosida quriladi. Ikki rejim:

| Rejim | Nima | Foyda |
|---|---|---|
| **Affiliate (havola)** | Foydalanuvchi «Sotib olish» bosganda hamkor (asaxiy.uz va b.) sahifasiga UTM/ref-kod bilan o'tadi | Hamkordan komissiya (haridning % qismi) |
| **To'g'ridan-to'g'ri** | Platforma o'zi sotadi (online/PDF/audio — faqat huquqi bor kontent) | To'liq daromad + o'qishni ichkarida tekshirish |

`/store` sahifasi:
- Kategoriyalar/janrlar bo'yicha filtr; qidiruv (muallif, nom, ISBN).
- Narx diapazoni, chegirma belgisi, «yangi», «mashhur» yorliqlari.
- Kitob sahifasi: tavsif, muallif, formatlar, narx, «Sovg'a qilish»,
  «Istaklar ro'yxati», reyting, sharhlar, o'xshash kitoblar.
- Har bir kitob sahifasi — **SEO landing** (5.13): server-render, to'liq meta,
  Schema.org `Book` markup.

### 3.2. Kitob formatlari: Online, PDF, Audio (+ qog'ozli)

`BookFormat` entity'si (Online / PDF / Audio / Paper):

- **Online o'qish** — serverda paginalangan matn, brauzer reader. Eng qimmatli
  format: o'qish joyi serverda kuzatiladi (4-bo'lim).
- **PDF** — brauzerda ko'ruvchi; faqat viewport'da real render bo'lgan sahifalar
  serverga hisobot qilinadi.
- **Audio** — pleer; pozitsiya va tinglash vaqti serverda.
- **Qog'ozli (Paper)** — do'konda sotiladi/almashinadi; challenge'da dalil-asosli
  tekshiruv bilan hisoblanadi (4.5).

### 3.3. Pullik obuna (Subscription)

| Daraja | Narx (taxminiy) | Imkoniyatlar |
|---|---|---|
| **Bepul (Free)** | 0 | Feed, post, kuzatish, cheklangan online o'qish (kuniga N bet), reklama bor, asosiy statistika |
| **Plus** | oyiga ~29 000 so'm | Cheksiz online o'qish, PDF, reklamasiz, offline rejim, kengaytirilgan statistika, highlight/izohlar |
| **Premium** | oyiga ~59 000 so'm | Plus + ovozli kitoblar, oilaviy rejim (3–5 kishi), eksklyuziv kitoblar, AI yordamchi to'liq, ustuvor yordam |

Qo'shimcha (v2): **yillik reja** (2 oy bepul ≈ −17%), **talaba chegirmasi**
(−30–40%, .edu email yoki talaba guvohnomasi orqali), **sovg'a obuna** (1/3/12 oy).

**To'lov darvozalari (O'zbekiston):** Click, Payme, Uzum Bank, Oson, Paynet.
Recurring (avtomatik yangilanish) provayderga qarab farq qiladi — kartani
saqlash (tokenization) bor provayder tanlanadi, bo'lmasa muddat tugashidan
3 kun oldin eslatma + bir bosishda yangilash.

**Xalqaro to'lovlar:** Stripe O'zbekistondagi yuridik shaxslarga hisob ochmaydi.
Variantlar: (a) xorijiy yurisdiktsiyada kompaniya + Stripe; (b) Merchant-of-Record
(Paddle, Lemon Squeezy) — soliq/valyutani o'zi hal qiladi; (c) xalqaro ekvayring
beruvchi mahalliy banklar. Yakuniy tanlov yurist bilan (8-bo'lim).

### 3.4. Kuzatuvchi mukofotlari (Follower Rewards) — anti-bot bilan

| Chegara | Sovg'a |
|---|---|
| 100 kuzatuvchi | 1 ta bepul online kitob yoki 7 kun Plus |
| 500 | 1 oy Premium yoki 30 000 so'mlik chegirma kodi |
| 1 000 | Hamkor do'kondan 1 ta haqiqiy (qog'oz) kitob |
| 5 000 | Yillik Premium + «Ko'zga ko'ringan» (Featured) belgisi |
| 10 000 | Homiylik shartnomasi imkoniyati (o'z kitobini targ'ib qilish) |

Mexanizm: `Follow` qo'shilganda `FollowerMilestoneReward` tekshiruvi (SignalR
orqali «tabriklaymiz» bildirishnomasi).

**Anti-bot qatlam (v2 da yangi — 0.2(2)-tuzatma):**
- Hisobga faqat **«sifatli obunachi»** kiradi: telefon yoki email tasdiqlangan,
  hisob yoshi ≥ 7 kun, so'nggi 30 kunda kamida 1 marta faollik (login/o'qish).
- Bitta qurilma/IP dan ommaviy obunalar (device fingerprint + IP klasteri)
  hisobga olinmaydi.
- 1 000+ chegaralar (moddiy sovg'a) — avtomatik tekshiruvdan keyin **qo'lda
  ko'rikdan** o'tadi (moderator 1 daqiqalik ko'rigi).
- Mukofot berilgach ommaviy «unfollow» to'lqini aniqlansa — keyingi chegaralar
  muzlatiladi, TrustScore tushadi.

### 3.5. Kitob do'konlari bilan shartnomalar (B2B Partnership)

`PartnerContract` entity'si:
- Hamkor nomi, API kaliti, komissiya foizi, hisobot davri.
- Sovg'a kvotasi (oyiga nechta kitob challenge g'olibiga beriladi).
- Chegirma kodlari (masalan `KITOBDAGIMEN10` — 10% chegirma) — kod ikkala
  tomonga trafik keltiradi: do'kon bizni reklama qiladi, biz do'konni.
- Moliyaviy hisob-kitob (oylik hisobot, akt).

Foyda: hamkor real savdo oladi; biz sovg'a kitoblar va chegirma kodlari orqali
foydalanuvchini platformaga bog'laymiz.

### 3.6. Sovg'a qilish, chegirma, bayramlar, Top 10, Eng yaxshilar

- **Sovg'a (Gift):** kitob yoki obuna sovg'a qilinadi; qabul qiluvchiga
  «Sizga kitob sovg'a qilindi» xabari (SignalR + Telegram).
- **Chegirma:** vaqtinchalik kodlar, hamkor kodlari, «birinchi xarid» chegirmasi.
- **Bayramiy sotuvlar:** Navro'z, Yangi yil, Hayit, Mustaqillik kuni, 8-mart,
  Bilimlar kuni (1-sentabr — maktab segmenti uchun kuchli sana) — maxsus sahifa.
- **Top 10:** haftalik/oylik eng ko'p sotilgan va eng ko'p o'qilgan kitoblar
  (Redis'da keshlangan reyting).
- **Eng yaxshi kitoblar:** 5 yulduzli reyting bo'yicha, janr kesimida.

### 3.7. Kitob almashish (Exchange) + joylashuv

Mavjud Exchange bo'limi kengaytiriladi. Asosiy tamoyil: **manzil faqat
so'rovdan keyin ochiladi**.

**Oqim (flow):**
1. Egasi kitobni ro'yxatga qo'shadi va **kitob rasmini yuklaydi** (majburiy —
   xaritada shu rasm ko'rinadi). Rasm `ImageSharp` orqali WebP'ga o'giriladi,
   **EXIF/GPS metadata olib tashlanadi** (aks holda rasm o'zi joylashuvni
   fosh qilishi mumkin).
2. Egasi **joylashuv belgilaydi** — ikki usuldan biri:
   - **a)** «Hozirgi joylashuvim» — brauzer Geolocation API
     (`navigator.geolocation.getCurrentPosition`); ruxsat berganida `lat/lng`
     olinadi.
   - **b)** Leaflet xaritasida o'zi bosib **pin qo'yadi**.
   - Ikkala usulda ham fallback: shahar/tuman ro'yxatidan tanlash.
3. **Maxfiylik (0.2(1)-tuzatma):** bazada aniq koordinata saqlanadi, lekin
   **ommaviy xaritaga chiqarishdan oldin server uni 300–500 m radiusda
   tasodifiy siljitadi (jitter) yoki 500 m katakka yaxlitlaydi (grid-snap)**.
   Shunda «faqat nuqta ko'rinadi» degani chindan ham uy manzilini fosh qilmaydi.
4. Egasi tasdiqlaydi → kitob `/map` da o'z rasmi bilan ko'rinadi (3.8).
5. Boshqa foydalanuvchi **«O'qimoqchiman»** (yoki «Almashmoqchiman») bosadi →
   **so'rov (request)** yaratiladi; egasiga SignalR/Telegram bildirishnoma.
6. **Egasi qabul qilgunga qadar aniq manzil yashirin.** Qabul qilingach
   so'rovchi aniq nuqtani va (egasi rozilik bergan bo'lsa) telefonini ko'radi.
7. Rad etilsa yoki muddat o'tsa — hech narsa ochilmaydi.
8. Qabul qilingach ikki tomon bog'lanish usulini tanlaydi (3.9): telefon yoki
   sayt ichidagi chat; qachon va qayerda uchrashishni kelishadi.

**Qo'shimcha qatlamlar:**
- Faqat autentifikatsiyalangan (telefon tasdiqlangan) foydalanuvchi so'rov
  yubora oladi — spam kamayadi.
- So'rov holatlari: kutilmoqda / qabul / rad / yakunlangan.
- Ikkala tomon bir-birini baholaydi (trust/reyting).
- Bir foydalanuvchi kuniga maksimal N ta so'rov (rate limit).
- «Xavfsiz uchrashuv» eslatmasi: ochiq/odam gavjum joyda uchrashish tavsiyasi.

### 3.8. O'zbekiston xaritasi (`/map`)

**Yangi xarita YARATILMAYDI** — ochiq kodli stek:
- **Leaflet.js** — bepul, yengil xarita kutubxonasi.
- **OpenStreetMap** plitkalari (API kalitsiz; OSM tile-usage siyosatiga rioya:
  kesh, o'z User-Agent, kerak bo'lsa pullik plitka provayderi).
- **O'zbekiston chegarasi GeoJSON** — markazlashtirish uchun (ochiq manba).
- **Markerlar:** `L.divIcon` ichida `<img>` — har nuqtada kitob rasmi
  (thumbnail, WebP, keshlangan). Bosilganda popup: kitob nomi, janri, egasi,
  «O'qimoqchiman» tugmasi.
- Ko'p nuqtada — **markercluster** plagini (guruhlash).

**Qayerdan ko'rinadi:** alohida `/map` sahifasi; Exchange ro'yxati ustida mini
xarita; filtrlar: viloyat/shahar, janr, «yaqinimdagilar» (radius).

**Texnik:** `ExchangeListing` → `Location` (lat, lng, jitterlangan lat/lng,
shahar) + `CoverImageUrl`. Backend so'rovi (query) faqat published va rasmi
bor listinglarni qaytaradi; jitterlangan koordinata beriladi (0.2(1)).
Rasmlar CDN/Redis keshda.

### 3.9. Bog'lanish usullari va guruhlar

**A) Ikki tomonlama bog'lanish:**
- **Telefon** — egasi ixtiyoriy rozilik bergan bo'lsa, so'rov qabul qilingach
  raqam ko'rsatiladi (faqat so'rovchiga; nusxalash loglari yoziladi).
- **Chat** — mavjud SignalR `Conversation`/`ChatHub` qayta ishlatiladi.

**B) Umumiy guruh (group chat):**
- Umumiy Exchange guruhi + viloyat/shahar guruhlari; mavjud `ChatHub`
  kengaytiriladi (group conversation).

**C) Guruhda o'qish xabari (reading activity feed):**
- Foydalanuvchining **tasdiqlangan** o'qishi guruhga avtomatik tushadi:
  > «**{Foydalanuvchi}** bugun **{Kitob}** ({Muallif}) kitobidan **{N} bet**
  > o'qidi (jami {Jami} bet).»
- Manba — `ReadingSession`/`ChallengeVerification` (4-bo'lim), ya'ni **faqat
  serverda tasdiqlangan son**: 10 bet o'qib 200 deb yozib bo'lmaydi, chunki
  xabar foydalanuvchi kiritganidan emas, server hisobidan olinadi.
- Reaksiya/like qo'yish mumkin; foydalanuvchi ulashishni o'chirib qo'ya oladi
  (maxfiylik sozlamasi).

**D) Guruh ichi challenge:**
- `Challenge` feature'ga **scope** qo'shiladi: global / group. Guruh
  challenge'ida faqat a'zolar qatnashadi, reyting ular orasida.
- G'olibga sovg'a yoki medal; 4-bo'limdagi himoya bu yerda ham amal qiladi.
- Guruh adminlari challenge e'lon qiladi va yakunlaydi.

**Xavfsizlik:** raqam faqat rozilik + qabul holatida; guruhda report/moderatsiya;
guruh xabari faqat tekshirilgan o'qishdan.

---

## 4. ENG MUHIM MUAMMO: o'qish progressini tekshirish (firibgarlikka qarshi)

### 4.1. Muammo

Hozirgi `UpdateReadingProgress` foydalanuvchi **o'zi kiritgan** sonni qabul
qiladi. «200 bet o'qidim» deb yozsa — tizim ko'r-ko'rona ishonadi, garchi 10 bet
o'qigan bo'lsa ham. Bu challenge'ni adolatsiz qiladi.

**Asosiy qoida:** *o'z-o'zidan kiritilgan son hech qachon challenge hisobiga
ketmaydi.* Raqamli formatlar uchun faqat **serverda tekshirilgan** o'qish
hisoblanadi (4.3); qog'ozli kitoblar uchun **dalil-asosli** tekshiruv (4.5).

### 4.2. Yechim — «Platforma o'qishning manbai» (source of truth)

```
Foydalanuvchi o'qiydi (reader) → brauzer har 15–30 s da heartbeat yuboradi
   { bookId, position, vaqt, nonce } → server ReadingSession yozadi
                                        ↓
        ChallengeProgress = serverda yozilgan real pozitsiya (o'zi kiritgani emas!)
```

### 4.3. Qatlamli himoya (5 qatlam) — raqamli formatlar

**1-qatlam — serverda kuzatilgan pozitsiya (asosiy).**
- Online reader: har sahifa ochilganda serverga xabar; `ReadingSession` da
  `{startPage, endPage, startedAt, endedAt}`.
- PDF: faqat viewport'da **real render bo'lgan** sahifalar hisoblanadi
  (scroll'dan uchib o'tganlar emas).
- Audio (0.2(7)-aniqlashtirish): bet ekvivalenti = tinglangan_daqiqa ×
  kitobning «bet/daqiqa» koeffitsienti. 1.5×–2× tezlikka ruxsat, lekin
  **devor-soat (wall-clock) vaqti ≥ audio_davomiylik ÷ tezlik** bo'lishi shart;
  pleerni oldinga surish «tinglangan» hisoblanmaydi.
- Challenge faqat shu sessiyalardan yig'ilgan betni oladi.

**2-qatlam — o'qish tezligi konverti (reading-speed envelope).**
- Matn uchun me'yor ~1.5–3 bet/daqiqa. 3 daqiqada «200 bet» — rad yoki vaqtga
  mos cheklov (maks ~9 bet).
- Har bet uchun minimal turish vaqti (dwell) ≥ 8–15 soniya.

**3-qatlam — heartbeat va sessiya yaxlitligi.**
- Har 15–30 s da `{bookId, position, nonce}`; pozitsiya faqat o'sishi kerak;
  katta sakrash (10→200 bir zumda) — cheklanadi.
- Kelajak vaqt/soat mantiqsizligi → sessiya bekor.
- Bitta kitobda bitta faol sessiya (parallel qurilmalar — shubha).
- Heartbeat server tomonda **imzolangan nonce** bilan (replay-attack'ka qarshi).

**4-qatlam — tasodifiy tekshiruv punktlari (proof-of-reading).**
- Har N betda yoki tasodifiy sahifada bitta oddiy savol / «shu sahifadan bir
  jumla belgilang» vazifasi. O'tilmagan betlar challenge'ga hisoblanmaydi.
- Challenge'dan tashqari o'qishda ixtiyoriy (bezovta qilmaslik uchun).

**5-qatlam — egalik va reputatsiya.**
- Challenge uchun kitob egalikda bo'lishi kerak (sotib olingan/bepul kutubxona).
- `TrustScore`: tekshirilgan o'qish oshiradi, anomaliya tushiradi.
- Hangfire kunlik ishi: «300 betlik kitobda 500 bet» kabi imkonsizliklarni skan
  qiladi va belgilaydi.
- Takroriy firibgarlik → challenge'dan chetlashtirish.
- Foydalanuvchilar shubhali natijani report qilishi mumkin.

### 4.4. Mualliflik huquqi (O'TA muhim ogohlantirish)

To'liq matn/audio saqlash uchun **noshirlik huquqi** kerak. Tavsiya etilgan
aralash model:

- **A — Affiliate + receipt:** hamkorda sotib olinadi; bizda namuna (sample)
  o'qiladi + xarid cheki tekshiriladi + namunaga comprehension savollari.
- **B — Litsenziyalash:** to'liq huquqli kontent (keyinroq).
- **C — Ommaviy mulk + CC:** o'zbek klassikalari (Abdulla Qodiriy, Oybek va b.)
  — to'liq joylash qonuniy, challenge uchun eng yaxshi manba.
- **D — O'z audio-nashrimiz:** ruxsat olingan kitoblar uchun.
- **E (v2 yangi) — Mualliflar platformasi (5.3):** muallif o'zi yuklaydi va
  litsenziyani o'zi beradi — huquq muammosi tug'ilmaydi.

**Tavsiya:** C + A + E aralashmasi bilan boshlash; keyin B/D.

### 4.5. Qog'ozli (bosma) kitoblarni challenge'ga hisoblash

Server qog'ozli o'qishni ko'ra olmaydi → **dalil-asosli tekshiruv**. Ball
avtomatik emas, **tasdiqdan keyin** yoziladi.

**1) Kitob identifikatsiyasi.** Kitob bazadagi yozuvga bog'lanadi, umumiy bet
soni ma'lum; da'vo undan oshsa — avtomatik rad. Kitob «mening kutubxonam»da
bo'lishi shart.

**2) Dalil yuklash.** Har da'vo uchun kamida bittasi:
- Hozirgi sahifaning rasmi (qo'lda ushlab); `ImageSharp` → WebP, EXIF olib
  tashlanadi; perceptual hash bilan takroriy rasm rad etiladi.
- Yoki namuna matndan tasodifiy savol (ko'p tanlovli) — buning uchun kitobdan
  namuna matn bazada bo'lishi kerak (ommaviy mulkda to'liq, boshqalarda bir
  necha sahifa).

**3) Vaqt va miqdor chegarasi.** Kuniga maks ~100 bet (sozlanadi); ikki da'vo
orasida ≥ 30 daqiqa; insoniy tezlik (1.5–3 bet/daq) qo'llanadi.

**4) Tasdiqlash oqimi.**
- Avtomatik: rasm aniq, son mantiqli, tezlik normal → darhol tasdiq.
- Yarim-avtomatik: rasm haqiqiyligi (takror emasmi, sahifa raqami o'qiladimi,
  oldingisidan kattami) AI/shablon bilan; shubhali — navbatga.
- Qo'lda: shubhalilarni admin ko'radi.
- Hamjamoa tasdiqlash (0.2(4)-tuzatma): yuqori TrustScore'li foydalanuvchilar
  boshqalar da'vosini tasdiqlashi mumkin — **moderatsiya yukini taqsimlash va
  firibgarlikni kamaytirish uchun** (kam miqdorda, o'zaro bog'liq bo'lmagan
  tekshiruvchilar).

**5) Reputatsiya.** TrustScore; past ball → faqat qo'lda tasdiq; Hangfire
anomaliya skani; takror firibgarlik → chetlashtirish.

**6) Foto-firibgarlikka qarshi.** Perceptual hash (takror rasm); sahifa raqami
faqat o'sishi kerak; tasdiqdan keyin faqat kattaroq bet uchun yangi da'vo.

**Qog'ozli vs raqamli:** reytingda ikkalasi bir xil «bet» bilan hisoblanadi;
farq faqat tasdiqlash usulida.

> Muhim: qog'ozli kitobda 100% firibsizlik imkonsiz, lekin dalil + tezlik
> chegarasi + reputatsiya aralashmasi firibgarlikni **iqtisodiy jihatdan
> foydasiz** qiladi — aldashga ketadigan kuch haqiqiy o'qishdan ko'p bo'ladi.

---

## 5. YANGI qo'shilgan funksiyalar (v2)

### 5.1. Telegram ekotizimi — O'zbekiston uchun №1 kanal

O'zbekistonda Telegram deyarli hamma foydalanadigan asosiy messenjer, shuning
uchun bu alohida «feature» emas, **strategik qatlam**:

- **Telegram Login** — bir bosishda ro'yxatdan o'tish (Google'ga qo'shimcha).
- **Bot:** kunlik o'qish eslatmasi, streak holati, challenge reytingidagi o'rin,
  «bugun X bet o'qidingiz» xulosasi, so'rov/sovg'a bildirishnomalari (SignalR
  push'ga qo'shimcha, chunki sayt yopiq bo'lsa ham bot yetib boradi).
- **Mini App:** feed, o'qish statistikasi va challenge reytingini Telegram
  ichida ochish — saytga kirmasdan.
- **Rasmiy kanal:** kunlik iqtibos kartochkalari (5.9), Top 10, g'oliblar
  e'loni — kontent-marketing manbasi.
- **Ulashish:** «streak kartochkam», «yil yakunim», iqtibos — bir tugma bilan
  Telegram'ga (viral tarqalish).

Texnik: `TelegramAccount` entity (chat_id, bog'langan user), Bot API webhook,
xabar navbati (Hangfire).

### 5.2. Telefon raqami bilan kirish (SMS-OTP)

Google hisobiga ega bo'lmagan katta auditoriya uchun: raqam + SMS kod (mahalliy
SMS-shlyuz, masalan Eskiz.uz yoki Play Mobile). Telefon tasdiqlash ayni paytda
**anti-bot poydevori** (3.4, 5.15) va Exchange xavfsizligi sharti.

### 5.3. Mualliflar platformasi (self-publishing)

Mahalliy mualliflar (yosh yozuvchilar, bloggerlar) o'z kitobini yuklaydi:
- `AuthorProfile` (tasdiqlangan muallif belgisi), kitob yuklash (matn/PDF/audio),
  narx belgilash yoki bepul tarqatish.
- **Daromad ulushi: 70% muallifga / 30% platformaga** (sozlanadi).
- Muallif sahifasi: barcha kitoblari, follow, donat tugmasi (5.12).
- Foyda: kontent tanqisligini yechadi, mualliflik huquqi muammosi yo'q
  (litsenziyani muallif beradi), mualliflar o'z auditoriyasini olib keladi
  (o'z-o'zidan marketing).
- Moderatsiya: yuklangan kontent plagiat/haqoratga tekshiriladi (navbat).

### 5.4. Jamoaviy audio ovozlashtirish (LibriVox modeli)

Ommaviy mulkdagi klassikalar uchun ko'ngillilar bob-bob ovoz yozadi:
- `NarrationProject` → boblar → ko'ngilli «take» yuklaydi → ovoz sifati
  bo'yicha jamoa ovoz beradi → eng yaxshisi rasmiy audio bo'ladi.
- Ovoz bergan ko'ngilliga: «Ovoz ustasi» yutug'i + Kitob tanga + ismi kitob
  sahifasida. Natija: **deyarli bepul audio-kutubxona** + faol hamjamiyat.

### 5.5. «Kitob tanga» — yagona ball ekonomikasi

Hozir mukofotlar tarqoq (chegirma kodi, Premium kunlari, kitob). Yagona ichki
valyuta hammasini bog'laydi:
- Tanga topish: tekshirilgan o'qish (kunlik limit bilan), streak, challenge,
  referal, sharh yozish, audio ovozlashtirish, yutuqlar.
- Tanga sarflash: chegirma, Plus/Premium kunlari, sovg'a, profil bezaklari,
  hamkor do'kon kuponlari.
- Texnik: `UserWallet`, `CoinTransaction` (double-entry, audit log).
- Muhim qoidalar: tanga **pulga qaytarib olinmaydi** (huquqiy soddalik),
  emissiya cheklangan (inflatsiya nazorati), firibgarlik aniqlansa tranzaksiya
  bekor qilinadi.

### 5.6. Alifbo va til: lotin ↔ kirill, RU interfeys

- Interfeys va kontent uchun **lotin↔kirill avtomatik transliteratsiya**
  (o'zbek matni uchun qoidalar aniq, kutubxona darajasida hal bo'ladi) —
  katta yoshli kirill auditoriyani yo'qotmaslik uchun.
- Interfeys tillari: o'zbek (lotin/kirill) + rus; keyin qoraqalpoq/ingliz.

### 5.7. AI yordamchi (o'qish tajribasi uchun)

- **Spoilersiz xulosalar:** «o'qigan joyimgacha nima bo'lgan edi?» — uzoq
  tanaffusdan keyin qaytishni osonlashtiradi (retention!).
- **Lug'at:** so'zni belgilang → ma'nosi/tarjimasi.
- **Tavsiyalar:** o'qish tarixiga asoslangan «Sizga yoqishi mumkin».
- **Kitob bo'yicha suhbat:** Premium'da, faqat huquqi bor kontent bo'yicha.
- Cheklov: AI faqat litsenziyalangan/ommaviy mulk matn ustida ishlaydi.

### 5.8. Highlight va izohlar (notes)

Matn belgilash, izoh yozish, iqtibosga aylantirish, eksport (Markdown).
Plus darajasida cheksiz. Belgilangan joy — tayyor iqtibos kartochkasi (5.9).

### 5.9. Iqtibos kartochkalari va «Yil yakuni» ulashish (viral yadro)

- **Iqtibos kartochkasi:** har qanday iqtibos/highlight → chiroyli dizayndagi
  rasm (kitob muqovasi, iqtibos, foydalanuvchi nomi, sayt logotipi) → bir
  tugmada Telegram/Instagram'ga. Har ulashish — bepul reklama.
- **«Kitob yakunim» (Spotify Wrapped uslubi):** yil oxirida (va oylik mini
  versiya) — «2026-da 4 120 bet, 17 kitob, eng sevimli janr: detektiv» —
  ulashiladigan kartochkalar seriyasi. Mavjud `YearReview` asosida.
- **Streak kartochkasi:** «30 kun ketma-ket o'qidim» — ulashish tugmasi bilan.

### 5.10. Maktab/universitet kabineti

- O'qituvchi sinf yaratadi (`SchoolClass`), o'quvchilarni taklif qiladi,
  sinf ichi challenge ochadi, progressni ko'radi (faqat tekshirilgan o'qish).
- O'zbekistondagi kitobxonlikni targ'ib qilish tashabbuslari bilan hamohang —
  B2B2C o'sish kanali (maktab orqali yuzlab o'quvchi bir yo'la keladi).
- Ta'lim tarifi: sinfga chegirmali paket.

### 5.11. Bolalar rejimi va ota-ona nazorati

- 13 yoshgacha alohida rejim: faqat bolalar adabiyoti katalogi, chat/Exchange
  o'chiq, reklama yo'q.
- Ota-ona hisobiga bog'lash, o'qish hisobotini olish.
- Bu ham xavfsizlik, ham «oilaviy obuna»ning qiymatini oshiradi.

### 5.12. Muallifga donat (tips)

Kitob/muallif sahifasida «Rahmat aytish» tugmasi — ixtiyoriy pul (Click/Payme).
Platforma ulushi 10–15%. Mualliflarni platformaga bog'laydi.

### 5.13. SEO strategiyasi (organik trafik)

- Har kitob, muallif, janr — indexlanadigan server-rendered sahifa (title,
  description, Schema.org `Book`/`Review` markup).
- Ommaviy mulk kitoblarining to'liq matni — «(kitob nomi) o'qish» so'rovlari
  uchun kuchli landing.
- Iqtiboslar sahifalari — «(muallif) iqtiboslari» so'rovlari.
- Blog: «yilning eng yaxshi 10 kitobi» kabi maqolalar.

### 5.14. Fokus rejimi (o'qish taymeri)

Pomodoro uslubidagi taymer: «25 daqiqa o'qish» seansi, bildirishnomalar
o'chadi, seans oxirida statistika. ReadingSession bilan tabiiy bog'lanadi.

### 5.15. Anti-bot infratuzilma (ko'ndalang qatlam)

3.4 va referal uchun umumiy: telefon tasdiqlash, device fingerprint, IP
klaster tahlili, hisob yoshi/faollik shartlari, rate limiting, `AuditLog`.
**Referal mukofoti** faqat taklif qilingan do'st telefonini tasdiqlab,
3 kun ichida kamida 30 daqiqa **tekshirilgan** o'qish qilgandan keyin beriladi.

---

## 6. Qo'shimcha funksiyalar ro'yxati (v1 dan saqlangan)

1. **O'qish zanjiri (Streak)** — ketma-ket kunlar; Hangfire eslatmasi.
2. **Yutuqlar (Achievements)** — «100 kitob», «Marafonchi», «Audio muxlisi».
3. **Kitob klublari (Book Clubs)** — guruhlar, guruh challenge, umumiy reyting.
4. **Muallif sahifalari** — (5.3 bilan birlashadi).
5. **Referal tizimi** — ikkala tomonga sovg'a (5.15 himoyasi bilan).
6. **Wishlist + narx signali** — narx tushganda xabar.
7. **Sharhlar va reyting** — 5 yulduz + matn, «foydali» belgisi.
8. **O'qish kalendari** — GitHub uslubidagi issiqlik xaritasi.
9. **Janr reytinglari** — «eng ko'p o'qilgan detektiv» va h.k.
10. **Offline rejim (PWA/TWA)** — yuklab olish, internetsiz o'qish, sinxron.
11. **Oilaviy obuna** — 3–5 kishi bitta Premium.
12. **Sovg'a kartalari / promo kodlar.**
13. **Audio qulayliklari** — tezlik, uyqu taymeri (sleep timer), bookmark.
14. **AI tavsiya** — (5.7 bilan birlashadi).
15. **Korporativ/ta'lim obunasi** — (5.10 bilan birlashadi).
16. **Challenge turlari** — kunlik, haftalik, jamoa, janr bo'yicha.
17. **Bildirishnomalar** — yangi kitob, chegirma, do'st g'olib bo'ldi
    (SignalR + Telegram bot).
18. **Accessibility** — katta shrift, yuqori kontrast, ekran o'quvchi mosligi.
19. **Kitob almashish ↔ challenge bog'lanishi.**
20. **Yillik yakun kengaytirilgan** — (5.9 bilan birlashadi).

---

## 7. Texnik amalga oshirish rejasi

Loyiha Clean Architecture + CQRS + MediatR qolipida; yangi bo'limlar
`Features/` papkasiga shu naqshda qo'shiladi.

### 7.1. Yangi Entity'lar (Domain)

**v1 dan:** `BookFormat`, `StoreProduct`, `PartnerContract`, `Subscription`,
`SubscriptionPlan`, `PaymentTransaction`, `Gift`, `DiscountCoupon`,
`ReadingSession`, `ReadingCheckpoint`, `ChallengeVerification`, `TrustScore`,
`Achievement`, `BookClub`, `WishlistItem`, `Review`, `FollowerMilestoneReward`,
`ExchangeListing` (+`Location`), `ExchangeRequest`, `ExchangeProof`,
`GroupConversation`, `GroupMember`, `GroupReadingActivity`, `GroupChallenge`.

**v2 da yangi:** `OtpCode` (SMS kirish), `TelegramAccount`, `UserWallet`,
`CoinTransaction`, `Highlight`, `Note`, `AuthorProfile`, `AuthorPayout`,
`Donation`, `NarrationProject`, `NarrationTake`, `SchoolClass`,
`ClassMembership`, `ReferralCode`, `ReferralConversion`, `DeviceFingerprint`,
`AuditLog`, `QuoteCardTemplate`.

Eslatma: `MapPin` alohida entity emas — `ExchangeListing` proyeksiyasi
(jitterlangan koordinata bilan).

### 7.2. Yangi Feature'lar (Application/Features)

`Store`, `Subscriptions`, `Payments` (Click/Payme/Uzum webhook — **idempotent**
handler'lar bilan!), `Gifts`, `Discounts`, `Partnerships`,
`ReadingVerification` (4-bo'lim yadrosi), `Achievements`, `BookClubs`,
`Reviews`, `Recommendations`, `Wishlist`, `Exchange`, `Map`, `GroupChat` —
hammasi v1 dagidek; qo'shimcha: `PhoneAuth`, `TelegramIntegration`, `Wallet`,
`Authors`, `Narration`, `Schools`, `Referrals`, `QuoteCards`, `Moderation`.

### 7.3. Qayta ishlatiladigan mexanizmlar

- `AsaxiyBookService` → `/store` manbasi.
- `Challenge` → `ChallengeVerification` bilan (g'olib faqat tekshirilgan
  progress bilan).
- Hangfire → anomaliya skani, eslatmalar, obuna yangilash, bot xabar navbati.
- SignalR → bildirishnomalar, guruh chat.
- Redis → reytinglar, Top 10, kesh, rate limiting.
- ImageSharp → rasm WebP + EXIF tozalash (Exchange, dalillar, iqtibos
  kartochkalari).

### 7.4. Bosqichma-bosqich yo'l xaritasi (v2)

| Faza | Nima qilinadi | Asosiy natija |
|---|---|---|
| **1. Poydevor** | SMS-OTP + Telegram login, lotin/kirill, huquqiy hujjatlar (oferta, maxfiylik), ma'lumotlar lokalizatsiyasi (8-bo'lim) | Keng auditoriyaga tayyor, qonuniy asos |
| **2. Store + kuzatuv** | `/store` (affiliate) + formatlar + `ReadingSession` (server kuzatuvi) | O'qish kuzatiladi, katalog bor |
| **3. Monetizatsiya** | Obuna + Click/Payme/Uzum + Kitob tanga asosi | Takrorlanuvchi daromad |
| **4. Adolatli challenge** | `ReadingVerification` to'liq (checkpoint, tezlik, reputatsiya, anomaly job) + qog'ozli dalil oqimi | Challenge firibsiz |
| **5. Sotuv + viral** | Sovg'a, chegirma, bayram, Top 10 + iqtibos kartochkalari, streak ulashish | Sotuv va organik tarqalish |
| **6. Almashish + xarita** | Exchange + joylashuv (jitter bilan) + `/map` (Leaflet/OSM) + kontakt oqimi | Jamoa qiymati |
| **7. Guruhlar** | Umumiy/viloyat guruhlari + o'qish xabari + guruh challenge + Telegram bot chuqur integratsiya | Jamoaviy faollik |
| **8. O'sish** | Kuzatuvchi mukofotlari + referal (anti-bot bilan) + B2B shartnomalar | O'sish mexanizmlari |
| **9. Kontent** | Mualliflar platformasi + jamoaviy audio + donat | Kontent oqimi, huquqiy toza |
| **10. Kengaytirish** | Maktab kabineti, AI yordamchi, offline PWA, klublar, bolalar rejimi | Mahkamlash |

> v1 da Store 1-fazada edi; v2 da oldiga «Poydevor» fazasi qo'yildi, chunki
> telefon-auth va huquqiy asos monetizatsiyadan oldin kerak (aks holda keyin
> qimmat migratsiya bo'ladi). Fazalar parallel yurishi mumkin.

---

## 8. Huquqiy muvofiqlik (v2 da yangi bo'lim)

> Quyidagilar rejalash uchun yo'nalish; yakuniy qarorlar O'zbekiston
> qonunchiligi bo'yicha yurist bilan tasdiqlanishi shart.

1. **Mualliflik huquqi** — 4.4 dagi model (ommaviy mulk + affiliate + mualliflar
   platformasi bilan boshlash). Huquqsiz to'liq matn/audio joylanmaydi.
2. **Shaxsiy ma'lumotlarni lokalizatsiya qilish** — O'zbekistonning «Shaxsiy
   ma'lumotlar to'g'risida»gi qonuni fuqarolar shaxsiy ma'lumotlarini
   O'zbekiston hududidagi serverlarda saqlashni talab qiladi. Biz telefon,
   joylashuv, chat kabi sezgir ma'lumot yig'amiz → **asosiy ma'lumotlar bazasi
   O'zbekistondagi data-markazda** joylashishi kerak; CDN/statik kontent
   chetda bo'lishi mumkin. Davlat reestrida operator sifatida ro'yxatdan
   o'tish masalasini tekshirish.
3. **Ommaviy oferta va maxfiylik siyosati** — obuna shartlari, avtomatik
   yangilanish, qaytarish (refund) siyosati, «sovg'a qaytarilmaydi» qoidasi,
   Kitob tanga shartlari (pulga qaytarilmasligi) yozma hujjatlarda.
4. **Yosh chegarasi** — 13 yoshgacha bolalar rejimi (5.11); Exchange va chat
   faqat kattalarga yoki ota-ona nazorati bilan.
5. **Soliq va rezidentlik** — IT Park rezidentligi soliq imtiyozlarini beradi;
   startap uchun deyarli majburiy qadam (shartlarini tekshirish).
6. **To'lovlar** — mahalliy provayderlar (Click/Payme/Uzum) shartnomalari;
   xalqaro uchun 3.3 dagi variantlar. O'zimiz to'lov qabul qilamiz, elektron
   pul emissiya qilmaymiz (litsenziya talab qilinmasligi uchun Kitob tanga
   faqat ichki, qaytarib olinmaydigan ball bo'lib qoladi).
7. **Moderatsiya majburiyatlari** — UGC (postlar, sharhlar, guruh chat) uchun
   report tizimi, taqiqlangan kontent siyosati, moderator jurnali (`AuditLog`).

---

## 9. Muvaffaqiyat ko'rsatkichlari (KPI)

- **MAU / DAU** va stickiness (DAU/MAU).
- **Retention: D1 / D7 / D30** (v2 da yangi — ushlab qolishning asosiy o'lchovi).
- **Obuna konversiyasi** — bepul → pullik (maqsad ≥ 3–5%).
- **Churn** — oylik obunani bekor qilish (maqsad ≤ 8%).
- **Tekshirilgan o'qish ulushi** — challenge progressining necha foizi
  serverda/dalil bilan tasdiqlangan (firibgarlik ↓ ko'rsatkichi).
- **Challenge ishtiroki** — MAU ning necha foizi.
- **K-faktor (viral koeffitsient)** — bitta foydalanuvchi o'rtacha nechta yangi
  foydalanuvchi olib keladi (maqsad ≥ 0.25).
- **Sovg'a/kupon redemption** soni.
- **Hamkor komissiyasi** va to'g'ridan-to'g'ri sotuv daromadi.
- **Kuzatuvchi o'sishi** (reward loop samaradorligi).
- **Ulashish soni** — iqtibos/streak/yil-yakuni kartochkalari (viral yadro
  salomatligi).

---

## 10. Xavflar va kamchiliklar

| Xavf | Ta'siri | Yumshatish |
|---|---|---|
| **Mualliflik huquqi** | To'liq kitob joylash qonunbuzarlik | Ommaviy mulk + affiliate/receipt + mualliflar platformasi (4.4, 5.3) |
| **Recurring to'lov (UZ)** | Obuna avtomatik yangilanmasligi | Tokenization'li provayder; bo'lmasa eslatma + bir bosishda yangilash (3.3) |
| **Stripe yo'qligi** | Xalqaro to'lovni qabul qila olmaslik | Xorijiy yuridik shaxs yoki Merchant-of-Record (3.3) |
| **Ma'lumot lokalizatsiyasi** | Qonunbuzarlik/bloklanish xavfi | Asosiy DB O'zbekistonda; yurist tekshiruvi (8-bo'lim) |
| **Hamkor API ishonchliligi** | Do'kon ma'lumoti uzilishi | Mavjud 4 ta zaxira yo'l + kesh |
| **Firibgarlik evolyutsiyasi** | Yangi aldash usullari | Qatlamli himoya + reputatsiya + anomaly job (4-bo'lim) |
| **Bot-obunachi/referal fermalari** | Soxta hisoblar bilan sovg'a yutish | Telefon tasdiqlash, faollik sharti, fingerprint, qo'lda ko'rik (3.4, 5.15) |
| **Kontent yetishmasligi** | Kam kitob → kam qiziqish | Ommaviy mulk + hamkor katalog + mualliflar platformasi + jamoaviy audio |
| **Chargeback/qaytarish** | Daromad yo'qotish | Aniq qaytarish siyosati; sovg'a = qaytarilmaydi (oferta) |
| **Joylashuv maxfiyligi / stalking** | Manzil oshkor bo'lishi | Jitter/grid-snap + so'rovdan keyingina aniq nuqta + EXIF tozalash (3.7) |
| **Qog'ozli kitob firibgarligi** | Rasm/savol orqali aldash | Dalil + hash + tezlik + reputatsiya + qo'lda tasdiqlash (4.5) |
| **OSM plitka limiti** | Xarita bloklanishi | Kesh + foydalanish siyosatiga rioya; kerak bo'lsa pullik plitka provayderi |
| **SMS xarajatlari** | OTP narxi o'sishi | Telegram-login'ni birinchi variant qilish; SMS faqat zarurda; rate limit |
| **Moderatsiya yuki** | UGC ko'payishi bilan xarajat | Hamjamoa tasdiqlash, avtomatik filtrlar, report tizimi |

---

## 11. Xulosa

v2 reja `kitobdagimen.uz` ni ijtimoiy tarmoqdan to'liq **o'qish ekotizimiga**
aylantiradi: do'kon, to'rt format (online/PDF/audio/qog'oz), pullik obuna,
o'sish mexanizmlari, B2B hamkorlik, almashish + O'zbekiston xaritasi, Telegram
ekotizimi, mualliflar platformasi va yagona ball ekonomikasi.

Yadro — **firibgarlikka qarshi himoyalangan challenge**:
- Raqamli formatlarda ikkinchi «pozitsiya» serverda yo'q — 10 bet o'qib 200 deb
  yozishning texnik imkoni yo'q.
- Qog'ozli kitobda dalil + tezlik chegarasi + reputatsiya firibgarlikni
  iqtisodiy jihatdan foydasiz qiladi.
- v2 da bu himoya **kuzatuvchi mukofotlari va referalga ham** kengaytirildi
  (bot-fermalarga qarshi), joylashuv maxfiyligi kamchiligi (aniq nuqta = uy
  manzili) yopildi.

> **Keyingi qadamlar:** (1) Faza 1 uchun `PhoneAuth`/`TelegramAccount` va
> huquqiy hujjatlar; (2) Faza 2 uchun `ReadingSession` + `Store` sketch;
> (3) Faza 6 uchun `ExchangeListing`/`Location`(jitter)/`ExchangeRequest` +
> `/map` sketch; (4) mualliflik huquqi modelini (4.4) yakuniy tasdiqlash;
> (5) BIZNES_REJA.md dagi 90 kunlik ishga tushirish rejasini boshlash.
