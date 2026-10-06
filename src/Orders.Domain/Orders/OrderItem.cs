using Orders.Domain.Common;
using Orders.Domain.Resources;

namespace Orders.Domain.Orders;

/// <summary>
/// An item of an order. Keeps a snapshot of the product name and price at the moment
/// it was added, so later catalog changes do not affect existing orders.
/// Can only be created and changed by <see cref="Order"/>.
/// </summary>
public sealed class OrderItem : Entity
{
    /// <summary>
    /// Maximum units of a single product in an order.
    /// </summary>
    public const int MaxQuantity = 1000;

    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal Subtotal => UnitPrice * Quantity;

    // Required by EF Core
    private OrderItem() { }

    internal OrderItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        EnsurePositive(quantity);
        EnsureWithinLimit(currentQuantity: 0, quantity);

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    internal void IncreaseQuantity(int quantity)
    {
        EnsurePositive(quantity);
        EnsureWithinLimit(Quantity, quantity);

        Quantity += quantity;
    }

    /// <summary>
    /// Removes some units, keeping at least one. Removing every unit is handled by <see cref="Order"/>.
    /// </summary>
    internal void DecreaseQuantity(int quantity)
    {
        EnsurePositive(quantity);

        if (quantity >= Quantity)
            throw new DomainException(nameof(DomainErrors.QuantityExceedsItem));

        Quantity -= quantity;
    }

    private static void EnsurePositive(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException(nameof(DomainErrors.InvalidQuantity));
    }

    // Compared as "quantity > MaxQuantity - currentQuantity" so the check itself can never overflow
    private static void EnsureWithinLimit(int currentQuantity, int quantity)
    {
        if (quantity > MaxQuantity - currentQuantity)
            throw new DomainException(nameof(DomainErrors.QuantityExceedsLimit));
    }
}
