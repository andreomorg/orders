using Orders.Domain.Common;
using Orders.Domain.Orders;
using Orders.Domain.Products;
using Orders.Infrastructure.Persistence.Repositories;
using Orders.Tests.Domain;

namespace Orders.Tests.Infrastructure.Persistence;

public class OrderRepositoryTests
{
    private readonly TestDatabase _database = new();
    private readonly Product _coffee = TestData.Product("Coffee", 10m);
    private readonly Product _cake = TestData.Product("Cake", 7.5m);

    [Fact]
    public async Task AddAsync_WithItems_PersistsOrderAndItems()
    {
        var order = TestData.OrderWith((_coffee, 2), (_cake, 1));

        await SaveNewAsync(order);
        var reloaded = await ReloadAsync(order.Id);

        Assert.Equal(OrderStatus.Open, reloaded.Status);
        Assert.Equal(order.CreatedAt, reloaded.CreatedAt);
        Assert.Null(reloaded.ClosedAt);
        Assert.Equal(27.5m, reloaded.Total);
        var coffeeItem = Assert.Single(reloaded.Items, item => item.ProductId == _coffee.Id);
        Assert.Equal("Coffee", coffeeItem.ProductName);
        Assert.Equal(10m, coffeeItem.UnitPrice);
        Assert.Equal(2, coffeeItem.Quantity);
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingOrder_ReturnsNull()
    {
        await using var context = _database.CreateContext();

        var order = await new OrderRepository(context).GetByIdAsync(Guid.NewGuid());

        Assert.Null(order);
    }

    [Fact]
    public async Task SaveChanges_AfterAddingItemToSavedOrder_PersistsNewItem()
    {
        var order = TestData.OrderWith((_coffee, 1));
        await SaveNewAsync(order);

        await UpdateAsync(order.Id, saved => saved.AddItem(_cake, 2));
        var reloaded = await ReloadAsync(order.Id);

        Assert.Equal(2, reloaded.Items.Count);
        Assert.Equal(2, Assert.Single(reloaded.Items, item => item.ProductId == _cake.Id).Quantity);
    }

    [Fact]
    public async Task SaveChanges_AfterIncreasingItemQuantity_PersistsQuantity()
    {
        var order = TestData.OrderWith((_coffee, 1));
        await SaveNewAsync(order);

        await UpdateAsync(order.Id, saved => saved.AddItem(_coffee, 2));
        var reloaded = await ReloadAsync(order.Id);

        Assert.Equal(3, Assert.Single(reloaded.Items).Quantity);
    }

    [Fact]
    public async Task SaveChanges_AfterRemovingPartOfItem_PersistsQuantity()
    {
        var order = TestData.OrderWith((_coffee, 3));
        await SaveNewAsync(order);

        await UpdateAsync(order.Id, saved => saved.RemoveItem(_coffee.Id, 1));
        var reloaded = await ReloadAsync(order.Id);

        Assert.Equal(2, Assert.Single(reloaded.Items).Quantity);
    }

    [Fact]
    public async Task SaveChanges_AfterRemovingWholeItem_PersistsRemoval()
    {
        var order = TestData.OrderWith((_coffee, 1), (_cake, 1));
        await SaveNewAsync(order);

        await UpdateAsync(order.Id, saved => saved.RemoveItem(_coffee.Id));
        var reloaded = await ReloadAsync(order.Id);

        Assert.Equal(_cake.Id, Assert.Single(reloaded.Items).ProductId);
    }

    [Fact]
    public async Task SaveChanges_AfterClosingOrder_PersistsClosedStatus()
    {
        var order = TestData.OrderWith((_coffee, 1));
        await SaveNewAsync(order);

        await UpdateAsync(order.Id, saved => saved.Close());
        var reloaded = await ReloadAsync(order.Id);

        Assert.Equal(OrderStatus.Closed, reloaded.Status);
        Assert.NotNull(reloaded.ClosedAt);
    }

    private async Task SaveNewAsync(Order order)
    {
        await using var context = _database.CreateContext();
        await new OrderRepository(context).AddAsync(order);
        await context.SaveChangesAsync();
    }

    private async Task UpdateAsync(Guid orderId, Action<Order> change)
    {
        await using var context = _database.CreateContext();
        var order = await new OrderRepository(context).GetByIdAsync(orderId);
        change(order!);
        // Saves through IUnitOfWork, the same way the application layer does
        await ((IUnitOfWork)context).SaveChangesAsync();
    }

    private async Task<Order> ReloadAsync(Guid orderId)
    {
        await using var context = _database.CreateContext();
        var order = await new OrderRepository(context).GetByIdAsync(orderId);
        Assert.NotNull(order);
        return order;
    }
}
