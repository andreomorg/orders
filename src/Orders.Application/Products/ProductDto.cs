namespace Orders.Application.Products;

public sealed record ProductDto(Guid Id, string Name, decimal Price);
