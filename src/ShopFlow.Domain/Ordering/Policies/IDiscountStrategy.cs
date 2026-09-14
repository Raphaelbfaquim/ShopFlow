using ShopFlow.Domain.Shared;

namespace ShopFlow.Domain.Ordering.Policies;

public interface IDiscountStrategy
{
    string Name { get; }

    Money Calculate(Money subtotal);
}
