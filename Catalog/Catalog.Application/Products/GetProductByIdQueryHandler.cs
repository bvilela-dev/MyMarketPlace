using Catalog.Application.Abstractions;
using Marketplace.Contracts.Grpc;
using MediatR;

namespace Catalog.Application.Products;

public sealed class GetProductByIdQueryHandler(IProductReadService productReadService) : IRequestHandler<GetProductByIdQuery, ProductDetailsDto?>
{
    public Task<ProductDetailsDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        => productReadService.GetByIdAsync(request.ProductId, cancellationToken);
}