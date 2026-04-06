namespace Marketplace.Contracts.Grpc;

public sealed record UserAddressValidationDto(
    bool IsValid,
    Guid UserId,
    Guid AddressId,
    string Street,
    string Number,
    string City,
    string State,
    string ZipCode,
    string Country);