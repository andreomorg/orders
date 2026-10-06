using System.Net;
using System.Net.Http.Json;
using Orders.Application.Common;
using Orders.Application.Orders;
using Orders.Domain.Orders;
using static Orders.Tests.Api.OrdersApiClientExtensions;

namespace Orders.Tests.Api;

public sealed class OrdersEndpointsTests : IDisposable
{
    private readonly OrdersApiFactory _factory = new();
    private readonly HttpClient _client;

    public OrdersEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Create_ReturnsCreatedOpenEmptyOrderWithLocation()
    {
        var response = await _client.PostAsync("/api/orders", content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.ReadAsync<OrderDto>();
        Assert.Equal($"/api/orders/{order.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Empty(order.Items);
        Assert.Equal(0m, order.Total);
    }

    [Fact]
    public async Task AddItem_AddsCatalogProductToOrder()
    {
        var order = await _client.CreateOrderAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/orders/{order.Id}/items",
            new { productId = MouseId, quantity = 3 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadAsync<OrderDto>();
        var item = Assert.Single(updated.Items);
        Assert.Equal(MouseId, item.ProductId);
        Assert.Equal("Mouse", item.ProductName);
        Assert.Equal(120m, item.UnitPrice);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(360m, updated.Total);
    }

    [Fact]
    public async Task AddItem_WithProductAlreadyInOrder_AddsUpQuantity()
    {
        var order = await _client.CreateOrderAsync();
        await _client.AddItemAsync(order.Id, MouseId, 2);

        var updated = await _client.AddItemAsync(order.Id, MouseId, 1);

        Assert.Equal(3, Assert.Single(updated.Items).Quantity);
    }

    [Fact]
    public async Task RemoveItem_WithQuantity_DecreasesQuantity()
    {
        var order = await _client.CreateOrderAsync();
        await _client.AddItemAsync(order.Id, MouseId, 3);

        var response = await _client.DeleteAsync($"/api/orders/{order.Id}/items/{MouseId}?quantity=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, Assert.Single((await response.ReadAsync<OrderDto>()).Items).Quantity);
    }

    [Fact]
    public async Task RemoveItem_WithoutQuantity_RemovesWholeItem()
    {
        var order = await _client.CreateOrderAsync();
        await _client.AddItemAsync(order.Id, MouseId, 3);
        await _client.AddItemAsync(order.Id, NotebookId, 1);

        var response = await _client.DeleteAsync($"/api/orders/{order.Id}/items/{MouseId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(NotebookId, Assert.Single((await response.ReadAsync<OrderDto>()).Items).ProductId);
    }

    [Fact]
    public async Task Close_ClosesOrder()
    {
        var order = await _client.CreateOrderAsync();
        await _client.AddItemAsync(order.Id, MouseId, 1);

        var response = await _client.PostAsync($"/api/orders/{order.Id}/close", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var closed = await response.ReadAsync<OrderDto>();
        Assert.Equal(OrderStatus.Closed, closed.Status);
        Assert.NotNull(closed.ClosedAt);
    }

    [Fact]
    public async Task GetById_ReturnsOrderWithItems()
    {
        var order = await _client.CreateOrderAsync();
        await _client.AddItemAsync(order.Id, NotebookId, 1);
        await _client.AddItemAsync(order.Id, MouseId, 2);

        var response = await _client.GetAsync($"/api/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var found = await response.ReadAsync<OrderDto>();
        Assert.Equal(order.Id, found.Id);
        Assert.Equal(2, found.Items.Count);
        Assert.Equal(4740m, found.Total);
    }

    [Fact]
    public async Task List_ReturnsRequestedPageMostRecentFirst()
    {
        var orders = new List<OrderDto>();
        for (var i = 0; i < 5; i++)
        {
            // Keeps creation times distinct, so the expected order is predictable
            await Task.Delay(5);
            orders.Add(await _client.CreateOrderAsync());
        }

        var response = await _client.GetAsync("/api/orders?page=2&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.ReadAsync<PagedResult<OrderDto>>();
        Assert.Equal([orders[2].Id, orders[1].Id], page.Items.Select(order => order.Id));
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task List_WithoutParameters_UsesDefaultPagination()
    {
        await _client.CreateOrderAsync();

        var page = await (await _client.GetAsync("/api/orders")).ReadAsync<PagedResult<OrderDto>>();

        Assert.Equal(Pagination.DefaultPage, page.Page);
        Assert.Equal(Pagination.DefaultPageSize, page.PageSize);
        Assert.Single(page.Items);
    }

    [Theory]
    [InlineData("Closed", OrderStatus.Closed)]
    [InlineData("open", OrderStatus.Open)]
    public async Task List_WithStatus_ReturnsOnlyOrdersWithThatStatus(string status, OrderStatus expected)
    {
        var closed = await _client.CreateOrderAsync();
        await _client.AddItemAsync(closed.Id, MouseId, 1);
        await _client.CloseOrderAsync(closed.Id);
        await _client.CreateOrderAsync();
        await _client.CreateOrderAsync();

        var page = await (await _client.GetAsync($"/api/orders?status={status}")).ReadAsync<PagedResult<OrderDto>>();

        Assert.All(page.Items, order => Assert.Equal(expected, order.Status));
        Assert.Equal(expected == OrderStatus.Closed ? 1 : 2, page.TotalCount);
    }

    [Fact]
    public async Task Responses_UseCamelCaseAndStatusAsText()
    {
        var response = await _client.PostAsync("/api/orders", content: null);

        var json = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"status\":\"Open\"", json);
        Assert.Contains("\"createdAt\":", json);
    }
}
