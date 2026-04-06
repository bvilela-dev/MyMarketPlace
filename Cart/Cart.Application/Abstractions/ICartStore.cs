using Cart.Domain.Entities;

namespace Cart.Application.Abstractions;

public interface ICartStore
{
    Task<ShoppingCart?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SaveAsync(ShoppingCart cart, CancellationToken cancellationToken = default);
}