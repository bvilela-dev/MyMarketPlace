namespace Marketplace.SharedKernel.Abstractions;

public interface IDomainEvent
{
    Guid EventId { get; }

    DateTime OccurredOnUtc { get; }
}