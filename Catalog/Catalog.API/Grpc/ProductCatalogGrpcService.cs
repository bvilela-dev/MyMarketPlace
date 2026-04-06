using Catalog.Application.Abstractions;
using Grpc.Core;

namespace Catalog.API.Grpc;

public sealed class ProductCatalogGrpcService(IProductReadService productReadService) : ProductCatalog.ProductCatalogBase
{
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