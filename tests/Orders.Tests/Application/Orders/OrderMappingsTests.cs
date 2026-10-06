using Orders.Application.Orders;
using Orders.Tests.Domain;

namespace Orders.Tests.Application.Orders;

public class OrderMappingsTests
{
    [Fact]
    public void OrderToDto_CopiesAllFields()
    {
        var coffee = TestData.Product("Coffee", 10m);
        var cake = TestData.Product("Cake", 7.5m);
        var order = TestData.OrderWith((coffee, 2), (cake, 1));
        order.Close();

        var dto = order.ToDto();

        Assert.Equal(order.Id, dto.Id);
        Assert.Equal(order.Status, dto.Status);
        Assert.Equal(order.CreatedAt, dto.CreatedAt);
        Assert.Equal(order.ClosedAt, dto.ClosedAt);
        Assert.Equal(27.5m, dto.Total);
        Assert.Equal(2, dto.Items.Count);
        Assert.Contains(dto.Items, item => item.ProductId == coffee.Id);
        Assert.Contains(dto.Items, item => item.ProductId == cake.Id);
    }

    [Fact]
    public void OrderItemToDto_CopiesAllFields()
    {
        var product = TestData.Product("Coffee", 10m);
        var item = TestData.OrderWith((product, 3)).Items.Single();

        var dto = item.ToDto();

        Assert.Equal(product.Id, dto.ProductId);
        Assert.Equal("Coffee", dto.ProductName);
        Assert.Equal(10m, dto.UnitPrice);
        Assert.Equal(3, dto.Quantity);
        Assert.Equal(30m, dto.Subtotal);
    }
}
