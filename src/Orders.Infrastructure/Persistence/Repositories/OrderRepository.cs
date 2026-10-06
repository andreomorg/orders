using Microsoft.EntityFrameworkCore;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(OrdersDbContext context) : IOrderRepository
{
    // Tracked, because the use cases change the order and then save it
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Orders.FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Order> Orders, int TotalCount)> ListAsync(
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Orders.AsNoTracking();

        if (status is not null)
            query = query.Where(order => order.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (orders, totalCount);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        await context.Orders.AddAsync(order, cancellationToken);
}
