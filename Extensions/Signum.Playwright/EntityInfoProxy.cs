namespace Signum.Playwright;

/// <summary>
/// Represents entity information parsed from data attributes
/// </summary>
public class EntityInfoProxy
{
    public string TypeName { get; set; }
    public PrimaryKey? Id { get; set; }
    public string? ToStringValue { get; set; }
    public bool IsNew { get; set; }

    public EntityInfoProxy(string typeName, PrimaryKey? id, string? toString = null, bool isNew = false)
    {
        TypeName = typeName;
        Id = id;
        ToStringValue = toString;
        IsNew = isNew;
    }

    public static EntityInfoProxy? Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Format: TypeName;Id;ToString or TypeName;(new);ToString
        var parts = value.Split(new[] { ';' }, StringSplitOptions.None);
        
        if (parts.Length < 2)
            return null;

        var typeName = parts[0];
        var isNew = parts[1] == "(new)";
        PrimaryKey? id = null;

        if (!isNew && !string.IsNullOrEmpty(parts[1]))
        {
            var type = TypeLogic.NameToType.TryGetC(typeName);
            if (type != null)
            {
                id = PrimaryKey.Parse(parts[1], type);
            }
        }

        var toStringValue = parts.Length > 2 ? string.Join(";", parts.Skip(2)) : null;

        return new EntityInfoProxy(typeName, id, toStringValue, isNew);
    }

    public Lite<Entity> ToLite()
    {
        if (IsNew || !Id.HasValue)
            throw new InvalidOperationException("Cannot create Lite from new entity");

        var type = TypeLogic.NameToType.TryGetC(TypeName);
        if (type == null)
            throw new InvalidOperationException($"Type {TypeName} not found in TypeLogic");

        return Lite.Create(type, Id.Value, ToStringValue);
    }

    public Lite<T> ToLite<T>() where T : Entity
    {
        return (Lite<T>)(object)ToLite();
    }

    public string ToJsString()
    {
        if (IsNew)
            return $"{TypeName};(new);{ToStringValue}";

        return $"{TypeName};{Id};{ToStringValue}";
    }

    public override string ToString()
    {
        return ToJsString();
    }
}
