namespace Identity.Application.Models;

public sealed record AddressDto(Guid Id, string Street, string Number, string City, string State, string ZipCode, string Country);