using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Engine.Maps;
using Signum.Engine.Authorization;
using Signum.Engine;
using System.Windows;
using System.Linq.Expressions;
using System.Xml.Linq;
using System.IO;
using System.Data.SqlClient;
using System.Threading;
using Signum.Engine.Basics;
using Signum.Entities.Basics;
using System.Reflection;
using System.Data.Common;
using Signum.Engine.Cache;
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
                new InvalidateWith(typeof(RuleTypeEntity), typeof(RoleEntity)));

            sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(AuthCache_PreDeleteSqlSync);

            Validator.PropertyValidator((RuleTypeEntity r) => r.Conditions).StaticPropertyValidation += TypeAuthCache_StaticPropertyValidation;
        }

        string TypeAuthCache_StaticPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
        {
            RuleTypeEntity rt = (RuleTypeEntity)sender;
            if (rt.Resource == null)
            {
                if (rt.Conditions.Any())
                    return "Default {0} should not have conditions".FormatWith(typeof(RuleTypeEntity).NiceName());

                return null;
            }

            Type type = TypeLogic.DnToType[rt.Resource];
            var conditions = rt.Conditions.Where(a => !TypeConditionLogic.IsDefined(type, a.Condition));

            if (conditions.IsEmpty())
                return null;

            return "Type {0} has no definitions for the conditions: {1}".FormatWith(type.Name, conditions.CommaAnd(a => a.Condition.Key));
        }

        static SqlPreCommand AuthCache_PreDeleteSqlSync(Entity arg)
        {
            TypeEntity type = (TypeEntity)arg;

            var command = Administrator.UnsafeDeletePreCommand(Database.Query<RuleTypeEntity>().Where(a => a.Resource == type));

            return command;
        }

        TypeAllowedAndConditions IManualAuth<Type, TypeAllowedAndConditions>.GetAllowed(Lite<RoleEntity> role, Type key)
        {
            TypeEntity resource = TypeLogic.TypeToEntity[key];

            ManualResourceCache miniCache = new ManualResourceCache(resource, merger);

            return miniCache.GetAllowed(role);
        }

        void IManualAuth<Type, TypeAllowedAndConditions>.SetAllowed(Lite<RoleEntity> role, Type key, TypeAllowedAndConditions allowed)
        {
            TypeEntity resource = TypeLogic.TypeToEntity[key];

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
                TypeAllowedAndConditions result;
                if (rules.TryGetValue(role, out result))
                    return result;

                return GetAllowedBase(role);
            }

            public TypeAllowedAndConditions GetAllowedBase(Lite<RoleEntity> role)
            {
                IEnumerable<Lite<RoleEntity>> related = AuthLogic.RelatedTo(role);

                return merger.Merge(resource.ToType(), role, related.Select(r => KVP.Create(r, GetAllowed(r))));
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
                          .ToDictionary(ru => TypeLogic.DnToType[ru.Resource], ru => ru.ToTypeAllowedAndConditions()));

                Dictionary<Lite<RoleEntity>, RoleAllowedCache> newRules = new Dictionary<Lite<RoleEntity>, RoleAllowedCache>();
                foreach (var role in roles)
                {
                    var related = AuthLogic.RelatedTo(role);

                    newRules.Add(role, new RoleAllowedCache(role, merger, related.Select(r => newRules[r]).ToList(), realRules.TryGetC(role)));
                }

                return newRules;
            }
        }

        internal void GetRules(BaseRulePack<TypeAllowedRule> rules, IEnumerable<TypeEntity> resources)
        {
            RoleAllowedCache cache = runtimeRules.Value[rules.Role];

            rules.MergeStrategy = AuthLogic.GetMergeStrategy(rules.Role);
            rules.SubRoles = AuthLogic.RelatedTo(rules.Role).ToMList();
            rules.Rules = (from r in resources
                           let type = TypeLogic.DnToType[r]
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

                        var shouldConditions = ar.Allowed.Conditions.Select(a => new RuleTypeConditionEntity
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
            return runtimeRules.Value[role].GetAllowed(key);
        }

        internal TypeAllowedAndConditions GetAllowedBase(Lite<RoleEntity> role, Type key)
        {
            return runtimeRules.Value[role].GetAllowedBase(key);
        }

        internal DefaultDictionary<Type, TypeAllowedAndConditions> GetDefaultDictionary()
        {
            return runtimeRules.Value[RoleEntity.Current.ToLite()].DefaultDictionary();
        }

        public class RoleAllowedCache
        {
            readonly IMerger<Type, TypeAllowedAndConditions> merger;
            readonly DefaultDictionary<Type, TypeAllowedAndConditions> rules;
            readonly List<RoleAllowedCache> baseCaches;
            readonly Lite<RoleEntity> role;

            public RoleAllowedCache(Lite<RoleEntity> role, IMerger<Type, TypeAllowedAndConditions> merger, List<RoleAllowedCache> baseCaches, Dictionary<Type, TypeAllowedAndConditions> newValues)
            {
                this.role = role;

                this.merger = merger;

                this.baseCaches = baseCaches;

                Func<Type, TypeAllowedAndConditions> defaultAllowed = merger.MergeDefault(role);

                Func<Type, TypeAllowedAndConditions> baseAllowed = k => merger.Merge(k, role, baseCaches.Select(b => KVP.Create(b.role, b.GetAllowed(k))));


                var keys = baseCaches
                    .Where(b => b.rules.OverrideDictionary != null)
                    .SelectMany(a => a.rules.OverrideDictionary.Keys)
                    .ToHashSet();

                Dictionary<Type, TypeAllowedAndConditions> tmpRules = keys.ToDictionary(k => k, baseAllowed);
                if (newValues != null)
                    tmpRules.SetRange(newValues);

                tmpRules = Simplify(tmpRules, defaultAllowed, baseAllowed);

                rules = new DefaultDictionary<Type, TypeAllowedAndConditions>(defaultAllowed, tmpRules);
            }

            internal static Dictionary<Type, TypeAllowedAndConditions> Simplify(Dictionary<Type, TypeAllowedAndConditions> dictionary,
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
                return merger.Merge(k, role, baseCaches.Select(b => KVP.Create(b.role, b.GetAllowed(k))));
            }

            internal DefaultDictionary<Type, TypeAllowedAndConditions> DefaultDictionary()
            {
                return this.rules;
            }
        }

        internal XElement ExportXml(List<Type> allTypes)
        {
            var rules = runtimeRules.Value;

            return new XElement("Types",
                (from r in AuthLogic.RolesInOrder()
                 let rac = rules[r]
                 select new XElement("Role",
                     new XAttribute("Name", r.ToString()),
                         from k in allTypes ?? rac.DefaultDictionary().OverrideDictionary.Try(dic => dic.Keys).EmptyIfNull()
                         let allowedBase = rac.GetAllowedBase(k)
                         let allowed = rac.GetAllowed(k)
                         where allTypes != null || !allowed.Equals(allowedBase)
                         let resource = TypeLogic.GetCleanName(k)
                         orderby resource
                         select new XElement("Type",
                            new XAttribute("Resource", resource),
                            new XAttribute("Allowed", allowed.Fallback.ToString()),
                            from c in allowed.Conditions
                            let conditionName = c.TypeCondition.Key
                            orderby conditionName
                            select new XElement("Condition",
                                new XAttribute("Name", conditionName),
                                new XAttribute("Allowed", c.Allowed.ToString()))
                         )
                     )
                ));
        }

        internal static readonly string typeReplacementKey = "AuthRules:" + typeof(TypeEntity).Name;
        internal static readonly string typeConditionReplacementKey = "AuthRules:" + typeof(TypeConditionSymbol).Name;

        internal SqlPreCommand ImportXml(XElement element, Dictionary<string, Lite<RoleEntity>> roles, Replacements replacements)
        {
            var current = Database.RetrieveAll<RuleTypeEntity>().GroupToDictionary(a => a.Role);
            var xRoles = element.Element("Types").Try(t => t.Elements("Role")).EmptyIfNull();
            var should = xRoles.ToDictionary(x => roles[x.Attribute("Name").Value]);

            Table table = Schema.Current.Table(typeof(RuleTypeEntity));

            replacements.AskForReplacements(
                xRoles.SelectMany(x => x.Elements("Type")).Select(x => x.Attribute("Resource").Value).ToHashSet(),
                TypeLogic.NameToType.Where(a => !a.Value.IsEnumEntity()).Select(a => a.Key).ToHashSet(), typeReplacementKey);

            replacements.AskForReplacements(
                xRoles.SelectMany(x => x.Elements("Type")).SelectMany(t => t.Elements("Condition")).Select(x => x.Attribute("Name").Value).ToHashSet(),
                SymbolLogic<TypeConditionSymbol>.AllUniqueKeys(),
                typeConditionReplacementKey);

            Func<string, TypeEntity> getResource = s =>
            {
                Type type = TypeLogic.NameToType.TryGetC(replacements.Apply(typeReplacementKey, s));

                if (type == null)
                    return null;

                return TypeLogic.TypeToEntity[type];
            };


            return Synchronizer.SynchronizeScript(should, current,
                (role, x) =>
                {
                    var dic = (from xr in x.Elements("Type")
                               let t = getResource(xr.Attribute("Resource").Value)
                               where t != null
                               select KVP.Create(t, new
                               {
                                   Allowed = xr.Attribute("Allowed").Value.ToEnum<TypeAllowed>(),
                                   Condition = Conditions(xr, replacements)
                               })).ToDictionary("Type rules for {0}".FormatWith(role));

                    SqlPreCommand restSql = dic.Select(kvp => table.InsertSqlSync(new RuleTypeEntity
                    {
                        Resource = kvp.Key,
                        Role = role,
                        Allowed = kvp.Value.Allowed,
                        Conditions = kvp.Value.Condition
                    }, comment: Comment(role, kvp.Key, kvp.Value.Allowed))).Combine(Spacing.Simple).TryDo(p => p.GoBefore = true);

                    return restSql;
                },
                (role, list) => list.Select(rt => table.DeleteSqlSync(rt)).Combine(Spacing.Simple).TryDo(p => p.GoBefore = true),
                (role, x, list) =>
                {
                    var dic = (from xr in x.Elements("Type")
                               let t = getResource(xr.Attribute("Resource").Value)
                               where t != null && !t.ToType().IsEnumEntity()
                               select KVP.Create(t, xr)).ToDictionary("Type rules for {0}".FormatWith(role));

                    SqlPreCommand restSql = Synchronizer.SynchronizeScript(
                        dic,
                        list.Where(a => a.Resource != null).ToDictionary(a => a.Resource),
                        (r, xr) =>
                        {
                            var a = xr.Attribute("Allowed").Value.ToEnum<TypeAllowed>();
                            var conditions = Conditions(xr, replacements);

                            return table.InsertSqlSync(new RuleTypeEntity { Resource = r, Role = role, Allowed = a, Conditions = conditions }, comment: Comment(role, r, a));
                        },
                        (r, rt) => table.DeleteSqlSync(rt, Comment(role, r, rt.Allowed)),
                        (r, xr, pr) =>
                        {
                            var oldA = pr.Allowed;
                            pr.Allowed = xr.Attribute("Allowed").Value.ToEnum<TypeAllowed>();
                            var conditions = Conditions(xr, replacements);

                            if (!pr.Conditions.SequenceEqual(conditions))
                                pr.Conditions = conditions;

                            return table.UpdateSqlSync(pr, comment: Comment(role, r, oldA, pr.Allowed));
                        },
                        Spacing.Simple).TryDo(p => p.GoBefore = true);

                    return restSql;
                }, Spacing.Double);
        }

        private static MList<RuleTypeConditionEntity> Conditions(XElement xr, Replacements replacements)
        {
            return (from xc in xr.Elements("Condition")
                    let cn = SymbolLogic<TypeConditionSymbol>.TryToSymbol(replacements.Apply(typeConditionReplacementKey, xc.Attribute("Name").Value))
                    where cn != null
                    select new RuleTypeConditionEntity
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
