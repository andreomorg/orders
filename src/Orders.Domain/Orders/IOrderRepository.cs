namespace Orders.Domain.Orders;

public interface IOrderRepository
{
    /// <summary>
    /// Gets an order with its items.
    /// </summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists orders page by page, optionally filtered by status.
    /// </summary>
    Task<(IReadOnlyList<Order> Orders, int TotalCount)> ListAsync(
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}
