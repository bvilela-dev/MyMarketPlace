using Identity.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Auth;

/// <summary>
/// Refreshes expired access tokens using a valid refresh token.
/// </summary>
/// <param name="dbContext">The Identity persistence abstraction.</param>
/// <param name="tokenService">The token generation service.</param>
public sealed class RefreshTokenCommandHandler(IIdentityDbContext dbContext, ITokenService tokenService) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    /// <inheritdoc />
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(candidate => candidate.RefreshTokens)
            .FirstOrDefaultAsync(candidate => candidate.RefreshTokens.Any(token => token.Token == request.RefreshToken), cancellationToken)
            ?? throw new InvalidOperationException("Refresh token is invalid.");

        var currentToken = user.RefreshTokens.First(token => token.Token == request.RefreshToken);
        if (currentToken.IsRevoked || currentToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Refresh token is expired or revoked.");
        }

        currentToken.Revoke();

        var nextTokens = tokenService.Generate(user);
        user.AddRefreshToken(nextTokens.RefreshToken, nextTokens.RefreshTokenExpiresAtUtc);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.Id, user.Name, user.Email, nextTokens.AccessToken, nextTokens.AccessTokenExpiresAtUtc, nextTokens.RefreshToken, nextTokens.RefreshTokenExpiresAtUtc);
    }
}