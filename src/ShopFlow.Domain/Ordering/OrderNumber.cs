using ShopFlow.BuildingBlocks.Domain;
using ShopFlow.Domain.Exceptions;

namespace ShopFlow.Domain.Ordering;

public sealed class OrderNumber : ValueObject
{
    public string Value { get; }

    private OrderNumber()
    {
        Value = string.Empty;
    }

    private OrderNumber(string value)
    {
        Value = value;
    }

    public static OrderNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("OrderNumber.Empty", "Número do pedido é obrigatório.");
        }

        return new OrderNumber(value.Trim().ToUpperInvariant());
    }

    public static OrderNumber New(DateTime utcNow)
    {
        var stamp = utcNow.ToString("yyyyMMddHHmmss");
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return Create($"SF-{stamp}-{suffix}");
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
