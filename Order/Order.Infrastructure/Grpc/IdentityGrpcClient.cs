using Identity.API.Grpc;
using Marketplace.Contracts.Grpc;
using Order.Application.Abstractions;

namespace Order.Infrastructure.Grpc;

/// <summary>
/// Implements Identity gRPC calls for the Order service.
/// </summary>
public sealed class IdentityGrpcClient(UserValidation.UserValidationClient client) : IIdentityGrpcClient
{
    /// <summary>
    /// Validates the relationship between a user and address through the Identity service.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="addressId">The address identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The validation result.</returns>
    public async Task<UserAddressValidationDto> ValidateUserAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default)
    {
        var response = await client.ValidateUserAddressAsync(new ValidateUserAddressRequest
        {
            UserId = userId.ToString(),
            AddressId = addressId.ToString()
        }, cancellationToken: cancellationToken);

        return new UserAddressValidationDto(
            response.IsValid,
            response.IsValid ? Guid.Parse(response.UserId) : userId,
            response.IsValid ? Guid.Parse(response.AddressId) : addressId,
            response.Street,
            response.Number,
            response.City,
            response.State,
            response.ZipCode,
            response.Country);
    }
}