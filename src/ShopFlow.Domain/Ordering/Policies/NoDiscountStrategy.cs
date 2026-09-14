using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Ordering.Policies;

public sealed class NoDiscountStrategy : IDiscountStrategy
{
    public string Name => "None";

    public Money Calculate(Money subtotal) => Money.Zero(subtotal.Currency);
}
