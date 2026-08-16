using Identity.Application.Abstractions;
using Marketplace.SharedKernel.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Auth;

/// <summary>
/// Troca um refresh token valido por um novo par de tokens (rotacao).
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que rotacionar?</b> O refresh token vive semanas. Se fosse reutilizavel a
/// vontade, uma copia vazada valeria como acesso permanente. Com rotacao, cada uso
/// invalida o anterior — e o token roubado deixa de funcionar assim que o dono legitimo
/// renovar a sessao.
/// </para>
/// <para>
/// <b>A busca e feita pelo hash.</b> O banco nunca viu o token em texto puro; o handler
/// calcula o hash do valor apresentado e procura por ele. E o mesmo raciocinio de
/// autenticacao por senha.
/// </para>
/// <para>
/// <b>Evolucao natural (fora do escopo):</b> detectar reuso. Se um token <i>ja revogado</i>
/// e apresentado, isso indica que alguem tem uma copia antiga — a reacao adequada e
/// revogar toda a familia de tokens do usuario. O metodo
/// <c>User.RevokeAllRefreshTokens</c> ja existe justamente para esse gancho.
/// </para>
/// </remarks>
/// <param name="dbContext">Contrato de persistencia do Identity.</param>
/// <param name="tokenService">Servico de geracao e hash de tokens.</param>
public sealed class RefreshTokenCommandHandler(
    IIdentityDbContext dbContext,
    ITokenService tokenService) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    /// <inheritdoc />
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);

        var user = await dbContext.Users
            .Include(candidate => candidate.RefreshTokens)
            .FirstOrDefaultAsync(
                candidate => candidate.RefreshTokens.Any(token => token.TokenHash == tokenHash),
                cancellationToken);

        // Mensagem unica para "token inexistente", "expirado" e "ja revogado": detalhar
        // ajudaria apenas quem esta testando tokens roubados.
        if (user is null)
        {
            throw new AuthenticationFailedException("Refresh token invalido ou expirado.");
        }

        var currentToken = user.RefreshTokens.First(token => token.TokenHash == tokenHash);

        if (!currentToken.IsActive(utcNow))
        {
            throw new AuthenticationFailedException("Refresh token invalido ou expirado.");
        }

        // Rotacao: o token apresentado morre aqui, antes de o novo ser emitido.
        currentToken.Revoke();

        var tokens = tokenService.Generate(user);
        user.AddRefreshToken(tokenService.HashRefreshToken(tokens.RefreshToken), tokens.RefreshTokenExpiresAtUtc);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Name,
            user.Email,
            tokens.AccessToken,
            tokens.AccessTokenExpiresAtUtc,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAtUtc);
    }
}
