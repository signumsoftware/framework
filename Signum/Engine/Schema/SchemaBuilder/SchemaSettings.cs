using System.Data;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using Signum.Utilities.Reflection;
using NpgsqlTypes;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

namespace Signum.Engine.Maps;

public class SchemaSettings
{
    public SchemaSettings()
    {
    }

    public bool IsPostgres { get; set; }
    public bool PostresVersioningFunctionNoChecks { get; set; }

    public PrimaryKeyAttribute DefaultPrimaryKeyAttribute = new PrimaryKeyAttribute(typeof(int), true);

    public Action<Type> AssertNotIncluded = null!;

    public int MaxNumberOfParameters = 2000;
    public int MaxNumberOfStatementsInSaveQueries = 16;

    public HashSet<Type> ImplementedByAllPrimaryKeyTypes = new HashSet<Type> { typeof(int) };
    public int ImplementedByAllStringSize = 40;
    
    public ConcurrentDictionary<PropertyRoute, AttributeCollection?> FieldAttributesCache = new ConcurrentDictionary<PropertyRoute, AttributeCollection?>();
    public ConcurrentDictionary<Type, AttributeCollection> TypeAttributesCache = new ConcurrentDictionary<Type, AttributeCollection>();

    public Dictionary<Type, LambdaExpression> CustomOrder = new Dictionary<Type, LambdaExpression>();

    internal Dictionary<Type, string>? desambiguatedNames;
    public void Desambiguate(Type type, string cleanName)
    {
        if (desambiguatedNames == null)
            desambiguatedNames = new Dictionary<Type, string>();

        desambiguatedNames[type] = cleanName;
    }

    public Dictionary<Type, string> UdtSqlName = new Dictionary<Type, string>()
    {
        //{ typeof(SqlHierarchyId), "HierarchyId"},
        //{ typeof(SqlGeography), "Geography"},
        //{ typeof(SqlGeometry), "Geometry"},
    };

    public Dictionary<Type, AbstractDbType> TypeValues = new Dictionary<Type, AbstractDbType>
    {
        {typeof(bool),           new AbstractDbType(SqlDbType.Bit,              NpgsqlDbType.Boolean)},

        {typeof(byte),           new AbstractDbType(SqlDbType.TinyInt,          NpgsqlDbType.Smallint)},
        {typeof(short),          new AbstractDbType(SqlDbType.SmallInt,         NpgsqlDbType.Smallint)},
        {typeof(int),            new AbstractDbType(SqlDbType.Int,              NpgsqlDbType.Integer)},
        {typeof(long),           new AbstractDbType(SqlDbType.BigInt,           NpgsqlDbType.Bigint)},

        {typeof(float),          new AbstractDbType(SqlDbType.Real,             NpgsqlDbType.Real)},
        {typeof(double),         new AbstractDbType(SqlDbType.Float,            NpgsqlDbType.Double)},
        {typeof(decimal),        new AbstractDbType(SqlDbType.Decimal,          NpgsqlDbType.Numeric)},

        {typeof(char),           new AbstractDbType(SqlDbType.NChar,            NpgsqlDbType.Char)},
        {typeof(string),         new AbstractDbType(SqlDbType.NVarChar,         NpgsqlDbType.Varchar)},
        {typeof(DateOnly),       new AbstractDbType(SqlDbType.Date,             NpgsqlDbType.Date)},
        {typeof(DateTime),       new AbstractDbType(SqlDbType.DateTime2,        NpgsqlDbType.Timestamp)},
        {typeof(DateTimeOffset), new AbstractDbType(SqlDbType.DateTimeOffset,   NpgsqlDbType.Timestamp /*not really*/)},
        {typeof(TimeSpan),       new AbstractDbType(SqlDbType.Time,             NpgsqlDbType.Time)},
        {typeof(TimeOnly),       new AbstractDbType(SqlDbType.Time,             NpgsqlDbType.Time)},

        {typeof(byte[]),         new AbstractDbType(SqlDbType.VarBinary,        NpgsqlDbType.Bytea)},
        {typeof(float[]),        new AbstractDbType(SqlDbType.Vector,           NpgsqlDbType.Array | NpgsqlDbType.Real)},

        {typeof(Guid),           new AbstractDbType(SqlDbType.UniqueIdentifier, NpgsqlDbType.Uuid)},
    };

