using Orders.Domain.Common;
using Orders.Domain.Orders;

namespace Orders.Tests.Domain.Orders;

public class OrderTests
{
    [Fact]
    public void Constructor_CreatesOpenEmptyOrder()
    {
        var before = DateTime.UtcNow;

        var order = new Order();

        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.False(order.IsClosed);
        Assert.Empty(order.Items);
        Assert.Equal(0m, order.Total);
        Assert.Null(order.ClosedAt);
        Assert.Equal(DateTimeKind.Utc, order.CreatedAt.Kind);
        Assert.InRange(order.CreatedAt, before, DateTime.UtcNow);
    }

    [Fact]
    public void Total_WithSeveralItems_SumsUnitPriceTimesQuantity()
    {
        var order = TestData.OrderWith(
            (TestData.Product("Coffee", 10m), 2),
            (TestData.Product("Cake", 7.5m), 3));

        Assert.Equal(42.5m, order.Total);
    }

    [Fact]
    public void Close_WhenOrderHasItems_ClosesOrder()
    {
        var order = TestData.OrderWith((TestData.Product(), 1));
        var before = DateTime.UtcNow;

        order.Close();

        Assert.Equal(OrderStatus.Closed, order.Status);
        Assert.True(order.IsClosed);
        Assert.NotNull(order.ClosedAt);
        Assert.InRange(order.ClosedAt.Value, before, DateTime.UtcNow);
    }

    [Fact]
    public void Close_WhenOrderHasNoItems_ThrowsOrderWithoutItems()
    {
        var order = new Order();

        var exception = Assert.Throws<DomainException>(order.Close);

        Assert.Equal("OrderWithoutItems", exception.Code);
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Null(order.ClosedAt);
    }

    [Fact]
    public void Close_WhenOrderIsAlreadyClosed_ThrowsOrderClosed()
    {
        var order = TestData.ClosedOrderWith(TestData.Product());

        var exception = Assert.Throws<DomainException>(order.Close);

        Assert.Equal("OrderClosed", exception.Code);
    }
}
