using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Signum.API;
using Signum.API.Json;
using Signum.Authorization;
using Signum.DynamicQuery.Tokens;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Collections.Frozen;
using System.Linq;
using System.Runtime.CompilerServices;

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

            PropertyRoute.SetIsAllowedCallback(pp => pp.CanBeAllowedFor(PropertyAllowed.Read));

            AuthLogic.ExportToXml += cache.ExportXml;
            AuthLogic.ImportFromXml += cache.ImportXml;
            AuthLogic.HasRuleOverridesEvent += role => cache.HasRealOverrides(role);
            sb.Schema.Table<PropertyRouteEntity>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(AuthCache_PreDeleteSqlSync);
            QueryLogic.Queries.QueryExecuted += Queries_QueryExecuted;
            Schema.Current.EntityEventsGlobal.AdditionalBindings.Add(PropertyRoute.Root(typeof(ExceptionEntity) /*mock*/), new AdditionalBinding<>)
        }
    }

    static IDisposable? Queries_QueryExecuted(DynamicQueryContainer.ExecuteType type, object queryName, BaseQueryRequest? request)
    {
        if (request == null)
            return null;

        var tokens = request.AllTokens();

        var propertyRoutes = tokens.SelectMany(t => GetAllPropertyRoutes(t)).Distinct().ToList();

        var role = RoleEntity.Current;
        var list = (from pr in propertyRoutes
                    let pa = cache.GetAllowed(role, pr)
                    where pa.Min() == PropertyAllowed.None && pa.Max() > PropertyAllowed.None
                    let ta = TypeAuthLogic.GetAllowed(pr.RootType).ToPropertyAllowed()
                    where !ta.Equals(pa)
                    select new { pr, ta, pa }).ToList();

        if (list.Count == 0)
            return null;

        var problematicPropertioes = list.Select(a => a.pr).ToHashSet();

        var simpleFilters = request.Filters.Where(a => a.GetTokens().SelectMany(a => GetAllPropertyRoutes(a)).All(t => !problematicPropertioes.Contains(t)));

        var entityColumn = QueryLogic.Queries.QueryDescription(queryName).Columns.Single(q => q.IsEntity);

        foreach (var gr in list.GroupBy(a => new { a.ta, a.pa, a.pr.RootType }, a => a.pr))
        {
            if (entityColumn.Implementations == null ||
                entityColumn.Implementations.Value.IsByAll ||
                !entityColumn.Implementations.Value.Types.Contains(gr.Key.RootType))
                throw new UnauthorizedAccessException(AuthMessage.UnableToDetermineIfYouCanRead0.NiceToString(gr.CommaAnd()));

            var node = DiffNodes(gr.Key.pa, gr.Key.ta);

            node = node.Simplify();

            if(node.ConstantValue == true)
                throw new UnauthorizedAccessException(AuthMessage.TheQueryDoesNotEnsureThatYouCanRead0.NiceToString(gr.CommaAnd()));

            if (node.ConstantValue == false)
                continue; //possible?

            var query = QueryLogic.Queries.GetEntitiesFull(new QueryEntitiesRequest
            {
                QueryName = queryName,
                Filters = simpleFilters.ToList(),
                Orders = new List<Order>(),
                QueryUrl = request.QueryUrl,
                Count = null,
            });

            var pe = Expression.Parameter(typeof(Entity));

            var exp = node.ToExpression(Expression.Convert(pe, gr.Key.RootType), FilterQueryArgs.FromQuery(query));

            var lambda = Expression.Lambda<Func<Entity, bool>>(exp, pe);

            if (query.Any(lambda))
                throw new UnauthorizedAccessException(AuthMessage.TheQueryDoesNotEnsureThatYouCanRead0.NiceToString(gr.CommaAnd()));
        }
        
        return null;
    }

    static IEnumerable<PropertyRoute> GetAllPropertyRoutes(QueryToken token)
    {
        var t = token;
        PropertyRoute? lastPr = null; 
        while (t != null)
        {
            var pr = t.GetPropertyRoute();

            if (pr != null && pr != lastPr)
            {
                yield return pr; 
                lastPr = pr;
            }

            t = t.Parent;
        }
    }

    static TypeConditionNode DiffNodes(this WithConditions<PropertyAllowed> propertyAllowed, WithConditions<PropertyAllowed> typeAllowed)
    {
        if (!propertyAllowed.ConditionRules.Select(a => a.TypeConditions).SequenceEqual(
            typeAllowed.ConditionRules.Select(a => a.TypeConditions)))
            throw new InvalidOperationException("Property Allowed and Type Allowed not in sync");

        var baseValue = propertyAllowed.Fallback == PropertyAllowed.None && typeAllowed.Fallback > PropertyAllowed.None ? TypeConditionNode.True : TypeConditionNode.False;

        return propertyAllowed.ConditionRules.Zip(typeAllowed.ConditionRules).Aggregate(baseValue, (acum, tacRule) =>
        {
            var iExp = new AndNode(tacRule.First.TypeConditions.Select(a => (TypeConditionNode)new SymbolNode(a)).ToHashSet());

            if (tacRule.First.Allowed == PropertyAllowed.None  && tacRule.Second.Allowed > PropertyAllowed.None)
                return new OrNode([iExp, acum]);
            else
                return new AndNode([new NotNode(iExp), acum]);
        });
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
        cache.GetRules(result, PropertyRouteLogic.RetrieveOrGenerateProperties(typeEntity).Where(a => a.Path != "Id"));

        result.Rules.ForEach(r => r.Coerced = cache.CoerceValue(role, r.Resource.ToPropertyRoute(), new WithConditions<PropertyAllowed>(PropertyAllowed.Write)).ToModel());

        Type type = typeEntity.ToType();
        result.AvailableTypeConditions = TypeAuthLogic.GetAllowed(role, type).ConditionRules.Select(a => a.TypeConditions.ToList()).ToList();

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

    public static string? CanBeAllowedFor(this PropertyRoute route, PropertyAllowed requested)
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

    public static WithConditionsModel<AuthThumbnail>? GetAllowedThumbnail(Lite<RoleEntity> role, Type entityType, WithConditions<TypeAllowed> typeAllowedModel)
    {
        var wcps = PropertyRoute.GenerateRoutes(entityType).Select(pr => cache.GetAllowed(role, pr)).ToList();

        if (wcps.IsEmpty())
            return null;

        return new WithConditionsModel<AuthThumbnail>(
            wcps.Select(a => a.Fallback).Collapse()!.Value,
            typeAllowedModel.ConditionRules.Select(crt =>
            {
                var thumbnail = wcps.Select(a => a.ConditionRules.Single(cr => crt.TypeConditions.SequenceEqual(cr.TypeConditions)).Allowed).Collapse()!.Value;

                return new ConditionRuleModel<AuthThumbnail>(crt.TypeConditions, thumbnail);
            }));
    }

    public static bool IsAllowedFor<T, S>(T mod, Expression<Func<T, S>> property, PropertyAllowed allowed)
        where T : ModifiableEntity, IRootEntity
    {
        var taac = PropertyAuthLogic.GetPropertyAllowed(PropertyRoute.Construct(property));

        return IsAllowedFor(mod, taac, allowed);
    }

    public static bool IsAllowedFor(ModifiableEntity mod, PropertyRoute route, PropertyAllowed allowed)
    {

        var taac = PropertyAuthLogic.GetPropertyAllowed(route);

        return IsAllowedFor(mod, taac, allowed);
    }

    public static bool IsAllowedFor(ModifiableEntity mod, WithConditions<PropertyAllowed> paac, PropertyAllowed allowed)
    {
        if (AuthLogic.GloballyEnabled || ExecutionMode.InGlobal)
            return true;

        if (paac.Min() >= allowed)
            return true;

        if (paac.Max() <= allowed)
            return false;

        if (mod is Entity e)
            return giEvaluateAllowedFor.GetInvoker(mod.GetType())(e, paac, allowed);
        else
            throw new InvalidOperationException("Unexpected");
    }

    static GenericInvoker<Func<Entity, WithConditions<PropertyAllowed>, PropertyAllowed, bool>> giEvaluateAllowedFor =
        new ((e, cond, pa) => EvaluateIsAllowedFor((UserEntity)e, cond, pa));
    static bool EvaluateIsAllowedFor<T>(T entity, WithConditions<PropertyAllowed> paac, PropertyAllowed allowed)
        where T : Entity
    {
        foreach (var cond in paac.ConditionRules.Reverse())
        {
            if (cond.TypeConditions.All(tc =>
            {
                var func = TypeConditionLogic.GetInMemoryCondition<T>(tc)!;

                if (func == null)
                    throw new InvalidOperationException($"TypeCondition {tc} has no in-memory implementation for {typeof(T).Name}");

                return func(entity);
            }))
            {
                return cond.Allowed >= allowed;
            }
        }

        return paac.Fallback >= allowed;
    }
}
