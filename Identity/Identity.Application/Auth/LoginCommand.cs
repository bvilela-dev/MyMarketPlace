using MediatR;

namespace Identity.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;