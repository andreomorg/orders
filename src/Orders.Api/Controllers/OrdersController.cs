using Microsoft.AspNetCore.Mvc;
using Orders.Api.Contracts;
using Orders.Application.Common;
using Orders.Application.Orders;
using Orders.Domain.Orders;
using static Orders.Api.Contracts.MediaTypes;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    /// <summary>
    /// Starts a new, empty and open order.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created, Json)]
    public async Task<ActionResult<OrderDto>> Create(CancellationToken cancellationToken)
    {
        var order = await orders.CreateAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Lists orders page by page, most recent first, optionally filtered by status.
    /// </summary>
    /// <param name="status">Only orders with this status (<c>Open</c> or <c>Closed</c>).</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Orders per page, between 1 and 50.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderDto>>(StatusCodes.Status200OK, Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    public async Task<ActionResult<PagedResult<OrderDto>>> List(
        [FromQuery] OrderStatus? status,
        CancellationToken cancellationToken,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize) =>
        Ok(await orders.ListAsync(status, page, pageSize, cancellationToken));

    /// <summary>
    /// Gets an order with its products.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK, Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await orders.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Adds units of a catalog product to an open order. If the product is already in the order, the quantity is added up.
    /// </summary>
    [HttpPost("{id}/items")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK, Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemJson)]
    public async Task<ActionResult<OrderDto>> AddItem(
        Guid id,
        AddOrderItemRequest request,
        CancellationToken cancellationToken) =>
        // [Required] guarantees both values: a missing field returns 400 before reaching this point
        Ok(await orders.AddItemAsync(id, request.ProductId!.Value, request.Quantity!.Value, cancellationToken));

    /// <summary>
    /// Removes units of a product from an open order. Without <c>quantity</c>, the whole item is removed.
    /// </summary>
    /// <param name="id">Order id.</param>
    /// <param name="productId">Product id.</param>
    /// <param name="quantity">Units to remove. When omitted or equal to the item quantity, the item is removed.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpDelete("{id}/items/{productId}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK, Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemJson)]
    public async Task<ActionResult<OrderDto>> RemoveItem(
        Guid id,
        Guid productId,
        [FromQuery] int? quantity,
        CancellationToken cancellationToken) =>
        Ok(await orders.RemoveItemAsync(id, productId, quantity, cancellationToken));

    /// <summary>
    /// Closes an order. Only orders with at least one product can be closed; closed orders cannot be changed.
    /// </summary>
    [HttpPost("{id}/close")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK, Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemJson)]
    public async Task<ActionResult<OrderDto>> Close(Guid id, CancellationToken cancellationToken) =>
        Ok(await orders.CloseAsync(id, cancellationToken));
}
