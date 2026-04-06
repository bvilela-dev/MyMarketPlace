using Marketplace.Contracts.Grpc;

namespace Order.Application.Abstractions;

/// <summary>
/// Defines Identity gRPC operations required by the Order service.
/// </summary>
public interface IIdentityGrpcClient
{
    /// <summary>
    /// Validates that the specified user and address identifiers are valid.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="addressId">The address identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<UserAddressValidationDto> ValidateUserAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
}