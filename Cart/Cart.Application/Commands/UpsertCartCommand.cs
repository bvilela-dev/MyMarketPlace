using Cart.Domain.Entities;
using MediatR;

namespace Cart.Application.Commands;

/// <summary>
/// Cria ou substitui o carrinho de um usuario.
/// </summary>
/// <remarks>
/// O <paramref name="UserId"/> vem do token, preenchido pelo controller — nunca do
/// corpo da requisicao.
/// </remarks>
/// <param name="UserId">Usuario autenticado.</param>
/// <param name="Items">Conteudo completo do carrinho.</param>
public sealed record UpsertCartCommand(Guid UserId, IReadOnlyCollection<CartItem> Items) : IRequest<ShoppingCart>;
