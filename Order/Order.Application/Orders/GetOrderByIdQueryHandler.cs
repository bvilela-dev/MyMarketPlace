using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.Abstractions;

namespace Order.Application.Orders;

/// <summary>
/// Busca um pedido pelo identificador, restrito ao dono.
/// </summary>
/// <remarks>
/// Este endpoint e o que torna a coreografia visivel na demonstracao: consultando o
/// mesmo pedido algumas vezes seguidas, o status caminha de <c>PendingPayment</c> para
/// <c>Paid</c> e depois <c>Confirmed</c>, conforme os eventos vao sendo processados.
/// </remarks>
/// <param name="dbContext">Contrato de persistencia do Order.</param>
public sealed class GetOrderByIdQueryHandler(IOrderDbContext dbContext) : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    /// <inheritdoc />
    public Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        => dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == request.OrderId && order.UserId == request.UserId)
            .Select(order => new OrderDto(
                order.Id,
                order.UserId,
                order.Total,
                order.Status.ToString(),
                order.StatusReason,
                order.CreatedAtUtc,
                order.UpdatedAtUtc,
                new OrderAddressDto(
                    order.AddressSnapshot.Street,
                    order.AddressSnapshot.Number,
                    order.AddressSnapshot.City,
                    order.AddressSnapshot.State,
                    order.AddressSnapshot.ZipCode,
                    order.AddressSnapshot.Country),
                order.Items
                    .Select(item => new OrderItemDto(
                        item.ProductId,
                        item.Name,
                        item.UnitPrice,
                        item.Quantity,
                        item.UnitPrice * item.Quantity))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}
