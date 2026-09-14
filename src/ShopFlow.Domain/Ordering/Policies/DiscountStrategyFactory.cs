using ShopFlow.Domain.Ordering.Policies;

namespace ShopFlow.Domain.Ordering.Policies;

public static class DiscountStrategyFactory
{
    public static IDiscountStrategy Create(string? couponCode)
    {
        return couponCode?.Trim().ToUpperInvariant() switch
        {
            "WELCOME10" => new PercentageDiscountStrategy(10),
            "VIP20" => new PercentageDiscountStrategy(20),
            _ => new NoDiscountStrategy()
        };
    }
}
