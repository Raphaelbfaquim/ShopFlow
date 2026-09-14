namespace ShopFlow.BuildingBlocks.Domain;

public abstract class Enumeration : IComparable
{
    public int Id { get; }

    public string Name { get; }

    protected Enumeration(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public override string ToString() => Name;

    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration other)
        {
            return false;
        }

        return GetType() == other.GetType() && Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public int CompareTo(object? obj)
    {
        if (obj is not Enumeration other)
        {
            return 1;
        }

        return Id.CompareTo(other.Id);
    }

    public static IEnumerable<T> GetAll<T>()
        where T : Enumeration
    {
        return typeof(T)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
            .Select(field => field.GetValue(null))
            .Cast<T>();
    }

    public static T FromId<T>(int id)
        where T : Enumeration
    {
        return GetAll<T>().Single(item => item.Id == id);
    }
}
