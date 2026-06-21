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

        // asaxiy.uz kitoblar katalogini o'qiydigan HTTP-servis. Brauzerga o'xshash
        // User-Agent kerak, aks holda asaxiy bo'sh UA so'rovlarni rad etishi mumkin.
        //
        // asaxiy.uz Cloudflare ortida turadi va xorijiy data-markaz IP'larini (mas.
        // Hetzner Helsinki) 403 bilan bloklaydi. Shu sabab serverdan to'g'ridan-to'g'ri
        // so'rov ishlamaydi. Ikki muqobil bepul yechim bor (ikkalasi ham ixtiyoriy):
        //
        //   1) "Asaxiy:WorkerUrl" (env: Asaxiy__WorkerUrl) — bepul Cloudflare Worker
        //      reverse-proxy. asaxiy O'ZI Cloudflare ortida, shuning uchun Worker'dan
        //      chiqqan so'rov Cloudflare tarmog'idan ketadi (Hetzner ASN emas) va blokka
        //      tushmaydi. Worker HAR DOIM yoniq — uy kompyuteriga bog'liq emas. Tavsiya
        //      etiladigan yo'l. (Skript: deploy/asaxiy-proxy-worker.js)
        //      Ixtiyoriy "Asaxiy:WorkerSecret" (env: Asaxiy__WorkerSecret) bilan himoyalanadi.
        //
        //   2) "Asaxiy:ProxyUrl" (env: Asaxiy__ProxyUrl) — O'zbekiston IP'sidagi SOCKS/HTTP
        //      proksi (uy SSH tunnel). Faqat uy kompyuteri yoniq bo'lganda ishlaydi.
        //
        // WorkerUrl berilsa, u ustun (proksi e'tiborsiz). Hech biri bo'lmasa —
        // to'g'ridan-to'g'ri (lokal ish uchun).
        var asaxiyWorkerUrl = configuration["Asaxiy:WorkerUrl"];
        var asaxiyWorkerSecret = configuration["Asaxiy:WorkerSecret"];
        var asaxiyProxyUrl = configuration["Asaxiy:ProxyUrl"];
        var useWorker = !string.IsNullOrWhiteSpace(asaxiyWorkerUrl);

        var asaxiyHttp = services.AddHttpClient<IAsaxiyBookService, AsaxiyBookService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/124.0 Safari/537.36");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("uz,en;q=0.8");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = true };
            // SOCKS proksi faqat Worker ishlatilmaganda qo'llanadi.
            if (!useWorker && !string.IsNullOrWhiteSpace(asaxiyProxyUrl))
            {
                handler.Proxy = new System.Net.WebProxy(asaxiyProxyUrl);
                handler.UseProxy = true;
            }
            return handler;
        });

        if (useWorker)
        {
            asaxiyHttp.AddHttpMessageHandler(() =>
                new AsaxiyWorkerProxyHandler(asaxiyWorkerUrl!, asaxiyWorkerSecret));
        }

        services.AddIdentityServices(configuration);
        services.AddCaching(configuration);
        services.AddRealTime();
        services.AddBackgroundJobs(configuration);

        return services;
    }
}
