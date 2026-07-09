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
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

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

// Data Protection — "Yillik yakun" ulashish tokenlarini (foydalanuvchi id + yil)
// himoyalash uchun. Standart holatda kalitlar avtomatik boshqariladi.
builder.Services.AddDataProtection();

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
    headers["X-Frame-Options"] = "DENY";
    // no-referrer: tashqi rasmlar (Google avatar lh3.googleusercontent.com, kitob muqovalari)
    // Referer yuborilganda bloklanadi/429 qaytaradi. Referersiz so'rov ularni hamma qurilmada
    // (laptop ham) muammosiz yuklaydi. Bizning resurslarimiz same-origin — referer kerak emas.
    headers["Referrer-Policy"] = "no-referrer";
    headers["X-Permitted-Cross-Domain-Policies"] = "none";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://unpkg.com; " +
        "connect-src 'self' https://cdnjs.cloudflare.com; " +
        "form-action 'self'";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
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
}

app.Run();
