using System.Text.RegularExpressions;
using ShopFlow.BuildingBlocks.Domain;
using ShopFlow.Domain.Exceptions;

namespace ShopFlow.Domain.Shared;

public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email()
    {
        Value = string.Empty;
    }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Email.Empty", "E-mail é obrigatório.");
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!EmailRegex().IsMatch(normalized))
        {
            throw new DomainException("Email.Invalid", "E-mail inválido.");
        }

        return new Email(normalized);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
