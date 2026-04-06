using Identity.API.Grpc;
using Marketplace.Contracts.Grpc;
using Order.Application.Abstractions;

namespace Order.Infrastructure.Grpc;

public sealed class IdentityGrpcClient(UserValidation.UserValidationClient client) : IIdentityGrpcClient
{
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