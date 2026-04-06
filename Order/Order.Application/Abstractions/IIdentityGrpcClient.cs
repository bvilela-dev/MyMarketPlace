using Marketplace.Contracts.Grpc;

namespace Order.Application.Abstractions;

public interface IIdentityGrpcClient
{
    Task<UserAddressValidationDto> ValidateUserAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
}