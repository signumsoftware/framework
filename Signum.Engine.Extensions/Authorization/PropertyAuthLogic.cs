using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities;
using System.Reflection;

namespace Signum.Engine.Authorization
{
    public static class PropertyAuthLogic
    {
        static AuthCache<RulePropertyEntity, PropertyAllowedRule, PropertyRouteEntity, PropertyRoute, PropertyAllowed> cache = null!;

        public static IManualAuth<PropertyRoute, PropertyAllowed> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public readonly static Dictionary<PropertyRoute, PropertyAllowed> MaxAutomaticUpgrade = new Dictionary<PropertyRoute, PropertyAllowed>();

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                PropertyRouteLogic.Start(sb);

                sb.Include<RulePropertyEntity>()
                 .WithUniqueIndex(rt => new { rt.Resource, rt.Role });

                cache = new AuthCache<RulePropertyEntity, PropertyAllowedRule, PropertyRouteEntity, PropertyRoute, PropertyAllowed>(sb,
                    toKey: PropertyRouteEntity.ToPropertyRouteFunc,
                    toEntity: PropertyRouteLogic.ToPropertyRouteEntity,
                    isEquals: (p1, p2) => p1 == p2,
                    merger: new PropertyMerger(),
                    invalidateWithTypes: true,
                    coercer: PropertyCoercer.Instance);

                sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query =>
                {
                    Database.Query<RulePropertyEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete();
                    return null;
                };

                PropertyRoute.SetIsAllowedCallback(pp => pp.GetAllowedFor(PropertyAllowed.Read));

                AuthLogic.ExportToXml += exportAll => cache.ExportXml("Properties", "Property", p => TypeLogic.GetCleanName(p.RootType) + "|" + p.PropertyString(), pa => pa.ToString(),
                    exportAll ? TypeLogic.TypeToEntity.Keys.SelectMany(t => PropertyRoute.GenerateRoutes(t)).ToList() : null);
                AuthLogic.ImportFromXml += (x, roles, replacements) =>
                {
                    Dictionary<Type, Dictionary<string, PropertyRoute>> routesDicCache = new Dictionary<Type, Dictionary<string, PropertyRoute>>();

                    var groups = x.Element("Properties").Elements("Role").SelectMany(r => r.Elements("Property")).Select(p => new PropertyPair(p.Attribute("Resource").Value))
                        .AgGroupToDictionary(a => a.Type, gr => gr.Select(pp => pp.Property).ToHashSet());

                    foreach (var item in groups)
                    {
                        Type? type = TypeLogic.NameToType.TryGetC(replacements.Apply(TypeAuthCache.typeReplacementKey, item.Key));

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

                    return cache.ImportXml(x, "Properties", "Property", roles, s =>
                    {
                        var pp = new PropertyPair(s);

                        Type? type = TypeLogic.NameToType.TryGetC(replacements.Apply(TypeAuthCache.typeReplacementKey, pp.Type));
                        if (type == null)
                            return null;

                        PropertyRoute? route = routesDicCache[type].TryGetC(replacements.Apply(AuthPropertiesReplacementKey(type), pp.Property));
                        if (route == null)
                            return null;

                        var property = routes.GetOrCreate(route, () => new PropertyRouteEntity
                         {
                             Route = route,
                             RootType = TypeLogic.TypeToEntity[route.RootType],
                             Path = route.PropertyString()
                         }.Save());

                        return property;

                    }, EnumExtensions.ToEnum<PropertyAllowed>);
                };

                sb.Schema.Table<PropertyRouteEntity>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(AuthCache_PreDeleteSqlSync);
            }
        }

        static SqlPreCommand AuthCache_PreDeleteSqlSync(Entity arg)
        {
            return Administrator.DeleteWhereScript((RulePropertyEntity rt) => rt.Resource, (PropertyRouteEntity)arg);
        }

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


        public static PropertyRulePack GetPropertyRules(Lite<RoleEntity> role, TypeEntity typeEntity)
        {
            var result = new PropertyRulePack { Role = role, Type = typeEntity };
            cache.GetRules(result, PropertyRouteLogic.RetrieveOrGenerateProperties(typeEntity));

            var coercer = PropertyCoercer.Instance.GetCoerceValue(role);
            result.Rules.ForEach(r => r.CoercedValues = EnumExtensions.GetValues<PropertyAllowed>()
                .Where(a => !coercer(PropertyRouteEntity.ToPropertyRouteFunc(r.Resource), a).Equals(a))
                .ToArray());

            return result;
        }

        public static void SetPropertyRules(PropertyRulePack rules)
        {
            cache.SetRules(rules, r => r.RootType == rules.Type);
        }

        public static void SetMaxAutomaticUpgrade(PropertyRoute property, PropertyAllowed allowed)
        {
            MaxAutomaticUpgrade.Add(property, allowed);
        }

        public static PropertyAllowed GetPropertyAllowed(Lite<RoleEntity> role, PropertyRoute property)
        {
            return cache.GetAllowed(role, property);
        }

