using Marketplace.Contracts.Grpc;

namespace Catalog.Application.Abstractions;

/// <summary>
/// Defines product read operations used by the Catalog service.
/// </summary>
public interface IProductReadService
{
    /// <summary>
    /// Retrieves a product by identifier.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The product details when found; otherwise, <see langword="null"/>.</returns>
    Task<ProductDetailsDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);
}