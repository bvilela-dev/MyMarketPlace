using MediatR;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Orders;

namespace Order.API.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public Task<CreateOrderResponse> Create([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
        => sender.Send(command, cancellationToken);
}