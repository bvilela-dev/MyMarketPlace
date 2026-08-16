using Identity.Application.Models;
using MediatR;

namespace Identity.Application.Users;

/// <summary>
/// Consulta o perfil de um usuario.
/// </summary>
/// <param name="UserId">Identificador do usuario.</param>
public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDto?>;
