using Marketplace.Contracts.Grpc;
using MediatR;

namespace Catalog.Application.Products;

/// <summary>
/// Consulta um produto pelo identificador.
/// </summary>
/// <param name="ProductId">Identificador do produto.</param>
public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDetailsDto?>;
