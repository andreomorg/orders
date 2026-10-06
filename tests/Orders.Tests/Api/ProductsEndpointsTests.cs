using System.Net;
using Orders.Application.Products;

namespace Orders.Tests.Api;

public sealed class ProductsEndpointsTests : IDisposable
{
    private readonly OrdersApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task List_ReturnsSeededCatalogOrderedByName()
    {
        var response = await _factory.CreateClient().GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.ReadAsync<List<ProductDto>>();
        Assert.Equal(
            ["Headset", "Keyboard", "Monitor 27\"", "Mouse", "Notebook", "USB-C Cable"],
            products.Select(product => product.Name));
        Assert.Contains(products, product => product.Id == OrdersApiClientExtensions.NotebookId && product.Price == 4500m);
    }
}
