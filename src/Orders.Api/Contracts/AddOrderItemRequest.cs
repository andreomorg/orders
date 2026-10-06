using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Contracts;

/// <summary>
/// Product and quantity to add to an order.
/// </summary>
/// <param name="ProductId">Id of a product from the catalog (<c>GET /api/products</c>).</param>
/// <param name="Quantity">Units to add. Must be greater than zero.</param>
/// <remarks>
/// Both fields are nullable only so a missing field is reported as a 400 "required" error,
/// instead of silently becoming an empty Guid or zero.
/// </remarks>
public sealed record AddOrderItemRequest(
    [Required] Guid? ProductId,
    [Required] int? Quantity);
