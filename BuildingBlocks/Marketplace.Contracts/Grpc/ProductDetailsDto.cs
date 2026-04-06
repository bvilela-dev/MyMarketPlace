namespace Marketplace.Contracts.Grpc;

/// <summary>
/// Represents product details returned by the Catalog service over gRPC.
/// </summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="Name">The product name.</param>
/// <param name="Price">The product current price.</param>
/// <param name="AvailableQuantity">The available inventory quantity.</param>
public sealed record ProductDetailsDto(Guid ProductId, string Name, decimal Price, int AvailableQuantity);