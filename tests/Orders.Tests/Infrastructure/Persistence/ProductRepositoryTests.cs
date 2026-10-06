using Orders.Infrastructure.Persistence.Repositories;

namespace Orders.Tests.Infrastructure.Persistence;

public class ProductRepositoryTests
{
    private static readonly Guid NotebookId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly TestDatabase _database = new();

    [Fact]
    public async Task Seed_CreatesCatalogWithFixedIds()
    {
        await using var context = _database.CreateContext();

        var products = await new ProductRepository(context).ListAsync();

        Assert.Equal(
            [
                ("00000000-0000-0000-0000-000000000001", "Notebook", 4500.00m),
                ("00000000-0000-0000-0000-000000000002", "Mouse", 120.00m),
                ("00000000-0000-0000-0000-000000000003", "Keyboard", 250.00m),
                ("00000000-0000-0000-0000-000000000004", "Monitor 27\"", 1800.00m),
                ("00000000-0000-0000-0000-000000000005", "Headset", 350.00m),
                ("00000000-0000-0000-0000-000000000006", "USB-C Cable", 45.00m)
            ],
            products
                .Select(product => (product.Id.ToString(), product.Name, product.Price))
                .OrderBy(product => product.Item1));
    }

    [Fact]
    public async Task ListAsync_ReturnsProductsOrderedByName()
    {
        await using var context = _database.CreateContext();

        var products = await new ProductRepository(context).ListAsync();

        Assert.Equal(products.Select(product => product.Name).Order(), products.Select(product => product.Name));
    }

    [Fact]
    public async Task GetByIdAsync_WithSeededProduct_ReturnsProduct()
    {
        await using var context = _database.CreateContext();

        var product = await new ProductRepository(context).GetByIdAsync(NotebookId);

        Assert.NotNull(product);
        Assert.Equal("Notebook", product.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingProduct_ReturnsNull()
    {
        await using var context = _database.CreateContext();

        var product = await new ProductRepository(context).GetByIdAsync(Guid.NewGuid());

        Assert.Null(product);
    }
}
