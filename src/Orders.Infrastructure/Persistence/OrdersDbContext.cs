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
    // The InMemory provider has no transactions: when it detects a concurrency conflict it may already have
    // saved part of the changes (e.g. a new item of an order that another request closed). So the versions are
    // checked before saving, and check + save run one request at a time, which makes the save all-or-nothing.
    // A relational database would get this from a transaction instead.
    private static readonly SemaphoreSlim SaveLock = new(1, 1);

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        await SaveLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureOrdersWereNotChangedByOthersAsync(cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Translated so the layers above do not depend on EF Core
            throw new ConcurrencyException(exception);
        }
        finally
        {
            SaveLock.Release();
        }
    }

    private async Task EnsureOrdersWereNotChangedByOthersAsync(CancellationToken cancellationToken)
    {
        var changedOrders = ChangeTracker.Entries<Order>()
            .Where(entry => entry.State is EntityState.Modified or EntityState.Deleted);

        foreach (var entry in changedOrders)
        {
            var loadedVersion = entry.OriginalValues.GetValue<Guid>(nameof(Order.Version));

            var storedVersion = await Orders
                .AsNoTracking()
                .Where(order => order.Id == entry.Entity.Id)
                .Select(order => (Guid?)order.Version)
                .SingleOrDefaultAsync(cancellationToken);

            if (storedVersion != loadedVersion)
                throw new ConcurrencyException();
        }
    }
}
