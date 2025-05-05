
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Signum.DynamicQuery.Tokens;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;

namespace Signum.Authorization.Rules;

class PropertyCache : AuthCache<RulePropertyEntity, PropertyAllowedRule, PropertyRouteEntity, PropertyRoute, WithConditions<PropertyAllowed>, WithConditionsModel<PropertyAllowed>>
{
    public PropertyCache(SchemaBuilder sb) : base(sb, invalidateWithTypes: true)
    {
    }

    protected override Expression<Func<PropertyRouteEntity, PropertyRouteEntity, bool>> IsEqual => (p1, p2) => p1.Is(p2);

    protected override PropertyRoute ToKey(PropertyRouteEntity resource) => resource.ToPropertyRoute();

    protected override PropertyRouteEntity ToEntity(PropertyRoute key) => PropertyRouteLogic.ToPropertyRouteEntity(key);

    protected override WithConditions<PropertyAllowed> GetRuleAllowed(RulePropertyEntity rule) => new WithConditions<PropertyAllowed>(rule.Fallback, rule.ConditionRules.Select(c => new ConditionRule<PropertyAllowed>(c.Conditions.ToFrozenSet(), c.Allowed)).ToReadOnly());

    protected override RulePropertyEntity SetRuleAllowed(RulePropertyEntity rule, WithConditions<PropertyAllowed> allowed)
    {
        rule.Fallback = allowed.Fallback;
        var oldConditions = rule.ConditionRules.Select(a => new ConditionRule<PropertyAllowed>(a.Conditions.ToFrozenSet(), a.Allowed)).ToReadOnly();
        if (!oldConditions.SequenceEqual(allowed.ConditionRules))
            rule.ConditionRules = allowed.ConditionRules.Select(a => new RulePropertyConditionEntity
            {
                Allowed = a.Allowed,
                Conditions = a.TypeConditions.ToMList()
            }).ToMList();
        return rule;
    }

    protected override WithConditions<PropertyAllowed> ToAllowed(WithConditionsModel<PropertyAllowed> allowedModel) => allowedModel.ToImmutable();
    protected override WithConditionsModel<PropertyAllowed> ToAllowedModel(WithConditions<PropertyAllowed> allowed) => allowed.ToModel();

    static ConcurrentDictionary<(WithConditions<PropertyAllowed> allowed, WithConditions<TypeAllowed> taac), WithConditions<PropertyAllowed>> coerceCache =
        new ConcurrentDictionary<(WithConditions<PropertyAllowed> allowed, WithConditions<TypeAllowed> taac), WithConditions<PropertyAllowed>>();

    public override WithConditions<PropertyAllowed> CoerceValue(Lite<RoleEntity> role, PropertyRoute key, WithConditions<PropertyAllowed> allowed, bool manual = false)
    {
        if (!TypeLogic.TypeToEntity.ContainsKey(key.RootType))
            return WithConditions<PropertyAllowed>.Simple(PropertyAllowed.Write);

        var taac = manual ?
            TypeAuthLogic.Manual.GetAllowed(role, key.RootType) :
            TypeAuthLogic.GetAllowed(role, key.RootType);

        return coerceCache.GetOrAdd((allowed, taac), p =>
        {
            var ptaac = p.taac.ToPropertyAllowed();

            var adjusted = AdjustShape(values: p.allowed, shape: ptaac);

            var coerced = CoerceSimilar(adjusted, ptaac);

            return coerced;
        });
    }

    public static WithConditions<PropertyAllowed> CoerceSimilar(WithConditions<PropertyAllowed> value, WithConditions<PropertyAllowed> maxValue)
    {
        if (AlreadyCoerced(value, maxValue))
            return value;

        PropertyAllowed Min(PropertyAllowed a, PropertyAllowed b) => a < b ? a : b;

        return new WithConditions<PropertyAllowed>(Min(value.Fallback, maxValue.Fallback),
            value.ConditionRules.Select((c, i) => new ConditionRule<PropertyAllowed>(c.TypeConditions, Min(c.Allowed, maxValue.ConditionRules[i].Allowed))).ToReadOnly()
            ).Intern();
    }

