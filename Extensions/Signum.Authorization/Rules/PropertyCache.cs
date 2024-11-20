
using Microsoft.AspNetCore.DataProtection.KeyManagement;
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

    protected override WithConditions<PropertyAllowed> GetRuleAllowed(RulePropertyEntity rule) => new WithConditions<PropertyAllowed>(rule.Fallback,
            rule.ConditionRules.Select(c => new ConditionRule<PropertyAllowed>(c.Conditions.ToFrozenSet(), c.Allowed)).ToReadOnly());

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

    public override WithConditions<PropertyAllowed> CoerceValue(Lite<RoleEntity> role, PropertyRoute key, WithConditions<PropertyAllowed> allowed, bool manual = false)
    {
        if (!TypeLogic.TypeToEntity.ContainsKey(key.RootType))
            return new WithConditions<PropertyAllowed>(PropertyAllowed.Write);

        var taac = manual ? 
            TypeAuthLogic.Manual.GetAllowed(role, key.RootType) :
            TypeAuthLogic.GetAllowed(role, key.RootType);

        var paac = taac.ToPropertyAllowed();

        var result = Reduce(paac, allowed);

        return result;
    }


    WithConditions<PropertyAllowed> Reduce(WithConditions<PropertyAllowed> structure, WithConditions<PropertyAllowed> allowed)
    {
        PropertyAllowed Min(PropertyAllowed a, PropertyAllowed b) => a < b ? a : b;

        return new WithConditions<PropertyAllowed>(
            Min(structure.Fallback, allowed.Fallback),
            structure.ConditionRules.Select(cr =>
            {
                var similar = allowed.ConditionRules.SingleOrDefault(a => a.TypeConditions.SetEquals(cr.TypeConditions));

                return new ConditionRule<PropertyAllowed>(cr.TypeConditions,
                    similar.TypeConditions == null ? cr.Allowed : Min(cr.Allowed, similar.Allowed));
            }).ToReadOnly());
    }

    protected override WithConditions<PropertyAllowed> Merge(PropertyRoute key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, WithConditions<PropertyAllowed>>> baseValues)
    {
        var merge = AuthLogic.GetMergeStrategy(role);

        var best = merge == MergeStrategy.Union ?
            ConditionMerger<PropertyAllowed>.MergeBase(merge, baseValues.Select(a => a.Value).ToList(), MaxPropertyAllowed, PropertyAllowed.Write, PropertyAllowed.None) :
            ConditionMerger<PropertyAllowed>.MergeBase(merge, baseValues.Select(a => a.Value).ToList(), MinPropertyAllowed, PropertyAllowed.None, PropertyAllowed.Write);

        if (!PermissionAuthLogic.IsAuthorized(BasicPermission.AutomaticUpgradeOfProperties, role))
            return best;

        var maxUp = PropertyAuthLogic.MaxAutomaticUpgrade.TryGetS(key);

        if (maxUp.HasValue && maxUp <= best.Max())
            return best;

        if (baseValues.Where(a => a.Value.Equals(best)).All(a => GetDefaultFromType(key, a.Key).Equals(a.Value)))
        {
            var def = GetDefaultFromType(key, role);

            var upgrade = maxUp.HasValue && maxUp <= def ? maxUp.Value : def;

            return new WithConditions<PropertyAllowed>(upgrade);
        }

        return best;
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


    protected override Func<PropertyRoute, WithConditions<PropertyAllowed>> MergeDefault(Lite<RoleEntity> role)
    {
        return pr =>
        {
            if (AuthLogic.GetDefaultAllowed(role))
                return new WithConditions<PropertyAllowed>(PropertyAllowed.Write);

            if (!PermissionAuthLogic.IsAuthorized(BasicPermission.AutomaticUpgradeOfProperties, role))
                return new WithConditions<PropertyAllowed>(PropertyAllowed.None);

            var maxUp = PropertyAuthLogic.MaxAutomaticUpgrade.TryGetS(pr);

            var typeDefault = GetDefaultFromType(pr, role);

            var upgrade = maxUp.HasValue && maxUp <= typeDefault ?  maxUp.Value :  typeDefault;

            return new WithConditions<PropertyAllowed>(upgrade);
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
                return new WithConditions<PropertyAllowed>(
                    fallback: e.Attribute("Allowed")!.Value.ToEnum<PropertyAllowed>(),
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

