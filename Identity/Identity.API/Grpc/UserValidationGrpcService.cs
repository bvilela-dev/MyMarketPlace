using Grpc.Core;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Grpc;

/// <summary>
/// Provides gRPC validation of user and address data for downstream services.
/// </summary>
public sealed class UserValidationGrpcService(IdentityDbContext dbContext) : UserValidation.UserValidationBase
{
    /// <summary>
    /// Validates that the specified user and address identifiers belong together.
    /// </summary>
    /// <param name="request">The validation request payload.</param>
    /// <param name="context">The gRPC server call context.</param>
    /// <returns>A response describing whether the user and address are valid.</returns>
    public override async Task<ValidateUserAddressResponse> ValidateUserAddress(ValidateUserAddressRequest request, ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var addressId = Guid.Parse(request.AddressId);

        var user = await dbContext.Users
            .Include(candidate => candidate.Addresses)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, context.CancellationToken);

        var address = user?.Addresses.FirstOrDefault(candidate => candidate.Id == addressId);
        if (user is null || address is null)
        {
            return new ValidateUserAddressResponse { IsValid = false };
        }

        return new ValidateUserAddressResponse
        {
            IsValid = true,
            UserId = user.Id.ToString(),
            AddressId = address.Id.ToString(),
            Street = address.Street,
            Number = address.Number,
            City = address.City,
            State = address.State,
            ZipCode = address.ZipCode,
            Country = address.Country
        };
    }
}