    private static bool AlreadyCoerced(WithConditions<PropertyAllowed> value, WithConditions<PropertyAllowed> maxValue)
    {
        if (value.Fallback > maxValue.Fallback)
            return false;

        for (int i = 0; i < value.ConditionRules.Count; i++)
        {
            if (value.ConditionRules[i].Allowed > maxValue.ConditionRules[i].Allowed)
                return false;
        }

        return true;
    }

    static ConcurrentDictionary<(WithConditions<PropertyAllowed> shape, WithConditions<PropertyAllowed> values), WithConditions<PropertyAllowed>> cacheAdjustShape = new ();
    WithConditions<PropertyAllowed> AdjustShape(WithConditions<PropertyAllowed> values, WithConditions<PropertyAllowed> shape)
    {
        if (shape.ConditionRules.Count == values.ConditionRules.Count && 
            shape.ConditionRules.Select(a => a.TypeConditions).SequenceEqual(values.ConditionRules.Select(a=>a.TypeConditions)))
            return values;

        return cacheAdjustShape.GetOrAdd((shape, values), p => new WithConditions<PropertyAllowed>(p.values.Fallback,
            p.shape.ConditionRules.Select(cr =>
            {
                var rule = values.ConditionRules.LastOrDefault(cr2 => cr2.TypeConditions.All(c => cr.TypeConditions.Contains(c)));
                return new ConditionRule<PropertyAllowed>(cr.TypeConditions,
                    rule.TypeConditions != null ? rule.Allowed : values.Fallback);
            }).ToReadOnly()).Intern());
    }

    //static ConcurrentDictionary<(WithConditions<TypeAllowed> shape, WithConditions<TypeAllowed> values), WithConditions<TypeAllowed>> cacheAdjustShapeType = new();
    //WithConditions<TypeAllowed> AdjustShapeType(WithConditions<TypeAllowed> shape, WithConditions<TypeAllowed> values)
    //{
    //    if (shape.ConditionRules.Count == values.ConditionRules.Count &&
    //        shape.ConditionRules.Select(a => a.TypeConditions).SequenceEqual(values.ConditionRules.Select(a => a.TypeConditions)))
    //        return values;

    //    return cacheAdjustShapeType.GetOrAdd((shape, values), p => new WithConditions<TypeAllowed>(
    //        p.values.Fallback,
    //        p.shape.ConditionRules.Select(cr =>
    //        {
    //            var rule = values.ConditionRules.LastOrDefault(cr2 => cr2.TypeConditions.All(c => cr.TypeConditions.Contains(c)));
    //            return new ConditionRule<TypeAllowed>(cr.TypeConditions,
    //                rule.TypeConditions == null ? cr.Allowed : values.Fallback);
    //        }).ToReadOnly()).Intern());
    //}

    static ConcurrentDictionary<(WithConditions<PropertyAllowed> taac, PropertyAllowed pa), WithConditions<PropertyAllowed>> cacheWithShape = new();
    WithConditions<PropertyAllowed> WithShape(WithConditions<PropertyAllowed> taac, PropertyAllowed pa)
    {
        return cacheWithShape.GetOrAdd((taac, pa), p => new WithConditions<PropertyAllowed>(pa, p.taac.ConditionRules.Select(cr => new ConditionRule<PropertyAllowed>(cr.TypeConditions, pa)).ToReadOnly()).Intern());
    }

