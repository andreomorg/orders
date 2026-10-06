using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Products;

namespace Orders.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private const int NameMaxLength = 200;

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);
        // The domain generates the Id; EF must never generate one
        builder.Property(product => product.Id).ValueGeneratedNever();

        builder.Property(product => product.Name).HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(product => product.Price).HasPrecision(18, 2);

        // Store catalog with fixed Ids, so they can be documented and used in Swagger
        builder.HasData(
            CatalogItem(1, "Notebook", 4500.00m),
            CatalogItem(2, "Mouse", 120.00m),
            CatalogItem(3, "Keyboard", 250.00m),
            CatalogItem(4, "Monitor 27\"", 1800.00m),
            CatalogItem(5, "Headset", 350.00m),
            CatalogItem(6, "USB-C Cable", 45.00m));
    }

    private static object CatalogItem(int number, string name, decimal price) =>
        new { Id = Guid.Parse($"00000000-0000-0000-0000-{number:D12}"), Name = name, Price = price };
}
