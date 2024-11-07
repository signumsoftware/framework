
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;

namespace Signum.Authorization.Rules;

class PropertyCache : AuthCache<RulePropertyEntity, PropertyAllowedRule, PropertyRouteEntity, PropertyRoute, WithConditions<PropertyAllowed>>
{
    public PropertyCache(SchemaBuilder sb) : base(sb, invalidateWithTypes: true)
    {
    }

    protected override Expression<Func<PropertyRouteEntity, PropertyRouteEntity, bool>> IsEqual => (p1, p2) => p1.Is(p2);

    protected override PropertyRoute ToKey(PropertyRouteEntity resource) => resource.ToPropertyRoute();

    protected override PropertyRouteEntity ToEntity(PropertyRoute key) => PropertyRouteLogic.ToPropertyRouteEntity(key);

    protected override WithConditions<PropertyAllowed> GetRuleAllowed(RulePropertyEntity rule) => new WithConditions<PropertyAllowed>(rule.Fallback,
            rule.ConditionRules.Select(c => new ConditionRule<PropertyAllowed>(c.Conditions, c.Allowed)));

    protected override RulePropertyEntity SetRuleAllowed(RulePropertyEntity rule, WithConditions<PropertyAllowed> allowed)
    {
        rule.Fallback = allowed.Fallback;
        rule.ConditionRules = allowed.ConditionRules.Select(a => new RulePropertyConditionEntity
        {
            Allowed = a.Allowed,
            Conditions = a.TypeConditions.ToMList()
        }).ToMList();
        return rule;
    }

    protected override PropertyAllowedRule ToAllowedRule(PropertyRouteEntity resource, RoleAllowedCache ruleCache)
    {
        var r = base.ToAllowedRule(resource, ruleCache);
        Type type = resource.RootType.ToType();
        r.AvailableConditions = TypeConditionLogic.ConditionsFor(type).ToList();
        return r;
    }

    public override WithConditions<PropertyAllowed> CoerceValue(Lite<RoleEntity> role, PropertyRoute key, WithConditions<PropertyAllowed> allowed, bool manual)
    {
        if (!TypeLogic.TypeToEntity.ContainsKey(key.RootType))
            return new WithConditions<PropertyAllowed>(PropertyAllowed.Write);

        var aac = manual ? 
            TypeAuthLogic.Manual.GetAllowed(role, key.RootType) :
            TypeAuthLogic.GetAllowed(role, key.RootType);

        TypeAllowedBasic ta = aac.MaxUI();

        PropertyAllowed pa = ta.ToPropertyAllowed();

        if (allowed.Max() <= pa)
            return allowed;

        return new WithConditions<PropertyAllowed>(pa);
    }

    protected override WithConditions<PropertyAllowed> Merge(PropertyRoute key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, WithConditions<PropertyAllowed>>> baseValues)
    {
        var best = AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union ?
            ConditionMerger<PropertyAllowed>.MergeBase(baseValues.Select(a => a.Value), MaxPropertyAllowed, PropertyAllowed.Write, PropertyAllowed.None) :
            ConditionMerger<PropertyAllowed>.MergeBase(baseValues.Select(a => a.Value), MinPropertyAllowed, PropertyAllowed.None, PropertyAllowed.Write);

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

    PropertyAllowed GetDefaultFromType(PropertyRoute key, Lite<RoleEntity> role)
    {
        return TypeAuthLogic.GetAllowed(role, key.RootType).MaxUI().ToPropertyAllowed();
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

        return ImportXmlInternal(root, "Properties", "Role", roles,
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
                    conditions: e.Elements("Condition").Select(xc => new ConditionRule<PropertyAllowed>(
                        typeConditions: xc.Attribute("Name")!.Value.SplitNoEmpty(",").Select(s => SymbolLogic<TypeConditionSymbol>.TryToSymbol(replacements.Apply(typeConditionReplacementKey, s.Trim()))).NotNull(),
                        allowed: xc.Attribute("Allowed")!.Value.ToEnum<PropertyAllowed>())));
            });
    }

    protected override string AllowedComment(WithConditions<PropertyAllowed> allowed)
    {
        if (allowed.ConditionRules.Count == 0)
            return allowed.Fallback.ToString();

        return $"{allowed.Fallback} + {allowed.ConditionRules.Count} conditions";
    }
}

