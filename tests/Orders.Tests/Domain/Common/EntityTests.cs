using Orders.Domain.Orders;

namespace Orders.Tests.Domain.Common;

public class EntityTests
{
    [Fact]
    public void Equals_WithTwoNewEntities_ReturnsFalse()
    {
        var first = TestData.Product();
        var second = TestData.Product();

        Assert.NotEqual(first.Id, second.Id);
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Equals_WithSameInstance_ReturnsTrueAndSameHashCode()
    {
        var product = TestData.Product();
        var sameProduct = product;

        Assert.True(product.Equals(sameProduct));
        Assert.Equal(product.GetHashCode(), sameProduct.GetHashCode());
    }

    [Fact]
    public void Equals_WithNullOrOtherType_ReturnsFalse()
    {
        var product = TestData.Product();

        Assert.False(product.Equals(null));
        Assert.False(product.Equals(new Order()));
    }
}
