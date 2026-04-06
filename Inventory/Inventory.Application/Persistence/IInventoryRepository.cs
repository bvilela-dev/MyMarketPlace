namespace Inventory.Application.Persistence;

public interface IInventoryRepository
{
    Task ReserveAsync(Guid orderId, CancellationToken cancellationToken = default);
}