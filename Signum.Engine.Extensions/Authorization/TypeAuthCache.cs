using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities;
using Signum.Engine.Maps;
using Signum.Engine.Authorization;
using Signum.Engine;
using System.Xml.Linq;
using Signum.Engine.Basics;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Entities.Reflection;

namespace Signum.Entities.Authorization
{
    class TypeAuthCache : IManualAuth<Type, TypeAllowedAndConditions>
    {
        readonly ResetLazy<Dictionary<Lite<RoleEntity>, RoleAllowedCache>> runtimeRules;

        IMerger<Type, TypeAllowedAndConditions> merger;

        public TypeAuthCache(SchemaBuilder sb, IMerger<Type, TypeAllowedAndConditions> merger)
        {
            this.merger = merger;

            sb.Include<RuleTypeEntity>();

            sb.AddUniqueIndex<RuleTypeEntity>(rt => new { rt.Resource, rt.Role });

            runtimeRules = sb.GlobalLazy(NewCache,
                new InvalidateWith(typeof(RuleTypeEntity), typeof(RoleEntity)), AuthLogic.NotifyRulesChanged);

            sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query => { Database.Query<RuleTypeEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete(); return null; };
            sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand?>(AuthCache_PreDeleteSqlSync_Type);
            sb.Schema.Table<TypeConditionSymbol>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand?>(AuthCache_PreDeleteSqlSync_Condition);

            Validator.PropertyValidator((RuleTypeEntity r) => r.Conditions).StaticPropertyValidation += TypeAuthCache_StaticPropertyValidation;
        }

        string? TypeAuthCache_StaticPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
        {
            RuleTypeEntity rt = (RuleTypeEntity)sender;
            if (rt.Resource == null)
            {
                if (rt.Conditions.Any())
                    return "Default {0} should not have conditions".FormatWith(typeof(RuleTypeEntity).NiceName());

                return null;
            }

            try
            {

                Type type = TypeLogic.EntityToType.GetOrThrow(rt.Resource);
                var conditions = rt.Conditions.Where(a =>
                a.Condition.FieldInfo != null && /*Not 100% Sync*/
                !TypeConditionLogic.IsDefined(type, a.Condition));

                if (conditions.IsEmpty())
                    return null;

                return "Type {0} has no definitions for the conditions: {1}".FormatWith(type.Name, conditions.CommaAnd(a => a.Condition.Key));
            }
            catch (Exception ex) when (StartParameters.IgnoredDatabaseMismatches != null)
            {
                //This try { throw } catch is here to alert developers.
                //In production, in some cases its OK to attempt starting an application with a slightly different schema (dynamic entities, green-blue deployments).
                //In development, consider synchronize.
                StartParameters.IgnoredDatabaseMismatches.Add(ex);
                return null;
            }
        }

        static SqlPreCommand? AuthCache_PreDeleteSqlSync_Type(Entity arg)
        {
            TypeEntity type = (TypeEntity)arg;

            var command = Administrator.UnsafeDeletePreCommand(Database.Query<RuleTypeEntity>().Where(a => a.Resource == type));

            return command;
        }

        static SqlPreCommand? AuthCache_PreDeleteSqlSync_Condition(Entity arg)
        {
            TypeConditionSymbol condition = (TypeConditionSymbol)arg;

            var command = Administrator.UnsafeDeletePreCommandMList((RuleTypeEntity rt)=>rt.Conditions,  Database.MListQuery((RuleTypeEntity rt) => rt.Conditions).Where(mle => mle.Element.Condition == condition));

            return command;
        }

        TypeAllowedAndConditions IManualAuth<Type, TypeAllowedAndConditions>.GetAllowed(Lite<RoleEntity> role, Type key)
        {
            TypeEntity resource = TypeLogic.TypeToEntity.GetOrThrow(key);

            ManualResourceCache miniCache = new ManualResourceCache(resource, merger);

            return miniCache.GetAllowed(role);
        }

