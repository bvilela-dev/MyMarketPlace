using Marketplace.SharedKernel.Abstractions;

namespace Order.Domain.ValueObjects;

/// <summary>
/// Represents the immutable shipping address stored with an order.
/// </summary>
public sealed class AddressSnapshot : ValueObject
{
    private AddressSnapshot()
    {
    }

    /// <summary>
    /// Initializes a new address snapshot.
    /// </summary>
    /// <param name="street">The street name.</param>
    /// <param name="number">The street number.</param>
    /// <param name="city">The city name.</param>
    /// <param name="state">The state or province.</param>
    /// <param name="zipCode">The postal code.</param>
    /// <param name="country">The country name.</param>
    public AddressSnapshot(string street, string number, string city, string state, string zipCode, string country)
    {
        Street = street;
        Number = number;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

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

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return Number;
        yield return City;
        yield return State;
        yield return ZipCode;
        yield return Country;
    }
}