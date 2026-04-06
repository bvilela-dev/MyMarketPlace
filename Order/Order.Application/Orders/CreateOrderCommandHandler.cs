using System.Text.Json;
using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Persistence;
using MediatR;
using Order.Application.Abstractions;
using Order.Domain.Entities;
using Order.Domain.ValueObjects;

namespace Order.Application.Orders;

/// <summary>
/// Creates orders after validating the shipping address and current product availability.
/// </summary>
/// <param name="dbContext">The order persistence abstraction.</param>
/// <param name="catalogGrpcClient">The Catalog gRPC client.</param>
/// <param name="identityGrpcClient">The Identity gRPC client.</param>
public sealed class CreateOrderCommandHandler(
    IOrderDbContext dbContext,
    ICatalogGrpcClient catalogGrpcClient,
    IIdentityGrpcClient identityGrpcClient) : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    /// <inheritdoc />
    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("Order requires at least one item.");
        }

        var validatedAddress = await identityGrpcClient.ValidateUserAddressAsync(request.UserId, request.AddressId, cancellationToken);
        if (!validatedAddress.IsValid)
        {
            throw new InvalidOperationException("User or address is invalid.");
        }

        var orderItems = new List<OrderItem>(request.Items.Count);
        foreach (var requestedItem in request.Items)
        {
            var product = await catalogGrpcClient.GetProductAsync(requestedItem.ProductId, cancellationToken);
            if (product.AvailableQuantity < requestedItem.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for product {requestedItem.ProductId}.");
            }

            orderItems.Add(new OrderItem(product.ProductId, product.Name, product.Price, requestedItem.Quantity));
        }

        var addressSnapshot = new AddressSnapshot(
            validatedAddress.Street,
            validatedAddress.Number,
            validatedAddress.City,
            validatedAddress.State,
            validatedAddress.ZipCode,
            validatedAddress.Country);

        var order = new Order.Domain.Entities.Order(Guid.NewGuid(), request.UserId, addressSnapshot, orderItems);
        await dbContext.Orders.AddAsync(order, cancellationToken);

        await dbContext.OutboxMessages.AddAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(OrderCreatedEvent).AssemblyQualifiedName ?? typeof(OrderCreatedEvent).FullName ?? nameof(OrderCreatedEvent),
            Payload = JsonSerializer.Serialize(new OrderCreatedEvent(
                Guid.NewGuid(),
                order.Id,
                order.UserId,
                order.Total,
                "USD",
                new AddressSnapshotDto(addressSnapshot.Street, addressSnapshot.Number, addressSnapshot.City, addressSnapshot.State, addressSnapshot.ZipCode, addressSnapshot.Country),
                order.Items.Select(item => new OrderItemDto(item.ProductId, item.Name, item.UnitPrice, item.Quantity)).ToArray(),
                order.CreatedAtUtc)),
            OccurredOnUtc = DateTime.UtcNow
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CreateOrderResponse(order.Id, order.UserId, order.Total, order.Status);
    }
}