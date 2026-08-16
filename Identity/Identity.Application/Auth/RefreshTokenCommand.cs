using MediatR;

namespace Identity.Application.Auth;

/// <summary>
/// Comando de troca de um refresh token por um novo par de tokens.
/// </summary>
/// <param name="RefreshToken">Refresh token recebido na autenticacao anterior.</param>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;
