using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Tests.Support;

/// <summary>
/// Base for handler tests: gives each test an isolated in-memory database and a
/// convention-based Mapster mapper (same behaviour as the production registration).
/// </summary>
public abstract class TestBase
{
    /// <summary>Creates a fresh context over a uniquely named in-memory database.</summary>
    protected static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    /// <summary>A Mapster mapper using default convention-based mappings (User -> UserDto, etc.).</summary>
    protected static IMapper CreateMapper()
        => new Mapper(new TypeAdapterConfig());
}
