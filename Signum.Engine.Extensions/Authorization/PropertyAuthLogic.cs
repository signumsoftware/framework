using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using System.Reflection; 

namespace Signum.Engine.Authorization
{
    public static class PropertyAuthLogic
    {
        static AuthCache<RulePropertyDN, PropertyAllowedRule, PropertyRouteDN, PropertyRoute, PropertyAllowed> cache;

        public static IManualAuth<PropertyRoute, PropertyAllowed> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public readonly static HashSet<PropertyRoute> AvoidAutomaticUpgradeCollection = new HashSet<PropertyRoute>();

        public static void Start(SchemaBuilder sb, bool queries)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                PropertyRouteLogic.Start(sb);

                cache = new AuthCache<RulePropertyDN, PropertyAllowedRule, PropertyRouteDN, PropertyRoute, PropertyAllowed>(sb,
                    PropertyRouteLogic.ToPropertyRoute,
                    PropertyRouteLogic.ToPropertyRouteDN,
                    merger: new PropertyMerger(),
                    invalidateWithTypes : true,
                    coercer: PropertyCoercer.Instance);

                if (queries)
                {
                    PropertyRoute.SetIsAllowedCallback(pp => pp.GetAllowedFor(PropertyAllowed.Read));
                }

                AuthLogic.ExportToXml += exportAll => cache.ExportXml("Properties", "Property", p => TypeLogic.GetCleanName(p.RootType) + "|" + p.PropertyString(), pa => pa.ToString(), 
                    exportAll ? TypeLogic.TypeToDN.Keys.SelectMany(PropertyRoute.GenerateRoutes).ToList() : null);
                AuthLogic.ImportFromXml += (x, roles, replacements) =>
                {
                    Dictionary<Type, Dictionary<string, PropertyRoute>> routesDicCache = new Dictionary<Type, Dictionary<string, PropertyRoute>>();

                    string replacementKey = typeof(OperationSymbol).Name;

                    var groups =  x.Element("Properties").Elements("Role").SelectMany(r => r.Elements("Property")).Select(p => new PropertyPair(p.Attribute("Resource").Value))
                        .AgGroupToDictionary(a=>a.Type, gr=>gr.Select(pp=> pp.Property).ToHashSet());

                    foreach (var item in groups)
                    {
                        Type type = TypeLogic.NameToType.TryGetC(replacements.Apply(typeof(TypeDN).Name, item.Key));

                        if (type == null)
                            continue;

                        var dic = PropertyRoute.GenerateRoutes(type).ToDictionary(a => a.PropertyString());

                        replacements.AskForReplacements(
                           item.Value,
                           dic.Keys.ToHashSet(),
                           type.Name + " Properties");


                        routesDicCache[type] = dic;
                    }

                    var routes = Database.Query<PropertyRouteDN>().ToDictionary(a => a.ToPropertyRoute());

                    return cache.ImportXml(x, "Properties", "Property", roles, s =>
                    {
                        var pp = new PropertyPair(s);

                        Type type = TypeLogic.NameToType.TryGetC(replacements.Apply(typeof(TypeDN).Name, pp.Type));
                        if (type == null)
                            return null;

                        PropertyRoute route = routesDicCache[type].TryGetC(replacements.Apply(type.Name + " Properties", pp.Property));

                        if (route == null)
                            return null;

                        var property = routes.GetOrCreate(route, () => new PropertyRouteDN
                         {
                             Route = route,
                             RootType = TypeLogic.TypeToDN[route.RootType],
                             Path = route.PropertyString()
                         }.Save());

                        return property;

                    }, EnumExtensions.ToEnum<PropertyAllowed>);
                };
            }
        }

        struct PropertyPair
        {
            public readonly string Type;
            public readonly  string Property;
            public PropertyPair(string str)
            {
                var index = str.IndexOf("|");
                Type = str.Substring(0, index);
                Property = str.Substring(index + 1);
            }
        }


        public static PropertyRulePack GetPropertyRules(Lite<RoleDN> role, TypeDN typeDN)
        {
            var result = new PropertyRulePack { Role = role, Type = typeDN }; 
            cache.GetRules(result, PropertyRouteLogic.RetrieveOrGenerateProperties(typeDN));

            var coercer = PropertyCoercer.Instance.GetCoerceValue(role);
            result.Rules.ForEach(r => r.CoercedValues = EnumExtensions.GetValues<PropertyAllowed>()
                .Where(a => !coercer(PropertyRouteLogic.ToPropertyRoute(r.Resource), a).Equals(a))
                .ToArray());

            return result;
        }

        public static void SetPropertyRules(PropertyRulePack rules)
        {
            cache.SetRules(rules, r => r.RootType == rules.Type); 
        }

        public static PropertyAllowed GetPropertyAllowed(Lite<RoleDN> role, PropertyRoute property)
        {
            return cache.GetAllowed(role, property);
        }

        public static PropertyAllowed GetPropertyAllowed(this PropertyRoute route)
        {
            if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
                return PropertyAllowed.Modify;

            while (route.PropertyRouteType == PropertyRouteType.MListItems || route.PropertyRouteType == PropertyRouteType.LiteEntity)
                route = route.Parent;

            return cache.GetAllowed(RoleDN.Current.ToLite(), route);
        }

        public static string GetAllowedFor(this PropertyRoute route, PropertyAllowed requested)
        {
            if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
                return null;

            while (route.PropertyRouteType == PropertyRouteType.MListItems || route.PropertyRouteType == PropertyRouteType.LiteEntity)
                route = route.Parent;

            if (route.PropertyRouteType == PropertyRouteType.Root)
            {
                PropertyAllowed paType = TypeAuthLogic.GetAllowed(route.RootType).Max(ExecutionMode.InUserInterface).ToPropertyAllowed();
                if (paType < requested)
                    return "Type {0} is set to {1} for {2}".Formato(route.RootType.NiceName(), paType, RoleDN.Current);

                return null;
            }
            else
            {
                PropertyAllowed paProperty = cache.GetAllowed(RoleDN.Current.ToLite(), route);

                if (paProperty < requested)
                    return "Property {0} is set to {1} for {2}".Formato(route, paProperty, RoleDN.Current);

                return null;
            }
        }

        public static Dictionary<PropertyRoute, PropertyAllowed> OverridenProperties()
        {
            var dd = cache.GetDefaultDictionary();

            return dd.OverrideDictionary;
        }

        public static AuthThumbnail? GetAllowedThumbnail(Lite<RoleDN> role, Type entityType)
        {
            return PropertyRoute.GenerateRoutes(entityType).Select(pr => cache.GetAllowed(role, pr)).Collapse();
        }
    }

    class PropertyMerger : IMerger<PropertyRoute, PropertyAllowed>
    {
        public PropertyAllowed Merge(PropertyRoute key, Lite<RoleDN> role, IEnumerable<KeyValuePair<Lite<RoleDN>, PropertyAllowed>> baseValues)
        {
            PropertyAllowed best = AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union ?
                Max(baseValues.Select(a => a.Value)) :
                Min(baseValues.Select(a => a.Value));

            if (!BasicPermission.AutomaticUpgradeOfProperties.IsAuthorized(role) || PropertyAuthLogic.AvoidAutomaticUpgradeCollection.Contains(key))
                return best;

            if (baseValues.Where(a => a.Value.Equals(best)).All(a => GetDefault(key, a.Key).Equals(a.Value)))
                return GetDefault(key, role);

            return best;
        }

        PropertyAllowed GetDefault(PropertyRoute key, Lite<RoleDN> role)
        {
            return TypeAuthLogic.GetAllowed(role, key.RootType).MaxUI().ToPropertyAllowed();
        }

        static PropertyAllowed Max(IEnumerable<PropertyAllowed> baseValues)
        {
            PropertyAllowed result = PropertyAllowed.None;

            foreach (var item in baseValues)
            {
                if (item > result)
                    result = item;

                if (result == PropertyAllowed.Modify)
                    return result;
            }
            return result;
        }

        static PropertyAllowed Min(IEnumerable<PropertyAllowed> baseValues)
        {
            PropertyAllowed result = PropertyAllowed.Modify;

            foreach (var item in baseValues)
            {
                if (item < result)
                    result = item;

                if (result == PropertyAllowed.None)
                    return result;
            }
            return result;
        }

        public Func<PropertyRoute, PropertyAllowed> MergeDefault(Lite<RoleDN> role)
        {
            return pr =>
            {
                if (!BasicPermission.AutomaticUpgradeOfProperties.IsAuthorized(role) || PropertyAuthLogic.AvoidAutomaticUpgradeCollection.Contains(pr))
                    return AuthLogic.GetDefaultAllowed(role) ? PropertyAllowed.Modify : PropertyAllowed.None;

                return GetDefault(pr, role);
            };
        }
    }

    class PropertyCoercer : Coercer<PropertyAllowed, PropertyRoute>
    {
        public static readonly PropertyCoercer Instance = new PropertyCoercer();

        private PropertyCoercer()
        {
        }

        public override Func<PropertyRoute, PropertyAllowed, PropertyAllowed> GetCoerceValue(Lite<RoleDN> role)
        {
            return (pr, a) =>
            {
                if (!TypeLogic.TypeToDN.ContainsKey(pr.RootType))
                    return PropertyAllowed.Modify;

                TypeAllowedAndConditions aac = TypeAuthLogic.GetAllowed(role, pr.RootType);

                TypeAllowedBasic ta = aac.MaxUI();

                PropertyAllowed pa = ta.ToPropertyAllowed();

                return a < pa ? a : pa; ;
            };
        }

        public override Func<Lite<RoleDN>, PropertyAllowed, PropertyAllowed> GetCoerceValueManual(PropertyRoute pr)
        {
            return (role, a) =>
            {
                if (!TypeLogic.TypeToDN.ContainsKey(pr.RootType))
                    return PropertyAllowed.Modify;

                TypeAllowedAndConditions aac = TypeAuthLogic.Manual.GetAllowed(role, pr.RootType);

                TypeAllowedBasic ta = aac.MaxUI();

                PropertyAllowed pa = ta.ToPropertyAllowed();

                return a < pa ? a : pa; ;
            };
        }
    }
}
