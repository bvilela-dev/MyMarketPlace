using MediatR;

namespace Cart.Application.Commands;

/// <summary>
/// Esvazia o carrinho de um usuario.
/// </summary>
/// <param name="UserId">Usuario autenticado.</param>
public sealed record ClearCartCommand(Guid UserId) : IRequest;
