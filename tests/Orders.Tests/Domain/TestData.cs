using Orders.Domain.Orders;
using Orders.Domain.Products;

namespace Orders.Tests.Domain;

/// <summary>
/// Ready-made domain objects to keep the Arrange step of the tests short.
/// </summary>
internal static class TestData
{
    public static Product Product(string name = "Coffee", decimal price = 10m) => new(name, price);

    public static Order OrderWith(params (Product Product, int Quantity)[] items)
    {
        var order = new Order();

        foreach (var (product, quantity) in items)
            order.AddItem(product, quantity);

        return order;
    }

    public static Order ClosedOrderWith(Product product, int quantity = 1)
    {
        var order = OrderWith((product, quantity));
        order.Close();
        return order;
    }
}