        void IManualAuth<Type, TypeAllowedAndConditions>.SetAllowed(Lite<RoleEntity> role, Type key, TypeAllowedAndConditions allowed)
        {
            TypeEntity resource = TypeLogic.TypeToEntity.GetOrThrow(key);

            ManualResourceCache miniCache = new ManualResourceCache(resource, merger);

            if (miniCache.GetAllowed(role).Equals(allowed))
                return;

            IQueryable<RuleTypeEntity> query = Database.Query<RuleTypeEntity>().Where(a => a.Resource == resource && a.Role == role);
            if (miniCache.GetAllowedBase(role).Equals(allowed))
            {
                if (query.UnsafeDelete() == 0)
                    throw new InvalidOperationException("Inconsistency in the data");
            }
            else
            {
                query.UnsafeDelete();

                allowed.ToRuleType(role, resource).Save();
            }
        }

        public class ManualResourceCache
        {
            readonly Dictionary<Lite<RoleEntity>, TypeAllowedAndConditions> rules;

            readonly IMerger<Type, TypeAllowedAndConditions> merger;

            readonly TypeEntity resource;

            public ManualResourceCache(TypeEntity resource, IMerger<Type, TypeAllowedAndConditions> merger)
            {
                this.resource = resource;

                var list = Database.Query<RuleTypeEntity>().Where(r => r.Resource == resource || r.Resource == null).ToList();

                rules = list.Where(a => a.Resource != null).ToDictionary(a => a.Role, a => a.ToTypeAllowedAndConditions());

                this.merger = merger;
            }

            public TypeAllowedAndConditions GetAllowed(Lite<RoleEntity> role)
            {
                if (rules.TryGetValue(role, out var result))
                    return result;

                return GetAllowedBase(role);
            }

            public TypeAllowedAndConditions GetAllowedBase(Lite<RoleEntity> role)
            {
                IEnumerable<Lite<RoleEntity>> related = AuthLogic.RelatedTo(role);

                return merger.Merge(resource.ToType(), role, related.Select(r => KeyValuePair.Create(r, GetAllowed(r))));
            }

        }

        Dictionary<Lite<RoleEntity>, RoleAllowedCache> NewCache()
        {
            using (AuthLogic.Disable())
            using (new EntityCache(EntityCacheType.ForceNewSealed))
            {
                List<Lite<RoleEntity>> roles = AuthLogic.RolesInOrder().ToList();

                var rules = Database.Query<RuleTypeEntity>().ToList();

                var errors = GraphExplorer.FullIntegrityCheck(GraphExplorer.FromRoots(rules));
                if (errors != null)
                    throw new IntegrityCheckException(errors);

                Dictionary<Lite<RoleEntity>, Dictionary<Type, TypeAllowedAndConditions>> realRules =
                   rules.AgGroupToDictionary(ru => ru.Role, gr => gr
                          .SelectCatch(ru => KeyValuePair.Create(TypeLogic.EntityToType.GetOrThrow(ru.Resource), ru.ToTypeAllowedAndConditions()))
                          .ToDictionaryEx());

                Dictionary<Lite<RoleEntity>, RoleAllowedCache> newRules = new Dictionary<Lite<RoleEntity>, RoleAllowedCache>();
                foreach (var role in roles)
                {
                    var related = AuthLogic.RelatedTo(role);

                    newRules.Add(role, new RoleAllowedCache(role, merger, related.Select(r => newRules.GetOrThrow(r)).ToList(), realRules.TryGetC(role)));
                }

                return newRules;
            }
        }

