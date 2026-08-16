namespace Marketplace.Contracts.Events;

/// <summary>
/// Item de pedido trafegando em eventos de integracao.
/// </summary>
/// <remarks>
/// O <see cref="UnitPrice"/> viaja junto de proposito: e o preco <b>congelado</b> no
/// momento da compra. Consultar o preco atual do catalogo ao processar o evento faria
/// o valor cobrado mudar caso o produto entrasse em promocao entre a compra e o
/// processamento.
/// </remarks>
/// <param name="ProductId">Identificador do produto.</param>
/// <param name="Name">Nome do produto no momento da compra.</param>
/// <param name="UnitPrice">Preco unitario praticado.</param>
/// <param name="Quantity">Quantidade comprada.</param>
public sealed record OrderItemDto(Guid ProductId, string Name, decimal UnitPrice, int Quantity);
