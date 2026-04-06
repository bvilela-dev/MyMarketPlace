using Catalog.Application.Abstractions;
using Marketplace.Contracts.Grpc;
using MediatR;

namespace Catalog.Application.Products;

/// <summary>
/// Handles product lookup queries.
/// </summary>
public sealed class GetProductByIdQueryHandler(IProductReadService productReadService) : IRequestHandler<GetProductByIdQuery, ProductDetailsDto?>
{
    /// <summary>
    /// Handles the request to retrieve a product by identifier.
    /// </summary>
    /// <param name="request">The query payload.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The product details when found; otherwise, <see langword="null"/>.</returns>
    public Task<ProductDetailsDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        => productReadService.GetByIdAsync(request.ProductId, cancellationToken);
}