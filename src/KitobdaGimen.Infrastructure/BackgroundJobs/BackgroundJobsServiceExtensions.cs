using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KitobdaGimen.Infrastructure.BackgroundJobs;

public static class BackgroundJobsServiceExtensions
{
    /// <summary>
    /// Registers Hangfire with PostgreSQL storage and its server. Can be disabled via
    /// <c>Hangfire:Enabled = false</c> (useful for local runs without a database).
    /// </summary>
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool?>("Hangfire:Enabled") ?? true;
        if (!enabled)
        {
            return services;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Hangfire uchun 'DefaultConnection' topilmadi.");

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            // Connection-level pooling is on by default. Hangfire retries failed jobs at the
            // job level, so a transient Postgres blip during a recurring job is recovered
            // automatically rather than dropped permanently.
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer();

        // Kunlik "kitob o'qing" eslatma jobi (boyo'g'li yuboradi). Hangfire uni
        // o'z scope'ida resolve qiladi, shuning uchun scoped (IAppDbContext bog'liq).
        services.AddScoped<ReadingReminderJob>();
        services.AddScoped<ChallengeFinalizeJob>();

        // Jismoniy kitob band qilishlari muddatini (24 soat) tekshiruvchi job.
        services.AddScoped<ReservationExpiryJob>();

        return services;
    }
}
