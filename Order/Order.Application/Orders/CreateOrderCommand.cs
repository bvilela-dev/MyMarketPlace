using MediatR;

namespace Order.Application.Orders;

/// <summary>
/// Represents the request to create a new order.
/// </summary>
/// <param name="UserId">The ordering user identifier.</param>
/// <param name="AddressId">The selected address identifier.</param>
/// <param name="Items">The requested order items.</param>
public sealed record CreateOrderCommand(Guid UserId, Guid AddressId, IReadOnlyCollection<CreateOrderItemRequest> Items) : IRequest<CreateOrderResponse>;

/// <summary>
/// Represents an individual line item in an order creation request.
/// </summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="Quantity">The requested quantity.</param>
public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);

/// <summary>
/// Represents the result returned after an order is created.
/// </summary>
/// <param name="OrderId">The created order identifier.</param>
/// <param name="UserId">The ordering user identifier.</param>
/// <param name="Total">The calculated order total.</param>
/// <param name="Status">The initial order status.</param>
public sealed record CreateOrderResponse(Guid OrderId, Guid UserId, decimal Total, string Status);