using Cart.Domain.Entities;

namespace Cart.Application.Abstractions;

/// <summary>
/// Defines cart persistence operations.
/// </summary>
public interface ICartStore
{
    /// <summary>
    /// Retrieves the cart for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The shopping cart when found; otherwise, <see langword="null"/>.</returns>
    Task<ShoppingCart?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a shopping cart.
    /// </summary>
    /// <param name="cart">The cart to persist.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    Task SaveAsync(ShoppingCart cart, CancellationToken cancellationToken = default);
}