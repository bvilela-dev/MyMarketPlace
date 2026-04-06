using Identity.Application.Models;
using MediatR;

namespace Identity.Application.Users;

/// <summary>
/// Represents the request to retrieve a user by identifier.
/// </summary>
/// <param name="UserId">The user identifier.</param>
public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDto?>;