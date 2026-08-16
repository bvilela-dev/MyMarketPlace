namespace Inventory.Application.Persistence;

/// <summary>
/// Operacoes de persistencia do estoque.
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// Garante que existe uma linha de estoque para o produto.
    /// </summary>
    /// <remarks>
    /// Idempotente: se a linha ja existir, nada e alterado (a quantidade nao e somada
    /// de novo). Isso permite reprocessar <c>ProductCreatedEvent</c> sem inflar o saldo.
    /// </remarks>
    /// <param name="productId">Produto correspondente.</param>
    /// <param name="initialQuantity">Quantidade inicial, usada apenas na criacao.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Task da operacao assincrona.</returns>
    Task EnsureStockAsync(Guid productId, int initialQuantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserva, numa unica transacao, as quantidades solicitadas de varios produtos.
    /// </summary>
    /// <remarks>
    /// <b>Tudo ou nada.</b> Se qualquer item do pedido nao tiver saldo, nenhuma reserva
    /// e efetivada. Reservar parcialmente deixaria o pedido num limbo: unidades presas
    /// para uma venda que nao vai acontecer.
    /// </remarks>
    /// <param name="quantitiesByProduct">Quantidade solicitada por produto.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Task da operacao assincrona.</returns>
    /// <exception cref="Marketplace.SharedKernel.Exceptions.BusinessRuleException">
    /// Lancada quando algum produto nao existe ou nao tem saldo suficiente.
    /// </exception>
    Task ReserveAsync(IReadOnlyDictionary<Guid, int> quantitiesByProduct, CancellationToken cancellationToken = default);
}
