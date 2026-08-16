using MediatR;

namespace Order.Application.Orders;

/// <summary>
/// Lista os pedidos do usuario autenticado, do mais recente para o mais antigo.
/// </summary>
/// <param name="UserId">Usuario autenticado.</param>
/// <param name="Page">Numero da pagina (base 1).</param>
/// <param name="PageSize">Itens por pagina.</param>
public sealed record ListUserOrdersQuery(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<IReadOnlyCollection<OrderSummaryDto>>;

/// <summary>
/// Resumo de pedido usado na listagem.
/// </summary>
/// <remarks>
/// Versao enxuta de <see cref="OrderDto"/>: a listagem nao carrega itens nem endereco.
/// Trazer o pedido completo de 50 registros multiplicaria o volume de dados sem que a
/// tela de listagem use quase nada disso.
/// </remarks>
/// <param name="Id">Identificador do pedido.</param>
/// <param name="Total">Valor total.</param>
/// <param name="Status">Estado atual.</param>
/// <param name="ItemCount">Quantidade de linhas do pedido.</param>
/// <param name="CreatedAtUtc">Momento (UTC) da criacao.</param>
/// <param name="UpdatedAtUtc">Momento (UTC) da ultima mudanca de estado.</param>
public sealed record OrderSummaryDto(Guid Id, decimal Total, string Status, int ItemCount, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