    readonly Dictionary<SqlDbType, int> defaultSizeSqlServer = new Dictionary<SqlDbType, int>()
    {
        {SqlDbType.NVarChar, 200},
        {SqlDbType.VarChar, 200},
        {SqlDbType.VarBinary, int.MaxValue},
        {SqlDbType.Binary, 8000},
        {SqlDbType.Char, 1},
        {SqlDbType.NChar, 1},
    };

    readonly Dictionary<NpgsqlDbType, int> defaultSizePostgreSql = new Dictionary<NpgsqlDbType, int>()
    {
        {NpgsqlDbType.Varbit, 200},
        {NpgsqlDbType.Varchar, 200},
        {NpgsqlDbType.Char, 1},
    };

    readonly Dictionary<SqlDbType, byte> defaultPrecisionSqlServer = new Dictionary<SqlDbType, byte>()
    {
        {SqlDbType.Decimal, 18},
    };

    readonly Dictionary<NpgsqlDbType, byte> defaultPrecisionPostgreSql = new Dictionary<NpgsqlDbType, byte>()
    {
        {NpgsqlDbType.Numeric, 18},
    };

    readonly Dictionary<SqlDbType, byte> defaultScaleSqlServer = new Dictionary<SqlDbType, byte>()
    {
        {SqlDbType.Decimal, 2},
    };

    readonly Dictionary<NpgsqlDbType, byte> defaultScalePostgreSql = new Dictionary<NpgsqlDbType, byte>()
    {
        {NpgsqlDbType.Numeric, 2},
    };

    public AttributeCollection FieldAttributes<T, S>(Expression<Func<T, S>> propertyRoute)
        where T : IRootEntity
    {
        return FieldAttributes(PropertyRoute.Construct(propertyRoute))!;
    }

    public AttributeCollection? FieldAttributes(PropertyRoute propertyRoute)
    {
        using (HeavyProfiler.LogNoStackTrace("FieldAttributes"))
        {
            return FieldAttributesCache.GetOrAdd(propertyRoute, pr =>
            {
                switch (propertyRoute.PropertyRouteType)
                {
                    case PropertyRouteType.FieldOrProperty:
                        if (propertyRoute.FieldInfo == null)
                            return null;
                        return CreateFieldAttributeCollection(propertyRoute);
                    case PropertyRouteType.MListItems:
                        if (propertyRoute.Parent!.FieldInfo == null)
                            return null;
                        return CreateFieldAttributeCollection(propertyRoute.Parent!);
                    default:
                        throw new InvalidOperationException("Route of type {0} not supported for this method".FormatWith(propertyRoute.PropertyRouteType));
                }
            });
        }
    }

    AttributeCollection CreateFieldAttributeCollection(PropertyRoute route)
    {
        var fieldAttributes = route.FieldInfo!.GetCustomAttributes(false).Cast<Attribute>();
        var fieldAttributesInProperty = route.PropertyInfo == null ? Enumerable.Empty<Attribute>() : route.PropertyInfo.GetCustomAttributes(false).Cast<Attribute>();
        return new AttributeCollection(AttributeTargets.Field, fieldAttributes.Concat(fieldAttributesInProperty).ToList(), () => AssertNotIncluded(route.RootType));
    }

    public AttributeCollection TypeAttributes<T>() where T : Entity
    {
        return TypeAttributes(typeof(T));
    }

    public AttributeCollection TypeAttributes(Type entityType)
    {
        if (!typeof(Entity).IsAssignableFrom(entityType) && !typeof(IView).IsAssignableFrom(entityType))
            throw new InvalidOperationException("{0} is not an Entity or View".FormatWith(entityType.Name));

        if (entityType.IsAbstract)
            throw new InvalidOperationException("{0} is abstract".FormatWith(entityType.Name));

        return TypeAttributesCache.GetOrAdd(entityType, t =>
        {
            var list = entityType.GetCustomAttributes(true).Cast<Attribute>().ToList();

            var enumType = EnumEntity.Extract(entityType);

            if (enumType != null)
                foreach (var at in enumType.GetCustomAttributes(true).Cast<Attribute>().ToList())
                {
                    list.RemoveAll(a => a.GetType() == at.GetType());
                    list.Add(at);
                }

            return new AttributeCollection(AttributeTargets.Class, list, () => AssertNotIncluded(entityType));
        });
    }


