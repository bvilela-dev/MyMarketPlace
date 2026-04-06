using Cart.Domain.Entities;
using MediatR;

namespace Cart.Application.Queries;

public sealed record GetCartQuery(Guid UserId) : IRequest<ShoppingCart?>;