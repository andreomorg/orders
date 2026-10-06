using Orders.Domain.Orders;
using Orders.Infrastructure.Persistence.Repositories;
using Orders.Tests.Domain;

namespace Orders.Tests.Infrastructure.Persistence;

public class OrderRepositoryListTests
{
    // CreatedAt comes from DateTime.UtcNow; waiting between orders keeps their creation times distinct,
    // so the expected order of the listing is predictable
    private static readonly TimeSpan DelayBetweenOrders = TimeSpan.FromMilliseconds(5);

    private readonly TestDatabase _database = new();

    [Fact]
    public async Task ListAsync_ReturnsMostRecentOrdersFirst()
    {
        var orders = await SaveOrdersAsync(3);

        var (result, _) = await ListAsync(status: null, page: 1, pageSize: 10);

        Assert.Equal(orders.Select(order => order.Id).Reverse(), result.Select(order => order.Id));
    }

    [Fact]
    public async Task ListAsync_WithSecondPage_ReturnsThatPageAndTotalCount()
    {
        var orders = await SaveOrdersAsync(5);

        var (result, totalCount) = await ListAsync(status: null, page: 2, pageSize: 2);

        // Newest first: page 2 holds the 3rd and 4th most recent orders
        Assert.Equal([orders[2].Id, orders[1].Id], result.Select(order => order.Id));
        Assert.Equal(5, totalCount);
    }

    [Fact]
    public async Task ListAsync_WithPageBeyondLast_ReturnsEmptyPageAndTotalCount()
    {
        await SaveOrdersAsync(3);

        var (result, totalCount) = await ListAsync(status: null, page: 5, pageSize: 2);

        Assert.Empty(result);
        Assert.Equal(3, totalCount);
    }

    [Theory]
    [InlineData(OrderStatus.Open, 2)]
    [InlineData(OrderStatus.Closed, 1)]
    public async Task ListAsync_WithStatus_ReturnsOnlyOrdersWithThatStatus(OrderStatus status, int expectedCount)
    {
        var product = TestData.Product();
        await SaveAsync(new Order(), TestData.OrderWith((product, 1)), TestData.ClosedOrderWith(product));

        var (result, totalCount) = await ListAsync(status, page: 1, pageSize: 10);

        Assert.All(result, order => Assert.Equal(status, order.Status));
        Assert.Equal(expectedCount, result.Count);
        Assert.Equal(expectedCount, totalCount);
    }

    [Fact]
    public async Task ListAsync_LoadsOrderItems()
    {
        var order = TestData.OrderWith((TestData.Product("Coffee"), 2), (TestData.Product("Cake"), 1));
        await SaveAsync(order);

        var (result, _) = await ListAsync(status: null, page: 1, pageSize: 10);

        Assert.Equal(2, Assert.Single(result).Items.Count);
    }

    [Fact]
    public async Task ListAsync_WithoutOrders_ReturnsEmptyPage()
    {
        var (result, totalCount) = await ListAsync(status: null, page: 1, pageSize: 10);

        Assert.Empty(result);
        Assert.Equal(0, totalCount);
    }

    private async Task<IReadOnlyList<Order>> SaveOrdersAsync(int count)
    {
        var orders = new List<Order>();

        for (var i = 0; i < count; i++)
        {
            if (i > 0)
                await Task.Delay(DelayBetweenOrders);

            orders.Add(new Order());
        }

        await SaveAsync([.. orders]);
        return orders;
    }

    private async Task SaveAsync(params Order[] orders)
    {
        await using var context = _database.CreateContext();
        var repository = new OrderRepository(context);

        foreach (var order in orders)
            await repository.AddAsync(order);

        await context.SaveChangesAsync();
    }

    private async Task<(IReadOnlyList<Order> Orders, int TotalCount)> ListAsync(
        OrderStatus? status,
        int page,
        int pageSize)
    {
        await using var context = _database.CreateContext();
        return await new OrderRepository(context).ListAsync(status, page, pageSize);
    }
}
