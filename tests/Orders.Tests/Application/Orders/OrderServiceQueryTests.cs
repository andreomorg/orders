using NSubstitute;
using Orders.Application.Common;
using Orders.Application.Orders;
using Orders.Domain.Common;
using Orders.Domain.Orders;
using Orders.Domain.Products;
using Orders.Tests.Domain;

namespace Orders.Tests.Application.Orders;

public class OrderServiceQueryTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly OrderService _service;

    public OrderServiceQueryTests()
    {
        _service = new OrderService(
            _orders,
            Substitute.For<IProductRepository>(),
            Substitute.For<IUnitOfWork>());
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingOrder_ReturnsOrderWithItems()
    {
        var product = TestData.Product();
        var order = TestData.OrderWith((product, 2));
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var dto = await _service.GetByIdAsync(order.Id);

        Assert.Equal(order.Id, dto.Id);
        Assert.Equal(product.Id, Assert.Single(dto.Items).ProductId);
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingOrder_ThrowsOrderNotFound()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));

        Assert.Equal("OrderNotFound", exception.Code);
    }

    [Fact]
    public async Task ListAsync_WithValidParameters_ReturnsPagedOrders()
    {
        var first = TestData.OrderWith((TestData.Product(), 1));
        var second = new Order();
        IReadOnlyList<Order> page = [first, second];
        _orders.ListAsync(OrderStatus.Open, 2, 5, Arg.Any<CancellationToken>()).Returns((page, 12));

        var result = await _service.ListAsync(OrderStatus.Open, page: 2, pageSize: 5);

        Assert.Equal([first.Id, second.Id], result.Items.Select(order => order.Id));
        Assert.Single(result.Items[0].Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(12, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task ListAsync_WithoutParameters_UsesDefaults()
    {
        _orders.ListAsync(default, default, default, default)
            .ReturnsForAnyArgs(((IReadOnlyList<Order>)[], 0));

        var result = await _service.ListAsync();

        await _orders.Received(1).ListAsync(null, 1, 10, Arg.Any<CancellationToken>());
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public async Task ListAsync_WithPageSizeAtLimits_IsAccepted(int pageSize)
    {
        _orders.ListAsync(default, default, default, default)
            .ReturnsForAnyArgs(((IReadOnlyList<Order>)[], 0));

        var result = await _service.ListAsync(pageSize: pageSize);

        Assert.Equal(pageSize, result.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ListAsync_WithInvalidPage_ThrowsInvalidPageWithoutQueryingRepository(int page)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.ListAsync(page: page));

        Assert.Equal("InvalidPage", exception.Code);
        await _orders.DidNotReceiveWithAnyArgs().ListAsync(default, default, default, default);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task ListAsync_WithInvalidPageSize_ThrowsInvalidPageSizeWithoutQueryingRepository(int pageSize)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.ListAsync(pageSize: pageSize));

        Assert.Equal("InvalidPageSize", exception.Code);
        await _orders.DidNotReceiveWithAnyArgs().ListAsync(default, default, default, default);
    }
}
