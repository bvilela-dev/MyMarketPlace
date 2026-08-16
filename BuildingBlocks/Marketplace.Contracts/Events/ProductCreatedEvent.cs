namespace Marketplace.Contracts.Events;

/// <summary>
/// Um produto foi cadastrado no catalogo.
/// </summary>
/// <remarks>
/// <para>
/// Publicado pelo <b>Catalog</b> via outbox e consumido pelo <b>Inventory</b>, que cria
/// a linha de estoque correspondente.
/// </para>
/// <para>
/// <b>Por que isso e melhor do que "semear os dois bancos com os mesmos GUIDs"?</b>
/// Porque o alinhamento passa a ser garantido pelo sistema, e nao por dois scripts que
/// alguem precisa lembrar de manter iguais. Cadastrar um produto novo cria o estoque
/// automaticamente — e o mesmo caminho de codigo que roda na demonstracao roda em
/// producao.
/// </para>
/// <para>
/// Este e tambem um exemplo direto de <b>consistencia eventual</b>: por alguns
/// milissegundos o produto existe no Catalog e ainda nao no Inventory. Aceitar essa
/// janela e o preco de nao ter uma transacao distribuida entre os dois bancos.
/// </para>
/// </remarks>
/// <param name="EventId">Identificador unico desta ocorrencia.</param>
/// <param name="ProductId">Identificador do produto criado.</param>
/// <param name="Name">Nome do produto.</param>
/// <param name="Price">Preco de tabela.</param>
/// <param name="InitialQuantity">Quantidade inicial de estoque.</param>
/// <param name="CreatedAtUtc">Momento (UTC) do cadastro.</param>
public sealed record ProductCreatedEvent(
    Guid EventId,
    Guid ProductId,
    string Name,
    decimal Price,
    int InitialQuantity,
    DateTime CreatedAtUtc);
