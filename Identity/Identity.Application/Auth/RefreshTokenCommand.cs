using MediatR;

namespace Identity.Application.Auth;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;