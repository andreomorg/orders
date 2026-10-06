using Orders.Domain.Common;
using Orders.Domain.Resources;
using Orders.Domain.Products;

namespace Orders.Domain.Orders;

/// <summary>
/// A store order (aggregate root). Every change to its items goes through here,
/// which guarantees the order business rules.
/// </summary>
public sealed class Order : Entity
{
    private readonly List<OrderItem> _items = [];

    public OrderStatus Status { get; private set; } = OrderStatus.Open;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(item => item.Subtotal);
    public bool IsClosed => Status == OrderStatus.Closed;

    /// <summary>
    /// Adds a product to the order. If the product is already in the order, its quantity is increased.
    /// </summary>
    public void AddItem(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        EnsureIsOpen();

        var existingItem = FindItem(product.Id);
        if (existingItem is not null)
        {
            existingItem.IncreaseQuantity(quantity);
            return;
        }

        _items.Add(new OrderItem(product.Id, product.Name, product.Price, quantity));
    }

    /// <summary>
    /// Removes units of a product from the order. When <paramref name="quantity"/> is not informed
    /// or equals the item quantity, the whole item is removed.
    /// </summary>
    public void RemoveItem(Guid productId, int? quantity = null)
    {
        EnsureIsOpen();

        var item = FindItem(productId)
            ?? throw new DomainException(nameof(DomainErrors.ProductNotInOrder));

        if (quantity is null || quantity == item.Quantity)
        {
            _items.Remove(item);
            return;
        }

        item.DecreaseQuantity(quantity.Value);
    }

    /// <summary>
    /// Closes the order. An order can only be closed when it has at least one product.
    /// </summary>
    public void Close()
    {
        EnsureIsOpen();

        if (_items.Count == 0)
            throw new DomainException(nameof(DomainErrors.OrderWithoutItems));

        Status = OrderStatus.Closed;
        ClosedAt = DateTime.UtcNow;
    }

    private OrderItem? FindItem(Guid productId) =>
        _items.FirstOrDefault(item => item.ProductId == productId);

    private void EnsureIsOpen()
    {
        if (IsClosed)
            throw new DomainException(nameof(DomainErrors.OrderClosed));
    }
}
