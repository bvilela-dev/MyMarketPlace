using MediatR;

namespace Identity.Application.Auth;

public sealed record RegisterUserCommand(string Name, string Email, string Password) : IRequest<AuthResponse>;