    protected override WithConditions<PropertyAllowed> Merge(PropertyRoute pr, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, WithConditions<PropertyAllowed>>> baseValues)
    {
        var merge = AuthLogic.GetMergeStrategy(role);

        var tac = GetDefaultFromType(pr, role);

        var baseAdjusted = baseValues.Select(a => AdjustShape(values: a.Value, shape: tac)).ToList();

        Func<IEnumerable<PropertyAllowed>, PropertyAllowed> collapse = merge == MergeStrategy.Union ? MaxPropertyAllowed : MinPropertyAllowed;

        var best = 
            baseAdjusted.Count == 0 ? WithShape(tac, merge == MergeStrategy.Union ? PropertyAllowed.None : PropertyAllowed.Write) :
            baseAdjusted.Count == 1 ? baseAdjusted.SingleEx() :
            new WithConditions<PropertyAllowed>(collapse(baseAdjusted.Select(a => a.Fallback)),
                tac.ConditionRules.Select((a, i) => new ConditionRule<PropertyAllowed>(a.TypeConditions, collapse(baseAdjusted.Select(b => b.ConditionRules[i].Allowed)))).ToReadOnly());

        if (!PermissionAuthLogic.IsAuthorized(BasicPermission.AutomaticUpgradeOfProperties, role))
            return best;


        var maxUp = PropertyAuthLogic.MaxAutomaticUpgrade.TryGetS(pr);
        PropertyAllowed AutomaticUpgrade(PropertyAllowed mergedValue, IEnumerable<(PropertyAllowed baseValue, PropertyAllowed defaultBaseValue)> bases, PropertyAllowed defaultValue)
        {
            if(bases.Where(a=>a.baseValue == mergedValue).All(a=> mergedValue == a.defaultBaseValue))
            {
                var upgrade = maxUp.HasValue && maxUp <= defaultValue ? maxUp.Value : defaultValue;
                return upgrade;
            }

            return mergedValue;
        }

        var baseDefaults = baseValues.Select(a => AdjustShape(values: GetDefaultFromType(pr, a.Key), shape: tac)).ToList();
        var result = new WithConditions<PropertyAllowed>(AutomaticUpgrade(best.Fallback, baseAdjusted.Zip(baseDefaults, (b, d) => (b.Fallback, d.Fallback)), tac.Fallback),
            tac.ConditionRules.Select((cr, i) => new ConditionRule<PropertyAllowed>(cr.TypeConditions,
             AutomaticUpgrade(best.ConditionRules[i].Allowed, baseAdjusted.Zip(baseDefaults, (b, d) => (b.ConditionRules[i].Allowed, d.ConditionRules[i].Allowed)), cr.Allowed)
            )).ToReadOnly());

        return result.Intern();
    }

    static PropertyAllowed MinPropertyAllowed(IEnumerable<PropertyAllowed> collection)
    {
        PropertyAllowed result = PropertyAllowed.Write;

        foreach (var item in collection)
        {
            if (item < result)
                result = item;

            if (result == PropertyAllowed.None)
                return result;

        }
        return result;
    }

    static PropertyAllowed MaxPropertyAllowed(IEnumerable<PropertyAllowed> collection)
    {
        PropertyAllowed result = PropertyAllowed.None;

        foreach (var item in collection)
        {
            if (item > result)
                result = item;

            if (result == PropertyAllowed.Write)
                return result;

        }
        return result;
    }


    public static WithConditions<PropertyAllowed> CoerceSimple(WithConditions<PropertyAllowed> value, PropertyAllowed maxValue)
    {
        if (value.Fallback <= maxValue && value.ConditionRules.All(a=>a.Allowed <=  maxValue))
            return value;

        PropertyAllowed Min(PropertyAllowed a) => a < maxValue ? a : maxValue;

        return new WithConditions<PropertyAllowed>(Min(value.Fallback),
            value.ConditionRules.Select(c => new ConditionRule<PropertyAllowed>(c.TypeConditions, Min(c.Allowed))).ToReadOnly()
            ).Intern();
    }

    protected override Func<PropertyRoute, WithConditions<PropertyAllowed>> GetDefaultValue(Lite<RoleEntity> role)
    {
        return pr =>
        {
            var typeAllowed = TypeAuthLogic.GetAllowed(role, pr.RootType).ToPropertyAllowed();
            if (AuthLogic.GetDefaultAllowed(role))
                return typeAllowed;

            if (!PermissionAuthLogic.IsAuthorized(BasicPermission.AutomaticUpgradeOfProperties, role))
                return CoerceSimple(typeAllowed, PropertyAllowed.None);

            var maxUp = PropertyAuthLogic.MaxAutomaticUpgrade.TryGetS(pr);
            if (maxUp == null)
                return typeAllowed;

            return CoerceSimple(typeAllowed, maxUp.Value);
        };
    }

    WithConditions<PropertyAllowed> GetDefaultFromType(PropertyRoute key, Lite<RoleEntity> role)
    {
        return TypeAuthLogic.GetAllowed(role, key.RootType).ToPropertyAllowed();
    }


