using Microsoft.AspNetCore.Routing;
using Signum.API.Json;
using Signum.DynamicQuery.Tokens;
using Signum.Entities;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.Eventing.Reader;

namespace Signum.Authorization.Rules;

public static class PropertyAuthLogic
{
    static PropertyCache cache = null!;

    public static IManualAuth<PropertyRoute, WithConditions<PropertyAllowed>> Manual { get { return cache; } }

    public static bool IsStarted { get { return cache != null; } }

    public readonly static Dictionary<PropertyRoute, PropertyAllowed> MaxAutomaticUpgrade = new Dictionary<PropertyRoute, PropertyAllowed>();

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        AuthLogic.AssertStarted(sb);
        PropertyRouteLogic.Start(sb);

        sb.Include<RulePropertyEntity>()
            .WithUniqueIndex(rt => new { rt.Resource, rt.Role })
            .WithVirtualMList(rt => rt.ConditionRules, c => c.RuleProperty);

        cache = new PropertyCache(sb);

        
        PropertyRoute.SetIsAllowedCallback(pp => pp.CanBeAllowedFor(PropertyAllowed.Read));


        TypeConditionsPerType = sb.GlobalLazy(() => new ConcurrentDictionary<(Lite<RoleEntity> role, Type type), bool>(),
            new InvalidateWith(typeof(RulePropertyEntity), typeof(RulePropertyConditionEntity)));

        AuthLogic.HasRuleOverridesEvent += role => cache.HasRealOverrides(role);

        sb.Schema.Synchronizing += rep => TypeConditionRuleSync.NotDefinedTypeCondition<RulePropertyConditionEntity>(rep, rt => rt.Conditions, rtc => rtc.RuleProperty.Entity.Resource.RootType, rtc => rtc.RuleProperty.Entity.Role);
        sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query => { Database.Query<RulePropertyEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete(); return null; };
        sb.Schema.EntityEvents<RoleEntity>().PreDeleteSqlSync += role => Administrator.UnsafeDeletePreCommandVirtualMList(Database.Query<RulePropertyEntity>().Where(a => a.Role.Is(role)));
        sb.Schema.EntityEvents<PropertyRouteEntity>().PreDeleteSqlSync += t => Administrator.UnsafeDeletePreCommandVirtualMList(Database.Query<RulePropertyEntity>().Where(a => a.Resource.Is(t)));
        sb.Schema.EntityEvents<TypeConditionSymbol>().PreDeleteSqlSync += condition => TypeConditionRuleSync.DeletedTypeCondition<RulePropertyConditionEntity>(rt => rt.Conditions, mle => mle.Element.Is(condition));

        QueryToken.IsValueHidden = QueryToken_IsValueHidden;
        TypeAuthLogic.HasTypeConditionInProperties = RequiresTypeConditionForProperties;


