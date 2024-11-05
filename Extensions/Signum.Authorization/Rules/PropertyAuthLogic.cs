using Signum.API;
using Signum.API.Json;
using Signum.Authorization;
using Signum.DynamicQuery.Tokens;
using Signum.Utilities.Reflection;

namespace Signum.Authorization.Rules;

public static class PropertyAuthLogic
{
    static PropertyCache cache = null!;

    public static IManualAuth<PropertyRoute, WithConditions<PropertyAllowed>> Manual { get { return cache; } }

    public static bool IsStarted { get { return cache != null; } }

    public readonly static Dictionary<PropertyRoute, PropertyAllowed> MaxAutomaticUpgrade = new Dictionary<PropertyRoute, PropertyAllowed>();

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            AuthLogic.AssertStarted(sb);
            PropertyRouteLogic.Start(sb);

            sb.Include<RulePropertyEntity>()
                .WithUniqueIndex(rt => new { rt.Resource, rt.Role })
                .WithVirtualMList(rt => rt.ConditionRules, c => c.RuleProperty);

            cache = new PropertyCache(sb);

            sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query =>
            {
                Database.Query<RulePropertyEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete();
                return null;
            };

            PropertyRoute.SetIsAllowedCallback(pp => pp.GetAllowedFor(PropertyAllowed.Read));

            AuthLogic.ExportToXml += cache.ExportXml;
            AuthLogic.ImportFromXml += cache.ImportXml;
            AuthLogic.HasRuleOverridesEvent += role => cache.HasRealOverrides(role);
            sb.Schema.Table<PropertyRouteEntity>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(AuthCache_PreDeleteSqlSync);
        }
    }

    public static Func<Type, Dictionary<string, PropertyConverter>>? GetPropertyConverters;
    public static FluentInclude<T> WithSecuredProperty<T>(this FluentInclude<T> fi,
        Expression<Func<T, object?>> property,
        Expression<Func<T, PropertyAllowed?>> allowedProperty)
        where T : Entity
    {
        if (GetPropertyConverters == null)
            throw new ArgumentNullException(nameof(GetPropertyConverters));

        var pcs = GetPropertyConverters!(typeof(T));
        var piAllowed = ReflectionTools.GetPropertyInfo(allowedProperty);

        var pi = ReflectionTools.GetPropertyInfo(property);

        var allowedCompiled = allowedProperty.Compile();
        pcs.GetOrThrow(pi!.Name.FirstLower()).AvoidWriteJsonProperty = (ctx) =>
        {
            var allowed = allowedCompiled((T)ctx.Entity);
            return allowed == PropertyAllowed.None;
        };

        Validator.PropertyValidator(property).IsReadonly += (e, pi) => pi.PropertyEquals(property) ? allowedCompiled(e) <= PropertyAllowed.Read : null;

        EntityPropertyToken.CustomPropertyExpression.Add(PropertyRoute.Construct(property), (ctx, baseExpression) =>
        {
            var entityExpression = baseExpression.ExtractEntity(true);

            var allowed = Expression.Property(entityExpression, piAllowed);
            var allowedIsNone = Expression.Equal(allowed, Expression.Constant(PropertyAllowed.None).Nullify());

            var prop = Expression.Property(entityExpression, pi);

            Expression result = Expression.Condition(allowedIsNone, Expression.Constant(null, prop.Type), prop);

            return result;
        });

        return fi;
    }

    static SqlPreCommand AuthCache_PreDeleteSqlSync(Entity arg)
    {
        return Administrator.DeleteWhereScript((RulePropertyEntity rt) => rt.Resource, (PropertyRouteEntity)arg);
    }

    private static string AuthPropertiesReplacementKey(Type type)
    {
        return "AuthRules:" + type.Name + " Properties";
    }


    public static PropertyRulePack GetPropertyRules(Lite<RoleEntity> role, TypeEntity typeEntity)
    {
        var result = new PropertyRulePack { Role = role, Type = typeEntity };
        cache.GetRules(result, PropertyRouteLogic.RetrieveOrGenerateProperties(typeEntity));

        result.Rules.ForEach(r => r.CoercedValues = EnumExtensions.GetValues<PropertyAllowed>()
            .Select(pa => new WithConditions<PropertyAllowed>(pa))
            .Where(paac => !cache.CoerceValue(role, r.Resource.ToPropertyRoute(), paac).Equals(paac))
            .ToArray());

        return result;
    }

    public static void SetPropertyRules(PropertyRulePack rules)
    {
        cache.SetRules(rules, r => r.RootType.Is(rules.Type));
    }

    public static void SetMaxAutomaticUpgrade(PropertyRoute property, PropertyAllowed allowed)
    {
        MaxAutomaticUpgrade.Add(property, allowed);
    }

    public static WithConditions<PropertyAllowed> GetPropertyAllowed(Lite<RoleEntity> role, PropertyRoute property)
    {
        return cache.GetAllowed(role, property);
    }

    public static WithConditions<PropertyAllowed> GetPropertyAllowed(this PropertyRoute route)
    {
        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return new WithConditions<PropertyAllowed>(PropertyAllowed.Write);

        route = route.SimplifyToPropertyOrRoot();

        if (!typeof(Entity).IsAssignableFrom(route.RootType))
            return new WithConditions<PropertyAllowed>(PropertyAllowed.Write);

        return cache.GetAllowed(RoleEntity.Current, route);
    }

    public static PropertyAllowed GetAllowUnathenticated(this PropertyRoute route)
    {
        var hasAttr = route.RootType.HasAttribute<AllowUnathenticatedAttribute>() ||
            (route.PropertyInfo != null && route.PropertyInfo!.HasAttribute<AllowUnathenticatedAttribute>()) ||
            (route.FieldInfo != null && route.FieldInfo!.HasAttribute<AllowUnathenticatedAttribute>());

        return hasAttr ? PropertyAllowed.Write : PropertyAllowed.None;
    }

    public static string? GetAllowedFor(this PropertyRoute route, PropertyAllowed requested)
    {
        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return null;

        route = route.SimplifyToPropertyOrRoot();

        if (route.PropertyRouteType == PropertyRouteType.Root || route.IsToStringProperty())
        {
            PropertyAllowed paType = TypeAuthLogic.GetAllowed(route.RootType).MaxUI().ToPropertyAllowed();
            if (paType < requested)
                return "Type {0} is set to {1} for {2}".FormatWith(route.RootType.NiceName(), paType, RoleEntity.Current);

            return null;
        }
        else
        {
            var paProperty = cache.GetAllowed(RoleEntity.Current, route).Max();

            if (paProperty < requested)
                return "Property {0} is set to {1} for {2}".FormatWith(route, paProperty, RoleEntity.Current);

            return null;
        }
    }

    public static Dictionary<PropertyRoute, WithConditions<PropertyAllowed>>? OverridenProperties()
    {
        var dd = cache.GetDefaultDictionary();

        return dd.OverrideDictionary;
    }

    public static AuthThumbnail? GetAllowedThumbnail(Lite<RoleEntity> role, Type entityType)
    {
        return PropertyRoute.GenerateRoutes(entityType).Select(pr => cache.GetAllowed(role, pr).Max()).Collapse();
    }
}
