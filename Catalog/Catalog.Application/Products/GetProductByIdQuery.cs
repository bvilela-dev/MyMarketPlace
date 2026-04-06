using Marketplace.Contracts.Grpc;
using MediatR;

namespace Catalog.Application.Products;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDetailsDto?>;