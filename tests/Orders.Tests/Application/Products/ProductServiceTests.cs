using NSubstitute;
using Orders.Application.Products;
using Orders.Domain.Products;
using Orders.Tests.Domain;

namespace Orders.Tests.Application.Products;

public class ProductServiceTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _service = new ProductService(_products);
    }

    [Fact]
    public async Task ListAsync_WithProducts_ReturnsMappedProducts()
    {
        var coffee = TestData.Product("Coffee", 10m);
        var cake = TestData.Product("Cake", 7.5m);
        _products.ListAsync(Arg.Any<CancellationToken>()).Returns([coffee, cake]);

        var result = await _service.ListAsync();

        Assert.Equal(
            [new ProductDto(coffee.Id, "Coffee", 10m), new ProductDto(cake.Id, "Cake", 7.5m)],
            result);
    }

    [Fact]
    public async Task ListAsync_WithEmptyCatalog_ReturnsEmptyList()
    {
        _products.ListAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await _service.ListAsync();

        Assert.Empty(result);
    }
}
