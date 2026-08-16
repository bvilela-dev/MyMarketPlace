namespace Identity.Application.Auth;

/// <summary>
/// Resposta dos endpoints de autenticacao.
/// </summary>
/// <remarks>
/// O cliente deve guardar o <paramref name="AccessToken"/> em memoria e envia-lo no
/// cabecalho <c>Authorization: Bearer</c>. Quando ele expirar (em
/// <paramref name="AccessTokenExpiresAtUtc"/>), usa-se <c>POST /api/auth/refresh</c>
/// com o <paramref name="RefreshToken"/> para obter um novo par, sem novo login.
/// </remarks>
/// <param name="UserId">Identificador do usuario autenticado.</param>
/// <param name="Name">Nome de exibicao.</param>
/// <param name="Email">E-mail normalizado.</param>
/// <param name="AccessToken">JWT de acesso, de vida curta.</param>
/// <param name="AccessTokenExpiresAtUtc">Vencimento (UTC) do access token.</param>
/// <param name="RefreshToken">Token opaco de renovacao, de vida longa.</param>
/// <param name="RefreshTokenExpiresAtUtc">Vencimento (UTC) do refresh token.</param>
public sealed record AuthResponse(
    Guid UserId,
    string Name,
    string Email,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
