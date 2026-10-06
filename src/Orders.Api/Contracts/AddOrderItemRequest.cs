namespace Orders.Api.Contracts;

/// <summary>
/// Product and quantity to add to an order.
/// </summary>
/// <param name="ProductId">Id of a product from the catalog (<c>GET /api/products</c>).</param>
/// <param name="Quantity">Units to add. Must be greater than zero.</param>
public sealed record AddOrderItemRequest(Guid ProductId, int Quantity);
