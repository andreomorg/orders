using Orders.Application.Common;
using Orders.Domain.Orders;

namespace Orders.Application.Orders;

public interface IOrderService
{
    /// <summary>
    /// Starts a new, empty and open order.
    /// </summary>
    Task<OrderDto> CreateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order with its items.
    /// </summary>
    Task<OrderDto> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists orders page by page (most recent first), optionally filtered by status.
    /// </summary>
    Task<PagedResult<OrderDto>> ListAsync(
        OrderStatus? status = null,
        int page = Pagination.DefaultPage,
        int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds units of a catalog product to an order.
    /// </summary>
    Task<OrderDto> AddItemAsync(Guid orderId, Guid productId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes units of a product from an order. Without <paramref name="quantity"/>, removes the whole item.
    /// </summary>
    Task<OrderDto> RemoveItemAsync(Guid orderId, Guid productId, int? quantity = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes an order.
    /// </summary>
    Task<OrderDto> CloseAsync(Guid orderId, CancellationToken cancellationToken = default);
}
