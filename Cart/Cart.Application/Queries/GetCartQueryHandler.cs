using Cart.Application.Abstractions;
using Cart.Domain.Entities;
using MediatR;

namespace Cart.Application.Queries;

/// <summary>
/// Busca o carrinho de um usuario.
/// </summary>
/// <param name="cartStore">Armazenamento do carrinho.</param>
public sealed class GetCartQueryHandler(ICartStore cartStore) : IRequestHandler<GetCartQuery, ShoppingCart?>
{
    /// <inheritdoc />
    public Task<ShoppingCart?> Handle(GetCartQuery request, CancellationToken cancellationToken)
        => cartStore.GetAsync(request.UserId, cancellationToken);
}
