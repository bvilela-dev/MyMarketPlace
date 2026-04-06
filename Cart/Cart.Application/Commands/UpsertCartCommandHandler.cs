using Cart.Application.Abstractions;
using Cart.Domain.Entities;
using MediatR;

namespace Cart.Application.Commands;

public sealed class UpsertCartCommandHandler(ICartStore cartStore) : IRequestHandler<UpsertCartCommand, ShoppingCart>
{
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