using Orders.Domain.Common;
using Orders.Domain.Orders;
using Orders.Domain.Products;

namespace Orders.Tests.Domain.Orders;

public class OrderVersionTests
{
    private readonly Product _product = TestData.Product();

    [Fact]
    public void AddItem_ChangesVersion()
    {
        var order = new Order();
        var version = order.Version;

        order.AddItem(_product, 1);

        Assert.NotEqual(version, order.Version);
    }

    [Fact]
    public void RemoveItem_ChangesVersion()
    {
        var order = TestData.OrderWith((_product, 2));
        var version = order.Version;

        order.RemoveItem(_product.Id, 1);

        Assert.NotEqual(version, order.Version);
    }

    [Fact]
    public void Close_ChangesVersion()
    {
        var order = TestData.OrderWith((_product, 1));
        var version = order.Version;

        order.Close();

        Assert.NotEqual(version, order.Version);
    }

    [Fact]
    public void FailedChange_KeepsVersion()
    {
        var order = new Order();
        var version = order.Version;

        Assert.Throws<DomainException>(order.Close);
        Assert.Throws<DomainException>(() => order.AddItem(_product, 0));
        Assert.Throws<DomainException>(() => order.RemoveItem(_product.Id));

        Assert.Equal(version, order.Version);
    }
}
