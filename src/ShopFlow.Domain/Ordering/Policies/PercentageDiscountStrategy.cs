using ShopFlow.Domain.Exceptions;
using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Ordering.Policies;

public sealed class PercentageDiscountStrategy : IDiscountStrategy
{
    public string Name => $"Percent-{Percent}";

    public decimal Percent { get; }

    public PercentageDiscountStrategy(decimal percent)
    {
        if (percent is <= 0 or > 50)
        {
            throw new DomainException("Discount.Percent", "Desconto percentual deve estar entre 0 e 50.");
        }

        Percent = percent;
    }

    public Money Calculate(Money subtotal) => subtotal.Percentage(Percent);
}
