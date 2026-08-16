using Marketplace.SharedKernel.Exceptions;

namespace Order.Domain.Entities;

/// <summary>
/// Item de um pedido.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que nome e preco sao copiados do catalogo em vez de referenciados?</b>
/// Porque o pedido precisa ser um registro historico fiel. Se o item apontasse para o
/// produto, uma mudanca de preco amanha reescreveria o valor de todos os pedidos
/// antigos — e a nota fiscal deixaria de bater com o que o cliente realmente pagou.
/// </para>
/// <para>
/// Esta e a diferenca entre dado <b>transacional</b> (imutavel apos o fato) e dado
/// <b>mestre</b> (sempre atualizado). Confundir os dois e uma das causas mais comuns de
/// divergencia contabil em e-commerce.
/// </para>
/// </remarks>
public sealed class OrderItem
{
    /// <summary>
    /// Construtor exigido pelo EF Core.
    /// </summary>
    private OrderItem()
    {
    }

    /// <summary>
    /// Cria um item de pedido.
    /// </summary>
    /// <param name="productId">Identificador do produto comprado.</param>
    /// <param name="name">Nome do produto no momento da compra.</param>
    /// <param name="unitPrice">Preco unitario praticado.</param>
    /// <param name="quantity">Quantidade comprada.</param>
    /// <exception cref="BusinessRuleException">
    /// Lancada quando a quantidade nao e positiva ou o preco e negativo.
    /// </exception>
    public OrderItem(Guid productId, string name, decimal unitPrice, int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("A quantidade do item deve ser maior que zero.");
        }

        if (unitPrice < 0)
        {
            throw new BusinessRuleException("O preco unitario nao pode ser negativo.");
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        Name = name;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    /// <summary>
    /// Identificador do item.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Produto comprado.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Nome do produto no momento da compra.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Preco unitario praticado.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Quantidade comprada.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Valor total da linha (preco unitario x quantidade).
    /// </summary>
    /// <remarks>
    /// Propriedade calculada, sem coluna correspondente no banco: guardar um valor
    /// derivado abriria a possibilidade de ele divergir dos campos que o originam.
    /// </remarks>
    public decimal LineTotal => UnitPrice * Quantity;
}
