using Cart.Domain.Entities;
using MediatR;

namespace Cart.Application.Queries;

/// <summary>
/// Represents the request to retrieve a cart by user identifier.
/// </summary>
/// <param name="UserId">The user identifier.</param>
public sealed record GetCartQuery(Guid UserId) : IRequest<ShoppingCart?>;