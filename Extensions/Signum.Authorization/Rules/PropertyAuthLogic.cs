using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Signum.API;
using Signum.API.Json;
using Signum.Authorization;
using Signum.Basics;
using Signum.DynamicQuery.Tokens;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;
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


            TypeConditionsPerType = sb.GlobalLazy(() => new ConcurrentDictionary<(Lite<RoleEntity> role, Type type), bool>(),
                new InvalidateWith(typeof(RulePropertyEntity), typeof(RulePropertyConditionEntity)));

            AuthLogic.ExportToXml += cache.ExportXml;
            AuthLogic.ImportFromXml += cache.ImportXml;
            AuthLogic.HasRuleOverridesEvent += role => cache.HasRealOverrides(role);
            sb.Schema.Table<PropertyRouteEntity>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(AuthCache_PreDeleteSqlSync);
            QueryLogic.Queries.QueryExecuted += Queries_QueryExecuted;
            TypeAuthLogic.HasTypeConditionInProperties = HasTypeConditionInProperties;
        }
    }




    static ResetLazy<ConcurrentDictionary<(Lite<RoleEntity> role, Type type), bool>> TypeConditionsPerType;
    static bool HasTypeConditionInProperties(Type type)
    {
        var role = RoleEntity.Current;
        return TypeConditionsPerType.Value.GetOrAdd((role, type), e =>
        {
            var tac = TypeAuthLogic.GetAllowed(e.type).ToPropertyAllowed();

            if (tac.ConditionRules.IsEmpty())
                return false;

            return PropertyRoute.GenerateRoutes(e.type).Any(pr => !cache.GetAllowed(e.role, pr).Equals(tac));
        });
    }

    static IDisposable? Queries_QueryExecuted(DynamicQueryContainer.ExecuteType type, object queryName, BaseQueryRequest? request)
    {
        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return null;

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

            if (node.ConstantValue == true)
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

            if (tacRule.First.Allowed == PropertyAllowed.None && tacRule.Second.Allowed > PropertyAllowed.None)
                return new OrNode([iExp, acum]);
            else
                return new AndNode([new NotNode(iExp), acum]);
        });
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

    public static bool GetAllowUnathenticated(this PropertyRoute route)
    {
        var hasAttr = route.RootType.HasAttribute<AllowUnathenticatedAttribute>() ||
            (route.PropertyInfo != null && route.PropertyInfo!.HasAttribute<AllowUnathenticatedAttribute>()) ||
            (route.FieldInfo != null && route.FieldInfo!.HasAttribute<AllowUnathenticatedAttribute>());

        return hasAttr;
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

    [MethodExpander(typeof(IsAllowedForPropertyExpander))]
    public static bool IsAllowedFor<T, S>(T mod, Expression<Func<T, S>> property, PropertyAllowed allowed, FilterQueryArgs args)
        where T : ModifiableEntity, IRootEntity
    {
        return IsAllowedFor(mod, PropertyRoute.Construct(property), allowed);
    }

    public class IsAllowedForPropertyExpander : IMethodExpander
    {
        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            Expression entity = arguments[0];
            LambdaExpression lambda = (LambdaExpression)arguments[1].StripQuotes();

            var pr = PropertyRoute.Root(entity.Type).Continue(Reflector.GetMemberListUntyped(lambda));

            PropertyAllowed requested = (PropertyAllowed)ExpressionEvaluator.Eval(arguments[2])!;
            FilterQueryArgs args = (FilterQueryArgs)ExpressionEvaluator.Eval(arguments[3])!;


            return IsAllowedExpression(entity, pr, requested, args);
        }
    }

    public static Expression IsAllowedExpression(Expression entity,  PropertyRoute route, PropertyAllowed requested, FilterQueryArgs args)
    {
        Type type = entity.Type;

        WithConditions<PropertyAllowed> tac = cache.GetAllowed(RoleEntity.Current, route);

        var node = tac.ToTypeConditionNode(requested);

        var simpleNode = node.Simplify();

        var expression = simpleNode.ToExpression(entity, args);

        return expression;
    }

    internal static PropertyAllowed GetAllowed(Entity rootEntity, PropertyRoute pr)
    {
        var paac = PropertyAuthLogic.GetPropertyAllowed(pr);

        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return PropertyAllowed.Write;

        return giGetAllowed.GetInvoker(rootEntity.GetType())(rootEntity, paac);
    }

    public static bool IsAllowedFor(IRootEntity mod, PropertyRoute route, PropertyAllowed allowed)
    {
        var paac = PropertyAuthLogic.GetPropertyAllowed(route);

        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return true;

        if (allowed <= paac.Min())
            return true;

        if (paac.Max() < allowed)
            return false;

        if (mod is Entity e)
            return giGetAllowed.GetInvoker(mod.GetType())(e, paac) >= allowed;
        else
            throw new InvalidOperationException("Unexpected");
    }


    static GenericInvoker<Func<Entity, WithConditions<PropertyAllowed>, PropertyAllowed>> giGetAllowed=
        new ((e, cond) => GetAllowed((ExceptionEntity)e, cond));
    static PropertyAllowed GetAllowed<T>(T entity, WithConditions<PropertyAllowed> paac)
        where T : Entity
    {
        foreach (var cond in paac.ConditionRules.Reverse())
        {
            if (cond.TypeConditions.All(tc => entity.InTypeCondition(tc)))
            {
                return cond.Allowed;
            }
        }

        return paac.Fallback;
    }

    internal static WithConditions<PropertyAllowed> GetAllowed(Lite<RoleEntity> role, PropertyRoute pr) => cache.GetAllowed(role, pr);

}
