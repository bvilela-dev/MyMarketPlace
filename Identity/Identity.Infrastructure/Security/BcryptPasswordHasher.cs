using Identity.Application.Abstractions;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Implements password hashing using BCrypt.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password using BCrypt.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <returns>The generated hash.</returns>
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    /// <summary>
    /// Verifies a plain-text password against a BCrypt hash.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <param name="passwordHash">The stored BCrypt hash.</param>
    /// <returns><see langword="true"/> when the password matches; otherwise, <see langword="false"/>.</returns>
    public bool Verify(string password, string passwordHash) => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}