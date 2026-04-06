using Identity.Application.Models;
using MediatR;

namespace Identity.Application.Users;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDto?>;