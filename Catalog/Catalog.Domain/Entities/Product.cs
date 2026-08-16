using Marketplace.SharedKernel.Abstractions;
using Marketplace.SharedKernel.Exceptions;

namespace Catalog.Domain.Entities;

/// <summary>
/// Agregado que representa um produto do catalogo.
/// </summary>
/// <remarks>
/// <para>
/// <b>Modelo rico, nao anemico.</b> Todas as propriedades tem <c>private set</c> e as
/// mudancas passam por metodos com nome de negocio (<see cref="ChangePrice"/>,
/// <see cref="AdjustAvailableQuantity"/>). O ganho pratico: e impossivel gravar um
/// produto com preco negativo, porque nao existe caminho no codigo que permita isso.
/// </para>
/// <para>
/// Compare com o modelo anemico — <c>{ get; set; }</c> em tudo e validacao espalhada
/// pelos servicos. Ali, basta <b>um</b> caminho esquecer a checagem para o dado invalido
/// entrar no banco. Aqui a regra mora junto do dado que ela protege.
/// </para>
/// <para>
/// <b>Nota sobre o campo <see cref="AvailableQuantity"/>.</b> Ele e a quantidade
/// exibida na vitrine, mantida pelo Catalog. A quantidade que vale para reserva e a do
/// <b>Inventory</b>, que e o dono da verdade do estoque. Duplicar o numero aqui e uma
/// escolha deliberada de leitura rapida (o Order consulta o Catalog antes de criar o
/// pedido, sem tocar no Inventory) — com a contrapartida de tolerar defasagem.
/// </para>
/// </remarks>
public sealed class Product : AggregateRoot
{
    /// <summary>
    /// Construtor exigido pelo EF Core.
    /// </summary>
    private Product()
    {
    }

    /// <summary>
    /// Cadastra um novo produto.
    /// </summary>
    /// <param name="id">Identificador do produto.</param>
    /// <param name="name">Nome do produto.</param>
    /// <param name="description">Descricao do produto.</param>
    /// <param name="price">Preco de tabela (deve ser positivo).</param>
    /// <param name="availableQuantity">Quantidade inicial (nao pode ser negativa).</param>
    /// <exception cref="BusinessRuleException">
    /// Lancada quando preco ou quantidade violam as regras do catalogo.
    /// </exception>
    public Product(Guid id, string name, string description, decimal price, int availableQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (price <= 0)
        {
            throw new BusinessRuleException("O preco do produto deve ser maior que zero.");
        }

        if (availableQuantity < 0)
        {
            throw new BusinessRuleException("A quantidade disponivel nao pode ser negativa.");
        }

        Id = id;
        Name = name.Trim();
        Description = (description ?? string.Empty).Trim();
        Price = price;
        AvailableQuantity = availableQuantity;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Nome do produto.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Descricao do produto.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Preco de tabela.
    /// </summary>
    /// <remarks>
    /// <c>decimal</c>, jamais <c>double</c>. Ponto flutuante binario nao representa
    /// 0,1 exatamente, e somar centavos acumula erro — em dinheiro isso vira divergencia
    /// de fechamento. <c>decimal</c> e base 10 e exato para valores monetarios.
    /// </remarks>
    public decimal Price { get; private set; }

    /// <summary>
    /// Quantidade disponivel para exibicao na vitrine.
    /// </summary>
    public int AvailableQuantity { get; private set; }

    /// <summary>
    /// Momento (UTC) do cadastro.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Altera o preco do produto.
    /// </summary>
    /// <param name="newPrice">Novo preco (deve ser positivo).</param>
    /// <exception cref="BusinessRuleException">Lancada quando o preco nao e positivo.</exception>
    public void ChangePrice(decimal newPrice)
    {
        if (newPrice <= 0)
        {
            throw new BusinessRuleException("O preco do produto deve ser maior que zero.");
        }

        Price = newPrice;
    }

    /// <summary>
    /// Ajusta a quantidade disponivel somando um delta.
    /// </summary>
    /// <param name="delta">Variacao (positiva para reposicao, negativa para baixa).</param>
    /// <exception cref="BusinessRuleException">
    /// Lancada quando o ajuste levaria a quantidade a um valor negativo.
    /// </exception>
    public void AdjustAvailableQuantity(int delta)
    {
        var updated = AvailableQuantity + delta;

        if (updated < 0)
        {
            throw new BusinessRuleException($"Estoque insuficiente para o produto {Id}.");
        }

        AvailableQuantity = updated;
    }
}
