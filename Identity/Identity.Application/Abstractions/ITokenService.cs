using Identity.Application.Models;
using Identity.Domain.Entities;

namespace Identity.Application.Abstractions;

/// <summary>
/// Defines token generation operations for the Identity service.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token and refresh token pair for the specified user.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>The generated token pair.</returns>
    TokenPair Generate(User user);

    /// <summary>
    /// Generates a cryptographically strong refresh token string.
    /// </summary>
    /// <returns>A new refresh token value.</returns>
    string GenerateRefreshToken();
}