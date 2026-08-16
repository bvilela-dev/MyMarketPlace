using MediatR;

namespace Order.Application.Orders;

/// <summary>
/// Consulta um pedido do usuario autenticado.
/// </summary>
/// <remarks>
/// O <paramref name="UserId"/> faz parte da consulta, e nao de uma checagem posterior.
/// Filtrar por dono direto no <c>WHERE</c> torna impossivel um pedido alheio ser
/// carregado por engano — a autorizacao vira parte da propria query.
/// </remarks>
/// <param name="OrderId">Identificador do pedido.</param>
/// <param name="UserId">Usuario autenticado.</param>
public sealed record GetOrderByIdQuery(Guid OrderId, Guid UserId) : IRequest<OrderDto?>;
