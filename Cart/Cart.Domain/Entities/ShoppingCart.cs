using Marketplace.SharedKernel.Exceptions;

namespace Cart.Domain.Entities;

/// <summary>
/// Carrinho de compras de um usuario.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que o carrinho fica no Redis e nao no Postgres?</b> Porque o dado do carrinho
/// e volatil, de altissima frequencia de escrita e sem valor historico: cada clique em
/// "+1" e uma gravacao, e ninguem precisa auditar carrinhos abandonados de dois anos
/// atras. Isso e o oposto do perfil de um pedido — que e imutavel, consultado com
/// moderacao e precisa durar para sempre.
/// </para>
/// <para>
/// <b>Estrategia de gravacao:</b> o carrinho inteiro e substituido a cada operacao
/// (whole-cart replace), em vez de aplicar deltas. Com no maximo algumas dezenas de
/// itens, o custo e desprezivel e o ganho e enorme: some toda a classe de bugs de
/// mesclagem entre abas ou dispositivos concorrentes.
/// </para>
/// </remarks>
public sealed class ShoppingCart
{
    /// <summary>
    /// Numero maximo de linhas distintas em um carrinho.
    /// </summary>
    public const int MaxItems = 100;

    /// <summary>
    /// Usuario dono do carrinho.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Itens do carrinho.
    /// </summary>
    public IReadOnlyCollection<CartItem> Items { get; init; } = [];

    /// <summary>
    /// Momento (UTC) da ultima alteracao.
    /// </summary>
    public DateTime UpdatedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Valor total do carrinho.
    /// </summary>
    /// <remarks>
    /// Calculado, nunca armazenado. Vale como estimativa de vitrine: o preco que sera
    /// efetivamente cobrado e o que o Order busca no Catalog no momento da compra.
    /// </remarks>
    public decimal Total => Items.Sum(item => item.UnitPrice * item.Quantity);

    /// <summary>
    /// Cria um carrinho consolidando itens repetidos do mesmo produto.
    /// </summary>
    /// <remarks>
    /// A consolidacao evita que o mesmo produto apareca em duas linhas do carrinho —
    /// o que confundiria a exibicao e a checagem de limite de itens.
    /// </remarks>
    /// <param name="userId">Usuario dono do carrinho.</param>
    /// <param name="items">Itens informados pelo cliente.</param>
    /// <returns>Carrinho normalizado.</returns>
    /// <exception cref="BusinessRuleException">
    /// Lancada quando o carrinho ultrapassa <see cref="MaxItems"/> linhas distintas.
    /// </exception>
    public static ShoppingCart Create(Guid userId, IEnumerable<CartItem> items)
    {
        var consolidated = items
            .GroupBy(item => item.ProductId)
            .Select(group => group.First() with { Quantity = group.Sum(item => item.Quantity) })
            .ToArray();

        if (consolidated.Length > MaxItems)
        {
            throw new BusinessRuleException($"O carrinho pode ter no maximo {MaxItems} produtos distintos.");
        }

        return new ShoppingCart
        {
            UserId = userId,
            Items = consolidated,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Linha do carrinho.
/// </summary>
/// <param name="ProductId">Produto.</param>
/// <param name="Name">Nome exibido do produto.</param>
/// <param name="UnitPrice">Preco unitario exibido.</param>
/// <param name="Quantity">Quantidade desejada.</param>
public sealed record CartItem(Guid ProductId, string Name, decimal UnitPrice, int Quantity);