    public void AssertNotIgnored<T, S>(Expression<Func<T, S>> propertyRoute, string errorContext, string solution = "by using SchemaBuilderSettings.FieldAttributes to remove IgnoreAttribute") where T : Entity
    {
        var pr = PropertyRoute.Construct<T, S>(propertyRoute);

        if (FieldAttribute<IgnoreAttribute>(pr) != null)
            throw new InvalidOperationException($"In order to {errorContext} you need to override the attributes for {pr} {solution}");
    }

    public void AssertIgnored<T, S>(Expression<Func<T, S>> propertyRoute, string errorContext, string solution = "by using SchemaBuilderSettings.FieldAttributes to add IgnoreAttribute") where T : Entity
    {
        var pr = PropertyRoute.Construct<T, S>(propertyRoute);

        if (FieldAttribute<IgnoreAttribute>(pr) == null)
            throw new InvalidOperationException($"In order to {errorContext} you need to override the attributes for {pr} {solution}");
    }

    public A? FieldAttribute<A>(PropertyRoute propertyRoute) where A : Attribute
    {
        using (HeavyProfiler.LogNoStackTrace("FieldAttribute"))
        {
            if (propertyRoute.PropertyRouteType == PropertyRouteType.Root || propertyRoute.PropertyRouteType == PropertyRouteType.LiteEntity)
                throw new InvalidOperationException("Route of type {0} not supported for this method".FormatWith(propertyRoute.PropertyRouteType));

            return (A?)FieldAttributes(propertyRoute)?.FirstOrDefault(a => a.GetType() == typeof(A));
        }
    }

    static V? ValidatorAttribute<V>(PropertyRoute propertyRoute) where V : ValidatorAttribute
    {
        if (!typeof(ModifiableEntity).IsAssignableFrom(propertyRoute.RootType))
            return null;

        if (propertyRoute.PropertyRouteType == PropertyRouteType.MListItems)
            propertyRoute = propertyRoute.Parent!;

        if (propertyRoute.PropertyInfo == null)
            return null;

        var pp = Validator.TryGetPropertyValidator(propertyRoute);

        if (pp == null)
            return null;

        return pp.Validators.OfType<V>().FirstOrDefault();
    }

    public A? TypeAttribute<A>(Type entityType) where A : Attribute
    {
        return (A?)TypeAttributes(entityType).FirstOrDefault(a => a.GetType() == typeof(A));
    }

    internal IsNullable GetIsNullable(PropertyRoute propertyRoute, bool forceNull)
    {
        var result = GetIsNullablePrivate(propertyRoute);

        if (result == IsNullable.No && forceNull)
            return IsNullable.Forced;

        return result;
    }

    private IsNullable GetIsNullablePrivate(PropertyRoute propertyRoute)
    {
        if (FieldAttribute<ForceNotNullableAttribute>(propertyRoute) != null)
            return IsNullable.No;

        if (FieldAttribute<ForceNullableAttribute>(propertyRoute) != null)
            return IsNullable.Yes;

        if (propertyRoute.PropertyRouteType == PropertyRouteType.MListItems)
            return IsNullable.No;

        if (propertyRoute.Type.IsValueType)
            return propertyRoute.Type.IsNullable() ? IsNullable.Yes : IsNullable.No;

        var nullable = propertyRoute.FieldInfo?.IsNullable();
        if (nullable == true)
            return IsNullable.Yes;

        return IsNullable.No;
    }

    public bool ImplementedBy<T>(Expression<Func<T, object?>> propertyRoute, Type typeToImplement) where T : Entity
    {
        var imp = GetImplementations(propertyRoute);
        return !imp.IsByAll && imp.Types.Contains(typeToImplement);
    }

    public void AssertImplementedBy<T>(Expression<Func<T, object?>> propertyRoute, Type typeToImplement) where T : Entity
    {
        var route = PropertyRoute.Construct(propertyRoute);

        Implementations imp = GetImplementations(route);

        if (imp.IsByAll || !imp.Types.Contains(typeToImplement))
            throw new InvalidOperationException("Route {0} is not ImplementedBy {1}".FormatWith(route, typeToImplement.Name) +
                "\n\n" +
                Implementations.ConsiderMessage(route, imp.Types.And(typeToImplement).ToString(t => $"typeof({t.TypeName()})", ", ")));
    }

