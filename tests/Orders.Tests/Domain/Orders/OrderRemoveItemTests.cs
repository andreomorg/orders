using Orders.Domain.Common;
using Orders.Domain.Products;

namespace Orders.Tests.Domain.Orders;

public class OrderRemoveItemTests
{
    private readonly Product _product = TestData.Product();

    [Fact]
    public void RemoveItem_WithQuantityLowerThanItemQuantity_DecreasesQuantity()
    {
        var order = TestData.OrderWith((_product, 3));

        order.RemoveItem(_product.Id, 1);

        Assert.Equal(2, Assert.Single(order.Items).Quantity);
    }

    [Fact]
    public void RemoveItem_WithQuantityEqualToItemQuantity_RemovesItem()
    {
        var order = TestData.OrderWith((_product, 3));

        order.RemoveItem(_product.Id, 3);

        Assert.Empty(order.Items);
    }

    [Fact]
    public void RemoveItem_WithoutQuantity_RemovesItem()
    {
        var order = TestData.OrderWith((_product, 3));

        order.RemoveItem(_product.Id);

        Assert.Empty(order.Items);
    }

    [Fact]
    public void RemoveItem_WithQuantityGreaterThanItemQuantity_ThrowsQuantityExceedsItem()
    {
        var order = TestData.OrderWith((_product, 3));

        var exception = Assert.Throws<DomainException>(() => order.RemoveItem(_product.Id, 5));

        Assert.Equal("QuantityExceedsItem", exception.Code);
        Assert.Equal(3, Assert.Single(order.Items).Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RemoveItem_WithQuantityNotPositive_ThrowsInvalidQuantity(int quantity)
    {
        var order = TestData.OrderWith((_product, 3));

        var exception = Assert.Throws<DomainException>(() => order.RemoveItem(_product.Id, quantity));

        Assert.Equal("InvalidQuantity", exception.Code);
        Assert.Equal(3, Assert.Single(order.Items).Quantity);
    }

    [Fact]
    public void RemoveItem_WithProductNotInOrder_ThrowsProductNotInOrder()
    {
        var order = TestData.OrderWith((_product, 3));

        var exception = Assert.Throws<DomainException>(() => order.RemoveItem(Guid.NewGuid()));

        Assert.Equal("ProductNotInOrder", exception.Code);
        Assert.Single(order.Items);
    }

    [Fact]
    public void RemoveItem_WhenOrderIsClosed_ThrowsOrderClosed()
    {
        var order = TestData.ClosedOrderWith(_product, 3);

        var exception = Assert.Throws<DomainException>(() => order.RemoveItem(_product.Id));

        Assert.Equal("OrderClosed", exception.Code);
        Assert.Equal(3, Assert.Single(order.Items).Quantity);
    }

    [Fact]
    public void RemoveItem_WithTwoProductsInOrder_RemovesOnlyInformedProduct()
    {
        var cake = TestData.Product("Cake");
        var order = TestData.OrderWith((_product, 1), (cake, 1));

        order.RemoveItem(_product.Id);

        Assert.Equal(cake.Id, Assert.Single(order.Items).ProductId);
    }
}
