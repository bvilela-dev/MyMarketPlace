namespace Identity.Application.Abstractions;

/// <summary>
/// Defines password hashing operations used by the Identity service.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <returns>The hashed password.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies a plain-text password against a stored hash.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <param name="passwordHash">The stored password hash.</param>
    /// <returns><see langword="true"/> when the password matches the hash; otherwise, <see langword="false"/>.</returns>
    bool Verify(string password, string passwordHash);
}