    public Implementations GetImplementations<T>(Expression<Func<T, object?>> propertyRoute) where T : Entity
    {
        return GetImplementations(PropertyRoute.Construct(propertyRoute));
    }

    public Implementations GetImplementations(PropertyRoute propertyRoute)
    {
        var cleanType = propertyRoute.Type.CleanType();
        if (!propertyRoute.Type.CleanType().IsIEntity())
            throw new InvalidOperationException("{0} is not a {1}".FormatWith(propertyRoute, typeof(IEntity).Name));

        return Implementations.FromAttributes(cleanType, propertyRoute,
            FieldAttribute<ImplementedByAttribute>(propertyRoute),
            FieldAttribute<ImplementedByAllAttribute>(propertyRoute));
    }

    public AbstractDbType ToAbstractDbType(DbTypeAttribute att)
    {
        if (att.HasNpgsqlDbType && att.HasSqlDbType)
            return new AbstractDbType(att.SqlDbType, att.NpgsqlDbType);

        if (att.HasNpgsqlDbType)
            return new AbstractDbType(att.NpgsqlDbType);

        if (att.HasSqlDbType)
            return new AbstractDbType(att.SqlDbType);

        throw new InvalidOperationException("Not type found in DbTypeAttribute");
    }

    internal DbTypePair GetSqlDbType(DbTypeAttribute? att, Type type)
    {
        if (att != null && (att.HasSqlDbType || att.HasNpgsqlDbType))
            return new DbTypePair(ToAbstractDbType(att), att.UserDefinedTypeName);

        return GetSqlDbTypePair(type.UnNullify());
    }

    internal DbTypePair? TryGetSqlDbType(DbTypeAttribute? att, Type type)
    {
        if (att != null && (att.HasSqlDbType || att.HasNpgsqlDbType))
            return new DbTypePair(ToAbstractDbType(att), att.UserDefinedTypeName);

        return TryGetSqlDbTypePair(type.UnNullify());
    }

    internal int? GetSqlSize(DbTypeAttribute? att, PropertyRoute? route, AbstractDbType dbType)
    {
        if (this.IsPostgres && dbType.PostgreSql == NpgsqlDbType.Bytea)
            return null;

        if (att != null && att.HasSize)
            return att.Size;

        if (route != null && route.Type == typeof(string))
        {
            var sla = ValidatorAttribute<StringLengthValidatorAttribute>(route);
            if (sla != null)
                return sla.Max == -1 ? int.MaxValue : sla.Max;
        }

        if (!this.IsPostgres)
            return defaultSizeSqlServer.TryGetS(dbType.SqlServer);
        else
            return defaultSizePostgreSql.TryGetS(dbType.PostgreSql);
    }

    internal byte? GetSqlPrecision(DbTypeAttribute? att, PropertyRoute? route, AbstractDbType dbType)
    {
        if (this.IsPostgres && dbType.PostgreSql == NpgsqlDbType.Bytea)
            return null;

        if (att != null && att.HasPrecision)
            return att.Precision;

        /*            if (route != null && route.Type == typeof(string))
                    {
                        var sla = ValidatorAttribute<StringLengthValidatorAttribute>(route);
                        if (sla != null)
                            return sla.Max == -1 ? int.MaxValue : sla.Max;
                    }*/

        if (!this.IsPostgres)
            return defaultPrecisionSqlServer.TryGetS(dbType.SqlServer);
        else
            return defaultPrecisionPostgreSql.TryGetS(dbType.PostgreSql);
    }

