using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities;
using Signum.Engine.Maps;
using Signum.Engine.Authorization;
using Signum.Engine;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace Signum.Entities.Authorization
{
    public interface IMerger<K, A>
    {
        A Merge(K key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, A>> baseValues);
        Func<K, A> MergeDefault(Lite<RoleEntity> role);
    }

    public interface IManualAuth<K, A>
    {
        A GetAllowed(Lite<RoleEntity> role, K key);
        void SetAllowed(Lite<RoleEntity> role, K key, A allowed);
    }

    class Coercer<A, K>
    {
        public static readonly Coercer<A, K> None = new Coercer<A, K>();

        public virtual Func<Lite<RoleEntity>, A, A> GetCoerceValueManual(K key) { return (role, allowed) => allowed; }
        public virtual Func<K, A, A> GetCoerceValue(Lite<RoleEntity> role) { return (key, allowed) => allowed; }
    }

    class AuthCache<RT, AR, R, K, A> : IManualAuth<K, A>
        where RT : RuleEntity<R, A>, new()
        where AR : AllowedRule<R, A>, new()
        where A : notnull
        where R : class
        where K : notnull
    {
        readonly ResetLazy<Dictionary<Lite<RoleEntity>, RoleAllowedCache>> runtimeRules;

        Func<R, K> ToKey;
        Func<K, R> ToEntity;
        Expression<Func<R, R, bool>> IsEquals;
        IMerger<K, A> merger;
        Coercer<A, K> coercer;

        public AuthCache(SchemaBuilder sb, Func<R, K> toKey, Func<K, R> toEntity, 
            Expression<Func<R, R, bool>> isEquals, IMerger<K, A> merger, bool invalidateWithTypes, Coercer<A, K>? coercer = null)
        {
            this.ToKey = toKey;
            this.ToEntity = toEntity;
            this.merger = merger;
            this.IsEquals = isEquals;
            this.coercer = coercer ?? Coercer<A, K>.None;

            runtimeRules = sb.GlobalLazy(this.NewCache,
                invalidateWithTypes ?
                new InvalidateWith(typeof(RT), typeof(RoleEntity), typeof(RuleTypeEntity)) :
                new InvalidateWith(typeof(RT), typeof(RoleEntity)), AuthLogic.NotifyRulesChanged);
        }

        A IManualAuth<K, A>.GetAllowed(Lite<RoleEntity> role, K key)
        {
            R resource = ToEntity(key);

            ManualResourceCache miniCache = new ManualResourceCache(key, resource, IsEquals, merger, coercer.GetCoerceValueManual(key));

            return miniCache.GetAllowed(role);
        }

        void IManualAuth<K, A>.SetAllowed(Lite<RoleEntity> role, K key, A allowed)
        {
            R resource = ToEntity(key);

            var keyCoercer = coercer.GetCoerceValueManual(key);

            ManualResourceCache miniCache = new ManualResourceCache(key, resource, IsEquals, merger, keyCoercer);

            allowed = keyCoercer(role, allowed);

            if (miniCache.GetAllowed(role).Equals(allowed))
                return;

            IQueryable<RT> query = Database.Query<RT>().Where(a => IsEquals.Evaluate(a.Resource, resource) && a.Role == role);
            if (miniCache.GetAllowedBase(role).Equals(allowed))
            {
                if (query.UnsafeDelete() == 0)
                    throw new InvalidOperationException("Inconsistency in the data");
            }
            else
            {
                if (query.UnsafeUpdate().Set(a => a.Allowed, a => allowed).Execute() == 0)
                    new RT
                    {
                        Role = role,
                        Resource = resource,
                        Allowed = allowed,
                    }.Save();
            }
        }

        public class ManualResourceCache
        {
            readonly Dictionary<Lite<RoleEntity>, A> specificRules;

            readonly IMerger<K, A> merger;

            readonly Func<Lite<RoleEntity>, A, A> coercer;

            readonly K key;

            public ManualResourceCache(K key, R resource, Expression<Func<R, R, bool>> isEquals,  IMerger<K, A> merger, Func<Lite<RoleEntity>, A, A> coercer)
            {
                this.key = key;

                var list = (from r in Database.Query<RT>()
                            where isEquals.Evaluate(r.Resource, resource)
                            select new { r.Role, r.Allowed }).ToList();

                specificRules = list.ToDictionary(a => a.Role!, a => a.Allowed); /*CSBUG*/

                this.coercer = coercer;
                this.merger = merger;
            }

            public A GetAllowed(Lite<RoleEntity> role)
            {
                if (specificRules.TryGetValue(role, out A result))
                    return coercer(role, result);

                return GetAllowedBase(role);
            }

            public A GetAllowedBase(Lite<RoleEntity> role)
            {
                var result = merger.Merge(key, role, AuthLogic.RelatedTo(role).Select(r => KeyValuePair.Create(r, GetAllowed(r))));

                return coercer(role, result);
            }
        }

        Dictionary<Lite<RoleEntity>, RoleAllowedCache> NewCache()
        {
            List<Lite<RoleEntity>> roles = AuthLogic.RolesInOrder().ToList();

            Dictionary<Lite<RoleEntity>, Dictionary<K, A>> realRules =
               Database.Query<RT>()
               .Select(a => new { a.Role, a.Allowed, a.Resource })
                  .AgGroupToDictionary(ru => ru.Role!, gr => gr
                    .SelectCatch(ru => KeyValuePair.Create(ToKey(ru.Resource!), ru.Allowed))
                    .ToDictionaryEx());

            Dictionary<Lite<RoleEntity>, RoleAllowedCache> newRules = new Dictionary<Lite<RoleEntity>, RoleAllowedCache>();
            foreach (var role in roles)
            {
                var related = AuthLogic.RelatedTo(role);

                newRules.Add(role, new RoleAllowedCache(
                    role,
                    merger,
                    related.Select(r => newRules.GetOrThrow(r)).ToList(),
                    realRules.TryGetC(role),
                    coercer.GetCoerceValue(role)));
            }

            return newRules;
        }

        internal void GetRules(BaseRulePack<AR> rules, IEnumerable<R> resources)
        {
            RoleAllowedCache cache = runtimeRules.Value.GetOrThrow(rules.Role);

            rules.MergeStrategy = AuthLogic.GetMergeStrategy(rules.Role);
            rules.SubRoles = AuthLogic.RelatedTo(rules.Role).ToMList();
            rules.Rules = (from r in resources
                           let k = ToKey(r)
                           select new AR()
                           {
                               Resource = r,
                               AllowedBase = cache.GetAllowedBase(k),
                               Allowed = cache.GetAllowed(k)
                           }).ToMList();
        }

        internal void SetRules(BaseRulePack<AR> rules, Expression<Func<R, bool>> filterResources)
        {
            using (AuthLogic.Disable())
            {
                var current = Database.Query<RT>().Where(r => r.Role == rules.Role && filterResources.Evaluate(r.Resource)).ToDictionary(a => a.Resource);
                var should = rules.Rules.Where(a => a.Overriden).ToDictionary(r => r.Resource);

                Synchronizer.Synchronize(should, current,
                    (p, ar) => new RT { Resource = p, Role = rules.Role, Allowed = ar.Allowed }.Save(),
                    (p, pr) => pr.Delete(),
                    (p, ar, pr) =>
                    {
                        pr.Allowed = ar.Allowed;
                        if (pr.IsGraphModified)
                            pr.Save();
                    });
            }
        }

        internal A GetAllowed(Lite<RoleEntity> role, K key)
        {
            return runtimeRules.Value.GetOrThrow(role).GetAllowed(key);
        }

        internal DefaultDictionary<K, A> GetDefaultDictionary()
        {
            return runtimeRules.Value.GetOrThrow(RoleEntity.Current).DefaultDictionary();
        }

        public class RoleAllowedCache
        {
            readonly Lite<RoleEntity> role;
            readonly IMerger<K, A> merger;
            readonly Func<K, A, A> coercer;

            readonly DefaultDictionary<K, A> rules;
            readonly List<RoleAllowedCache> baseCaches;


            public RoleAllowedCache(Lite<RoleEntity> role, IMerger<K, A> merger, List<RoleAllowedCache> baseCaches, Dictionary<K, A>? newValues, Func<K, A, A> coercer)
            {
                this.role = role;

                this.merger = merger;
                this.coercer = coercer;

                this.baseCaches = baseCaches;

                Func<K, A> defaultAllowed = merger.MergeDefault(role);

                Func<K, A> baseAllowed = k => merger.Merge(k, role, baseCaches.Select(b => KeyValuePair.Create(b.role, b.GetAllowed(k))));

                var keys = baseCaches
                    .Where(b => b.rules.OverrideDictionary != null)
                    .SelectMany(a => a.rules.OverrideDictionary!.Keys)
                    .ToHashSet();

                Dictionary<K, A>? tmpRules = keys.ToDictionary(k => k, baseAllowed);
                if (newValues != null)
                    tmpRules.SetRange(newValues);

                tmpRules = Simplify(tmpRules, defaultAllowed, baseAllowed);

                rules = new DefaultDictionary<K, A>(defaultAllowed, tmpRules);
            }

            internal Dictionary<K, A>? Simplify(Dictionary<K, A> dictionary, Func<K, A> defaultAllowed, Func<K, A> baseAllowed)
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

            public A GetAllowed(K key)
            {
                var raw = rules.GetAllowed(key);

                return coercer(key, raw);
            }

            public A GetAllowedBase(K key)
            {
                var raw = merger.Merge(key, role, baseCaches.Select(b => KeyValuePair.Create(b.role, b.GetAllowed(key))));

                return coercer(key, raw);
            }

            internal DefaultDictionary<K, A> DefaultDictionary()
            {
                return this.rules;
            }
        }

        internal XElement ExportXml(XName rootName, XName elementName, Func<K, string> resourceToString, Func<A, string> allowedToString, List<K>? allKeys)
        {
            var rules = runtimeRules.Value;

            return new XElement(rootName,
                (from r in AuthLogic.RolesInOrder()
                 let rac = rules.GetOrThrow(r)
                 select new XElement("Role",
                     new XAttribute("Name", r.ToString()),
                         from k in allKeys ?? (rac.DefaultDictionary().OverrideDictionary?.Keys).EmptyIfNull()
                         let allowedBase = rac.GetAllowedBase(k)
                         let allowed = rac.GetAllowed(k)
                         where allKeys != null || !allowed.Equals(allowedBase)
                         let resource = resourceToString(k)
                         orderby resource
                         select new XElement(elementName,
                            new XAttribute("Resource", resource),
                            new XAttribute("Allowed", allowedToString(allowed)))
                )));
        }


        internal SqlPreCommand? ImportXml(XElement element, XName rootName, XName elementName, Dictionary<string, Lite<RoleEntity>> roles,
            Func<string, R?> toResource, Func<string, A> parseAllowed)
        {
            var current = Database.RetrieveAll<RT>().GroupToDictionary(a => a.Role);
            var xRoles = (element.Element(rootName)?.Elements("Role")).EmptyIfNull();
            var should = xRoles.ToDictionary(x => roles.GetOrThrow(x.Attribute("Name").Value));

            Table table = Schema.Current.Table(typeof(RT));

            return Synchronizer.SynchronizeScript(Spacing.Double, should, current,
                createNew: (role, x) =>
                {
                    var dic = (from xr in x.Elements(elementName)
                               let r = toResource(xr.Attribute("Resource").Value)
                               where r != null
                               select KeyValuePair.Create(r, parseAllowed(xr.Attribute("Allowed").Value)))
                               .ToDictionaryEx("{0} rules for {1}".FormatWith(typeof(R).NiceName(), role));

                    SqlPreCommand? restSql = dic.Select(kvp => table.InsertSqlSync(new RT
                    {
                        Resource = kvp.Key,
                        Role = role,
                        Allowed = kvp.Value
                    }, comment: Comment(role, kvp.Key, kvp.Value))).Combine(Spacing.Simple)?.Do(p => p.GoBefore = true);

                    return restSql;
                },
                removeOld: (role, list) => list.Select(rt => table.DeleteSqlSync(rt, null)).Combine(Spacing.Simple)?.Do(p => p.GoBefore = true),
                mergeBoth: (role, x, list) =>
                {
                    var def = list.SingleOrDefaultEx(a => a.Resource == null);

                    var shouldResources = (from xr in x.Elements(elementName)
                               let r = toResource(xr.Attribute("Resource").Value)
                               where r != null
                               select KeyValuePair.Create(ToKey(r), xr))
                               .ToDictionaryEx("{0} rules for {1}".FormatWith(typeof(R).NiceName(), role));

                    var currentResources = list.Where(a => a.Resource != null).ToDictionary(a => ToKey(a.Resource));

                    SqlPreCommand? restSql = Synchronizer.SynchronizeScript(Spacing.Simple, shouldResources, currentResources,
                        (r, xr) =>
                        {
                            var a = parseAllowed(xr.Attribute("Allowed").Value);
                            return table.InsertSqlSync(new RT { Resource = ToEntity(r), Role = role, Allowed = a }, comment: Comment(role, ToEntity(r), a));
                        },
                        (r, rt) => table.DeleteSqlSync(rt, null, Comment(role, ToEntity(r), rt.Allowed)),
                        (r, xr, rt) =>
                        {
                            var oldA = rt.Allowed;
                            rt.Allowed = parseAllowed(xr.Attribute("Allowed").Value);
                            return table.UpdateSqlSync(rt, null, comment: Comment(role, ToEntity(r), oldA, rt.Allowed));
                        })?.Do(p => p.GoBefore = true);

                    return restSql;
                });
        }


        internal static string Comment(Lite<RoleEntity> role, R resource, A allowed)
        {
            return "{0} {1} for {2} ({3})".FormatWith(typeof(R).NiceName(), resource.ToString(), role, allowed);
        }

        internal static string Comment(Lite<RoleEntity> role, R resource, A from, A to)
        {
            return "{0} {1} for {2} ({3} -> {4})".FormatWith(typeof(R).NiceName(), resource.ToString(), role, from, to);
        }
    }
}
