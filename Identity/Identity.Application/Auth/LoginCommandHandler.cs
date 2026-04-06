using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Auth;

/// <summary>
/// Authenticates existing users.
/// </summary>
/// <param name="dbContext">The Identity persistence abstraction.</param>
/// <param name="passwordHasher">The password hashing service.</param>
/// <param name="tokenService">The token generation service.</param>
public sealed class LoginCommandHandler(IIdentityDbContext dbContext, IPasswordHasher passwordHasher, ITokenService tokenService) : IRequestHandler<LoginCommand, AuthResponse>
{
    /// <inheritdoc />
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(candidate => candidate.RefreshTokens)
            .FirstOrDefaultAsync(candidate => candidate.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken)
            ?? throw new InvalidOperationException("Invalid credentials.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid credentials.");
        }

        var tokens = tokenService.Generate(user);
        user.AddRefreshToken(tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.Id, user.Name, user.Email, tokens.AccessToken, tokens.AccessTokenExpiresAtUtc, tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc);
    }
}