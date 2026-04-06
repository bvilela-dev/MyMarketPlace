using Cart.Application.Abstractions;
using Cart.Domain.Entities;
using MediatR;

namespace Cart.Application.Queries;

/// <summary>
/// Handles cart retrieval queries.
/// </summary>
/// <param name="cartStore">The cart persistence abstraction.</param>
public sealed class GetCartQueryHandler(ICartStore cartStore) : IRequestHandler<GetCartQuery, ShoppingCart?>
{
    /// <inheritdoc />
    public Task<ShoppingCart?> Handle(GetCartQuery request, CancellationToken cancellationToken)
        => cartStore.GetAsync(request.UserId, cancellationToken);
}