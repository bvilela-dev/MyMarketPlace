namespace Marketplace.Contracts.Events;

/// <summary>
/// Represents an immutable address snapshot exchanged between services.
/// </summary>
/// <param name="Street">The street name.</param>
/// <param name="Number">The street number.</param>
/// <param name="City">The city name.</param>
/// <param name="State">The state or province.</param>
/// <param name="ZipCode">The postal code.</param>
/// <param name="Country">The country name.</param>
public sealed record AddressSnapshotDto(string Street, string Number, string City, string State, string ZipCode, string Country);