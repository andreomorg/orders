using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Orders.Tests.Api;

public sealed class DocumentationTests : IDisposable
{
    private readonly OrdersApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Root_RedirectsToSwagger()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task SwaggerUi_IsAvailable()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesAllRoutesAndStatusAsText()
    {
        var document = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var paths = document.GetProperty("paths");
        Assert.Equal(
            ["/api/orders", "/api/orders/{id}", "/api/orders/{id}/close", "/api/orders/{id}/items",
             "/api/orders/{id}/items/{productId}", "/api/products"],
            paths.EnumerateObject().Select(path => path.Name).Order());

        var status = document.GetProperty("components").GetProperty("schemas").GetProperty("OrderStatus");
        Assert.Equal(["Open", "Closed"], status.GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
    }

    [Theory]
    [InlineData("200", "application/json")]
    [InlineData("400", "application/problem+json")]
    [InlineData("404", "application/problem+json")]
    public async Task OpenApiDocument_DocumentsResponseContentTypes(string status, string contentType)
    {
        var document = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var content = document.GetProperty("paths").GetProperty("/api/orders/{id}").GetProperty("get")
            .GetProperty("responses").GetProperty(status).GetProperty("content");

        Assert.Equal([contentType], content.EnumerateObject().Select(type => type.Name));
    }
}
