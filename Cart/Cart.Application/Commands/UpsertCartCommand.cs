using Cart.Domain.Entities;
using MediatR;

namespace Cart.Application.Commands;

/// <summary>
/// Represents the request to create or update a user cart.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="Items">The cart items.</param>
public sealed record UpsertCartCommand(Guid UserId, IReadOnlyCollection<CartItem> Items) : IRequest<ShoppingCart>;