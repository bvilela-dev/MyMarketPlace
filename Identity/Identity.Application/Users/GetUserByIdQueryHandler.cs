using Identity.Application.Abstractions;
using Identity.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Users;

/// <summary>
/// Retrieves user profiles by identifier.
/// </summary>
/// <param name="dbContext">The Identity persistence abstraction.</param>
public sealed class GetUserByIdQueryHandler(IIdentityDbContext dbContext) : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    /// <inheritdoc />
    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(candidate => candidate.Addresses)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken);

        return user is null
            ? null
            : new UserDto(
                user.Id,
                user.Name,
                user.Email,
                user.CreatedAt,
                user.Addresses.Select(address => new AddressDto(address.Id, address.Street, address.Number, address.City, address.State, address.ZipCode, address.Country)).ToArray());
    }
}