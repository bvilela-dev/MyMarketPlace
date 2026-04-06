using Catalog.Application.Abstractions;
using Grpc.Core;

namespace Catalog.API.Grpc;

/// <summary>
/// Provides gRPC access to catalog product data.
/// </summary>
public sealed class ProductCatalogGrpcService(IProductReadService productReadService) : ProductCatalog.ProductCatalogBase
{
    /// <summary>
    /// Retrieves product data for downstream services over gRPC.
    /// </summary>
    /// <param name="request">The product lookup request.</param>
    /// <param name="context">The gRPC server context.</param>
    /// <returns>The product response payload.</returns>
    public override async Task<GetProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        var product = await productReadService.GetByIdAsync(Guid.Parse(request.ProductId), context.CancellationToken);
        if (product is null)
        {
            return new GetProductResponse { Found = false };
        }

        return new GetProductResponse
        {
            Found = true,
            ProductId = product.ProductId.ToString(),
            Name = product.Name,
            Price = (double)product.Price,
            AvailableQuantity = product.AvailableQuantity
        };
    }
}