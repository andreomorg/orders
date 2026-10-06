using Orders.Domain.Orders;

namespace Orders.Application.Orders;

public sealed record OrderDto(
    Guid Id,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    decimal Total,
    IReadOnlyList<OrderItemDto> Items);
