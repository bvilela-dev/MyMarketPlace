using Marketplace.Contracts.Grpc;

namespace Catalog.Application.Abstractions;

public interface IProductReadService
{
    Task<ProductDetailsDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);
}