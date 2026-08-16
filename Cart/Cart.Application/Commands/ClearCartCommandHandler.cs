using Cart.Application.Abstractions;
using MediatR;

namespace Cart.Application.Commands;

/// <summary>
/// Remove o carrinho de um usuario.
/// </summary>
/// <param name="cartStore">Armazenamento do carrinho.</param>
public sealed class ClearCartCommandHandler(ICartStore cartStore) : IRequestHandler<ClearCartCommand>
{
    /// <inheritdoc />
    public Task Handle(ClearCartCommand request, CancellationToken cancellationToken)
        => cartStore.DeleteAsync(request.UserId, cancellationToken);
}
