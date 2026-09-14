using ShopFlow.BuildingBlocks.Domain;
using ShopFlow.Domain.Exceptions;

namespace ShopFlow.Domain.Shared;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }

    public string Currency { get; }

    private Money()
    {
        Currency = "BRL";
    }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Of(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
        {
            throw new DomainException("Money.Invalid", "Valor monetário não pode ser negativo.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new DomainException("Money.Currency", "Moeda deve ter 3 caracteres (ex.: BRL).");
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), currency.Trim().ToUpperInvariant());
    }

    public static Money Zero(string currency = "BRL") => Of(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return Of(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return Of(Amount - other.Amount, Currency);
    }

    public Money Multiply(int quantity)
    {
        if (quantity < 0)
        {
            throw new DomainException("Money.Quantity", "Quantidade não pode ser negativa.");
        }

        return Of(Amount * quantity, Currency);
    }

    public Money Percentage(decimal percent)
    {
        if (percent < 0)
        {
            throw new DomainException("Money.Percent", "Percentual não pode ser negativo.");
        }

        return Of(Amount * percent / 100m, Currency);
    }

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public override string ToString() => $"{Currency} {Amount:N2}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new DomainException("Money.CurrencyMismatch", $"Moedas diferentes: {Currency} e {other.Currency}.");
        }
    }
}
