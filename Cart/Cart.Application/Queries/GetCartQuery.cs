using Cart.Domain.Entities;
using MediatR;

namespace Cart.Application.Queries;

/// <summary>
/// Consulta o carrinho de um usuario.
/// </summary>
/// <param name="UserId">Usuario autenticado.</param>
public sealed record GetCartQuery(Guid UserId) : IRequest<ShoppingCart?>;
