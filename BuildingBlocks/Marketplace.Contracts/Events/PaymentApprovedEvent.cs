namespace Marketplace.Contracts.Events;

/// <summary>
/// Notifies consumers that a payment was approved.
/// </summary>
/// <param name="EventId">The unique identifier of the integration event.</param>
/// <param name="OrderId">The approved order identifier.</param>
/// <param name="UserId">The user associated with the payment.</param>
/// <param name="Total">The approved amount.</param>
/// <param name="ApprovedAtUtc">The UTC timestamp of approval.</param>
public sealed record PaymentApprovedEvent(Guid EventId, Guid OrderId, Guid UserId, decimal Total, DateTime ApprovedAtUtc);