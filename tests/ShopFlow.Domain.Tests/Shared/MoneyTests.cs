using FluentAssertions;
using ShopFlow.Domain.Exceptions;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Tests.Shared;

public class MoneyTests
{
    [Fact]
    public void Of_Should_Round_To_Two_Decimals()
    {
        var money = Money.Of(10.456m);

        money.Amount.Should().Be(10.46m);
        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Of_Should_Reject_Negative_Amount()
    {
        var act = () => Money.Of(-1);

        act.Should().Throw<DomainException>().Where(error => error.Code == "Money.Invalid");
    }

    [Fact]
    public void Add_Should_Sum_Same_Currency()
    {
        var total = Money.Of(10) + Money.Of(5.5m);

        total.Amount.Should().Be(15.5m);
    }

    [Fact]
    public void Add_Should_Reject_Different_Currencies()
    {
        var act = () => Money.Of(10, "BRL").Add(Money.Of(1, "USD"));

        act.Should().Throw<DomainException>().Where(error => error.Code == "Money.CurrencyMismatch");
    }

    [Fact]
    public void Percentage_Should_Calculate_Discount()
    {
        var discount = Money.Of(200).Percentage(10);

        discount.Amount.Should().Be(20m);
    }
}
