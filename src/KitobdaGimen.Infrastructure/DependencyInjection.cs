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
        services.AddHttpClient<IAsaxiyBookService, AsaxiyBookService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/124.0 Safari/537.36");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("uz,en;q=0.8");
        });

        services.AddIdentityServices(configuration);
        services.AddCaching(configuration);
        services.AddRealTime();
        services.AddBackgroundJobs(configuration);

        return services;
    }
}
