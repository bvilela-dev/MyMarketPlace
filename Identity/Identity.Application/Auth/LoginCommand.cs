using MediatR;

namespace Identity.Application.Auth;

/// <summary>
/// Comando de autenticacao de um usuario existente.
/// </summary>
/// <param name="Email">E-mail cadastrado.</param>
/// <param name="Password">Senha em texto puro.</param>
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
