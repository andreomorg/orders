using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Domain.Common;
using Orders.Domain.Orders;
using Orders.Domain.Products;
using Orders.Infrastructure.Persistence;
using Orders.Infrastructure.Persistence.Repositories;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    private const string DatabaseName = "OrdersDb";

    /// <summary>
    /// Registers the EF Core InMemory context, the repositories and the unit of work.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<OrdersDbContext>(options => options.UseInMemoryDatabase(DatabaseName));

        // Same context instance of the request, so it saves what the repositories changed
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<OrdersDbContext>());

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }

    /// <summary>
    /// Creates the database and applies the product catalog seed. Call once at startup.
    /// </summary>
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
