using Cart.Application.Abstractions;
using Cart.Domain.Entities;
using MediatR;

namespace Cart.Application.Commands;

/// <summary>
/// Handles cart upsert operations.
/// </summary>
/// <param name="cartStore">The cart persistence abstraction.</param>
public sealed class UpsertCartCommandHandler(ICartStore cartStore) : IRequestHandler<UpsertCartCommand, ShoppingCart>
{
    /// <inheritdoc />
    public async Task<ShoppingCart> Handle(UpsertCartCommand request, CancellationToken cancellationToken)
    {
        var cart = new ShoppingCart
        {
            UserId = request.UserId,
            Items = request.Items
        };

        await cartStore.SaveAsync(cart, cancellationToken);
        return cart;
    }
}