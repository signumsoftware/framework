namespace Signum.DynamicQuery;

public class QueryDescription
{
    public object QueryName { get; private set; }
    public List<ColumnDescription> Columns { get; private set; }
    
    public QueryDescription(object queryName, List<ColumnDescription> columns)
    {
        this.QueryName = queryName;
        this.Columns = columns;
    }
}

public class ColumnDescription
{
    public const string Entity = "Entity";
    public string Name { get; internal set; }
    public Type Type { get; internal set; }
    public string? Unit { get; internal set; }
    public string? Format { get; internal set; }
    public Implementations? Implementations { get; internal set; }
    public PropertyRoute[]? PropertyRoutes { get; internal set; }
    public string DisplayName { get; internal set; }

    public ColumnDescription(string name, Type type, string displayName)
    {
        this.Name = name;
        this.Type = type;
        this.DisplayName = displayName;
    }

    public bool IsEntity
    {
        get { return Name == Entity;  }
    }

    public override string ToString()
    {
        return DisplayName!;
    }
}
