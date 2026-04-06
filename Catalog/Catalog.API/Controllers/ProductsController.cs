using Catalog.Application.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

/// <summary>
/// Exposes product read endpoints.
/// </summary>
/// <param name="sender">Mediator used to dispatch product queries.</param>
[ApiController]
[Route("api/products")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Retrieves a product by identifier.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The product details when found; otherwise, <see cref="NotFoundResult"/>.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }
}