        AuthLogic.ExportToXml += cache.ExportXml;
        AuthLogic.ImportFromXml += cache.ImportXml;
    }

    static Expression? QueryToken_IsValueHidden(QueryToken expression, BuildExpressionContext context)
    {
        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return null;

        var pr = expression.GetPropertyRoute();

        if (pr == null)
            return null;

        var role = RoleEntity.Current;

        var pa = cache.GetAllowed(role, pr);

        if (!(pa.Min() == PropertyAllowed.None && pa.Max() > PropertyAllowed.None))
            return null;

        var ta = TypeAuthLogic.GetAllowed(pr.RootType).ToPropertyAllowed();

        if (ta.EqualsForRead(pa))
            return null;

        var parent = expression.Follow(a => a.Parent).FirstOrDefault(a => a.GetPropertyRoute() is PropertyRoute r && r.PropertyRouteType == PropertyRouteType.Root && r.RootType == pr.RootType);

        if (parent == null)
        {
            parent = context.Replacements.Keys.FirstOrDefault(qt => qt is ColumnToken ct && ct.IsEntity() && ct.Type.CleanType() == pr.RootType);

            if (parent == null)
                return Expression.Constant(true); // Unable to know
        }

        var node = DiffNodes(pa, ta);
        node = node.Simplify();


        var parentExpr = parent.BuildExpression(context);

        var args = giTrivialFilterQueryArgs.GetInvoker(pr.RootType)(); //TODO finish

        var exp = node.ToExpression(parentExpr.ExtractEntity(false), args);

        return exp;
    }

    internal static GenericInvoker<Func<FilterQueryArgs>> giTrivialFilterQueryArgs = new(() => GetTrivialFilterQueryArgs<ExceptionEntity>());

    private static FilterQueryArgs GetTrivialFilterQueryArgs<T>()
        where T : Entity
    {
        return FilterQueryArgs.FromQuery(Database.Query<T>());
    }

    static ResetLazy<ConcurrentDictionary<(Lite<RoleEntity> role, Type type), bool>> TypeConditionsPerType;
    static bool RequiresTypeConditionForProperties(Lite<RoleEntity> role, Type type)
    {
        return TypeConditionsPerType.Value.GetOrAdd((role, type), e =>
        {
            var taac = TypeAuthLogic.GetAllowed(e.role, e.type);
            if (taac.ConditionRules.IsEmpty())
                return false;

            var def = taac.ToPropertyAllowed();

            return PropertyRoute.GenerateRoutes(e.type).Any(pr => {

                var paac = cache.GetAllowed(e.role, pr);

                if (paac.Equals(def))
                    return false;

                return paac.CandidatesAssuming(taac).Distinct().Count() > 1; //If for all the type rules that are visible the property has the same value, we don't need the type conditions
            });
        });
    }

    static TypeConditionNode DiffNodes(this WithConditions<PropertyAllowed> propertyAllowed, WithConditions<PropertyAllowed> typeAllowed)
    {
        if (!propertyAllowed.ConditionRules.Select(a => a.TypeConditions).SequenceEqual(
            typeAllowed.ConditionRules.Select(a => a.TypeConditions), TypeConditionSetComparer.Instance))
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

    public static PropertyRulePack GetPropertyRules(Lite<RoleEntity> role, TypeEntity typeEntity)
    {
        var result = new PropertyRulePack { Role = role, Type = typeEntity };
        var properties = PropertyRouteLogic.RetrieveOrGenerateProperties(typeEntity)/*.Where(a => a.Path != "Id")*/.ToList();
        cache.GetRules(result, properties);

        result.Rules.ForEach(r => r.Coerced = cache.CoerceValue(role, r.Resource.ToPropertyRoute(), WithConditions<PropertyAllowed>.Simple(PropertyAllowed.Write)).ToModel());

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
                return WithConditions<PropertyAllowed>.Simple(PropertyAllowed.Write);

            route = route.SimplifyToPropertyOrRoot();

            if (!TypeLogic.TypeToEntity.ContainsKey(route.RootType))
                return WithConditions<PropertyAllowed>.Simple(PropertyAllowed.Write);

            return cache.GetAllowed(RoleEntity.Current, route);
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

    public static FrozenDictionary<PropertyRoute, WithConditions<PropertyAllowed>>? OverridenProperties(Type entityType)
    {
        var dd = cache.GetDefaultDictionary();
        if (dd.OverrideDictionary == null)
            return null;

        var lookup = GetLookupByType(dd);

        return lookup.TryGetC(entityType);
    }

    private static FrozenDictionary<Type, FrozenDictionary<PropertyRoute, WithConditions<PropertyAllowed>>> GetLookupByType(DefaultDictionary<PropertyRoute, WithConditions<PropertyAllowed>> dd)
    {
        if (dd.AdditionalDictionary == null)
        {
            var lookup = dd.OverrideDictionary!.AgGroupToFrozenDictionary(kvp => kvp.Key.RootType, gr => gr.ToFrozenDictionaryEx());
            dd.AdditionalDictionary = lookup;
            return lookup;
        }
        else
        {
            return (FrozenDictionary<Type, FrozenDictionary<PropertyRoute, WithConditions<PropertyAllowed>>>)dd.AdditionalDictionary;
        }
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
        return IsAllowedFor(mod, PropertyRoute.Construct(property), allowed)!;
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

    public static Expression IsAllowedExpression(Expression entity, PropertyRoute route, PropertyAllowed requested, FilterQueryArgs args)
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

        if (rootEntity.IsNew)
            return PropertyAllowed.Write;

        return giEvaluateAllowed.GetInvoker(rootEntity.GetType())(rootEntity, paac);
    }

    public static bool IsAllowedFor(IRootEntity? root, PropertyRoute route, PropertyAllowed allowed)
    {
        using (HeavyProfiler.LogNoStackTrace("IsAllowedFor", () => route.ToString()))
        {
            if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
                return true;

            var paac = PropertyAuthLogic.GetPropertyAllowed(route);
            var taac = root is ModifiableEntity mod && mod.Modified == ModifiedState.Sealed ? null : TypeAuthLogic.GetAllowed(route.RootType);
            if (allowed <= paac.Min(assumingTaac: taac))
                return true;

            if (paac.Max(assumingTaac: taac) < allowed)
                return false;

            if (root is Entity e)
            {
                if (e.IsNew)
                    return true;

                //if (!HasTypeConditionInProperties(route.RootType))
                //{
                //    throw new InvalidOperationException("Unexpected");  //Type is Write / Read and property same
                //}

                return giEvaluateAllowed.GetInvoker(root.GetType())(e, paac) >= allowed;
            }
            else if (root == null) //For example Embedded in a Query 
                return false;
            else
                throw new InvalidOperationException("Unexpected");
        }
    }

    static GenericInvoker<Func<Entity, WithConditions<PropertyAllowed>, PropertyAllowed>> giEvaluateAllowed =
        new((e, cond) => EvaluateAllowed((ExceptionEntity)e, cond));
    static PropertyAllowed EvaluateAllowed<T>(T entity, WithConditions<PropertyAllowed> paac)
        where T : Entity
    {
        using (HeavyProfiler.LogNoStackTrace("EvaluateAllowed"))
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
    }

    internal static WithConditions<PropertyAllowed> GetAllowed(Lite<RoleEntity> role, PropertyRoute pr) => cache.GetAllowed(role, pr);

    public static AuthSerializationMetadata? GetAuthSerializationMetadata(IRootEntity root)
    {
        var type = root.GetType();

        //if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
        //    return new AuthSerializationMetadata(type, PropertyAllowed.None);

        if (UserEntity.Current == null)
            return new AuthSerializationMetadata(type,
                type.HasAttribute<AllowUnauthenticatedAttribute>() ? PropertyAllowed.Write : PropertyAllowed.None,
                null);

        if (root is ModelEntity || !TypeLogic.TypeToEntity.ContainsKey(type))
            return new AuthSerializationMetadata(type, PropertyAllowed.Write, null);

        return giCalculateAuthSerializationMetadata.GetInvoker(type).Invoke((Entity)root);
    }

    public static AuthSerializationMetadata? GetAuthSerializationMetadataEmbedded(PropertyRoute route)
    {
        var type = route.RootType;

        //if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
        //    return new AuthSerializationMetadata(type, PropertyAllowed.None);

        if (UserEntity.Current == null)
            return new AuthSerializationMetadata(type,
                type.HasAttribute<AllowUnauthenticatedAttribute>() ? PropertyAllowed.Write : PropertyAllowed.None,
                null);


        var baseEmbedded = PropertyAuthLogic.GetPropertyAllowed(route);
        var paacDic = OverridenProperties(type)
            ?.Where(a => !a.Key.Equals(route.PropertyString()) && a.Key.PropertyString().StartsWith(route.PropertyString()))
            .ToDictionary(a=>a.Key, a => a.Value.Min(baseEmbedded));

        return new AuthSerializationMetadata(route.RootType, baseEmbedded.Min(baseEmbedded), paacDic);
    }

    static GenericInvoker<Func<Entity, AuthSerializationMetadata>> giCalculateAuthSerializationMetadata =
       new((e) => CalculateAuthSerializationMetadata<ExceptionEntity>((ExceptionEntity)e));
    static AuthSerializationMetadata CalculateAuthSerializationMetadata<T>(T entity)
        where T : Entity
    {
        var type = entity.GetType();
        var taac = TypeAuthLogic.GetAllowed(entity.GetType());
        var paacDic = PropertyAuthLogic.OverridenProperties(type);

        if (entity.IsNew)
        {
            return new AuthSerializationMetadata(typeof(T),
                taac.ToPropertyAllowed().Max(),
                paacDic?.ToDictionary(a => a.Key, a => a.Value.Max())
            );
        }

        var trivial = TypeAuthLogic.TrivialTypeGetUI(taac);
        if (trivial != null && !PropertyAuthLogic.RequiresTypeConditionForProperties(RoleEntity.Current, type))
        {
            return new AuthSerializationMetadata(typeof(T), trivial.Value.ToPropertyAllowed(),
                paacDic?.ToDictionary(a => a.Key, a => a.Value.CandidatesAssuming(taac).Distinct().SingleEx())
                );
        }

        for (int i = taac.ConditionRules.Count - 1; i >= 0; i--)
        {
            var rule = taac.ConditionRules[i];

            if (rule.Allowed != TypeAllowed.None || entity.Modified == ModifiedState.Sealed)
            {
                if (rule.TypeConditions.All(tc => entity.InTypeCondition(tc)))
                    return new AuthSerializationMetadata(typeof(T),
                        rule.Allowed.GetUI().ToPropertyAllowed(),
                        paacDic?.ToDictionary(a => a.Key, a => a.Value.ConditionRules[i].Allowed)
                    );

            }
        }

        return new AuthSerializationMetadata(typeof(T), 
            taac.Fallback.GetUI().ToPropertyAllowed(),
            paacDic?.ToDictionary(a => a.Key, a => a.Value.Fallback)
        );
    }
}


public class AuthSerializationMetadata : SerializationMetadata
{
    public readonly Type Type;

    public readonly PropertyAllowed Default;

    public readonly Dictionary<PropertyRoute, PropertyAllowed>? Properties;

    public AuthSerializationMetadata(Type type, PropertyAllowed @default, Dictionary<PropertyRoute, PropertyAllowed>? properties)
    {
        Type = type;
        Default = @default;
        Properties = properties;
    }

    public override string ToString()
    {
        return $"{Type} {Default}" + (Properties == null ? null : $" + {Properties.Count} Properties");
    }
}
