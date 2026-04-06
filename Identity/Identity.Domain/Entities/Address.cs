namespace Identity.Domain.Entities;

/// <summary>
/// Represents a user address in the Identity domain.
/// </summary>
public sealed class Address
{
    private Address()
    {
    }

    /// <summary>
    /// Initializes a new user address.
    /// </summary>
    /// <param name="id">The address identifier.</param>
    /// <param name="userId">The owning user identifier.</param>
    /// <param name="street">The street name.</param>
    /// <param name="number">The street number.</param>
    /// <param name="city">The city name.</param>
    /// <param name="state">The state or province.</param>
    /// <param name="zipCode">The postal code.</param>
    /// <param name="country">The country name.</param>
    public Address(Guid id, Guid userId, string street, string number, string city, string state, string zipCode, string country)
    {
        Id = id;
        UserId = userId;
        Street = street;
        Number = number;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    /// <summary>
    /// Gets the address identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the owning user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the street name.
    /// </summary>
    public string Street { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the street number.
    /// </summary>
    public string Number { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the city name.
    /// </summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the state or province.
    /// </summary>
    public string State { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the postal code.
    /// </summary>
    public string ZipCode { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the country name.
    /// </summary>
    public string Country { get; private set; } = string.Empty;
}