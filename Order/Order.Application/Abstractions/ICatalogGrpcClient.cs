using Marketplace.Contracts.Grpc;

namespace Order.Application.Abstractions;

/// <summary>
/// Defines Catalog gRPC operations required by the Order service.
/// </summary>
public interface ICatalogGrpcClient
{
    /// <summary>
    /// Retrieves product data from the Catalog service.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The product details.</returns>
    Task<ProductDetailsDto> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
}