using Marketplace.Contracts.Grpc;

namespace Order.Application.Abstractions;

public interface ICatalogGrpcClient
{
    Task<ProductDetailsDto> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
}