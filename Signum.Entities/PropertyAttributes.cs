
namespace Signum.Entities;

[AttributeUsage(AttributeTargets.Property)]
public sealed class QueryablePropertyAttribute : Attribute
{
    public bool AvailableForQueries { get; set; } = true;

    public QueryablePropertyAttribute()
    {
    }

    public QueryablePropertyAttribute(bool availableForQueries)
    {
        this.AvailableForQueries = availableForQueries;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class HiddenPropertyAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Property)]
public class UnitAttribute : Attribute
{
    public static Dictionary<string, Func<string>> UnitTranslations = new Dictionary<string, Func<string>>();

    public static string? GetTranslation(string? unitName)
    {
        if (!unitName.HasText())
            return null;

        if (UnitTranslations.TryGetValue(unitName, out var func))
            return func();

        return unitName;
    }


    public string UnitName { get; private set; }
    public UnitAttribute(string unitName)
    {
        this.UnitName = unitName;
    }
}
