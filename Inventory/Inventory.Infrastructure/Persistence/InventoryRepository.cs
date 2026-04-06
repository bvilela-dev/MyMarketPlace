using Inventory.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Persists inventory reservation changes.
/// </summary>
public sealed class InventoryRepository(InventoryDbContext dbContext) : IInventoryRepository
{
    /// <summary>
    /// Reserves stock for an order.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>A task representing the asynchronous reservation operation.</returns>
    public async Task ReserveAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var stockItem = await dbContext.StockItems.OrderBy(item => item.ProductId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"No inventory configured to reserve stock for order {orderId}.");

        if (stockItem.QuantityAvailable <= 0)
        {
            throw new InvalidOperationException("Stock is not available.");
        }

        stockItem.QuantityAvailable -= 1;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}