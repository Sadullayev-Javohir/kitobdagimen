using System.Threading.RateLimiting;
using Hangfire;
using KitobdaGimen.Application;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Infrastructure;
using KitobdaGimen.Infrastructure.BackgroundJobs;
using KitobdaGimen.Infrastructure.Persistence;
using KitobdaGimen.Web.Hubs;
using KitobdaGimen.Web.Middleware;
using KitobdaGimen.Web.RealTime;
using KitobdaGimen.Web.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // JSON javoblarni camelCase formatda qaytarish (imageUrl, fullName, ...)
        // JavaScript'da property nomlari kichik harf bilan boshlanadi.
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

// Data Protection — foydalanuvchi sessiyasi, antiforgery tokenlari va yillik yakun
// tokenlarini himoyalash. Server/konteyner qayta ishga tushganda yoki deploy bo'lganda
// kalitlar yo'qolib ketmasligi uchun doimiy papkaga (diskka) saqlanadi.
var dataProtectionKeysFolder = builder.Configuration["DataProtection:KeysFolder"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "dataprotection-keys");
try
{
    Directory.CreateDirectory(dataProtectionKeysFolder);
}
catch { /* ignore */ }

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysFolder))
    .SetApplicationName("KitobdaGimen");

// Reverse-proxy (nginx/Hetzner) ortida to'g'ri ishlash: HTTPS sxemasi va haqiqiy
// mijoz IP'si X-Forwarded-* sarlavhalaridan olinadi. Busiz: cookie Secure flagi
// qo'yilmaydi (Request.IsHttps=false) va rate-limit barcha foydalanuvchini bitta
// proxy IP'siga jamlaydi. Faqat ishonchli yagona proxy ortida ishlating.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Rate limiting — DoS / brute-force / flood'ga qarshi. Har bir mijoz IP'si uchun
// daqiqada cheklov. Static fayllar UseRouting'dan oldin serve qilingani uchun bu
// chegaraga tushmaydi; faqat marshrutlangan so'rovlar (controller/hub) hisoblanadi.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 600,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Real-time push implementations (SignalR hubs live in this layer). AddSignalR() itself
// is registered by Infrastructure.AddRealTime().
builder.Services.AddScoped<IChatNotifier, SignalRChatNotifier>();
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();
// Online presence (Redis, TTL heartbeat). Scoped because it persists LastSeenAt via the scoped DbContext.
builder.Services.AddScoped<IPresenceService, RedisPresenceService>();

// ── Telegram Bot va OTP integratsiyasi ───────────────────────────────────────────────
builder.Services.Configure<KitobdaGimen.Web.Telegram.TelegramBotSettings>(
    builder.Configuration.GetSection(KitobdaGimen.Web.Telegram.TelegramBotSettings.SectionName));
builder.Services.AddHttpClient("TelegramBot", c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddHttpClient("TelegramBotPolling", c => c.Timeout = TimeSpan.FromSeconds(45));
builder.Services.AddSingleton<KitobdaGimen.Web.Telegram.TelegramBotService>();
builder.Services.AddSingleton<ITelegramBotService>(sp =>
    sp.GetRequiredService<KitobdaGimen.Web.Telegram.TelegramBotService>());
builder.Services.AddHostedService<KitobdaGimen.Web.Telegram.TelegramBotHostedService>();

// ── Server monitoring (admin dashboard) ─────────────────────────────────────────────
// A single central collector gathers server health every few seconds into an in-memory
// ring buffer; the admin panel reads the latest snapshot. All singletons so the counters
// and CPU-delta state persist across requests. Can be disabled via Monitoring:Enabled=false.
var monitoringEnabled = builder.Configuration.GetValue<bool?>("Monitoring:Enabled") ?? true;
builder.Services.Configure<KitobdaGimen.Application.Features.Admin.Monitoring.MonitoringThresholds>(
    builder.Configuration.GetSection("Monitoring:Thresholds"));
builder.Services.AddSingleton<KitobdaGimen.Web.Monitoring.RealtimeConnectionCounter>();
builder.Services.AddSingleton<KitobdaGimen.Web.Monitoring.HttpMetrics>();
builder.Services.AddSingleton<KitobdaGimen.Web.Monitoring.SystemMetricsReader>();
builder.Services.AddSingleton<IServerMetricsStore>(_ =>
    new KitobdaGimen.Web.Monitoring.ServerMetricsStore(
        builder.Configuration.GetValue<int?>("Monitoring:HistorySize") ?? 150));
if (monitoringEnabled)
{
    builder.Services.AddHostedService<KitobdaGimen.Web.Monitoring.MetricsCollectorService>();
}

var app = builder.Build();

// Apply pending migrations and seed canonical genres + sample books. Best-effort:
// startup continues even if Postgres is unreachable (see DbInitializer).
await DbInitializer.InitializeAsync(app.Services);

// Application exception → JSON translation runs first so it wraps everything below.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Rescue listener: agar foydalanuvchi 400 xatolik sahifasini qayta yuklasa (F5),
// yuborilgan matnni server logiga to'liq yozib oladi (antiforgery xato bersa ham).
app.Use(async (context, next) =>
{
    if (context.Request.Path.Value?.Contains("/posts/create") == true && context.Request.Method == "POST")
    {
        try
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            Serilog.Log.Information("RESCUE_POST_BODY: {Body}", body);
        }
        catch { /* best effort */ }
    }
    await next();
});

