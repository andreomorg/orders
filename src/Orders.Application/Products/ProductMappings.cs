using Orders.Domain.Products;

namespace Orders.Application.Products;

public static class ProductMappings
{
    public static ProductDto ToDto(this Product product) =>
        new(product.Id, product.Name, product.Price);
}
