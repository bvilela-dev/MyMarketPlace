using Identity.Domain.Events;
using Marketplace.SharedKernel.Abstractions;

namespace Identity.Domain.Entities;

/// <summary>
/// Represents a marketplace user aggregate.
/// </summary>
public sealed class User : AggregateRoot
{
    private readonly List<Address> _addresses = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private User()
    {
    }

    /// <summary>
    /// Initializes a new user aggregate.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="name">The user display name.</param>
    /// <param name="email">The user email address.</param>
    /// <param name="passwordHash">The password hash.</param>
    public User(Guid id, string name, string email, string passwordHash)
    {
        Id = id;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;

        Raise(new UserCreatedDomainEvent(id, name, email, CreatedAt));
    }

    /// <summary>
    /// Gets the user display name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the unique email address.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the stored password hash.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the user addresses.
    /// </summary>
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    /// <summary>
    /// Gets the issued refresh tokens.
    /// </summary>
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    /// <summary>
    /// Adds a new address to the user.
    /// </summary>
    /// <returns>The created address entity.</returns>
    public Address AddAddress(string street, string number, string city, string state, string zipCode, string country)
    {
        var address = new Address(Guid.NewGuid(), Id, street, number, city, state, zipCode, country);
        _addresses.Add(address);
        return address;
    }

    /// <summary>
    /// Adds a refresh token to the user.
    /// </summary>
    /// <param name="token">The refresh token value.</param>
    /// <param name="expiresAt">The UTC expiration timestamp.</param>
    /// <returns>The created refresh token entity.</returns>
    public RefreshToken AddRefreshToken(string token, DateTime expiresAt)
    {
        var refreshToken = new RefreshToken(Guid.NewGuid(), Id, token, expiresAt);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }
}