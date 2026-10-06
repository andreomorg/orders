using Orders.Application.Common;
using Orders.Application.Resources;
using Orders.Domain.Common;
using Orders.Domain.Orders;
using Orders.Domain.Products;

namespace Orders.Application.Orders;

/// <summary>
/// Order use cases. Coordinates the domain (load, change, save); the business rules live in <see cref="Order"/>.
/// </summary>
public sealed class OrderService(
    IOrderRepository orders,
    IProductRepository products,
    IUnitOfWork unitOfWork) : IOrderService
{
    public async Task<OrderDto> CreateAsync(CancellationToken cancellationToken = default)
    {
        var order = new Order();

        await orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }

    public async Task<OrderDto> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAsync(orderId, cancellationToken);
        return order.ToDto();
    }

    public async Task<PagedResult<OrderDto>> ListAsync(
        OrderStatus? status = null,
        int page = Pagination.DefaultPage,
        int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        Pagination.Validate(page, pageSize);

        var (pageOrders, totalCount) = await orders.ListAsync(status, page, pageSize, cancellationToken);

        return new PagedResult<OrderDto>(
            pageOrders.Select(order => order.ToDto()).ToList(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<OrderDto> AddItemAsync(
        Guid orderId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAsync(orderId, cancellationToken);
        var product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApplicationErrors.ProductNotFound));

        order.AddItem(product, quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }

    public async Task<OrderDto> RemoveItemAsync(
        Guid orderId,
        Guid productId,
        int? quantity = null,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAsync(orderId, cancellationToken);

        order.RemoveItem(productId, quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }

    public async Task<OrderDto> CloseAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAsync(orderId, cancellationToken);

        order.Close();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }

    private async Task<Order> GetOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        await orders.GetByIdAsync(orderId, cancellationToken)
        ?? throw new NotFoundException(nameof(ApplicationErrors.OrderNotFound));
}
