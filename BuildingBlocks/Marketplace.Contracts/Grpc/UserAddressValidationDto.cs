namespace Marketplace.Contracts.Grpc;

/// <summary>
/// Represents the result of validating a user and address pair through gRPC.
/// </summary>
/// <param name="IsValid">Indicates whether the user and address are valid.</param>
/// <param name="UserId">The validated user identifier.</param>
/// <param name="AddressId">The validated address identifier.</param>
/// <param name="Street">The address street.</param>
/// <param name="Number">The address number.</param>
/// <param name="City">The address city.</param>
/// <param name="State">The address state.</param>
/// <param name="ZipCode">The address postal code.</param>
/// <param name="Country">The address country.</param>
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