namespace Marketplace.Contracts.Events;

public sealed record AddressSnapshotDto(string Street, string Number, string City, string State, string ZipCode, string Country);