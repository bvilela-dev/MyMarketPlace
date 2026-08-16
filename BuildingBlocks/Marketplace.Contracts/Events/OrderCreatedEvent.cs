namespace Marketplace.Contracts.Events;

/// <summary>
/// Um pedido foi criado e aguarda processamento do pagamento.
/// </summary>
/// <remarks>
/// <para>
/// Primeiro evento da coreografia de checkout. Publicado pelo <b>Order</b> atraves do
/// outbox e consumido pelo <b>Payment</b>.
/// </para>
/// <para>
/// <b>Por que o evento carrega os itens e o endereco em vez de so o <c>OrderId</c>?</b>
/// Este e o classico dilema entre <i>event-carried state transfer</i> e <i>thin event</i>:
/// </para>
/// <list type="bullet">
///   <item><b>Evento magro</b> (so o id): consumidor precisa chamar o Order de volta
///   para buscar os detalhes. Cria acoplamento temporal — se o Order estiver fora do ar,
///   o Payment trava, e o desacoplamento que motivou usar fila se perde.</item>
///   <item><b>Evento gordo</b> (adotado aqui): o consumidor e autossuficiente e processa
///   mesmo com o produtor indisponivel. O custo e mensagem maior e o risco de dado
///   defasado — irrelevante neste caso, pois pedido criado nao muda de itens.</item>
/// </list>
/// </remarks>
/// <param name="EventId">Identificador unico desta ocorrencia (usado na deduplicacao).</param>
/// <param name="OrderId">Identificador do pedido.</param>
/// <param name="UserId">Usuario que criou o pedido.</param>
/// <param name="Total">Valor total do pedido.</param>
/// <param name="Currency">Codigo ISO da moeda do total (ex.: <c>"BRL"</c>).</param>
/// <param name="Address">Copia do endereco de entrega no momento da compra.</param>
/// <param name="Items">Itens do pedido, com preco congelado.</param>
/// <param name="CreatedAtUtc">Momento (UTC) da criacao do pedido.</param>
public sealed record OrderCreatedEvent(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    decimal Total,
    string Currency,
    AddressSnapshotDto Address,
    IReadOnlyCollection<OrderItemDto> Items,
    DateTime CreatedAtUtc);
