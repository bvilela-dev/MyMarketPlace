namespace Marketplace.Contracts.Events;

/// <summary>
/// Represents an order item payload exchanged through integration events.
/// </summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="Name">The product name.</param>
/// <param name="UnitPrice">The product unit price.</param>
/// <param name="Quantity">The ordered quantity.</param>
public sealed record OrderItemDto(Guid ProductId, string Name, decimal UnitPrice, int Quantity);