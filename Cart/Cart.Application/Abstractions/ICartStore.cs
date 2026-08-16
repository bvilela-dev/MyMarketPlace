using Cart.Domain.Entities;

namespace Cart.Application.Abstractions;

/// <summary>
/// Armazenamento do carrinho de compras.
/// </summary>
public interface ICartStore
{
    /// <summary>
    /// Busca o carrinho de um usuario.
    /// </summary>
    /// <param name="userId">Usuario dono do carrinho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Carrinho encontrado ou <see langword="null"/>.</returns>
    Task<ShoppingCart?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grava o carrinho completo, substituindo o anterior.
    /// </summary>
    /// <param name="cart">Carrinho a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Task da operacao assincrona.</returns>
    Task SaveAsync(ShoppingCart cart, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove o carrinho de um usuario.
    /// </summary>
    /// <remarks>
    /// Usado ao esvaziar o carrinho manualmente. Num sistema completo tambem seria
    /// chamado apos a criacao do pedido, provavelmente por um consumidor de
    /// <c>OrderCreatedEvent</c>.
    /// </remarks>
    /// <param name="userId">Usuario dono do carrinho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Task da operacao assincrona.</returns>
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}