    internal byte? GetSqlScale(DbTypeAttribute? att, PropertyRoute? route, AbstractDbType dbType)
    {
        bool isDecimal = dbType.IsDecimal();
        if (att != null && att.HasScale)
        {
            if (!isDecimal)
                throw new InvalidOperationException($"{dbType} can not have Scale");

            return att.Scale;
        }

        if (isDecimal && route != null)
        {
            var dv = ValidatorAttribute<DecimalsValidatorAttribute>(route);
            if (dv != null)
                return dv.DecimalPlaces;
        }

        if (!this.IsPostgres)
            return defaultScaleSqlServer.TryGetS(dbType.SqlServer);
        else
            return defaultScalePostgreSql.TryGetS(dbType.PostgreSql);
    }
    internal string? GetCollation(DbTypeAttribute? att)
    {
        return att?.GetCollation(this.IsPostgres);
    }

    internal AbstractDbType DefaultSqlType(Type type)
    {
        return this.TypeValues.GetOrThrow(type, "Type {0} not registered");
    }

    public DbTypePair GetSqlDbTypePair(Type type)
    {
        var result = TryGetSqlDbTypePair(type);
        if (result == null)
            throw new ArgumentException($"Type {type.Name} has no DB representation");

        return result;
    }

    public DbTypePair? TryGetSqlDbTypePair(Type type)
    {
        if (TypeValues.TryGetValue(type, out AbstractDbType result))
            return new DbTypePair(result, null);

        if (this.IsPostgres && type == typeof(SqlHierarchyId))
            return new DbTypePair(new AbstractDbType(NpgsqlDbType.LTree), null);

        string? udtTypeName = GetUdtName(type);
        if (udtTypeName != null)
            return new DbTypePair(new AbstractDbType(SqlDbType.Udt), udtTypeName);

        return null;
    }

    public string? GetUdtName(Type udtType)
    {
        var att = udtType.GetCustomAttribute<SqlUserDefinedTypeAttribute>();

        if (att == null)
            return null;

        return UdtSqlName[udtType];
    }

    public bool IsDbType(Type type)
    {
        return type.IsEnum || TryGetSqlDbTypePair(type) != null;
    }

    public void RegisterCustomOrder<T>(Expression<Func<T, string>> customOrder) where T : Entity
    {
        this.CustomOrder.Add(typeof(T), customOrder);
    }
}



public class DbTypePair
{
    public AbstractDbType DbType { get; private set; }
    public string? UserDefinedTypeName { get; private set; }

    public DbTypePair(AbstractDbType type, string? udtTypeName)
    {
        this.DbType = type;
        this.UserDefinedTypeName = udtTypeName;
    }
}

public class AttributeCollection : Collection<Attribute>
{
    readonly AttributeTargets Targets;
    readonly Action assertNotIncluded;

    public AttributeCollection(AttributeTargets targets, IList<Attribute> attributes, Action assertNotIncluded) : base(attributes)
    {
        this.Targets = targets;
        this.assertNotIncluded = assertNotIncluded;
    }

    protected override void InsertItem(int index, Attribute item)
    {
        assertNotIncluded();

        if (!IsCompatibleWith(item, Targets))
            throw new InvalidOperationException("The attribute {0} is not compatible with targets {1}".FormatWith(item, Targets));

        base.InsertItem(index, item);
    }


    public static bool IsCompatibleWith(Attribute a, AttributeTargets targets)
    {
        using (HeavyProfiler.LogNoStackTrace("IsCompatibleWith"))
        {
            var au = a.GetType().GetCustomAttribute<AttributeUsageAttribute>()!;

            return au != null && (au.ValidOn & targets) != 0;
        }
    }

    public new AttributeCollection Add(Attribute attr)
    {
        base.Add(attr);

        return this;
    }

    public AttributeCollection Replace(Attribute attr)
    {
        if (attr is ImplementedByAttribute || attr is ImplementedByAllAttribute)
            this.RemoveAll(a => a is ImplementedByAttribute || a is ImplementedByAllAttribute);
        else
            this.RemoveAll(a => a.GetType() == attr.GetType());

        this.Add(attr);

        return this;
    }

    public AttributeCollection Remove<A>() where A : Attribute
    {
        this.RemoveAll(a => a is A);

        return this;
    }

    protected override void ClearItems()
    {
        assertNotIncluded();

        base.ClearItems();
    }

    protected override void SetItem(int index, Attribute item)
    {
        assertNotIncluded();

        base.SetItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        assertNotIncluded();

        base.RemoveItem(index);
    }
}

internal enum ReferenceFieldType
{
    Reference,
    ImplementedBy,
    ImplmentedByAll,
}
