namespace ShopFlow.BuildingBlocks.Guards;

public static class Guard
{
    public static string AgainstNullOrWhiteSpace(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} é obrigatório.", name);
        }

        return value.Trim();
    }

    public static T AgainstNull<T>(T? value, string name)
        where T : class
    {
        return value ?? throw new ArgumentNullException(name);
    }

    public static int AgainstNegative(int value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} não pode ser negativo.");
        }

        return value;
    }

    public static int AgainstZeroOrNegative(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} deve ser maior que zero.");
        }

        return value;
    }

    public static decimal AgainstNegative(decimal value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} não pode ser negativo.");
        }

        return value;
    }
}
