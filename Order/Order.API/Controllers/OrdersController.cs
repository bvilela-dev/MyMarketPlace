using MediatR;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Orders;

namespace Order.API.Controllers;

/// <summary>
/// Exposes order creation endpoints.
/// </summary>
/// <param name="sender">Mediator used to dispatch order commands.</param>
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Creates a new order.
    /// </summary>
    /// <param name="command">The order creation payload.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created order response.</returns>
    [HttpPost]
    public Task<CreateOrderResponse> Create([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
        => sender.Send(command, cancellationToken);
}