namespace Marketplace.Contracts.Events;

/// <summary>
/// Notifies consumers that a payment attempt failed.
/// </summary>
/// <param name="EventId">The unique identifier of the integration event.</param>
/// <param name="OrderId">The order identifier.</param>
/// <param name="UserId">The user associated with the payment attempt.</param>
/// <param name="Reason">The failure reason.</param>
/// <param name="FailedAtUtc">The UTC timestamp of failure.</param>
public sealed record PaymentFailedEvent(Guid EventId, Guid OrderId, Guid UserId, string Reason, DateTime FailedAtUtc);