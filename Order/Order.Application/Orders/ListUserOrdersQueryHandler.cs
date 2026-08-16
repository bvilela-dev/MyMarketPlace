using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.Abstractions;

namespace Order.Application.Orders;

/// <summary>
/// Lista os pedidos de um usuario, paginados.
/// </summary>
/// <param name="dbContext">Contrato de persistencia do Order.</param>
public sealed class ListUserOrdersQueryHandler(IOrderDbContext dbContext)
    : IRequestHandler<ListUserOrdersQuery, IReadOnlyCollection<OrderSummaryDto>>
{
    private const int MaxPageSize = 100;

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<OrderSummaryDto>> Handle(ListUserOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        return await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.UserId == request.UserId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenBy(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(order => new OrderSummaryDto(
                order.Id,
                order.Total,
                order.Status.ToString(),
                order.Items.Count,
                order.CreatedAtUtc,
                order.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
