using Orders.Domain.Common;
using Orders.Domain.Orders;

namespace Orders.Tests.Domain.Orders;

public class OrderAddItemTests
{
    [Fact]
    public void AddItem_WithNewProduct_AddsItemWithProductSnapshot()
    {
        var product = TestData.Product("Coffee", 12.5m);
        var order = new Order();

        order.AddItem(product, 2);

        var item = Assert.Single(order.Items);
        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal("Coffee", item.ProductName);
        Assert.Equal(12.5m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(25m, item.Subtotal);
    }

    [Fact]
    public void AddItem_WithProductAlreadyInOrder_IncreasesQuantity()
    {
        var product = TestData.Product();
        var order = TestData.OrderWith((product, 2));

        order.AddItem(product, 3);

        var item = Assert.Single(order.Items);
        Assert.Equal(5, item.Quantity);
    }

    [Fact]
    public void AddItem_WithDifferentProducts_AddsOneItemForEach()
    {
        var coffee = TestData.Product("Coffee");
        var cake = TestData.Product("Cake");
        var order = new Order();

        order.AddItem(coffee, 1);
        order.AddItem(cake, 1);

        Assert.Equal(2, order.Items.Count);
        Assert.Contains(order.Items, item => item.ProductId == coffee.Id);
        Assert.Contains(order.Items, item => item.ProductId == cake.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_WithNewProductAndQuantityNotPositive_ThrowsInvalidQuantity(int quantity)
    {
        var order = new Order();

        var exception = Assert.Throws<DomainException>(() => order.AddItem(TestData.Product(), quantity));

        Assert.Equal("InvalidQuantity", exception.Code);
        Assert.Empty(order.Items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_WithExistingProductAndQuantityNotPositive_ThrowsInvalidQuantity(int quantity)
    {
        var product = TestData.Product();
        var order = TestData.OrderWith((product, 2));

        var exception = Assert.Throws<DomainException>(() => order.AddItem(product, quantity));

        Assert.Equal("InvalidQuantity", exception.Code);
        Assert.Equal(2, Assert.Single(order.Items).Quantity);
    }

    [Fact]
    public void AddItem_WithQuantityAtLimit_AddsItem()
    {
        var order = new Order();

        order.AddItem(TestData.Product(), OrderItem.MaxQuantity);

        Assert.Equal(OrderItem.MaxQuantity, Assert.Single(order.Items).Quantity);
    }

    [Theory]
    [InlineData(OrderItem.MaxQuantity + 1)]
    [InlineData(int.MaxValue)]
    public void AddItem_WithNewProductAndQuantityAboveLimit_ThrowsQuantityExceedsLimit(int quantity)
    {
        var order = new Order();

        var exception = Assert.Throws<DomainException>(() => order.AddItem(TestData.Product(), quantity));

        Assert.Equal("QuantityExceedsLimit", exception.Code);
        Assert.Empty(order.Items);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void AddItem_WhenSumExceedsLimit_ThrowsQuantityExceedsLimit(int quantity)
    {
        var product = TestData.Product();
        var order = TestData.OrderWith((product, OrderItem.MaxQuantity - 1));

        var exception = Assert.Throws<DomainException>(() => order.AddItem(product, quantity));

        Assert.Equal("QuantityExceedsLimit", exception.Code);
        Assert.Equal(OrderItem.MaxQuantity - 1, Assert.Single(order.Items).Quantity);
    }

    [Fact]
    public void AddItem_WhenOrderIsClosed_ThrowsOrderClosed()
    {
        var product = TestData.Product();
        var order = TestData.ClosedOrderWith(product);

        var exception = Assert.Throws<DomainException>(() => order.AddItem(TestData.Product("Cake"), 1));

        Assert.Equal("OrderClosed", exception.Code);
        Assert.Equal(product.Id, Assert.Single(order.Items).ProductId);
    }

    [Fact]
    public void AddItem_WithNullProduct_ThrowsArgumentNullException()
    {
        var order = new Order();

        Assert.Throws<ArgumentNullException>(() => order.AddItem(null!, 1));
    }
}
