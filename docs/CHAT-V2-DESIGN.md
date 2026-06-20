# /chat 2.0 — Loyihalash hujjati (foydalanuvchi qidiruvi · taklif tizimi · 3D boyo'g'li · online/last-seen · double-tick)

> Bu **dizayn/loyiha** hujjati (talab: "BUNI LOYIHALASHTIR"). Kod yozishdan oldin to'liq arxitektura
> shu yerda qotiriladi. Amalga oshirish `PROGRESS.md` dagi yangi bosqichlar bo'yicha, bosqichma-bosqich.
> Mavjud Clean Architecture (Domain → Application(CQRS+MediatR) → Infrastructure → Web) buzilmaydi.

## 1. Talab (qisqacha)

1. `/chat` da **mavjud foydalanuvchilarni qidirish** — natijada to'liq ism, username, bio, story'lari ko'rinadi.
2. Har bir foydalanuvchiga **"Taklif qilish"** tugmasi.
3. `/chat` dagi **"Bilimdon boyo'g'li"** (3D, three.js) — boshini aylantiradi, ko'zlarini har tomonga yuradi,
   hamma tomonga qaraydi. Kimdir senga taklif yuborsa **o'sha zahoti** real-time aytadi.
4. Taklifni **qabul qilsang** — o'sha foydalanuvchi sening `/chat`ingda, sen esa uning `/chat`ida ko'rinasan.
5. Profilda **online holati** va **oxirgi marta qachon online** bo'lgani ko'rinadi.
6. O'qilgan xabarlarda **2 ta tick** (double-tick) belgisi.
7. Xabar matni **5000 belgidan oshmasin** (hozir validator 4000 — o'zgartiriladi).

## 2. Mavjud holatdan farq (nima o'zgaradi)

| Soha | Hozir | 2.0 |
|------|-------|-----|
| Suhbat ochish | Har kim har kimga `GetOrCreateConversation` bilan yozaveradi | Avval **taklif → qabul** (Connection) kerak |
| Chat ro'yxati manbai | Xabari bor `Conversation`lar | **Qabul qilingan Connection**lar (xabar bo'lmasa ham ko'rinadi) |
| Qidiruv | Yo'q | `/chat`da foydalanuvchi qidiruvi |
| Online/last-seen | Yo'q | SignalR presence + Redis + `User.LastSeenAt` |
| O'qildi | `Message.IsRead` bor, UI'da ko'rsatilmaydi | 1 tick (yuborildi) / 2 tick ko'k (o'qildi) |
| Matn limiti | 4000 | 5000 |
| Boyo'g'li | Yo'q | 3D animatsion ko'makchi + real-time taklif e'loni |

---

## 3. Domain qatlami o'zgarishlari

### 3.1 Yangi entity: `Connection` (taklif/aloqa)
`src/KitobdaGimen.Domain/Entities/Connection.cs`

```
Connection : BaseEntity
  int       RequesterId      // taklif yuborgan
  User      Requester
  int       AddresseeId      // taklif olgan
  User      Addressee
  ConnectionStatus Status    // Pending | Accepted | Declined
  DateTime  CreatedAt        // taklif yuborilgan vaqt
  DateTime? RespondedAt      // qabul/rad etilgan vaqt
```

Enum `src/KitobdaGimen.Domain/Enums/ConnectionStatus.cs`: `Pending=0, Accepted=1, Declined=2`.
(Hozircha `Enums/` papka bo'sh — spec'da shunday rejalashtirilgan, shu yerda to'ldiriladi.)

`User.cs` ga 2 ta navigation qo'shiladi:
```
ICollection<Connection> SentConnections      // RequesterId
ICollection<Connection> ReceivedConnections  // AddresseeId
```

### 3.2 `User` ga presence ustuni
```
DateTime? LastSeenAt   // oxirgi online vaqt (Redis ishlamasa fallback, va "oxirgi marta" uchun doimiy manba)
```
Online holati **Redis presence** (tezkor, ephemeral) orqali; `LastSeenAt` esa diskonnektda DB ga yoziladi va
"oxirgi marta N daqiqa oldin" matnining manbai bo'ladi.

### 3.3 `Message` — yetkazilish holati (double-tick uchun)
`Message.IsRead` (bor) yetarli emas: 1 tick (yuborildi/yetkazildi) va 2 ko'k tick (o'qildi) farqi kerak.
Minimal yondashuv: mavjud `IsRead` ni ishlatamiz —
- **1 kulrang tick** = yuborilgan/yetkazilgan (`IsRead=false`)
- **2 ko'k tick** = `IsRead=true`

(Ixtiyoriy kengaytma — alohida `DeliveredAt`/`ReadAt` — hozircha SHART EMAS; `IsRead` bool kifoya.
Agar kelajakda "yetkazildi vs yuborildi" kerak bo'lsa, `MessageStatus` enum sifatida qo'shiladi.)

### 3.4 EF Configuration
`ConnectionConfiguration`:
- `RequesterId → User` **Restrict**, `AddresseeId → User` **Restrict** (User'ga ikki FK — Follow/Conversation bilan bir xil konvensiya).
- **Unique filtered index** tartiblangan juftlik bo'yicha takror taklifni bloklash uchun. PostgreSQL'da
  yo'naltirilgan juftlik bir xil, lekin teskari juftlik ham bo'lishi mumkin —
  shuning uchun **ikkala yo'nalishni** tekshirish handler'da (3.5) qilinadi, index esa `(RequesterId, AddresseeId)` unique.
- `Status` — `HasConversion<int>()`.

`User.LastSeenAt` — oddiy nullable ustun, indeks shart emas.

Migratsiya: `AddConnectionsAndLastSeen` — `Connections` jadvali + `Users.LastSeenAt` ustuni.
Startup `DbInitializer.MigrateAsync` avtomatik qo'llaydi (mavjud konvensiya).

---

## 4. Application qatlami (CQRS — MediatR)

Yangi feature papkasi: `Features/Connections/` + `Features/Chat` kengaytmalari + `Features/Profile`/`Users` qidiruv.

### 4.1 Foydalanuvchi qidiruvi
`Features/Users/Queries/SearchUsers/`
- `SearchUsersQuery { string Q; int Page=1; int PageSize=20 }`
- Handler: `Users` dan `Q` bo'yicha (`FullName ILIKE %q%` OR `Username ILIKE %q%`), o'zini chiqarib tashlaydi.
  Har bir natija uchun **joriy foydalanuvchi bilan connection holati** ham qaytariladi (tugma holatini bilish uchun).
- DTO `UserSearchResultDto`:
  ```
  int Id; string? Username; string FullName; string? AvatarUrl; string? Bio;
  bool HasStory;                  // faol (muddati tugamagan) story bormi — StoryQueryableExtensions.WhereActive()
  bool IsOnline;                  // Web qatlamida Redis presence bilan to'ldiriladi (Application buni null beradi)
  DateTime? LastSeenAt;
  ConnectionState ConnectionState; // None | Pending_Outgoing | Pending_Incoming | Connected | (Self)
  ```
- `ConnectionState` — tugma qanday ko'rinishini hal qiladi:
  `None`→"Taklif qilish"; `Pending_Outgoing`→"Yuborildi" (disabled); `Pending_Incoming`→"Qabul qilish";
  `Connected`→"Suhbatlashish".
- **Story'lari**: qidiruv natijasida faqat `HasStory` flag. Story tafsilotlari mavjud
  `GetUserStoriesQuery` (`/stories/user/{id}`) orqali bosilganda ochiladi (kod takrorlanmaydi).
- IsOnline/LastSeenAt: Application DB'dan `LastSeenAt` ni beradi; **IsOnline** ni Web controller Redis presence'dan
  to'ldiradi (Application Redis presence'ni bilmaydi — faqat Web qatlami SignalR bilan biladi).

### 4.2 Taklif (Connection) komandalar/so'rovlar
`Features/Connections/`
- **`SendConnectionRequestCommand { int AddresseeId }`** → `ConnectionDto`
  - O'ziga taklif yo'q (`ForbiddenAccessException`).
  - Agar **teskari yo'nalishda Pending** mavjud bo'lsa → uni **auto-accept** qiladi (ikkalasi bir-birini taklif qilgan).
  - Agar allaqachon `Accepted` bo'lsa → no-op (mavjudini qaytaradi).
  - Aks holda yangi `Pending` yaratadi.
  - **Real-time**: addressee'ga `INotificationService.NotifyAsync` (yangi `NotificationType.ConnectionRequest`) —
    boyo'g'li shuni eshitadi (6-bo'lim).
- **`RespondToConnectionCommand { int ConnectionId; bool Accept }`** → `ConnectionDto`
  - Faqat `Addressee` javob bera oladi.
  - `Accept=true` → `Status=Accepted`, `RespondedAt=now`. **Conversation'ni shu yerda yaratadi**
    (`ConversationHelper.GetOrCreateAsync`) — qabuldan keyin ikkalasining chatida ko'rinadi.
  - `Accept=false` → `Status=Declined`.
  - **Real-time**: requester'ga xabar (`ConnectionAccepted`/`ConnectionDeclined`) — uning chat ro'yxati yangilanadi.
- **`GetPendingRequestsQuery`** → kelgan (`Pending_Incoming`) takliflar ro'yxati (badge/bildirishnoma paneli uchun).
- **`CancelConnectionRequestCommand { int ConnectionId }`** (ixtiyoriy) — requester yuborganini bekor qiladi.

`ConnectionDto { int Id; UserSummaryDto OtherUser; ConnectionStatus Status; bool IamRequester; DateTime CreatedAt }`.

### 4.3 Chat ro'yxati manbasini o'zgartirish
`GetConversationsQueryHandler` **qabul qilingan Connection'lar** asosida quriladi:
- Avval `Connections` dan `Accepted` va (Requester==me OR Addressee==me) bo'lganlarni olamiz → "do'stlar" ro'yxati.
- Har biri uchun mos `Conversation` (bo'lsa) + oxirgi xabar + o'qilmaganlar soni.
- **Xabar yo'q bo'lsa ham** suhbat ro'yxatda ko'rinadi (`LastMessageText=null` → "Yangi suhbat").
- Tartib: oxirgi xabar vaqti bo'yicha, keyin connection `RespondedAt` bo'yicha.
- `OtherUser` ga `IsOnline`/`LastSeenAt` qo'shiladi (Web Redis bilan boyitadi).

`SendMessageCommandHandler` ga **gate**: xabar yuborishdan oldin ikkala foydalanuvchi o'rtasida `Accepted`
connection borligini tekshirish (`ForbiddenAccessException` aks holda). Bu "qabul qilmasdan yozib bo'lmaydi" qoidasini ta'minlaydi.

### 4.4 5000 belgi limiti
`SendMessageCommandValidator`: `MaximumLength(4000)` → **`MaximumLength(5000)`**, xabar "Xabar 5000 belgidan oshmasligi kerak.".
Frontend'da `maxlength="5000"` + jonli sanagich (X/5000).

### 4.5 Double-tick (o'qildi)
`MessageDto.IsRead` allaqachon bor. Frontend shu bool asosida tick chizadi (5.4).
`MarkMessagesReadCommandHandler` (bor) o'qilgach `IsRead=true` qiladi — qo'shimcha: **jo'natuvchiga real-time
"o'qildi" signali** kerak (ko'k tick darhol ko'rinishi uchun):
- `IChatNotifier` ga yangi metod: `MessagesReadAsync(int senderUserId, int conversationId, CancellationToken)`.
- `MarkMessagesReadCommandHandler` o'qilgan xabarlar egasiga (`SenderId`) shu signalni yuboradi.
- Frontend `MessagesRead` event'ida o'sha suhbatdagi "out" xabarlarni 2 ko'k tickga o'tkazadi.

### 4.6 Presence (online/last-seen) — interfeys
Application faqat abstraksiya biladi; SignalR Web'da:
- Yangi interfeys `IPresenceService` (Application/Common/Interfaces):
  ```
  Task SetOnlineAsync(int userId);
  Task SetOfflineAsync(int userId);              // LastSeenAt DB ga yoziladi (Web impl)
  Task<bool> IsOnlineAsync(int userId);
  Task<IReadOnlyDictionary<int,bool>> AreOnlineAsync(IEnumerable<int> userIds);
  ```
- Web implementatsiyasi `RedisPresenceService` — Redis `IConnectionMultiplexer` bilan (4.7).

### 4.7 Presence — Infrastructure/Web implementatsiyasi
- **Redis kalit**: `presence:online` — **Set** (online userId'lar) yoki har user uchun TTL'li kalit
  `presence:user:{id}` (heartbeat'da yangilanadi, TTL ~60s). TTL yondashuv "yiqilgan ulanish" muammosini avtomatik hal qiladi.
- **SignalR `ChatHub` kengaytmasi**:
  - `OnConnectedAsync` → connection sonini oshiradi (`presence:conn:{id}` INCR), `SetOnlineAsync`.
  - `OnDisconnectedAsync` → DECR; 0 ga tushsa `SetOfflineAsync` (+`LastSeenAt=now` DB ga).
  - **Heartbeat**: klient har ~30s `Heartbeat()` hub metodini chaqiradi → TTL yangilanadi (tab ochiq turса online qoladi).
- **Online holatining tarqalishi**: kimdir online/offline bo'lganda uning **do'stlariga** (Accepted connection'lar)
  `PresenceChanged {userId, isOnline, lastSeenAt}` event'i yuboriladi → chat ro'yxatidagi nuqta jonli yangilanadi.

---

## 5. Web qatlami (Controllers, SignalR, Views)

### 5.1 Yangi/yangilangan endpointlar (`ChatController` + yangi `ConnectionsController`)
```
GET  /chat/search?q=...&page=     → SearchUsersQuery (IsOnline Redis bilan boyitilgan) → Json
POST /chat/connect                → SendConnectionRequestCommand {addresseeId}
POST /chat/connect/{id}/respond   → RespondToConnectionCommand {accept}
POST /chat/connect/{id}/cancel    → CancelConnectionRequestCommand
GET  /chat/requests               → GetPendingRequestsQuery (kelgan takliflar)
```
Mavjudlar saqlanadi (`/chat`, `/chat/send`, `/chat/{id}/messages`, `/chat/{id}/read`, `/chat/start`).
`/chat/send` endi gate orqali o'tadi (Accepted connection talab).

### 5.2 SignalR hub'lar
- `ChatHub`: `Heartbeat()` metodi + presence hooklari (4.7). `MessagesRead` va `PresenceChanged` push'lari.
- `NotificationHub`: taklif e'lonlari shu yerdан (`NotificationType.ConnectionRequest`) — boyo'g'li tinglaydi.
  (Yoki ChatHub'da — bitta ulanish kifoya; **NotificationHub** ni ishlatamiz, chunki u allaqachon umumiy bildirishnomalar uchun.)

### 5.3 `/chat` sahifa layout (Views/Chat/Index.cshtml qayta tuziladi)
Uch ustunli (desktop), mobil'da bir vaqtda bittasi:
```
┌───────────────┬──────────────────────────┬───────────────┐
│  SIDEBAR      │      SUHBAT (messages)   │  BOYO'G'LI     │
│  [🔍 qidir]   │  header: ism+online nuqta│  3D canvas    │
│  qidiruv      │  ...xabarlar (tick'lar)  │  "Hush, men   │
│  natijalari   │  [matn 5000] [yubor]     │  tinglayapman"│
│  ─────────    │                          │  pending      │
│  Suhbatlar    │                          │  takliflar    │
│  (do'stlar)   │                          │  paneli       │
└───────────────┴──────────────────────────┴───────────────┘
```
- **Qidiruv bloki** (sidebar tepasi): input → 300ms debounce → `/chat/search` → natija kartalari:
  avatar (story bo'lsa gradient halqa, bosilsa story viewer), to'liq ism, `@username`, bio (2 qator clamp),
  online nuqta / "oxirgi marta ...", va holatga qarab tugma (Taklif qilish / Yuborildi / Qabul qilish / Suhbatlashish).
- **Suhbatlar ro'yxati**: qabul qilingan do'stlar; har birida avatar + online nuqta + oxirgi xabar + unread badge.
- **Boyo'g'li paneli** (o'ng): 3D canvas (6-bo'lim) + kelgan takliflar ro'yxati (qabul/rad tugmalari).
  Mobil'da boyo'g'li sidebar tepasida kichik, yoki suzuvchi (floating) burchakda.

### 5.4 Double-tick UI
`out` (mening) xabarlarimda vaqt yonida tick:
- `IsRead=false` → `<span class="ticks">✓</span>` (bitta, kulrang).
- `IsRead=true` → `<span class="ticks read">✓✓</span>` (ikkita, ko'k).
- SVG ikonka afzal (chiroyli double-tick). `MessagesRead` event kelganda DOM'dagi barcha `out` ticklar `read` ga o'tadi.
- `in` (kelgan) xabarlarda tick yo'q.

### 5.5 Online indikatori
- Yashil nuqta avatar burchagida (`.online-dot`), `IsOnline` bo'lsa.
- Offline bo'lsa profil/header'da "oxirgi marta {humanize(LastSeenAt)}" — o'zbekcha: "hozir online",
  "5 daqiqa oldin", "kecha", "12-iyun" (JS humanize helper).
- `PresenceChanged` event jonli yangilaydi.

---

## 6. 🦉 "Bilimdon boyo'g'li" — 3D ko'makchi (markaziy qism)

> Talab: hamma tomonga qaraydi, boshini aylantiradi, ko'zlari har tomonga yuradi, mukammal loyihalashtirilgan;
> kerak bo'lsa three.js. Kimdir "Taklif qilish" bossa — **o'sha zahoti** xabar beradi.

### 6.1 Texnologiya
- **three.js** (ESM, CDN `https://unpkg.com/three@0.160/build/three.module.js` yoki `wwwroot/lib/three`).
  Faqat `/chat` da **lazy-load** (`<script type="module">`, dynamic import) — boshqa sahifalar sekinlashmaydi.
- **Protsedural model** (asset fayl SHART EMAS): boyo'g'li primitivlardan quriladi —
  tana (sfera/lathe), bosh (sfera), 2 ko'z (oq sfera + qora qorachiq sfera), tumshuq (konus),
  quloq patlari (konus), qanotlar (yassi geometriya). Past-poli, loyiha ranglariga bo'yalgan
  (accent to'q sariq / krem / yashil). Bu repo'ga bog'liqlik qo'shmaydi va to'liq nazorat beradi.
  - (Muqobil: GLTF model yuklash. Lekin protsedural — eng kam tashqi bog'liqlik, eng yaxshi nazorat.)
- Render: `WebGLRenderer({alpha:true, antialias:true})`, shaffof fon, `devicePixelRatio` (max 2),
  kichik canvas (~220×260). `OrbitControls` SHART EMAS (foydalanuvchi aylantirmaydi — boyo'g'li o'zi harakatlanadi).
- Yorug'lik: `HemisphereLight` (yumshoq) + `DirectionalLight` (hajm uchun). Soya SHART EMAS (perf).

### 6.2 Rig (suyak iyerarxiyasi)
```
owlRoot
 └─ bodyGroup
     └─ headGroup        ← yaw (chap/o'ng) + pitch (yuqori/past) shu yerda
         ├─ leftEye
         │   └─ leftPupil   ← ko'z ichida kichik offset (saccade)
         ├─ rightEye
         │   └─ rightPupil
         ├─ beak
         └─ earTufts
```
- **Bosh**: `headGroup.rotation.y` (yaw, ±0.9 rad), `.rotation.x` (pitch, ±0.5 rad). Harakat **easing** bilan
  (joriy→nishon `lerp`, ~0.08 koeffitsient) — silliq, mexanik emas.
- **Qorachiqlar**: `pupil.position` ko'z sferasi yuzasida kichik radiusda siljiydi (nishon yo'nalishi bo'yicha).
  Bosh + qorachiq birga harakatlanib "qarash" hissi beradi.
- **Ko'z pirpiratish (blink)**: ko'zni `scale.y` 1→0.05→1 (yoki qovoq geometriyasi) tasodifiy 2–6s oralig'ida.

### 6.3 Holat mashinasi (animatsiya states)
```
IDLE        — tasodifiy "qarash" nuqtalari (har 1.5–4s yangi nishon), tasodifiy blink,
              tana yengil "nafas" (scale pulsatsiyasi). Boyo'g'li "hamma tomonga qaraydi".
CURIOUS     — kursorni kuzatish: nishon = sichqoncha pozitsiyasi (canvas ustida bo'lganda yoki
              butun sahifa bo'ylab proyeksiya). Foydalanuvchi qidiruv yozayotganda ham CURIOUS.
ALERT       — TAKLIF KELDI: boshini tez viewer (kamera) tomonga buradi, ko'zlari kattalashadi
              (eye scale ↑), quloq patlari "diq" turadi, bitta qanot silkitadi, **"hoot" tovushi**
              (ixtiyoriy, qisqa WebAudio bip), va **speech bubble** chiqadi:
              «🦉 Sizga {Ism} taklif yubordi!» + [Qabul] [Rad] tugmalari.
SPEAKING    — bubble ochiq turganda boshini ozgina chayqaydi (idle+).
SLEEP/IDLE-LOW — tab yashirin (visibilitychange) yoki prefers-reduced-motion: animatsiya to'xtaydi/sekinlashadi.
```
Holatlar orasidagi o'tish silliq (bosh nishoni almashadi, lerp davom etadi).

### 6.4 Real-time integratsiya (taklif e'loni)
- `/chat` yuklanganda boyo'g'li `NotificationHub` ga ulanadi (yoki mavjud ulanishni tinglaydi).
- Server `SendConnectionRequestCommand` da addressee'ga `NotifyAsync(NotificationType.ConnectionRequest, {fromUser})`.
- Klient `on("ReceiveNotification")` da `type==ConnectionRequest` bo'lsa → boyo'g'li **ALERT** ga o'tadi,
  speech bubble + tovush. Bu "o'sha zahotiyoq xabar beradi" talabini bajaradi.
- Qabul tugmasi → `/chat/connect/{id}/respond {accept:true}` → suhbat ro'yxati yangilanadi (yangi do'st ko'rinadi),
  boyo'g'li "HAPPY" mikro-animatsiya (bosh chayqash + blink) qiladi.
- Bir nechta taklif kelса — navbat (queue): bubble'lar ketma-ket ko'rsatiladi yoki "Sizда 3 ta yangi taklif" deb umumlashtiradi.

### 6.5 Sifat / performans / fallback
- **WebGL yo'q bo'lsa** (`!window.WebGLRenderingContext` yoki context xato) → CSS/SVG statik boyo'g'li +
  oddiy CSS ko'z-yurish animatsiyasi (zaxira). Funksiya (taklif e'loni) baribir ishlaydi — faqat 2D.
- **prefers-reduced-motion** → idle harakat minimal, faqat ALERT'da bitta yengil signal.
- **Tab yashirin** → `cancelAnimationFrame` (CPU/GPU tejaladi), ko'rinishga qaytганда davom.
- `dispose()` — sahifadan chiqishda geometry/material/renderer tozalanadi (memory leak yo'q).
- Kod alohida modul: `wwwroot/js/owl.js` (ESM) — `Owl.mount(canvas)`, `owl.alert(name, onAccept, onDecline)`,
  `owl.lookAt(x,y)`, `owl.setState(...)`. `/chat` script'i shuni ishlatadi.

---

## 7. Real-time event'lar xulosasi (SignalR shartnoma)

| Event (server→client) | Hub | Yuk (payload) | Klient reaksiyasi |
|----|----|----|----|
| `ReceiveMessage` | Chat | `MessageDto` | xabarni qo'shadi, ochiq bo'lsa `read` |
| `MessagesRead` | Chat | `{conversationId}` | "out" ticklarni ko'k double-tick'ga |
| `PresenceChanged` | Chat | `{userId,isOnline,lastSeenAt}` | nuqta/last-seen yangilanadi |
| `ReceiveNotification` (ConnectionRequest) | Notification | `{connectionId, fromUser}` | **boyo'g'li ALERT** + bubble |
| `ReceiveNotification` (ConnectionAccepted) | Notification | `{connectionId, user}` | chat ro'yxatiga yangi do'st |

| Metod (client→server) | Hub | Maqsad |
|----|----|----|
| `Heartbeat()` | Chat | presence TTL yangilash (har ~30s) |

---

## 8. Xavfsizlik / qirra holatlar (edge cases)

- O'ziga taklif/xabar — bloklangan (mavjud tekshiruvlar + yangi).
- Ikki tomon bir-birini taklif qilsa → auto-accept (4.2).
- Allaqachon do'st bo'lganga qayta taklif — no-op.
- Qabul qilmasdan `/chat/send` — `ForbiddenAccessException` (4.3 gate).
- Rad etilgan taklifни qayta yuborish — ruxsat (yangi Pending) yoki cooldown (ixtiyoriy).
- Akkaunt o'chirilganda `Connections` ham FK-xavfsiz o'chirilishi kerak — `DeleteAccountCommandHandler` ga
  `Connections` (ikkala yo'nalish) RemoveRange qo'shiladi (mavjud Restrict tartibiga mos).
- 5000+ belgi — server validator + frontend `maxlength` + sanagich.
- XSS — xabar/bio/ism `escapeHtml` (mavjud konvensiya) bilan render.
- Presence Redis yiqilsa — `IsOnline` `false` ga fallback, `LastSeenAt` DB'dan ko'rsatiladi (ilova ishlayveradi).

---

## 9. Amalga oshirish bosqichlari (PROGRESS.md ga qo'shiladi)

- **C1 — Domain+DB**: `Connection` entity + `ConnectionStatus` enum + `User.LastSeenAt` + Configuration + migratsiya.
- **C2 — Connections feature**: Send/Respond/Cancel/GetPending komandalar+validatorlar+handlerlar + DTO.
- **C3 — User qidiruv**: `SearchUsersQuery` + `UserSearchResultDto` + connection-state hisoblash.
- **C4 — Chat gate + ro'yxat**: `GetConversations` ni Connection asosiga o'tkazish; `SendMessage` gate; 5000 limit.
- **C5 — Presence**: `IPresenceService` + `RedisPresenceService` + ChatHub connect/disconnect/heartbeat + PresenceChanged.
- **C6 — Read receipts real-time**: `IChatNotifier.MessagesReadAsync` + handler + frontend double-tick.
- **C7 — Web endpointlar**: ChatController/ConnectionsController yangilanishi + Json'lar + Redis boyitish.
- **C8 — Frontend /chat qayta tuzish**: qidiruv UI, suhbat ro'yxati, online nuqta, last-seen, double-tick, 5000 sanagich.
- **C9 — 🦉 Boyo'g'li**: `owl.js` (three.js protsedural model + rig + holat mashinasi) + ALERT integratsiya + fallback.
- **C10 — Testlar + build + tekshirish**: handler testlari (Send/Respond/Search/gate), `dotnet build` 0/0, `dotnet test`,
  ishlayotgan ilovada (5261) curl + vizual tekshirish; `DeleteAccount` ga Connections qo'shilganini sinash.

Har bosqich tugagach `PROGRESS.md` yangilanadi (loyiha qoidasi).

---

## 10. O'zgaradigan/yangi fayllar (rejadagi xarita)

**Domain**: `Entities/Connection.cs` (yangi), `Enums/ConnectionStatus.cs` (yangi),
`Entities/User.cs` (+nav, +LastSeenAt), `Persistence/Configurations/ConnectionConfiguration.cs` (yangi),
`Persistence/Configurations/UserConfiguration.cs` (LastSeenAt), yangi migratsiya.

**Application**: `Features/Connections/**` (yangi), `Features/Users/Queries/SearchUsers/**` (yangi),
`Common/Interfaces/IPresenceService.cs` (yangi), `IChatNotifier.cs` (+MessagesReadAsync),
`Features/Chat/Queries/GetConversations/*` (qayta), `Features/Chat/Commands/SendMessage/*` (gate + 5000),
`Features/Chat/Commands/MarkMessagesRead/*` (read signal), `IAppDbContext` (+`DbSet<Connection>`),
`Features/Profile/.../DeleteAccount...` (Connections cleanup), `Common/Models/UserSummaryDto` yoki
`ConversationDto`/`MessageDto` (IsOnline/LastSeenAt qo'shimchalari).

**Infrastructure**: `AppDbContext` (+DbSet), `RealTime/RedisPresenceService.cs` (yoki Web'da).

**Web**: `Controllers/ChatController.cs` (+search/connect/respond/requests), `Controllers/ConnectionsController.cs` (ixtiyoriy),
`Hubs/ChatHub.cs` (+presence+heartbeat), `RealTime/SignalRChatNotifier.cs` (+MessagesRead),
`RealTime/SignalRNotificationService.cs` (ConnectionRequest), `RealTime/RedisPresenceService.cs`,
`Views/Chat/Index.cshtml` (qayta), `wwwroot/js/owl.js` (yangi), `wwwroot/js/site.js` yoki chat-specific JS,
`wwwroot/css/site.css` (qidiruv kartalari, online nuqta, double-tick, boyo'g'li paneli, bubble).
`Program.cs` (IPresenceService DI), `lib/three` yoki CDN.
