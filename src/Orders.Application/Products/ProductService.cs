using Orders.Domain.Products;

namespace Orders.Application.Products;

public sealed class ProductService(IProductRepository products) : IProductService
{
    public async Task<IReadOnlyList<ProductDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var catalog = await products.ListAsync(cancellationToken);
        return catalog.Select(product => product.ToDto()).ToList();
    }
}
