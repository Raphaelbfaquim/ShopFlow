using ShopFlow.BuildingBlocks.Domain;
using ShopFlow.BuildingBlocks.Guards;

namespace ShopFlow.Domain.Shared;

public sealed class Address : ValueObject
{
    public string Street { get; }

    public string Number { get; }

    public string City { get; }

    public string State { get; }

    public string ZipCode { get; }

    public string Country { get; }

    private Address()
    {
        Street = string.Empty;
        Number = string.Empty;
        City = string.Empty;
        State = string.Empty;
        ZipCode = string.Empty;
        Country = "BR";
    }

    private Address(string street, string number, string city, string state, string zipCode, string country)
    {
        Street = street;
        Number = number;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    public static Address Create(
        string street,
        string number,
        string city,
        string state,
        string zipCode,
        string country = "BR")
    {
        return new Address(
            Guard.AgainstNullOrWhiteSpace(street, nameof(street)),
            Guard.AgainstNullOrWhiteSpace(number, nameof(number)),
            Guard.AgainstNullOrWhiteSpace(city, nameof(city)),
            Guard.AgainstNullOrWhiteSpace(state, nameof(state)),
            Guard.AgainstNullOrWhiteSpace(zipCode, nameof(zipCode)),
            Guard.AgainstNullOrWhiteSpace(country, nameof(country)));
    }

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
