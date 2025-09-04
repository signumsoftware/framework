using System.Data;
using NpgsqlTypes;

namespace Signum.Entities;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class IndexAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class UniqueIndexAttribute : Attribute
{
    public bool AllowMultipleNulls { get; set; }

    public bool AvoidAttachToUniqueIndexes { get; set; }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AttachToUniqueIndexesAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class AssemblySchemaNameAttribute : Attribute
{
    public static Dictionary<Type, Assembly> OverridenAssembly = new Dictionary<Type, Assembly>();

    public string SchemaName { get; private set; }

    public bool AvoidIdiomatic { get; set; }

    public string? ForNamespace { get; set; }

    public AssemblySchemaNameAttribute(string schemaName)
    {
        this.SchemaName = schemaName;
    }
}

public struct Implementations : IEquatable<Implementations>
{
    object? arrayOrType;

    public bool IsByAll { get { return arrayOrType == null; } }
    public IEnumerable<Type> Types
    {
        get
        {
            if (arrayOrType == null)
                throw new InvalidOperationException("ImplementedByAll");

            return Enumerate();
        }
    }

    private IEnumerable<Type> Enumerate()
    {
        if (arrayOrType is Type t)
        {
            yield return t;
        }
        else if (arrayOrType is Type[] ts)
        {
            foreach (var item in ts)
                yield return item;
        }
        else
            throw new InvalidOperationException("IsByAll");
    }

    public static Implementations? TryFromAttributes(Type t, PropertyRoute route, ImplementedByAttribute? ib, ImplementedByAllAttribute? iba)
    {
        if (ib != null && iba != null)
            throw new NotSupportedException("Route {0} contains both {1} and {2}".FormatWith(route, ib.GetType().Name, iba.GetType().Name));

        if (ib != null) return Implementations.By(ib.ImplementedTypes);
        if (iba != null) return Implementations.ByAll;

        if (Error(t) == null)
            return Implementations.By(t);

        return null;
    }


    public static Implementations FromAttributes(Type t, PropertyRoute route, ImplementedByAttribute? ib, ImplementedByAllAttribute? iba)
    {
        Implementations? imp = TryFromAttributes(t, route, ib, iba);

        if (imp == null)
        {
            var message = Error(t) + @". Set implementations for {0}.".FormatWith(route);

            if (t.IsInterface || t.IsAbstract)
            {
                message += "\n" + ConsiderMessage(route, "typeof(YourConcrete" + t.TypeName() + ")");
            }

            throw new InvalidOperationException(message);
        }

        return imp.Value;
    }

    internal static string ConsiderMessage(PropertyRoute route, string targetTypes)
    {
        return $@"Consider writing something like this in your Starter class:

sb.Schema.Settings.FieldAttributes(({route.RootType.TypeName()} a) => a.{route.PropertyString().Replace("/", ".First().")}).Replace(new ImplementedByAttribute({targetTypes}))

";
    }

    public static Implementations ByAll { get { return new Implementations(); } }

    public static Implementations By(Type type)
    {
        var error = Error(type);

        if (error.HasText())
            throw new InvalidOperationException(error);

        return new Implementations { arrayOrType = type };
    }

    public static Implementations By(params Type[] types)
    {
        if (types == null || types.Length == 0)
            return new Implementations { arrayOrType = types ?? Array.Empty<Type>() };

        if (types.Length == 1)
            return By(types[0]);

        var error = types.Select(Error).NotNull().ToString("\n");

        if (error.HasText())
            throw new InvalidOperationException(error);

        return new Implementations { arrayOrType = types.ToArray() };
    }

    static string? Error(Type type)
    {
        if(type.IsLite())
            return "{0} is a Lite".FormatWith(type.CleanType().TypeName());

        if (type.IsInterface)
            return "{0} is an interface".FormatWith(type.Name);

        if (type.IsAbstract)
            return "{0} is abstract".FormatWith(type.Name);

        if (!type.IsEntity())
            return "{0} is not {1}".FormatWith(type.Name, typeof(Entity).Name);

        return null;
    }

    public string Key()
    {
        if (IsByAll)
            return "[ALL]";

        return Types.ToString(TypeLogic.GetCleanName, ", ");
    }


