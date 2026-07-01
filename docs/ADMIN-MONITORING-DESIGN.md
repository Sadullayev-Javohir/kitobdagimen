# Admin — Real-time Server Monitoring (Dizayn)

> Maqsad: `/admin` panelida **serverning real vaqtdagi holatini** va **mavjud xavf-xatarlarni**
> ko'rsatadigan jonli (real-time) monitoring sahifasini qo'shish. "Server qanday ishlayapti,
> qanday risklar bor" degan savolga bir qarashda javob beradigan dashboard.
>
> Bu hujjat — implementatsiyadan oldingi **loyiha (design)**. Kod yozilmagan, faqat
> arxitektura, fayllar ro'yxati va bosqichma-bosqich reja.

---

## 1. Hozirgi holat (nimadan boshlaymiz)

- `/admin` → `AdminController.Index` faqat **foydalanuvchilar ro'yxatini** ko'rsatadi
  (`GetAdminUsersQuery` → `Views/Admin/Index.cshtml`).
- Avtorizatsiya `AdminGuard.RequireAsync` orqali — rol **DB'dan** o'qiladi (JWT'dan emas),
  shuning uchun yangi berilgan rol darhol kuchga kiradi. Rollar: `User < Admin < SuperAdmin`.
- Real-time uchun infratuzilma **allaqachon mavjud**:
  - SignalR (`ChatHub`, `NotificationHub`) — `Infrastructure.AddRealTime()` registratsiya qiladi.
  - Redis (`IConnectionMultiplexer`, singleton, `AbortOnConnectFail=false`) — kesh + presence.
  - `IPresenceService` (Redis) — onlayn foydalanuvchilar.
  - Hangfire + PostgreSQL — fon vazifalari (`/hangfire` dashboard).
  - Serilog — loglar (hozircha faqat Console'ga yozadi).
  - Rate limiter — IP bo'yicha daqiqada 600 so'rov, 429 qaytaradi.

Bu komponentlarning hammasi monitoring uchun **manba** bo'lib xizmat qiladi — yangi
tashqi kutubxona (Prometheus/Grafana) shart emas, hammasini ichkarida yig'amiz.

---

## 2. Ko'lam (scope)

### Kiradi
- Yangi sahifa: `GET /admin/monitor` — jonli dashboard (faqat **SuperAdmin**).
- Backend metrik yig'uvchi: ichki `BackgroundService` + so'rovlarni hisoblovchi middleware.
- Real-time uzatish: `AdminMonitorHub` (SignalR) har 2–3 soniyada snapshot push qiladi.
- Xavf-xatar (risk) baholash: qoidaga asoslangan health-check'lar (OK / Ogohlantirish / Jiddiy).
- REST fallback: `GET /admin/monitor/snapshot` (SignalR ulanmasa, polling uchun).

### Kirmaydi (kelajak)
- Tashqi APM (Application Insights, Prometheus exporter).
- Tarixiy metrikalarni DB'ga uzoq saqlash (faqat in-memory ring buffer ~ohirgi 5–10 daqiqa).
- Alert yuborish (email/telegram) — keyingi bosqichda `INotificationService` ustiga qurish mumkin.

---

## 3. Qaysi metrikalar ko'rsatiladi

Har bir blok dashboardda alohida **kartochka** sifatida ko'rinadi.

### 3.1. Tizim / Process (System)
| Metrika | Manba |
|---|---|
| CPU foizi (process) | `Process.TotalProcessorTime` delta / vaqt / `Environment.ProcessorCount` |
| RAM (Working Set, MB) | `Process.WorkingSet64` |
| Managed heap (MB) | `GC.GetTotalMemory(false)` |
| GC kolleksiyalar (Gen0/1/2) | `GC.CollectionCount(n)` |
| ThreadPool: ishchi/IO threadlar | `ThreadPool.GetAvailableThreads / GetMaxThreads` |
| ThreadPool navbati uzunligi | `ThreadPool.PendingWorkItemCount` |
| Uptime | `DateTime.UtcNow - Process.StartTime.ToUniversalTime()` |
| .NET versiyasi, OS, host nomi | `RuntimeInformation`, `Environment.MachineName` |

### 3.2. Ma'lumotlar bazasi (PostgreSQL)
| Metrika | Manba |
|---|---|
| Ulanish holati (up/down) | `AppDbContext.Database.CanConnectAsync()` |
| Ping latency (ms) | `SELECT 1` ni o'lchab |
| Faol ulanishlar soni | `SELECT count(*) FROM pg_stat_activity WHERE datname = current_database()` |
| Eng uzun so'rov (s) | `pg_stat_activity` dan `max(now()-query_start)` (faqat active) |
| DB hajmi (MB) | `pg_database_size(current_database())` (kamdan-kam, har 60s) |

### 3.3. Redis
| Metrika | Manba |
|---|---|
| Ulanish holati | `IConnectionMultiplexer.IsConnected` |
| Ping latency (ms) | `IServer.PingAsync()` / `db.Ping()` |
| Ishlatilgan xotira | `INFO memory` → `used_memory_human` |
| Ulangan mijozlar | `INFO clients` → `connected_clients` |

> Eslatma: Redis yo'q bo'lsa ham sayt ishlaydi. Bu holatda kartochka "off" (kulrang)
> ko'rsatiladi, **xato emas** — degraded rejim.

### 3.4. Real-time (SignalR + Presence)
| Metrika | Manba |
|---|---|
| Onlayn foydalanuvchilar soni | Redis `presence:conn:*` kalitlar soni (yangi `CountOnlineAsync`) |
| Faol SignalR ulanishlar (chat/notif/monitor) | Hub connection counter (interlocked) |

### 3.5. Hangfire (fon vazifalari)
| Metrika | Manba |
|---|---|
| Enqueued / Scheduled / Processing | `JobStorage.Current.GetMonitoringApi().GetStatistics()` |
| Succeeded / Failed (jami) | aynan o'sha `GetStatistics()` |
| Serverlar soni | `Servers` |
| Oxirgi muvaffaqiyatsiz joblar | `FailedJobs(0, 5)` |

### 3.6. HTTP trafik (o'zimiz yig'amiz)
| Metrika | Manba (yangi `RequestMetricsMiddleware`) |
|---|---|
| So'rovlar / soniya (RPS) | sirpanuvchi oyna hisoblagich |
| O'rtacha / p95 javob vaqti (ms) | latency histogram (sodda) |
| 4xx / 5xx ulushi | status kodlar bo'yicha hisoblagich |
| 429 (rate-limit rad) soni | `OnRejected` callback'da hisoblagich |
| Eng band 5 endpoint | path bo'yicha hisoblagich (top-N) |

### 3.7. Disk / Uploads
| Metrika | Manba |
|---|---|
| Uploads papka hajmi (MB) | `UploadPaths.Root` ni rekursiv yig'ish (har 60s, fonda) |
| Disk bo'sh joyi | `DriveInfo` |

---

## 4. Xavf-xatarlar (RISKLAR) — qanday aniqlanadi

Dashboard tepasida **"Xavf-xatarlar" paneli** bo'ladi. Har snapshotda backend bir nechta
**health rule** ni baholaydi va har biriga daraja beradi:

- 🟢 `Ok` — normal
- 🟡 `Warning` — e'tibor bering
- 🔴 `Critical` — darhol cho'ra

Eng yuqori daraja butun sahifa yuqorisidagi **global status banner** rangini belgilaydi
(yashil / sariq / qizil), shunda admin bir soniyada "hammasi joyidami?" ni biladi.

| Risk qoidasi | Warning | Critical |
|---|---|---|
| CPU band | > 75% (30s davomida) | > 90% |
| RAM (working set) | > 75% limitdan | > 90% |
| Managed heap o'sishi | barqaror o'sish (leak gumoni) | — |
| ThreadPool ochligi | navbat > 100 | navbat > 1000 |
| DB ulanish | latency > 200ms | ulana olmaydi (down) |
| DB faol ulanishlar | > 80% pool | pool tugagan |
| Redis | latency > 100ms | down (degraded rejim ogohlantirishi) |
| HTTP xato ulushi | 5xx > 1% | 5xx > 5% |
| 429 toshqini | daqiqada > 50 | daqiqada > 500 (DoS gumoni) |
| Hangfire failed joblar | oxirgi 1 soatda > 0 | navbat to'planib qolgan (processing stuck) |
| Disk bo'sh joy | < 15% | < 5% |
| Uploads hajmi | > belgilangan chegara | — |

Har bir risk obyekti: `{ key, severity, title (uz), detail (uz), value, threshold }`.
Frontend ularni ro'yxat qilib, eng jiddiyini tepaga chiqaradi.

---

## 5. Arxitektura

```
                       ┌─────────────────────────────────────────┐
                       │  MetricsCollectorService (BackgroundService)│
                       │  har 2–3s: snapshot yig'adi                │
                       │  - System (Process, GC, ThreadPool)        │
                       │  - DB ping / pg_stat_activity              │
                       │  - Redis INFO/ping                         │
                       │  - Hangfire statistics                     │
                       │  - HTTP metrikalar (middleware'dan o'qiydi) │
                       │  - Risklarni baholaydi (RiskEvaluator)     │
                       └───────────────┬─────────────────────────────┘
                                       │  ServerSnapshot (record)
                       ┌───────────────▼───────────────┐
        in-memory      │  IServerMetricsStore (singleton)│  ← oxirgi snapshot + ring buffer
        ring buffer    │  ~150 ta snapshot (~5–10 daqiqa)│     (sparkline grafiklar uchun)
                       └───────┬───────────────┬─────────┘
                               │               │
              push (SignalR)   │               │  pull (REST fallback)
                       ┌───────▼──────┐   ┌────▼──────────────────────┐
                       │ AdminMonitorHub│   │ GET /admin/monitor/snapshot│
                       │ "admins" group │   │ (SuperAdmin only)          │
                       └───────┬──────┘   └────────────────────────────┘
                               │  ReceiveSnapshot(json)
                       ┌───────▼─────────────────────────┐
                       │  /admin/monitor  (Razor view)     │
                       │  jonli kartochkalar + grafiklar   │
                       └───────────────────────────────────┘

      RequestMetricsMiddleware  ──(har so'rovda hisoblaydi)──►  HTTP metrikalar (atomik counterlar)
```

### Asosiy g'oya
- **Bitta** `MetricsCollectorService` markazlashgan tarzda snapshot yig'adi (har bir admin
  alohida DB/Redis so'rov yubormaydi — server zo'riqmaydi).
- Snapshot in-memory `IServerMetricsStore` da saqlanadi (oxirgisi + qisqa tarix).
- Yangi snapshot tayyor bo'lishi bilan `AdminMonitorHub` orqali "admins" guruhiga push qilinadi.
- Sahifa SignalR'ga ulanadi; ulanish uzilsa — `setInterval` bilan `/snapshot` ni polling qiladi.

---

## 6. Backend — yangi tiplar va fayllar

### Application qatlami (interfeyslar + DTO)
- `Common/Interfaces/IServerMetricsStore.cs`
  ```csharp
  public interface IServerMetricsStore {
      void Add(ServerSnapshot snapshot);
      ServerSnapshot? Latest { get; }
      IReadOnlyList<ServerSnapshot> History { get; } // ring buffer
  }
  ```
- `Features/Admin/Monitoring/ServerSnapshot.cs` — record (DTO). Tarkibi:
  `Timestamp, SystemMetrics, DbMetrics, RedisMetrics, RealtimeMetrics, HangfireMetrics,
  HttpMetrics, DiskMetrics, IReadOnlyList<RiskItem> Risks, RiskLevel OverallRisk`.
- `Features/Admin/Monitoring/RiskItem.cs`, `RiskLevel` enum (`Ok/Warning/Critical`).

> Eslatma: `ServerSnapshot` faqat **oddiy ma'lumot** (POCO/record), Application qatlamida
> turadi va hech qaysi Infrastructure tipiga bog'lanmaydi. Yig'ish logikasi Web/Infrastructure'da.

### Infrastructure / Web qatlami (yig'ish)
> Hangfire, Redis va SignalR Web/Infrastructure'da bo'lgani uchun kollektorni **Web** loyihasiga
> joylash eng sodda yo'l (qo'shimcha bog'liqliklarsiz). Quyidagilar `KitobdaGimen.Web` ichida:

- `Monitoring/ServerMetricsStore.cs` — `IServerMetricsStore` ni amalga oshiradi
  (singleton, lock-protected ring buffer, `Capacity = 150`).
- `Monitoring/MetricsCollectorService.cs` — `BackgroundService`. `PeriodicTimer(2.5s)` bilan
  ishlaydi; `IServiceScopeFactory` orqali har siklda scope ochib `AppDbContext`, `IConnectionMultiplexer`
  ni oladi; snapshot yig'adi; `IServerMetricsStore.Add` + `IHubContext<AdminMonitorHub>.Clients.Group("admins").SendAsync("ReceiveSnapshot", snapshot)`.
  Har bir manba **alohida try/catch** — bittasi yiqilsa boshqasi davom etadi (best-effort, presence
  servisidagi uslub kabi).
- `Monitoring/SystemMetricsReader.cs` — Process/GC/ThreadPool o'qiydi, CPU% ni delta orqali hisoblaydi.
- `Monitoring/RiskEvaluator.cs` — snapshot bo'laklaridan `List<RiskItem>` va `OverallRisk` chiqaradi.
  Chegaralar `appsettings`'dan (`Monitoring:Thresholds:*`) o'qiladi (qotirib qo'yilmaydi).
- `Monitoring/HttpMetrics.cs` — atomik hisoblagichlar (`long` Interlocked) + sirpanuvchi oyna;
  `RequestMetricsMiddleware` shuni yangilaydi, kollektor o'qiydi.
- `Middleware/RequestMetricsMiddleware.cs` — har so'rovda: `Stopwatch`, status kod, path normalizatsiyasi
  (`/posts/123` → `/posts/{id}`), 429 alohida. `UseRouting`'dan keyin, lekin endpoint'dan oldin.
- `Hubs/AdminMonitorHub.cs` — `[Authorize]` + ichida **SuperAdmin tekshiruvi** (DB'dan rol).
  `OnConnectedAsync` da agar SuperAdmin bo'lsa `"admins"` guruhiga qo'shadi, aks holda
  `Context.Abort()`. Ulanganda darhol oxirgi snapshotni yuboradi (bo'sh ekran bo'lmasligi uchun).

### Controller
`AdminController` ga qo'shiladi:
```csharp
[HttpGet("monitor")]                 // Razor sahifa (SuperAdmin)
public async Task<IActionResult> Monitor() { ... AdminGuard SuperAdmin ... return View(); }

[HttpGet("monitor/snapshot")]        // REST fallback (SuperAdmin), JSON
public async Task<IActionResult> Snapshot() { ... return Json(_store.Latest); }
```
Avtorizatsiya: `GetAdminUsersQuery` kabi yangi `GetServerSnapshotQuery` yaratib, ichida
`AdminGuard.RequireAsync(..., UserRole.SuperAdmin, ...)` chaqirish — qatlam izchilligi uchun
afzal. Yoki controller'da bevosita DB'dan rol o'qish (refresh-covers'dagi naqsh kabi).
**Tavsiya:** CQRS izchilligi uchun `GetServerSnapshotQuery`.

### Ro'yxatdan o'tkazish (`Program.cs`)
```csharp
builder.Services.AddSingleton<IServerMetricsStore, ServerMetricsStore>();
builder.Services.AddSingleton<HttpMetrics>();
builder.Services.AddHostedService<MetricsCollectorService>();
...
app.UseRouting();
app.UseMiddleware<RequestMetricsMiddleware>();   // routing'dan keyin
...
app.MapHub<AdminMonitorHub>("/hubs/admin-monitor");
```

---

## 7. Frontend — `Views/Admin/Monitor.cshtml`

Mavjud dizayn tizimidan foydalanadi (kartochka 20px radius, Primary `#1B4D3E`,
Accent `#E8703A`). Yangi tashqi kutubxona shart emas — kichik sparkline'larni inline SVG yoki
`<canvas>` bilan vanilla JS'da chizamiz (CSP allaqachon `script-src 'self' 'unsafe-inline'`).

Tuzilishi:
1. **Global status banner** (tepa) — `OverallRisk` rangida: "✅ Tizim barqaror" /
   "⚠️ N ta ogohlantirish" / "🔴 Jiddiy muammo".
2. **Xavf-xatarlar ro'yxati** — faqat `Warning`/`Critical` bo'lganlar ko'rinadi, eng jiddiyi tepada.
3. **Kartochkalar to'ri** (responsive grid):
   - Tizim: CPU% gauge, RAM, GC, ThreadPool, Uptime.
   - DB: holat nuqtasi (yashil/qizil), ping ms, faol ulanishlar.
   - Redis: holat, ping, xotira (yoki "o'chiq" kulrang).
   - Real-time: onlayn foydalanuvchilar, faol ulanishlar.
   - Hangfire: enqueued/processing/failed.
   - HTTP: RPS, p95 ms, 5xx%, 429.
   - Disk: uploads MB, bo'sh joy.
4. **Mini-grafiklar (sparkline)** — `History` dan CPU, RPS, latency oxirgi ~5 daqiqa.

JS oqimi:
```
@@aspnet/signalr (cdnjs, allaqachon ishlatiladi) → /hubs/admin-monitor ga ulanadi
  .on("ReceiveSnapshot", render)
  onclose → polling rejim: setInterval(fetch("/admin/monitor/snapshot"), 3000)
render(snapshot): kartochkalarni yangilaydi, ringe buffer'ga qo'shib sparkline chizadi
```

`ViewData["Robots"] = "noindex, nofollow"` (mavjud Admin sahifadagi kabi).
`Views/Admin/Index.cshtml` ga "📊 Server monitoring" tugmasi qo'shiladi (faqat SuperAdmin ko'radi).

---

## 8. Xavfsizlik

- **Faqat SuperAdmin.** Sabab: bu sahifa server ichki holatini (DB ulanishlar, xotira,
  xato darajalari) ochib beradi — oddiy Admin'ga ham ko'rsatish shart emas. `AdminGuard`
  bilan `UserRole.SuperAdmin` talab qilinadi (controller, query, **va** hub — uchchalasida).
- Hub'da `OnConnectedAsync` ichida rolni DB'dan tekshirib, SuperAdmin bo'lmasa `Context.Abort()`.
- Snapshot ichida **maxfiy ma'lumot bo'lmaydi**: connection string, parol, JWT kalit, foydalanuvchi
  PII chiqarilmaydi — faqat agregat sonlar va holatlar.
- `/admin/monitor/snapshot` ham `[Authorize]` + SuperAdmin guard ostida.
- Rate limiter'ga tushadi (marshrutlangan). SignalR push esa hisoblagichlarga ta'sir qilmaydi.
- CSP'ga o'zgartirish **shart emas** — SignalR (cdnjs) va inline script allaqachon ruxsat etilgan.

---

## 9. Ishlash ta'siri (performance)

- Kollektor **bitta** va markazlashgan: 100 ta admin ochsa ham DB/Redis'ga yuk bir xil
  (har 2.5s bitta yig'ish). Push esa arzon.
- Og'ir so'rovlar (DB hajmi, uploads papka hajmi) **alohida, kamroq** intervalda (har 60s).
- `pg_stat_activity` yengil, lekin baribir cache qilinadi (har siklda emas, har ~10s).
- Ring buffer in-memory, cheklangan (150 element) — xotira o'smaydi.
- HTTP metrik middleware — faqat `Interlocked.Increment` va `Stopwatch`, deyarli nol overhead.
- Hammasi best-effort: monitoring yiqilsa **asosiy sayt ta'sirlanmaydi** (try/catch + log).

---

## 10. Konfiguratsiya (`appsettings.json` ga yangi bo'lim)

```json
"Monitoring": {
  "Enabled": true,
  "IntervalSeconds": 2.5,
  "HeavyIntervalSeconds": 60,
  "HistorySize": 150,
  "Thresholds": {
    "CpuWarn": 75, "CpuCrit": 90,
    "MemWarnPct": 75, "MemCritPct": 90,
    "DbPingWarnMs": 200,
    "RedisPingWarnMs": 100,
    "Http5xxWarnPct": 1, "Http5xxCritPct": 5,
    "RateLimitWarnPerMin": 50, "RateLimitCritPerMin": 500,
    "DiskFreeWarnPct": 15, "DiskFreeCritPct": 5,
    "UploadsWarnMb": 5000
  }
}
```
`Monitoring:Enabled=false` bo'lsa kollektor va hub registratsiya qilinmaydi (masalan, juda
kichik muhitda o'chirish uchun). Default `true`.

---

## 11. Bosqichma-bosqich reja (implementatsiya tartibi)

1. **DTO + store** — `ServerSnapshot`, `RiskItem`, `RiskLevel`, `IServerMetricsStore` +
   `ServerMetricsStore` (singleton). Eng kichik, mustaqil bo'lak.
2. **HTTP metrikalar** — `HttpMetrics` + `RequestMetricsMiddleware`, `Program.cs` ga ulash.
3. **System reader** — `SystemMetricsReader` (CPU%, RAM, GC, ThreadPool).
4. **DB + Redis + Hangfire readerlar** — kollektor ichida yoki alohida kichik xizmatlar.
5. **RiskEvaluator** — chegaralar `appsettings`'dan; unit-testlar (sof funksiya, oson test).
6. **MetricsCollectorService** (`BackgroundService`) — hammasini bog'laydi, push qiladi.
7. **AdminMonitorHub** + SuperAdmin guard + ulanganda oxirgi snapshot.
8. **Controller** endpointlari (`/monitor`, `/monitor/snapshot`) + `GetServerSnapshotQuery`.
9. **Razor view** `Monitor.cshtml` + JS (SignalR + fallback) + sparkline.
10. **Index.cshtml** ga monitoring tugmasi (SuperAdmin).
11. **Build + test** (`dotnet build -c Release`, `dotnet test`) + qo'lda tekshirish.

Har bosqich mustaqil build bo'ladi va alohida tekshiriladi.

---

## 12. Test rejasi

- **Unit (xUnit, mavjud test loyihasida):**
  - `RiskEvaluator` — har bir chegara uchun OK/Warning/Critical to'g'ri chiqishini tekshirish
    (sof funksiya, DB kerak emas).
  - `ServerMetricsStore` — ring buffer cheklovi (151-chi qo'shilganda eng eskisi tushishi),
    `Latest` to'g'riligi.
- **Qo'lda (manual):**
  - SuperAdmin bilan `/admin/monitor` ochiladi va jonli yangilanadi.
  - Oddiy User / Admin kira olmaydi (redirect yoki 403, hub `Abort`).
  - Redis'ni o'chirib ko'rish → Redis kartochka "o'chiq", sahifa yiqilmaydi.
  - DB ulanishni uzib ko'rish → DB risk "Critical", banner qizil.
  - SignalR'ni bloklab fallback polling ishlashini tekshirish.

---

## 13. Yangi/o'zgaradigan fayllar (qisqacha)

**Yangi:**
- `src/KitobdaGimen.Application/Common/Interfaces/IServerMetricsStore.cs`
- `src/KitobdaGimen.Application/Features/Admin/Monitoring/{ServerSnapshot,RiskItem,RiskLevel}.cs`
- `src/KitobdaGimen.Application/Features/Admin/Monitoring/GetServerSnapshotQuery(+Handler).cs`
- `src/KitobdaGimen.Web/Monitoring/{ServerMetricsStore,MetricsCollectorService,SystemMetricsReader,RiskEvaluator,HttpMetrics}.cs`
- `src/KitobdaGimen.Web/Middleware/RequestMetricsMiddleware.cs`
- `src/KitobdaGimen.Web/Hubs/AdminMonitorHub.cs`
- `src/KitobdaGimen.Web/Views/Admin/Monitor.cshtml`
- `tests/.../Monitoring/RiskEvaluatorTests.cs`

**O'zgaradi:**
- `src/KitobdaGimen.Web/Program.cs` — DI registratsiya, middleware, `MapHub`.
- `src/KitobdaGimen.Web/Controllers/AdminController.cs` — `Monitor` + `Snapshot` actionlar.
- `src/KitobdaGimen.Web/Views/Admin/Index.cshtml` — monitoring tugmasi (SuperAdmin).
- `src/KitobdaGimen.Web/appsettings.json` — `Monitoring` bo'limi.
- `src/KitobdaGimen.Application/Common/Interfaces/IPresenceService.cs` — `CountOnlineAsync` qo'shish (ixtiyoriy).

---

## 14. Ochiq savollar / qarorlar

1. **Onlayn foydalanuvchilar sonini** Redis `SCAN presence:conn:*` bilan olamizmi yoki alohida
   `presence:online` SET yuritamizmi? SCAN katta bazada sekinroq; SET aniqroq, lekin presence
   servisini biroz o'zgartirishni talab qiladi. → **Tavsiya:** boshida SCAN (har 10s), keyin
   kerak bo'lsa SET'ga o'tish.
2. **Kollektor Web'da yoki Infrastructure'da?** Hangfire `JobStorage.Current` global, Redis va
   SignalR Web'dan qulay. → **Tavsiya:** Web (`Monitoring/` papka), qo'shimcha bog'liqliksiz.
3. **Tarixni DB'ga yozamizmi?** Hozircha yo'q (in-memory). Kelajakda kerak bo'lsa alohida jadval.
4. **Alert (email/telegram)** kerakmi? Hozir scope'dan tashqarida; `RiskEvaluator` natijasini
   keyin `INotificationService`/`IPushSender` ga ulash oson.

---

*Ushbu dizayn loyihaning mavjud arxitekturasiga (Clean Architecture, CQRS, best-effort
infratuzilma) to'liq mos. Tasdiqlangach, 11-bo'limdagi tartibda implementatsiya qilinadi.*
