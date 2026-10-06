using Orders.Domain.Orders;

namespace Orders.Application.Orders;

public static class OrderMappings
{
    public static OrderDto ToDto(this Order order) =>
        new(
            order.Id,
            order.Status,
            order.CreatedAt,
            order.ClosedAt,
            order.Total,
            order.Items.Select(item => item.ToDto()).ToList());

    public static OrderItemDto ToDto(this OrderItem item) =>
        new(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity, item.Subtotal);
}
