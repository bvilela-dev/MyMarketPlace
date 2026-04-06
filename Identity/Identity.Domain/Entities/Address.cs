namespace Identity.Domain.Entities;

public sealed class Address
{
    private Address()
    {
    }

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

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Street { get; private set; } = string.Empty;

    public string Number { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    public string ZipCode { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;
}