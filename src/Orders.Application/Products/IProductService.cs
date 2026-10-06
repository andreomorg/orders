namespace Orders.Application.Products;

public interface IProductService
{
    /// <summary>
    /// Lists the store catalog.
    /// </summary>
    Task<IReadOnlyList<ProductDto>> ListAsync(CancellationToken cancellationToken = default);
}