        internal void GetRules(BaseRulePack<TypeAllowedRule> rules, IEnumerable<TypeEntity> resources)
        {
            RoleAllowedCache cache = runtimeRules.Value.GetOrThrow(rules.Role);

            rules.MergeStrategy = AuthLogic.GetMergeStrategy(rules.Role);
            rules.SubRoles = AuthLogic.RelatedTo(rules.Role).ToMList();
            rules.Rules = (from r in resources
                           let type = TypeLogic.EntityToType.GetOrThrow(r)
                           select new TypeAllowedRule()
                           {
                               Resource = r,
                               AllowedBase = cache.GetAllowedBase(type),
                               Allowed = cache.GetAllowed(type),
                               AvailableConditions = TypeConditionLogic.ConditionsFor(type).ToReadOnly()
                           }).ToMList();
        }

        internal void SetRules(BaseRulePack<TypeAllowedRule> rules)
        {
            using (AuthLogic.Disable())
            {
                var current = Database.Query<RuleTypeEntity>().Where(r => r.Role == rules.Role && r.Resource != null).ToDictionary(a => a.Resource);
                var should = rules.Rules.Where(a => a.Overriden).ToDictionary(r => r.Resource);

                Synchronizer.Synchronize(should, current,
                    (type, ar) => ar.Allowed.ToRuleType(rules.Role, type).Save(),
                    (type, pr) => pr.Delete(),
                    (type, ar, pr) =>
                    {
                        pr.Allowed = ar.Allowed.Fallback.Value;

                        var shouldConditions = ar.Allowed.Conditions.Select(a => new RuleTypeConditionEmbedded
                        {
                            Allowed = a.Allowed,
                            Condition = a.TypeCondition,
                        }).ToMList();

                        if (!pr.Conditions.SequenceEqual(shouldConditions))
                            pr.Conditions = shouldConditions;

                        if (pr.IsGraphModified)
                            pr.Save();
                    });
            }
        }

        internal TypeAllowedAndConditions GetAllowed(Lite<RoleEntity> role, Type key)
        {
            return runtimeRules.Value.GetOrThrow(role).GetAllowed(key);
        }

        internal TypeAllowedAndConditions GetAllowedBase(Lite<RoleEntity> role, Type key)
        {
            return runtimeRules.Value.GetOrThrow(role).GetAllowedBase(key);
        }

        internal DefaultDictionary<Type, TypeAllowedAndConditions> GetDefaultDictionary()
        {
            return runtimeRules.Value.GetOrThrow(RoleEntity.Current).DefaultDictionary();
        }

        public class RoleAllowedCache
        {
            readonly IMerger<Type, TypeAllowedAndConditions> merger;
            readonly DefaultDictionary<Type, TypeAllowedAndConditions> rules;
            readonly List<RoleAllowedCache> baseCaches;
            readonly Lite<RoleEntity> role;

            public RoleAllowedCache(Lite<RoleEntity> role, IMerger<Type, TypeAllowedAndConditions> merger, List<RoleAllowedCache> baseCaches, Dictionary<Type, TypeAllowedAndConditions>? newValues)
            {
                this.role = role;

                this.merger = merger;

                this.baseCaches = baseCaches;

                Func<Type, TypeAllowedAndConditions> defaultAllowed = merger.MergeDefault(role);

                Func<Type, TypeAllowedAndConditions> baseAllowed = k => merger.Merge(k, role, baseCaches.Select(b => KeyValuePair.Create(b.role, b.GetAllowed(k))));


                var keys = baseCaches
                    .Where(b => b.rules.OverrideDictionary != null)
                    .SelectMany(a => a.rules.OverrideDictionary!.Keys)
                    .ToHashSet();

                Dictionary<Type, TypeAllowedAndConditions>? tmpRules = keys.ToDictionary(k => k, baseAllowed);
                if (newValues != null)
                    tmpRules.SetRange(newValues);

                tmpRules = Simplify(tmpRules, defaultAllowed, baseAllowed);

                rules = new DefaultDictionary<Type, TypeAllowedAndConditions>(defaultAllowed, tmpRules);
            }

