using NSubstitute;
using Orders.Application.Common;
using Orders.Application.Orders;
using Orders.Domain.Common;
using Orders.Domain.Orders;
using Orders.Domain.Products;
using Orders.Tests.Domain;

namespace Orders.Tests.Application.Orders;

public class OrderServiceCommandTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OrderService _service;

    public OrderServiceCommandTests()
    {
        _service = new OrderService(_orders, _products, _unitOfWork);
    }

    // CreateAsync

    [Fact]
    public async Task CreateAsync_AddsAndSavesNewOpenOrder()
    {
        var dto = await _service.CreateAsync();

        Assert.Equal(OrderStatus.Open, dto.Status);
        Assert.Empty(dto.Items);
        await _orders.Received(1).AddAsync(Arg.Is<Order>(order => order.Id == dto.Id), Arg.Any<CancellationToken>());
        await AssertSavedOnce();
    }

    // AddItemAsync

    [Fact]
    public async Task AddItemAsync_WithExistingOrderAndProduct_AddsItemAndSaves()
    {
        var order = GivenOrder(new Order());
        var product = GivenProduct(TestData.Product());

        var dto = await _service.AddItemAsync(order.Id, product.Id, 2);

        var item = Assert.Single(dto.Items);
        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal(2, item.Quantity);
        await AssertSavedOnce();
    }

    [Fact]
    public async Task AddItemAsync_WithMissingOrder_ThrowsOrderNotFoundWithoutSaving()
    {
        var product = GivenProduct(TestData.Product());

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.AddItemAsync(Guid.NewGuid(), product.Id, 1));

        Assert.Equal("OrderNotFound", exception.Code);
        await AssertNotSaved();
    }

    [Fact]
    public async Task AddItemAsync_WithMissingProduct_ThrowsProductNotFoundWithoutSaving()
    {
        var order = GivenOrder(new Order());

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.AddItemAsync(order.Id, Guid.NewGuid(), 1));

        Assert.Equal("ProductNotFound", exception.Code);
        Assert.Empty(order.Items);
        await AssertNotSaved();
    }

    [Fact]
    public async Task AddItemAsync_WhenOrderIsClosed_ThrowsOrderClosedWithoutSaving()
    {
        var product = GivenProduct(TestData.Product());
        var order = GivenOrder(TestData.ClosedOrderWith(product));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.AddItemAsync(order.Id, product.Id, 1));

        Assert.Equal("OrderClosed", exception.Code);
        await AssertNotSaved();
    }

    // RemoveItemAsync

    [Fact]
    public async Task RemoveItemAsync_WithPartialQuantity_DecreasesQuantityAndSaves()
    {
        var product = TestData.Product();
        var order = GivenOrder(TestData.OrderWith((product, 3)));

        var dto = await _service.RemoveItemAsync(order.Id, product.Id, 1);

        Assert.Equal(2, Assert.Single(dto.Items).Quantity);
        await AssertSavedOnce();
    }

    [Fact]
    public async Task RemoveItemAsync_WithMissingOrder_ThrowsOrderNotFoundWithoutSaving()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.RemoveItemAsync(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal("OrderNotFound", exception.Code);
        await AssertNotSaved();
    }

    [Fact]
    public async Task RemoveItemAsync_WithProductNotInOrder_ThrowsProductNotInOrderWithoutSaving()
    {
        var order = GivenOrder(TestData.OrderWith((TestData.Product(), 1)));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.RemoveItemAsync(order.Id, Guid.NewGuid()));

        Assert.Equal("ProductNotInOrder", exception.Code);
        await AssertNotSaved();
    }

    // CloseAsync

    [Fact]
    public async Task CloseAsync_WithItems_ClosesOrderAndSaves()
    {
        var order = GivenOrder(TestData.OrderWith((TestData.Product(), 1)));

        var dto = await _service.CloseAsync(order.Id);

        Assert.Equal(OrderStatus.Closed, dto.Status);
        Assert.NotNull(dto.ClosedAt);
        await AssertSavedOnce();
    }

    [Fact]
    public async Task CloseAsync_WithMissingOrder_ThrowsOrderNotFoundWithoutSaving()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _service.CloseAsync(Guid.NewGuid()));

        Assert.Equal("OrderNotFound", exception.Code);
        await AssertNotSaved();
    }

    [Fact]
    public async Task CloseAsync_WithoutItems_ThrowsOrderWithoutItemsWithoutSaving()
    {
        var order = GivenOrder(new Order());

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.CloseAsync(order.Id));

        Assert.Equal("OrderWithoutItems", exception.Code);
        await AssertNotSaved();
    }

    private Order GivenOrder(Order order)
    {
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        return order;
    }

    private Product GivenProduct(Product product)
    {
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        return product;
    }

    private Task AssertSavedOnce() =>
        _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

    private Task AssertNotSaved() =>
        _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
}
