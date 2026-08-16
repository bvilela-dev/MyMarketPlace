using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity.Application.Abstractions;
using Identity.Application.Models;
using Identity.Domain.Entities;
using Marketplace.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Geracao de access tokens (JWT) e refresh tokens.
/// </summary>
/// <remarks>
/// <para>
/// <b>Anatomia do JWT gerado</b> — tres partes separadas por ponto, em Base64Url:
/// </para>
/// <code>
/// header.payload.signature
///   header    → algoritmo (HS256) e tipo
///   payload   → as claims (sub, email, name, exp, iss, aud, jti)
///   signature → HMAC-SHA256(header + payload, segredo)
/// </code>
/// <para>
/// <b>O payload NAO e criptografado</b>, apenas assinado e codificado. Qualquer um cola
/// o token em jwt.io e le o conteudo. A assinatura garante <i>integridade</i> (ninguem
/// altera sem invalidar), nunca <i>sigilo</i> — por isso jamais se coloca dado sensivel
/// dentro de um JWT.
/// </para>
/// </remarks>
/// <param name="options">Configuracao do JWT (emissor, audiencia, segredo, validades).</param>
public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    /// <summary>
    /// Gera o par de tokens de um usuario autenticado.
    /// </summary>
    /// <param name="user">Usuario autenticado.</param>
    /// <returns>Par de tokens com os respectivos vencimentos.</returns>
    public TokenPair Generate(User user)
    {
        var utcNow = DateTime.UtcNow;
        var accessTokenExpiresAt = utcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var refreshTokenExpiresAt = utcNow.AddDays(_options.RefreshTokenLifetimeDays);

        var claims = new[]
        {
            // "sub" (subject) e a claim padrao para o identificador do usuario.
            // E dela que ICurrentUser extrai o UserId em todos os servicos.
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            // "jti" (JWT ID) da identidade unica ao token, permitindo uma futura
            // blocklist de tokens revogados antes do vencimento.
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("name", user.Name)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: utcNow,
            expires: accessTokenExpiresAt,
            signingCredentials: credentials);

        return new TokenPair(
            new JwtSecurityTokenHandler().WriteToken(token),
            accessTokenExpiresAt,
            GenerateRefreshToken(),
            refreshTokenExpiresAt);
    }

    /// <inheritdoc />
    public string HashRefreshToken(string refreshToken)
    {
        // SHA-256 puro basta aqui — e seria errado usar BCrypt. O refresh token tem 512
        // bits de entropia real, entao nao existe ataque de dicionario a defender; o
        // custo alto do BCrypt so tornaria cada renovacao de sessao mais lenta.
        // Para SENHA a conclusao e a oposta: entropia baixa exige hash lento.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Gera um refresh token aleatorio de 64 bytes.
    /// </summary>
    /// <remarks>
    /// Usa <see cref="RandomNumberGenerator"/> (gerador criptografico), nunca
    /// <see cref="Random"/>. O <c>Random</c> e deterministico a partir da semente:
    /// conhecendo alguns valores, um atacante consegue prever os proximos tokens.
    /// </remarks>
    /// <returns>Token em Base64.</returns>
    private static string GenerateRefreshToken()
    {
        Span<byte> buffer = stackalloc byte[64];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer);
    }
}
