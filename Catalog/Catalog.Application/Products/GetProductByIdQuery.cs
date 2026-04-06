using Marketplace.Contracts.Grpc;
using MediatR;

namespace Catalog.Application.Products;

/// <summary>
/// Represents the request to retrieve a catalog product by identifier.
/// </summary>
/// <param name="ProductId">The product identifier.</param>
public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDetailsDto?>;