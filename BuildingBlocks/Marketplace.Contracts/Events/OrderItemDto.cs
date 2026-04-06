namespace Marketplace.Contracts.Events;

public sealed record OrderItemDto(Guid ProductId, string Name, decimal UnitPrice, int Quantity);