    public override string ToString()
    {
        if (IsByAll)
            return "ImplementedByAll";

        return "ImplementedBy({0})".FormatWith(Types.ToString(t => t.Name, ", "));
    }

    public override bool Equals(object? obj)
    {
        return obj is Implementations imp && Equals(imp);
    }

    public bool Equals(Implementations other)
    {
        return IsByAll && other.IsByAll ||
            arrayOrType == other.arrayOrType ||
        Enumerable.SequenceEqual(Types.OrderBy(a => a.FullName), other.Types.OrderBy(a => a.FullName));
    }

    public override int GetHashCode()
    {
        return arrayOrType == null ? 0 : Types.Aggregate(0, (acum, type) => acum ^ type.GetHashCode());
    }

    public static bool operator ==(Implementations left, Implementations right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Implementations left, Implementations right)
    {
        return !(left == right);
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ImplementedByAttribute : Attribute
{
    Type[] implementedTypes;

    public Type[] ImplementedTypes
    {
        get { return implementedTypes; }
    }

    public ImplementedByAttribute(params Type[] types)
    {
        implementedTypes = types;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ImplementedByAllAttribute : Attribute
{
    public ImplementedByAllAttribute()
    {
    }
}

/// <summary>
/// Avoids that an Entity field has database representation (column or MList table)
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class IgnoreAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class FieldWithoutPropertyAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ForceNotNullableAttribute : Attribute
{
}


/// <summary>
/// Very rare. Reference types (classes) or Nullable are already nullable in the database.
/// This attribute is only necessary in the case an entity field is not-nullable but you can not make the DB column nullable because of legacy data, or cycles in a graph of entities.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ForceNullableAttribute : Attribute
{
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DbTypeAttribute : Attribute
{
    SqlDbType? sqlDbType;
    public bool HasSqlDbType => sqlDbType.HasValue;
    public SqlDbType SqlDbType
    {
        get { return sqlDbType!.Value; }
        set { sqlDbType = value; }
    }

    NpgsqlDbType? npgsqlDbType;
    public bool HasNpgsqlDbType => npgsqlDbType.HasValue;
    public NpgsqlDbType NpgsqlDbType
    {
        get { return npgsqlDbType!.Value; }
        set { npgsqlDbType = value; }
    }

    int? size;
    public bool HasSize => size.HasValue;
    public int Size
    {
        get { return size!.Value; }
        set { size = value; }
    }

    byte? precision;
    public bool HasPrecision => precision.HasValue;
    public byte Precision
    {
        get { return precision!.Value; }
        set { precision = value; }
    }


    byte? scale;
    public bool HasScale => scale.HasValue;
    public byte Scale
    {
        get { return scale!.Value; }
        set { scale = value; }
    }


    public string? UserDefinedTypeName { get; set; }

    public string? Default { get; set; }

    public string? DefaultSqlServer { get; set; }
    public string? DefaultPostgres { get; set; }

    public string? GetDefault(bool isPostgres)
    {
        return (isPostgres ? DefaultPostgres : DefaultSqlServer) ?? Default;
    }

    public string? Check { get; set; }

    public string? CheckSqlServer { get; set; }
    public string? CheckPostgres { get; set; }

    public string? GetCheck(bool isPostgres)
    {
        return (isPostgres ? CheckPostgres : CheckSqlServer) ?? Check;
    }


    public string? CollationSqlServer { get; set; }
    public string? CollationPostgres { get; set; }

    public string? GetCollation(bool isPostgres)
    {
        return (isPostgres ? CollationPostgres : CollationSqlServer);
    }

    public bool CollationPostgres_AvoidToLower { get; set; }

    public const string SqlServer_NewId = "NEWID()";
    public const string SqlServer_NewSequentialId = "NEWSEQUENTIALID()";
    public const string Postgres_UuidGenerateV1 = "uuid_generate_v1()";

    public DateTimeKind DateTimeKind { get; set; }
}


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property /*MList fields*/, Inherited = true, AllowMultiple = false)]
public sealed class PrimaryKeyAttribute : DbTypeAttribute
{
    public Type Type { get; set; }

    public string? Name { get; set; }

    public bool Identity { get; set; }

    bool identityBehaviour;

    public bool IdentityBehaviour
    {
        get { return identityBehaviour; }
        set
        {
            identityBehaviour = value;
            if (Type == typeof(Guid) && identityBehaviour)
            {
                this.DefaultSqlServer = SqlServer_NewId;
                this.DefaultPostgres = Postgres_UuidGenerateV1;
            }
        }
    }

    public PrimaryKeyAttribute(Type type)
    {
        this.Type = type;
        this.Identity = type != typeof(Guid);
        this.IdentityBehaviour = true;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class ColumnNameAttribute : Attribute
{
    public string Name { get; set; }

    public ColumnNameAttribute(string name)
    {
        this.Name = name;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class BackReferenceColumnNameAttribute : Attribute
{
    public string Name { get; set; }

    public BackReferenceColumnNameAttribute(string name)
    {
        this.Name = name;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ViewPrimaryKeyAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class CacheViewMetadataAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property /*MList fields*/, Inherited = true, AllowMultiple = false)]
public sealed class TableNameAttribute : Attribute
{
    public string Name { get; set; }
    public string? SchemaName { get; set; }
    public string? DatabaseName { get; set; }
    public string? ServerName { get; set; }

    public TableNameAttribute(string fullName)
    {
        var parts = fullName.Split('.');
        this.Name = parts.ElementAt(parts.Length - 1).Trim('[', ']');
        this.SchemaName = parts.ElementAtOrDefault(parts.Length - 2)?.Trim('[', ']');
        this.DatabaseName = parts.ElementAtOrDefault(parts.Length - 3)?.Trim('[', ']');
        this.ServerName = parts.ElementAtOrDefault(parts.Length - 4)?.Trim('[', ']');
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property /*MList fields*/, Inherited = true, AllowMultiple = false)]
public sealed class PartitionColumnAttribute : DbTypeAttribute
{
    public string? Name { get; set; }

    public Type? Type { get; set; }

    public string? SchemeName { get; set; }

    public PartitionColumnAttribute(string? schemeName = null)
    {
        this.SchemeName = schemeName;
    }
}


[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class TicksColumnAttribute : DbTypeAttribute
{
    public bool HasTicks { get; private set; }

    public string? Name { get; set; }

    public Type? Type { get; set; }

    public TicksColumnAttribute(bool hasTicks = true)
    {
        this.HasTicks = hasTicks;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ToStringColumnAttribute : DbTypeAttribute
{
    public string? Name { get; set; }

    public bool AvoidIdiomatic { get; set; }

    public Type? Type { get; set; }

    public bool Nullable { get; set; }

    public ToStringColumnAttribute()
    {
    }
}

/// <summary>
/// Activates SQL Server 2016 Temporal Tables
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property /*MList fields*/, Inherited = true, AllowMultiple = false)]
public sealed class SystemVersionedAttribute : Attribute
{
    public string? TemporalTableName { get; set; }
    public string StartDateColumnName { get; set; } = "SysStartDate";
    public string EndDateColumnName { get; set; } = "SysEndDate";
    public string PostgresSysPeriodColumname { get; set; } = "sys_period";
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class AvoidForeignKeyAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class AvoidExpandQueryAttribute : Attribute
{

}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class CombineStrategyAttribute : Attribute
{
    public readonly CombineStrategy Strategy;

    public CombineStrategyAttribute(CombineStrategy strategy)
    {
        this.Strategy = strategy;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public sealed class LiteModelAttribute : Attribute
{
    public Type LiteModelType { get; private set; }
    public Type? ForEntityType { get; set; }

    public LiteModelAttribute(Type liteModel)
    {
        this.LiteModelType = liteModel;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AutoExpandSubTokensAttribute : Attribute
{
    public bool AutoExpand { get; }

    public AutoExpandSubTokensAttribute(bool autoExpand)
    {
        AutoExpand = autoExpand;
    }
}


public enum CombineStrategy
{
    Union,
    Case,
}

public static class LinqHintEntities
{
    public static T CombineCase<T>(this T value) where T : IEntity
    {
        return value;
    }

    public static T CombineUnion<T>(this T value) where T : IEntity
    {
        return value;
    }
}
