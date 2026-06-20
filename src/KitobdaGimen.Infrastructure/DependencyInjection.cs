using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Infrastructure.BackgroundJobs;
using KitobdaGimen.Infrastructure.Caching;
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

        services.AddIdentityServices(configuration);
        services.AddCaching(configuration);
        services.AddRealTime();
        services.AddBackgroundJobs(configuration);

        return services;
    }
}
