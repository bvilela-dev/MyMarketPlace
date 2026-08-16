using Catalog.Application.Abstractions;
using Marketplace.Contracts.Grpc;
using MediatR;

namespace Catalog.Application.Products;

/// <summary>
/// Busca um produto usando o servico de leitura com cache.
/// </summary>
/// <remarks>
/// O handler nao sabe que existe Redis — apenas pede o produto a
/// <see cref="IProductReadService"/>. Trocar o cache por outro mecanismo (ou remove-lo)
/// nao altera uma linha deste caso de uso.
/// </remarks>
/// <param name="productReadService">Servico de leitura de produtos.</param>
public sealed class GetProductByIdQueryHandler(IProductReadService productReadService)
    : IRequestHandler<GetProductByIdQuery, ProductDetailsDto?>
{
    /// <inheritdoc />
    public Task<ProductDetailsDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        => productReadService.GetByIdAsync(request.ProductId, cancellationToken);
}