    public override XElement ExportXml(bool exportAll)
    {
        return ExportXmlInternal("Properties", "Property",
            resourceToString: p => TypeLogic.GetCleanName(p.RootType) + "|" + p.PropertyString(),
            allowedToXml: paac => [
                new XAttribute("Allowed", paac.Fallback.ToString()),
                paac.ConditionRules.Select(c => new XElement("Condition",
                   new XAttribute("Name", c.TypeConditions.ToString(", ")),
                   new XAttribute("Allowed", c.Allowed.ToString())))
                ],
            allKeys: exportAll ? TypeLogic.TypeToEntity.Keys.SelectMany(t => PropertyRoute.GenerateRoutes(t)).ToList() : null);
    }

    internal static readonly string typeReplacementKey = "AuthRules:" + typeof(TypeEntity).Name;
    internal static readonly string typeConditionReplacementKey = "AuthRules:" + typeof(TypeConditionSymbol).Name;

    private static string AuthPropertiesReplacementKey(Type type)
    {
        return "AuthRules:" + type.Name + " Properties";
    }

    struct PropertyPair
    {
        public readonly string Type;
        public readonly string Property;
        public PropertyPair(string str)
        {
            var index = str.IndexOf("|");
            Type = str.Substring(0, index);
            Property = str.Substring(index + 1);
        }
    }

    public override SqlPreCommand? ImportXml(XElement root, Dictionary<string, Lite<RoleEntity>> roles, Replacements replacements)
    {
        Dictionary<Type, Dictionary<string, PropertyRoute>> routesDicCache = new Dictionary<Type, Dictionary<string, PropertyRoute>>();

        var groups = root.Element("Properties")!.Elements("Role").SelectMany(r => r.Elements("Property")).Select(p => new PropertyPair(p.Attribute("Resource")!.Value))
            .AgGroupToDictionary(a => a.Type, gr => gr.Select(pp => pp.Property).ToHashSet());

        foreach (var item in groups)
        {
            Type? type = TypeLogic.NameToType.TryGetC(replacements.Apply(TypeCache.typeReplacementKey, item.Key));

            if (type == null)
                continue;

            var dic = PropertyRoute.GenerateRoutes(type).ToDictionary(a => a.PropertyString());

            replacements.AskForReplacements(
               item.Value,
               dic.Keys.ToHashSet(),
               AuthPropertiesReplacementKey(type));

            routesDicCache[type] = dic;
        }

        var routes = Database.Query<PropertyRouteEntity>().ToDictionary(a => a.ToPropertyRoute());

        return ImportXmlInternal(root, "Properties", "Property", roles,
            toResource: s =>
            {
                var pp = new PropertyPair(s);

                Type? type = TypeLogic.NameToType.TryGetC(replacements.Apply(TypeCache.typeReplacementKey, pp.Type));
                if (type == null)
                    return null;

                PropertyRoute? route = routesDicCache[type].TryGetC(replacements.Apply(AuthPropertiesReplacementKey(type), pp.Property));
                if (route == null)
                    return null;

                var property = routes.GetOrCreate(route, () => new PropertyRouteEntity
                {
                    RootType = TypeLogic.TypeToEntity[route.RootType],
                    Path = route.PropertyString()
                }.Save());

                return property;

            },
            parseAllowed: e =>
            {
                return new WithConditions<PropertyAllowed>(fallback: e.Attribute("Allowed")!.Value.ToEnum<PropertyAllowed>(),
                    conditionRules: e.Elements("Condition").Select(xc => new ConditionRule<PropertyAllowed>(
                        typeConditions: xc.Attribute("Name")!.Value.SplitNoEmpty(",").Select(s => SymbolLogic<TypeConditionSymbol>.TryToSymbol(replacements.Apply(typeConditionReplacementKey, s.Trim()))).NotNull().ToFrozenSet(),
                        allowed: xc.Attribute("Allowed")!.Value.ToEnum<PropertyAllowed>()))
                    .ToReadOnly());
            });
    }

    protected override string AllowedComment(WithConditions<PropertyAllowed> allowed)
    {
        if (allowed.ConditionRules.Count == 0)
            return allowed.Fallback.ToString();

        return $"{allowed.Fallback} + {allowed.ConditionRules.Count} conditions";
    }
}


