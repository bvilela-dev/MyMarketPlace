using Catalog.API.Grpc;
using Marketplace.Contracts.Grpc;
using Order.Application.Abstractions;

namespace Order.Infrastructure.Grpc;

/// <summary>
/// Implements Catalog gRPC calls for the Order service.
/// </summary>
public sealed class CatalogGrpcClient(ProductCatalog.ProductCatalogClient client) : ICatalogGrpcClient
{
    /// <summary>
    /// Retrieves a product from the Catalog service.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The product details.</returns>
    public async Task<ProductDetailsDto> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var response = await client.GetProductAsync(new GetProductRequest { ProductId = productId.ToString() }, cancellationToken: cancellationToken);
        if (!response.Found)
        {
            throw new KeyNotFoundException($"Product {productId} was not found.");
        }

        return new ProductDetailsDto(Guid.Parse(response.ProductId), response.Name, (decimal)response.Price, response.AvailableQuantity);
    }
}