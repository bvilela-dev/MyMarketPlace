using Cart.Application.Abstractions;
using Cart.Domain.Entities;
using MediatR;

namespace Cart.Application.Commands;

/// <summary>
/// Grava o carrinho completo de um usuario.
/// </summary>
/// <remarks>
/// Operacao naturalmente <b>idempotente</b>: enviar o mesmo carrinho dez vezes produz
/// exatamente o mesmo estado final. E por isso que o verbo HTTP correto aqui e
/// <c>PUT</c>, e nao <c>POST</c> — o cliente pode reenviar apos um timeout sem medo de
/// duplicar itens.
/// </remarks>
/// <param name="cartStore">Armazenamento do carrinho.</param>
public sealed class UpsertCartCommandHandler(ICartStore cartStore) : IRequestHandler<UpsertCartCommand, ShoppingCart>
{
    /// <inheritdoc />
    public async Task<ShoppingCart> Handle(UpsertCartCommand request, CancellationToken cancellationToken)
    {
        // A normalizacao (consolidar produtos repetidos, validar o limite) e regra de
        // dominio e mora na entidade, nao aqui.
        var cart = ShoppingCart.Create(request.UserId, request.Items);

        await cartStore.SaveAsync(cart, cancellationToken);

        return cart;
    }
}