        public static PropertyAllowed GetPropertyAllowed(this PropertyRoute route)
        {
            if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
                return PropertyAllowed.Write;

            route = route.SimplifyToPropertyOrRoot();

            if (!typeof(Entity).IsAssignableFrom(route.RootType))
                return PropertyAllowed.Write;

            return cache.GetAllowed(RoleEntity.Current, route);
        }

        public static PropertyAllowed GetNoUserPropertyAllowed(this PropertyRoute route)
        {
            var hasAttr = route.RootType.HasAttribute<AllowedNoUserAttribute>() ||
                (route.PropertyInfo != null && route.PropertyInfo!.HasAttribute<AllowedNoUserAttribute>()) ||
                (route.FieldInfo != null && route.FieldInfo!.HasAttribute<AllowedNoUserAttribute>());

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
                PropertyAllowed paProperty = cache.GetAllowed(RoleEntity.Current, route);

                if (paProperty < requested)
                    return "Property {0} is set to {1} for {2}".FormatWith(route, paProperty, RoleEntity.Current);

                return null;
            }
        }

        public static Dictionary<PropertyRoute, PropertyAllowed>? OverridenProperties()
        {
            var dd = cache.GetDefaultDictionary();

            return dd.OverrideDictionary;
        }

        public static AuthThumbnail? GetAllowedThumbnail(Lite<RoleEntity> role, Type entityType)
        {
            return PropertyRoute.GenerateRoutes(entityType).Select(pr => cache.GetAllowed(role, pr)).Collapse();
        }
    }

    class PropertyMerger : IMerger<PropertyRoute, PropertyAllowed>
    {
        public PropertyAllowed Merge(PropertyRoute key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, PropertyAllowed>> baseValues)
        {
            PropertyAllowed best = AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union ?
                Max(baseValues.Select(a => a.Value)) :
                Min(baseValues.Select(a => a.Value));

            if (!BasicPermission.AutomaticUpgradeOfProperties.IsAuthorized(role))
                return best;

            var maxUp = PropertyAuthLogic.MaxAutomaticUpgrade.TryGetS(key);

            if (maxUp.HasValue && maxUp <= best)
                return best;

            if (baseValues.Where(a => a.Value.Equals(best)).All(a => GetDefault(key, a.Key).Equals(a.Value)))
            {
                var def = GetDefault(key, role);

                return maxUp.HasValue && maxUp <= def ? maxUp.Value : def;
            }

            return best;
        }

        PropertyAllowed GetDefault(PropertyRoute key, Lite<RoleEntity> role)
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

                if (result == PropertyAllowed.Write)
                    return result;
            }
            return result;
        }

        static PropertyAllowed Min(IEnumerable<PropertyAllowed> baseValues)
        {
            PropertyAllowed result = PropertyAllowed.Write;

            foreach (var item in baseValues)
            {
                if (item < result)
                    result = item;

                if (result == PropertyAllowed.None)
                    return result;
            }
            return result;
        }

        public Func<PropertyRoute, PropertyAllowed> MergeDefault(Lite<RoleEntity> role)
        {
            return pr =>
            {
                if (!BasicPermission.AutomaticUpgradeOfProperties.IsAuthorized(role))
                    return AuthLogic.GetDefaultAllowed(role) ? PropertyAllowed.Write : PropertyAllowed.None;

                var maxUp = PropertyAuthLogic.MaxAutomaticUpgrade.TryGetS(pr);

                var def = GetDefault(pr, role);

                return maxUp.HasValue && maxUp <= def ? maxUp.Value : def;
            };
        }
    }

    class PropertyCoercer : Coercer<PropertyAllowed, PropertyRoute>
    {
        public static readonly PropertyCoercer Instance = new PropertyCoercer();

        private PropertyCoercer()
        {
        }

        public override Func<PropertyRoute, PropertyAllowed, PropertyAllowed> GetCoerceValue(Lite<RoleEntity> role)
        {
            return (pr, a) =>
            {
                if (!TypeLogic.TypeToEntity.ContainsKey(pr.RootType))
                    return PropertyAllowed.Write;

                TypeAllowedAndConditions aac = TypeAuthLogic.GetAllowed(role, pr.RootType);

                TypeAllowedBasic ta = aac.MaxUI();

                PropertyAllowed pa = ta.ToPropertyAllowed();

                return a < pa ? a : pa;
            };
        }

        public override Func<Lite<RoleEntity>, PropertyAllowed, PropertyAllowed> GetCoerceValueManual(PropertyRoute pr)
        {
            return (role, a) =>
            {
                if (!TypeLogic.TypeToEntity.ContainsKey(pr.RootType))
                    return PropertyAllowed.Write;

                TypeAllowedAndConditions aac = TypeAuthLogic.Manual.GetAllowed(role, pr.RootType);

                TypeAllowedBasic ta = aac.MaxUI();

                PropertyAllowed pa = ta.ToPropertyAllowed();

                return a < pa ? a : pa;
            };
        }
    }
}
