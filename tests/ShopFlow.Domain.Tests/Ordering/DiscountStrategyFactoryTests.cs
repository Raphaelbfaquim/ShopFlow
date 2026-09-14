using FluentAssertions;
using ShopFlow.Domain.Ordering.Policies;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Tests.Ordering;

public class DiscountStrategyFactoryTests
{
    [Theory]
    [InlineData(null, "None", 0)]
    [InlineData("welcome10", "Percent-10", 10)]
    [InlineData("VIP20", "Percent-20", 20)]
    public void Factory_Should_Return_Expected_Strategy(string? coupon, string name, decimal expectedDiscount)
    {
        var strategy = DiscountStrategyFactory.Create(coupon);

        strategy.Name.Should().Be(name);
        strategy.Calculate(Money.Of(100)).Amount.Should().Be(expectedDiscount);
    }
}
