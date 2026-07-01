using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Infrastructure.BackgroundJobs;
using KitobdaGimen.Infrastructure.Caching;
using KitobdaGimen.Infrastructure.External;
using KitobdaGimen.Infrastructure.Identity;
using KitobdaGimen.Infrastructure.Persistence;
using KitobdaGimen.Infrastructure.RealTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KitobdaGimen.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure services (persistence for now; caching, real-time and
    /// background jobs are added in later stages).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' topilmadi.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Web Push (VAPID) — real device push notifications (TWA delegates them to the
        // Android notification tray).
        services.AddScoped<IPushSender, KitobdaGimen.Infrastructure.Push.WebPushSender>();

        // asaxiy.uz kitoblar katalogini o'qiydigan HTTP-servis. Brauzerga o'xshash
        // User-Agent kerak, aks holda asaxiy bo'sh UA so'rovlarni rad etishi mumkin.
        //
        // asaxiy.uz Cloudflare ortida turadi va xorijiy data-markaz IP'larini (mas.
        // Hetzner Helsinki) 403 bilan bloklaydi. Shuning uchun bitta yo'lga tayanmaymiz —
        // AsaxiyBookService bir nechta transportni AVTOMATIK navbat bilan sinaydi va oxirgi
        // ishlaganini eslab qoladi (sticky). Transportlar (ustunlik tartibida):
        //
        //   1) Worker — "Asaxiy:WorkerUrl" (env: Asaxiy__WorkerUrl) bepul Cloudflare Worker
        //      reverse-proxy. Worker'dan chiqqan so'rov Cloudflare tarmog'idan ketadi va
        //      blokka tushmaydi. Ixtiyoriy "Asaxiy:WorkerSecret" bilan himoyalanadi.
        //   2) Proxy — "Asaxiy:ProxyUrl" (env: Asaxiy__ProxyUrl) O'zbekiston IP'sidagi
        //      SOCKS/HTTP proksi (uy SSH tunnel).
        //   3) Direct — to'g'ridan-to'g'ri (lokal ish yoki bloklanmagan server uchun).
        //   4) Jina — r.jina.ai o'qigich: Cloudflare bloki OSTIDA ham ishlaydigan, sozlamasiz,
        //      bepul zaxira yo'l. Hech qanday sozlama bo'lmasa ham qidiruv shu orqali ishlaydi.
        //
        // Bu yondashuv "umrbod ishlash"ni ta'minlaydi: birorta transport o'chsa, keyingisiga
        // avtomatik o'tadi; "Kitoblarni yangilash" tugmasi holatni tiklab, boshidan sinaydi.
        services.AddSingleton<AsaxiyTransportState>();
        services.AddSingleton(new AsaxiyOptions
        {
            WorkerUrl = configuration["Asaxiy:WorkerUrl"],
            WorkerSecret = configuration["Asaxiy:WorkerSecret"],
            ProxyUrl = configuration["Asaxiy:ProxyUrl"]
        });

        void ConfigureBrowserClient(System.Net.Http.HttpClient client)
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/124.0 Safari/537.36");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("uz,en;q=0.8");
        }

        // 1) Direct + Worker + public transportlar shu klientdan foydalanadi (proksisiz).
        services.AddHttpClient(AsaxiyClients.Direct, ConfigureBrowserClient)
            .ConfigurePrimaryHttpMessageHandler(() =>
                new System.Net.Http.HttpClientHandler { AllowAutoRedirect = true });

        // 2) SOCKS/HTTP proksi klienti (faqat Asaxiy:ProxyUrl berilganda).
        var asaxiyProxyUrl = configuration["Asaxiy:ProxyUrl"];
        services.AddHttpClient(AsaxiyClients.Proxy, ConfigureBrowserClient)
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new System.Net.Http.HttpClientHandler { AllowAutoRedirect = true };
                if (!string.IsNullOrWhiteSpace(asaxiyProxyUrl))
                {
                    handler.Proxy = new System.Net.WebProxy(asaxiyProxyUrl);
                    handler.UseProxy = true;
                }
                return handler;
            });

        // 3) Jina Reader klienti — sekinroq bo'lgani uchun uzunroq timeout.
        services.AddHttpClient(AsaxiyClients.Jina, client =>
        {
            ConfigureBrowserClient(client);
            client.Timeout = TimeSpan.FromSeconds(40);
        }).ConfigurePrimaryHttpMessageHandler(() =>
            new System.Net.Http.HttpClientHandler { AllowAutoRedirect = true });

        services.AddScoped<IAsaxiyBookService, AsaxiyBookService>();

        services.AddIdentityServices(configuration);
        services.AddCaching(configuration);
        services.AddRealTime();
        services.AddBackgroundJobs(configuration);

        return services;
    }
}