            internal static Dictionary<Type, TypeAllowedAndConditions>? Simplify(Dictionary<Type, TypeAllowedAndConditions> dictionary,
                Func<Type, TypeAllowedAndConditions> defaultAllowed,
                Func<Type, TypeAllowedAndConditions> baseAllowed)
            {
                if (dictionary == null || dictionary.Count == 0)
                    return null;

                dictionary.RemoveRange(dictionary.Where(p =>
                 p.Value.Equals(defaultAllowed(p.Key)) &&
                 p.Value.Equals(baseAllowed(p.Key))).Select(p => p.Key).ToList());

                if (dictionary.Count == 0)
                    return null;

                return dictionary;
            }

            public TypeAllowedAndConditions GetAllowed(Type k)
            {
                return rules.GetAllowed(k);
            }

            public TypeAllowedAndConditions GetAllowedBase(Type k)
            {
                return merger.Merge(k, role, baseCaches.Select(b => KeyValuePair.Create(b.role, b.GetAllowed(k))));
            }

            internal DefaultDictionary<Type, TypeAllowedAndConditions> DefaultDictionary()
            {
                return this.rules;
            }
        }

        internal XElement ExportXml(List<Type>? allTypes)
        {
            var rules = runtimeRules.Value;

            return new XElement("Types",
                (from r in AuthLogic.RolesInOrder()
                 let rac = rules.GetOrThrow(r)
                 select new XElement("Role",
                     new XAttribute("Name", r.ToString()),
                         from k in allTypes ?? (rac.DefaultDictionary().OverrideDictionary?.Keys).EmptyIfNull()
                         let allowedBase = rac.GetAllowedBase(k)
                         let allowed = rac.GetAllowed(k)
                         where allTypes != null || !allowed.Equals(allowedBase)
                         let resource = TypeLogic.GetCleanName(k)
                         orderby resource
                         select new XElement("Type",
                            new XAttribute("Resource", resource),
                            new XAttribute("Allowed", allowed.Fallback.ToString()),
                            from c in allowed.Conditions
                            select new XElement("Condition",
                                new XAttribute("Name", c.TypeCondition.Key),
                                new XAttribute("Allowed", c.Allowed.ToString()))
                         )
                     )
                ));
        }

        internal static readonly string typeReplacementKey = "AuthRules:" + typeof(TypeEntity).Name;
        internal static readonly string typeConditionReplacementKey = "AuthRules:" + typeof(TypeConditionSymbol).Name;