// Reverse-proxy sarlavhalarini eng oldin qo'llaymiz (scheme/IP shundan keyin to'g'ri).
app.UseForwardedHeaders();

// Xavfsizlik sarlavhalari — har bir javobga (static fayllar ham). Brauzer himoyasi:
// clickjacking, MIME-sniffing (yuklangan "rasm" ichidagi yashirin skript), referrer
// sizishi va keraksiz qurilma ruxsatlari. CSP loyihaning tashqi manbalariga moslangan:
// Google Fonts, cdnjs (SignalR), unpkg (three.js), Google avatar rasmlari.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    // no-referrer: tashqi rasmlar (kitob muqovalari va avatarlar) Referersiz muammosiz yuklanadi.
    headers["Referrer-Policy"] = "no-referrer";
    headers["X-Permitted-Cross-Domain-Policies"] = "none";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'self' https://web.telegram.org https://*.telegram.org; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://unpkg.com https://telegram.org; " +
        "connect-src 'self' https://cdnjs.cloudflare.com; " +
        "form-action 'self'";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseStaticFiles();

// Foydalanuvchi yuklamalari (avatar, muqova, post rasmlari) — publish'dan TASHQARIDAGI
// doimiy papkadan serve qilinadi, shunda deploy (rm -rf publish) ularni o'chirmaydi.
KitobdaGimen.Web.UploadPaths.Configure(app.Environment, app.Configuration);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(KitobdaGimen.Web.UploadPaths.Root),
    RequestPath = "/uploads"
});

app.UseRouting();

// Server monitoring: measure every routed request (latency + status) for the admin dashboard.
// After UseRouting (matched endpoint available for path normalization) and before the rate
// limiter, so 429 rejections are also counted as they unwind back through this middleware.
app.UseMiddleware<KitobdaGimen.Web.Middleware.RequestMetricsMiddleware>();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Landing (tanishuv) sahifasi — kirgan foydalanuvchi ham qayta ko'ra olishi uchun
// alohida toza manzil; Home/Index dan farqi: /feed ga YO'NALTIRMAYDI.
app.MapControllerRoute(
    name: "landing",
    pattern: "landing",
    defaults: new { controller = "Home", action = "Landing" });

// Foydalanish qo'llanmasi — toza manzil (/qollanma)
app.MapControllerRoute(
    name: "guide",
    pattern: "qollanma",
    defaults: new { controller = "Home", action = "Guide" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");

var hangfireEnabled = builder.Configuration.GetValue<bool?>("Hangfire:Enabled") ?? true;
if (hangfireEnabled)
{
    // Dashboard'ga faqat konfiguratsiyadagi admin email'lar kira oladi (ro'yxat bo'sh
    // bo'lsa — hech kim). Sozlash: Hangfire:DashboardEmails:0 = "admin@example.com"
    // (env: Hangfire__DashboardEmails__0). Aks holda /hangfire reverse-proxy ortida
    // butun internetga ochiq bo'lib qolardi.
    var adminEmails = builder.Configuration
        .GetSection("Hangfire:DashboardEmails").Get<string[]>() ?? Array.Empty<string>();

    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthFilter(adminEmails) }
    });

    // Kunlik o'qish eslatmasi: O'zbekiston vaqti bilan har kuni 20:00 da (UTC+5 -> 15:00 UTC).
    // Faol maqsadi bor, lekin bugun o'qimagan foydalanuvchilarga boyo'g'li eslatma yuboradi.
    RecurringJob.AddOrUpdate<ReadingReminderJob>(
        ReadingReminderJob.RecurringJobId,
        job => job.RunAsync(CancellationToken.None),
        "0 15 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    // Challenge g'oliblari FAQAT super admin tomonidan qo'lda aniqlanadi (/challenge/admin).
    // Avtomatik aniqlash o'chirilgan — avval rejalashtirilgan bo'lsa, uni olib tashlaymiz.
    RecurringJob.RemoveIfExists(ChallengeFinalizeJob.RecurringJobId);

    // Jismoniy kitob band qilishlari 24 soatdan so'ng avtomatik "Mavjud"ga qaytadi.
    // Har 15 daqiqada muddati o'tgan band qilishlarni tekshiramiz.
    RecurringJob.AddOrUpdate<ReservationExpiryJob>(
        ReservationExpiryJob.RecurringJobId,
        job => job.RunAsync(CancellationToken.None),
        "*/15 * * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    // Har kuni 23:59 (O'zbekiston vaqti UTC+5 -> 18:59 UTC) da Kun taqrizi yoki Kun iqtibosi (Jackpot) tanlanadi.
    RecurringJob.AddOrUpdate<DailyJackpotJob>(
        DailyJackpotJob.RecurringJobId,
        job => job.RunAsync(CancellationToken.None),
        "59 18 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    // Erta tonggi motivatsiya: O'zbekiston vaqti bilan har kuni 05:30 da (UTC+5 -> 00:30 UTC).
    // Kitobxonlarni erta uyg'onishga va mutolaaga chorlovchi sara iqtibos yuboriladi va bazadan olib tashlanadi.
    RecurringJob.AddOrUpdate<MorningQuoteJob>(
        MorningQuoteJob.RecurringJobId,
        job => job.RunAsync(CancellationToken.None),
        "30 0 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}

app.Run();
