using Microsoft.EntityFrameworkCore;
using Orders.Domain.Common;
using Orders.Domain.Orders;
using Orders.Domain.Products;

namespace Orders.Infrastructure.Persistence;

/// <summary>
/// EF Core context of the application. Also acts as the unit of work: the same instance used by the
/// repositories during a request is the one that saves the changes.
/// </summary>
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);
}
