using ShopFlow.BuildingBlocks.Domain;
using ShopFlow.Domain.Exceptions;

namespace ShopFlow.Domain.Shared;

public sealed class Sku : ValueObject
{
    public string Value { get; }

    private Sku()
    {
        Value = string.Empty;
    }

    private Sku(string value)
    {
        Value = value;
    }

    public static Sku Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length is < 3 or > 32)
        {
            throw new DomainException("Sku.Invalid", "SKU deve ter entre 3 e 32 caracteres.");
        }

        return new Sku(value.Trim().ToUpperInvariant());
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
