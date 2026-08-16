using Marketplace.SharedKernel.Exceptions;

namespace Inventory.Domain.Entities;

/// <summary>
/// Saldo de estoque de um produto.
/// </summary>
/// <remarks>
/// <para>
/// O Inventory e o <b>dono da verdade</b> sobre estoque. O numero exibido no Catalog e
/// uma copia para leitura rapida na vitrine; a reserva que vale e a que acontece aqui,
/// dentro de uma transacao.
/// </para>
/// <para>
/// <b>Antes esta classe era um saco de <c>{ get; set; }</c></b> e a "reserva" ficava
/// espalhada no repositorio — que, na pratica, pegava um item qualquer da tabela e
/// subtraia 1, sem olhar qual produto havia sido comprado. Trazer <see cref="Reserve"/>
/// e <see cref="Release"/> para a entidade torna esse tipo de erro impossivel de
/// escrever.
/// </para>
/// </remarks>
public sealed class StockItem
{
    /// <summary>
    /// Construtor exigido pelo EF Core.
    /// </summary>
    private StockItem()
    {
    }

    /// <summary>
    /// Cria o saldo inicial de um produto.
    /// </summary>
    /// <param name="id">Identificador da linha de estoque.</param>
    /// <param name="productId">Produto correspondente.</param>
    /// <param name="quantityAvailable">Quantidade inicial disponivel.</param>
    /// <exception cref="BusinessRuleException">Lancada quando a quantidade e negativa.</exception>
    public StockItem(Guid id, Guid productId, int quantityAvailable)
    {
        if (quantityAvailable < 0)
        {
            throw new BusinessRuleException("A quantidade inicial nao pode ser negativa.");
        }

        Id = id;
        ProductId = productId;
        QuantityAvailable = quantityAvailable;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Identificador da linha de estoque.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Produto correspondente.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Quantidade livre para novas reservas.
    /// </summary>
    public int QuantityAvailable { get; private set; }

    /// <summary>
    /// Quantidade ja reservada para pedidos pagos e ainda nao expedidos.
    /// </summary>
    /// <remarks>
    /// Manter reservado separado de disponivel — em vez de apenas subtrair — permite
    /// responder "quanto existe fisicamente no deposito?" e devolver a quantidade ao
    /// disponivel numa compensacao (ver <see cref="Release"/>).
    /// </remarks>
    public int QuantityReserved { get; private set; }

    /// <summary>
    /// Momento (UTC) da ultima movimentacao.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Quantidade fisica total (disponivel + reservada).
    /// </summary>
    public int QuantityOnHand => QuantityAvailable + QuantityReserved;

    /// <summary>
    /// Reserva unidades para um pedido pago.
    /// </summary>
    /// <param name="quantity">Quantidade a reservar.</param>
    /// <exception cref="BusinessRuleException">
    /// Lancada quando a quantidade nao e positiva ou o saldo disponivel e insuficiente.
    /// </exception>
    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("A quantidade reservada deve ser maior que zero.");
        }

        if (QuantityAvailable < quantity)
        {
            throw new BusinessRuleException(
                $"Estoque insuficiente para o produto {ProductId}: disponivel {QuantityAvailable}, solicitado {quantity}.");
        }

        QuantityAvailable -= quantity;
        QuantityReserved += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Devolve unidades reservadas ao saldo disponivel.
    /// </summary>
    /// <remarks>
    /// Operacao de compensacao: usada quando um pedido e cancelado depois da reserva.
    /// </remarks>
    /// <param name="quantity">Quantidade a liberar.</param>
    /// <exception cref="BusinessRuleException">
    /// Lancada quando se tenta liberar mais do que esta reservado.
    /// </exception>
    public void Release(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("A quantidade liberada deve ser maior que zero.");
        }

        if (QuantityReserved < quantity)
        {
            throw new BusinessRuleException(
                $"Nao ha {quantity} unidades reservadas do produto {ProductId} para liberar.");
        }

        QuantityReserved -= quantity;
        QuantityAvailable += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Repoe unidades no saldo disponivel (entrada de mercadoria).
    /// </summary>
    /// <param name="quantity">Quantidade recebida.</param>
    /// <exception cref="BusinessRuleException">Lancada quando a quantidade nao e positiva.</exception>
    public void Replenish(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("A quantidade reposta deve ser maior que zero.");
        }

        QuantityAvailable += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
