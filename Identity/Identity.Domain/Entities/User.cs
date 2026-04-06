using Identity.Domain.Events;
using Marketplace.SharedKernel.Abstractions;

namespace Identity.Domain.Entities;

public sealed class User : AggregateRoot
{
    private readonly List<Address> _addresses = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private User()
    {
    }

    public User(Guid id, string name, string email, string passwordHash)
    {
        Id = id;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;

        Raise(new UserCreatedDomainEvent(id, name, email, CreatedAt));
    }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public Address AddAddress(string street, string number, string city, string state, string zipCode, string country)
    {
        var address = new Address(Guid.NewGuid(), Id, street, number, city, state, zipCode, country);
        _addresses.Add(address);
        return address;
    }

    public RefreshToken AddRefreshToken(string token, DateTime expiresAt)
    {
        var refreshToken = new RefreshToken(Guid.NewGuid(), Id, token, expiresAt);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }
}