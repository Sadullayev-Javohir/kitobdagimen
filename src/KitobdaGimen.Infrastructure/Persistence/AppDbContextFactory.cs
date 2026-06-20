using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KitobdaGimen.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (<c>dotnet ef migrations</c>).
/// Reads the connection string from the <c>ConnectionStrings__DefaultConnection</c>
/// environment variable, falling back to a local development default.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=kitobdagimen;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
