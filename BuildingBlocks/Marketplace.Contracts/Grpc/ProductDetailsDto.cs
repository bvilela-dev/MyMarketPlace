namespace Marketplace.Contracts.Grpc;

public sealed record ProductDetailsDto(Guid ProductId, string Name, decimal Price, int AvailableQuantity);