using Marketplace.SharedKernel.Abstractions;

namespace Order.Domain.ValueObjects;

public sealed class AddressSnapshot : ValueObject
{
    private AddressSnapshot()
    {
    }

    public AddressSnapshot(string street, string number, string city, string state, string zipCode, string country)
    {
        Street = street;
        Number = number;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    public string Street { get; private set; } = string.Empty;

    public string Number { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    public string ZipCode { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

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