using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    private const int StatusMaxLength = 20;
    private const int ProductNameMaxLength = 200;

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);
        // The domain generates the Id; EF must never generate one
        builder.Property(order => order.Id).ValueGeneratedNever();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(StatusMaxLength)
            .IsRequired();

        builder.Property(order => order.CreatedAt).IsRequired();

        // Calculated by the domain, not stored
        builder.Ignore(order => order.Total);
        builder.Ignore(order => order.IsClosed);

        // Items belong to the order aggregate: owned, always loaded and saved together with it
        builder.OwnsMany(order => order.Items, items =>
        {
            items.WithOwner().HasForeignKey("OrderId");

            items.HasKey(item => item.Id);
            // Without this, EF treats an item added to an existing order as an update instead of an insert
            items.Property(item => item.Id).ValueGeneratedNever();

            items.Property(item => item.ProductName).HasMaxLength(ProductNameMaxLength).IsRequired();
            items.Property(item => item.UnitPrice).HasPrecision(18, 2);
            items.Ignore(item => item.Subtotal);
        });

        // EF reads and writes the private _items list; the public Items stays read-only
        builder.Navigation(order => order.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
