namespace Identity.Application.Models;

/// <summary>
/// Par de tokens produzido pelo servico de autenticacao.
/// </summary>
/// <param name="AccessToken">JWT de acesso (texto puro, entregue ao cliente).</param>
/// <param name="AccessTokenExpiresAtUtc">Vencimento (UTC) do access token.</param>
/// <param name="RefreshToken">
/// Refresh token em texto puro. E o unico momento em que este valor existe fora do
/// cliente — o banco armazena apenas o seu hash.
/// </param>
/// <param name="RefreshTokenExpiresAtUtc">Vencimento (UTC) do refresh token.</param>
public sealed record TokenPair(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);
