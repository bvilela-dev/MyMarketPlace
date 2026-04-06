using Inventory.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryRepository(InventoryDbContext dbContext) : IInventoryRepository
{
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