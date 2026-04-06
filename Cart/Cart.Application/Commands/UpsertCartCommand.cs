using Cart.Domain.Entities;
using MediatR;

namespace Cart.Application.Commands;

public sealed record UpsertCartCommand(Guid UserId, IReadOnlyCollection<CartItem> Items) : IRequest<ShoppingCart>;