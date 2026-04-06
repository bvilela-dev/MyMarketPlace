using Cart.Application.Commands;
using Cart.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Controllers;

[ApiController]
[Route("api/carts")]
public sealed class CartsController(ISender sender) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
    {
        var cart = await sender.Send(new GetCartQuery(userId), cancellationToken);
        return cart is null ? NotFound() : Ok(cart);
    }

    [HttpPut("{userId:guid}")]
    public Task<object> Put(Guid userId, [FromBody] UpsertCartRequest request, CancellationToken cancellationToken)
        => sender.Send(new UpsertCartCommand(userId, request.Items.Select(item => new Cart.Domain.Entities.CartItem(item.ProductId, item.Name, item.UnitPrice, item.Quantity)).ToArray()), cancellationToken)
            .ContinueWith(task => (object)task.Result, cancellationToken);
}

public sealed record UpsertCartRequest(IReadOnlyCollection<UpsertCartItemRequest> Items);

public sealed record UpsertCartItemRequest(Guid ProductId, string Name, decimal UnitPrice, int Quantity);