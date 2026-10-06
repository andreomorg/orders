using Microsoft.EntityFrameworkCore;
using Orders.Infrastructure.Persistence;

namespace Orders.Tests.Infrastructure;

/// <summary>
/// A fresh EF Core InMemory database for a single test, already created with the product catalog seed.
/// Each <see cref="CreateContext"/> call simulates a new request against the same database.
/// </summary>
internal sealed class TestDatabase
{
    private readonly DbContextOptions<OrdersDbContext> _options =
        new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase($"orders-tests-{Guid.NewGuid()}")
            .Options;

    public TestDatabase()
    {
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public OrdersDbContext CreateContext() => new(_options);
}
