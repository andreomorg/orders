using Orders.Domain.Common;
using Orders.Domain.Orders;
using Orders.Domain.Products;
using Orders.Infrastructure.Persistence.Repositories;
using Orders.Tests.Domain;

namespace Orders.Tests.Infrastructure.Persistence;

/// <summary>
/// Two requests load the same order; the first one saves, then the second one tries to save.
/// </summary>
public class OrderConcurrencyTests
{
    private readonly TestDatabase _database = new();
    private readonly Product _coffee = TestData.Product("Coffee", 10m);
    private readonly Product _cake = TestData.Product("Cake", 7.5m);

    [Fact]
    public async Task SaveChanges_WhenOrderWasClosedByAnotherRequest_ThrowsAndDoesNotAddItem()
    {
        var orderId = await SaveNewAsync(TestData.OrderWith((_coffee, 1)));
        await using var firstRequest = _database.CreateContext();
        await using var secondRequest = _database.CreateContext();
        var orderInFirst = await new OrderRepository(firstRequest).GetByIdAsync(orderId);
        var orderInSecond = await new OrderRepository(secondRequest).GetByIdAsync(orderId);

        orderInFirst!.Close();
        await ((IUnitOfWork)firstRequest).SaveChangesAsync();
        orderInSecond!.AddItem(_cake, 1);

        var exception = await Assert.ThrowsAsync<ConcurrencyException>(
            () => ((IUnitOfWork)secondRequest).SaveChangesAsync());

        Assert.Equal("ConcurrencyConflict", exception.Code);
        var reloaded = await ReloadAsync(orderId);
        Assert.Equal(OrderStatus.Closed, reloaded.Status);
        Assert.Equal(_coffee.Id, Assert.Single(reloaded.Items).ProductId);
    }

    [Fact]
    public async Task SaveChanges_WhenSameItemWasChangedByAnotherRequest_ThrowsAndKeepsFirstChange()
    {
        var orderId = await SaveNewAsync(TestData.OrderWith((_coffee, 1)));
        await using var firstRequest = _database.CreateContext();
        await using var secondRequest = _database.CreateContext();
        var orderInFirst = await new OrderRepository(firstRequest).GetByIdAsync(orderId);
        var orderInSecond = await new OrderRepository(secondRequest).GetByIdAsync(orderId);

        orderInFirst!.AddItem(_coffee, 2);
        await ((IUnitOfWork)firstRequest).SaveChangesAsync();
        orderInSecond!.AddItem(_coffee, 5);

        await Assert.ThrowsAsync<ConcurrencyException>(() => ((IUnitOfWork)secondRequest).SaveChangesAsync());

        var reloaded = await ReloadAsync(orderId);
        Assert.Equal(3, Assert.Single(reloaded.Items).Quantity);
    }

    [Fact]
    public async Task SaveChanges_WhenOrderWasNotChangedByAnotherRequest_Saves()
    {
        var orderId = await SaveNewAsync(TestData.OrderWith((_coffee, 1)));
        await using var request = _database.CreateContext();
        var order = await new OrderRepository(request).GetByIdAsync(orderId);

        order!.AddItem(_cake, 1);
        await ((IUnitOfWork)request).SaveChangesAsync();

        Assert.Equal(2, (await ReloadAsync(orderId)).Items.Count);
    }

    private async Task<Guid> SaveNewAsync(Order order)
    {
        await using var context = _database.CreateContext();
        await new OrderRepository(context).AddAsync(order);
        await context.SaveChangesAsync();
        return order.Id;
    }

    private async Task<Order> ReloadAsync(Guid orderId)
    {
        await using var context = _database.CreateContext();
        var order = await new OrderRepository(context).GetByIdAsync(orderId);
        Assert.NotNull(order);
        return order;
    }
}
