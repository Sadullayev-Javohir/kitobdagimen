using Microsoft.Extensions.DependencyInjection;

namespace KitobdaGimen.Infrastructure.RealTime;

public static class RealTimeServiceExtensions
{
    /// <summary>
    /// Registers SignalR. The hubs themselves and their endpoint mappings live in the Web layer
    /// (ChatHub, NotificationHub) and are wired up in a later stage.
    /// </summary>
    public static IServiceCollection AddRealTime(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }
}
