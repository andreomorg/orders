using Orders.Domain.Common;
using Orders.Domain.Products;

namespace Orders.Tests.Domain.Products;

public class ProductTests
{
    [Fact]
    public void Constructor_WithValidData_SetsProperties()
    {
        var product = new Product("Coffee", 12.5m);

        Assert.Equal("Coffee", product.Name);
        Assert.Equal(12.5m, product.Price);
        Assert.NotEqual(Guid.Empty, product.Id);
    }

    [Fact]
    public void Constructor_WithSurroundingSpacesInName_TrimsName()
    {
        var product = new Product("  Coffee  ", 12.5m);

        Assert.Equal("Coffee", product.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsProductNameRequired(string name)
    {
        var exception = Assert.Throws<DomainException>(() => new Product(name, 12.5m));

        Assert.Equal("ProductNameRequired", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithPriceNotPositive_ThrowsProductPriceInvalid(decimal price)
    {
        var exception = Assert.Throws<DomainException>(() => new Product("Coffee", price));

        Assert.Equal("ProductPriceInvalid", exception.Code);
    }
}
