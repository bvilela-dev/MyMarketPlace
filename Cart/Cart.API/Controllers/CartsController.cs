using Cart.Application.Commands;
using Cart.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Controllers;

/// <summary>
/// Exposes cart read and write endpoints.
/// </summary>
/// <param name="sender">Mediator used to dispatch cart commands and queries.</param>
[ApiController]
[Route("api/carts")]
public sealed class CartsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Retrieves the shopping cart for a given user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The shopping cart when found; otherwise, <see cref="NotFoundResult"/>.</returns>
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
    {
        var cart = await sender.Send(new GetCartQuery(userId), cancellationToken);
        return cart is null ? NotFound() : Ok(cart);
    }

    /// <summary>
    /// Creates or replaces the shopping cart for a given user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="request">The cart payload.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The stored cart representation.</returns>
    [HttpPut("{userId:guid}")]
    public Task<object> Put(Guid userId, [FromBody] UpsertCartRequest request, CancellationToken cancellationToken)
        => sender.Send(new UpsertCartCommand(userId, request.Items.Select(item => new Cart.Domain.Entities.CartItem(item.ProductId, item.Name, item.UnitPrice, item.Quantity)).ToArray()), cancellationToken)
            .ContinueWith(task => (object)task.Result, cancellationToken);
}

/// <summary>
/// Represents the HTTP payload used to create or update a shopping cart.
/// </summary>
/// <param name="Items">The cart item collection.</param>
public sealed record UpsertCartRequest(IReadOnlyCollection<UpsertCartItemRequest> Items);

/// <summary>
/// Represents an item inside the cart HTTP payload.
/// </summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="Name">The product name.</param>
/// <param name="UnitPrice">The product unit price.</param>
/// <param name="Quantity">The item quantity.</param>
public sealed record UpsertCartItemRequest(Guid ProductId, string Name, decimal UnitPrice, int Quantity);