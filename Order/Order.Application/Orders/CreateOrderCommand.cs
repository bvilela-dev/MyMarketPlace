using MediatR;

namespace Order.Application.Orders;

public sealed record CreateOrderCommand(Guid UserId, Guid AddressId, IReadOnlyCollection<CreateOrderItemRequest> Items) : IRequest<CreateOrderResponse>;

public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);

public sealed record CreateOrderResponse(Guid OrderId, Guid UserId, decimal Total, string Status);