        internal SqlPreCommand? ImportXml(XElement element, Dictionary<string, Lite<RoleEntity>> roles, Replacements replacements)
        {
            var current = Database.RetrieveAll<RuleTypeEntity>().GroupToDictionary(a => a.Role);
            var xRoles = (element.Element("Types")?.Elements("Role")).EmptyIfNull();
            var should = xRoles.ToDictionary(x => roles.GetOrThrow(x.Attribute("Name").Value));

            Table table = Schema.Current.Table(typeof(RuleTypeEntity));

            replacements.AskForReplacements(
                xRoles.SelectMany(x => x.Elements("Type")).Select(x => x.Attribute("Resource").Value).ToHashSet(),
                TypeLogic.NameToType.Where(a => !a.Value.IsEnumEntity()).Select(a => a.Key).ToHashSet(), typeReplacementKey);

            replacements.AskForReplacements(
                xRoles.SelectMany(x => x.Elements("Type")).SelectMany(t => t.Elements("Condition")).Select(x => x.Attribute("Name").Value).ToHashSet(),
                SymbolLogic<TypeConditionSymbol>.AllUniqueKeys(),
                typeConditionReplacementKey);

            Func<string, TypeEntity?> getResource = s =>
            {
                Type? type = TypeLogic.NameToType.TryGetC(replacements.Apply(typeReplacementKey, s));

                if (type == null)
                    return null;

                return TypeLogic.TypeToEntity.GetOrThrow(type);
            };


            return Synchronizer.SynchronizeScript(Spacing.Double, should, current,
                createNew: (role, x) =>
                {
                    var dic = (from xr in x.Elements("Type")
                               let t = getResource(xr.Attribute("Resource").Value)
                               where t != null
                               select KeyValuePair.Create(t, new
                               {
                                   Allowed = xr.Attribute("Allowed").Value.ToEnum<TypeAllowed>(),
                                   Condition = Conditions(xr, replacements)
                               })).ToDictionaryEx("Type rules for {0}".FormatWith(role));

                    SqlPreCommand? restSql = dic.Select(kvp => table.InsertSqlSync(new RuleTypeEntity
                    {
                        Resource = kvp.Key,
                        Role = role,
                        Allowed = kvp.Value.Allowed,
                        Conditions = kvp.Value.Condition! /*CSBUG*/
                    }, comment: Comment(role, kvp.Key, kvp.Value.Allowed))).Combine(Spacing.Simple)?.Do(p => p.GoBefore = true);

                    return restSql;
                },
                removeOld: (role, list) => list.Select(rt => table.DeleteSqlSync(rt, null)).Combine(Spacing.Simple)?.Do(p => p.GoBefore = true),
                mergeBoth: (role, x, list) =>
                {
                    var dic = (from xr in x.Elements("Type")
                               let t = getResource(xr.Attribute("Resource").Value)
                               where t != null && !t.ToType().IsEnumEntity()
                               select KeyValuePair.Create(t, xr)).ToDictionaryEx("Type rules for {0}".FormatWith(role));

                    SqlPreCommand? restSql = Synchronizer.SynchronizeScript(
                        Spacing.Simple,
                        dic,
                        list.Where(a => a.Resource != null).ToDictionary(a => a.Resource),
                        createNew: (r, xr) =>
                        {
                            var a = xr.Attribute("Allowed").Value.ToEnum<TypeAllowed>();
                            var conditions = Conditions(xr, replacements);

                            return table.InsertSqlSync(new RuleTypeEntity { Resource = r, Role = role, Allowed = a, Conditions = conditions }, comment: Comment(role, r, a));
                        },
                        removeOld: (r, rt) => table.DeleteSqlSync(rt, null, Comment(role, r, rt.Allowed)),
                        mergeBoth: (r, xr, pr) =>
                        {
                            var oldA = pr.Allowed;
                            pr.Allowed = xr.Attribute("Allowed").Value.ToEnum<TypeAllowed>();
                            var conditions = Conditions(xr, replacements);

                            if (!pr.Conditions.SequenceEqual(conditions))
                                pr.Conditions = conditions;

                            return table.UpdateSqlSync(pr, null, comment: Comment(role, r, oldA, pr.Allowed));
                        })?.Do(p => p.GoBefore = true);

                    return restSql;
                });
        }

        private static MList<RuleTypeConditionEmbedded> Conditions(XElement xr, Replacements replacements)
        {
            return (from xc in xr.Elements("Condition")
                    let cn = SymbolLogic<TypeConditionSymbol>.TryToSymbol(replacements.Apply(typeConditionReplacementKey, xc.Attribute("Name").Value))
                    where cn != null
                    select new RuleTypeConditionEmbedded
                    {
                        Condition = cn,
                        Allowed = xc.Attribute("Allowed").Value.ToEnum<TypeAllowed>()
                    }).ToMList();
        }


        internal static string Comment(Lite<RoleEntity> role, TypeEntity resource, TypeAllowed allowed)
        {
            return "{0} {1} for {2} ({3})".FormatWith(typeof(TypeEntity).NiceName(), resource.ToString(), role, allowed);
        }

        internal static string Comment(Lite<RoleEntity> role, TypeEntity resource, TypeAllowed from, TypeAllowed to)
        {
            return "{0} {1} for {2} ({3} -> {4})".FormatWith(typeof(TypeEntity).NiceName(), resource.ToString(), role, from, to);
        }

    }


}
