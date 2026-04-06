namespace Inventory.Application.Persistence;

/// <summary>
/// Defines persistence operations for inventory reservations.
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// Reserves inventory for a paid order.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReserveAsync(Guid orderId, CancellationToken cancellationToken = default);
}