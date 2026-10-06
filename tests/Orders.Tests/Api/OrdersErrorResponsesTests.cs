using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Orders.Application.Orders;
using Orders.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using static Orders.Tests.Api.OrdersApiClientExtensions;

namespace Orders.Tests.Api;

public sealed class OrdersErrorResponsesTests : IDisposable
{
    private readonly OrdersApiFactory _factory = new();
    private readonly HttpClient _client;

    public OrdersErrorResponsesTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    // 400 — invalid input

    [Theory]
    [InlineData("page=0", "InvalidPage")]
    [InlineData("page=2147483647", "InvalidPage")]
    [InlineData("pageSize=0", "InvalidPageSize")]
    [InlineData("pageSize=51", "InvalidPageSize")]
    public async Task List_WithInvalidPagination_Returns400WithCode(string query, string code)
    {
        var response = await _client.GetAsync($"/api/orders?{query}");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, code);
    }

    [Theory]
    [InlineData("/api/orders?status=abc")]
    [InlineData("/api/orders/not-a-guid")]
    public async Task MalformedRequest_Returns400(string url)
    {
        var response = await _client.GetAsync(url);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("{}", "ProductId")]
    [InlineData("{\"productId\":\"00000000-0000-0000-0000-000000000002\"}", "Quantity")]
    public async Task AddItem_WithMissingField_Returns400NamingTheField(string body, string field)
    {
        var order = await _client.CreateOrderAsync();

        var response = await _client.PostAsync(
            $"/api/orders/{order.Id}/items",
            new StringContent(body, Encoding.UTF8, "application/json"));

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
    }

    // 404 — not found

    [Fact]
    public async Task GetById_WithMissingOrder_Returns404OrderNotFound()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "OrderNotFound");
    }

    [Fact]
    public async Task AddItem_WithMissingProduct_Returns404ProductNotFound()
    {
        var order = await _client.CreateOrderAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/orders/{order.Id}/items",
            new { productId = Guid.NewGuid(), quantity = 1 });

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "ProductNotFound");
    }

    [Fact]
    public async Task UnknownRoute_Returns404Problem()
    {
        var response = await _client.PostAsync("/api/orders//items", content: null);

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    // 422 — business rules

    [Theory]
    [InlineData(0, "InvalidQuantity")]
    [InlineData(1001, "QuantityExceedsLimit")]
    public async Task AddItem_WithInvalidQuantity_Returns422WithCode(int quantity, string code)
    {
        var order = await _client.CreateOrderAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/orders/{order.Id}/items",
            new { productId = MouseId, quantity });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, code);
    }

    [Fact]
    public async Task Close_WithoutItems_Returns422OrderWithoutItems()
    {
        var order = await _client.CreateOrderAsync();

        var response = await _client.PostAsync($"/api/orders/{order.Id}/close", content: null);

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "OrderWithoutItems");
    }

    [Fact]
    public async Task AddItem_ToClosedOrder_Returns422OrderClosed()
    {
        var order = await ClosedOrderAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/orders/{order.Id}/items",
            new { productId = NotebookId, quantity = 1 });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "OrderClosed");
    }

    [Fact]
    public async Task RemoveItem_FromClosedOrder_Returns422OrderClosed()
    {
        var order = await ClosedOrderAsync();

        var response = await _client.DeleteAsync($"/api/orders/{order.Id}/items/{MouseId}");

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "OrderClosed");
    }

    [Fact]
    public async Task RemoveItem_WithProductNotInOrder_Returns422ProductNotInOrder()
    {
        var order = await _client.CreateOrderAsync();

        var response = await _client.DeleteAsync($"/api/orders/{order.Id}/items/{MouseId}");

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "ProductNotInOrder");
    }

    // 409 and 500 — forced through a substitute service, since requests alone cannot produce them reliably

    [Fact]
    public async Task ConcurrencyConflict_Returns409ConcurrencyConflict()
    {
        var orders = Substitute.For<IOrderService>();
        orders.CloseAsync(default, default).ThrowsAsyncForAnyArgs(new ConcurrencyException());
        using var factory = new OrdersApiFactory(services => services.AddScoped(_ => orders));

        var response = await factory.CreateClient().PostAsync($"/api/orders/{Guid.NewGuid()}/close", content: null);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "ConcurrencyConflict");
    }

    [Fact]
    public async Task UnexpectedError_Returns500WithoutInternalDetails()
    {
        const string internalDetail = "connection string with a secret";
        var orders = Substitute.For<IOrderService>();
        orders.GetByIdAsync(default, default).ThrowsAsyncForAnyArgs(new InvalidOperationException(internalDetail));
        using var factory = new OrdersApiFactory(services => services.AddScoped(_ => orders));

        var response = await factory.CreateClient().GetAsync($"/api/orders/{Guid.NewGuid()}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.False(problem.TryGetProperty("detail", out _));
        Assert.DoesNotContain(internalDetail, problem.GetRawText());
    }

    private async Task<OrderDto> ClosedOrderAsync()
    {
        var order = await _client.CreateOrderAsync();
        await _client.AddItemAsync(order.Id, MouseId, 1);
        return await _client.CloseOrderAsync(order.Id);
    }

    private static async Task<JsonElement> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string? code = null)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());

        if (code is not null)
            Assert.Equal(code, problem.GetProperty("code").GetString());

        return problem;
    }
}
