using Orders.Domain.Common;
using Orders.Domain.Resources;

namespace Orders.Domain.Products;

/// <summary>
/// A product from the store catalog.
/// </summary>
public sealed class Product : Entity
{
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }

    // Required by EF Core
    private Product() { }

    public Product(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(nameof(DomainErrors.ProductNameRequired));

        if (price <= 0)
            throw new DomainException(nameof(DomainErrors.ProductPriceInvalid));

        Name = name.Trim();
        Price = price;
    }
}
