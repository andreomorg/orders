using Microsoft.AspNetCore.Mvc;
using Orders.Application.Products;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public sealed class ProductsController(IProductService products) : ControllerBase
{
    /// <summary>
    /// Lists the store catalog. Use these ids to add products to an order.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> List(CancellationToken cancellationToken) =>
        Ok(await products.ListAsync(cancellationToken));
}
