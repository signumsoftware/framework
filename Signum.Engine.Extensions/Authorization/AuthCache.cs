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
using System.Data.Common;
using Signum.Engine.Cache;

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
        where R : Entity
    {
        readonly ResetLazy<Dictionary<Lite<RoleEntity>, RoleAllowedCache>> runtimeRules;

        Func<R, K> ToKey;
        Func<K, R> ToEntity;
        IMerger<K, A> merger;
        Coercer<A, K> coercer;

        public AuthCache(SchemaBuilder sb, Func<R, K> toKey, Func<K, R> toEntity, IMerger<K, A> merger, bool invalidateWithTypes, Coercer<A, K> coercer = null)
        {
            this.ToKey = toKey;
            this.ToEntity = toEntity;
            this.merger = merger;
            this.coercer = coercer ?? Coercer<A, K>.None;

            sb.Include<RT>();

            runtimeRules = sb.GlobalLazy(this.NewCache,
                invalidateWithTypes ?
                new InvalidateWith(typeof(RT), typeof(RoleEntity), typeof(RuleTypeEntity)) :
                new InvalidateWith(typeof(RT), typeof(RoleEntity)));

            sb.AddUniqueIndex<RT>(rt => new { rt.Resource, rt.Role });

            sb.Schema.Table<R>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(AuthCache_PreDeleteSqlSync);
        }

        SqlPreCommand AuthCache_PreDeleteSqlSync(Entity arg)
        {
            var t = Schema.Current.Table<RT>();
            var f = (FieldReference)t.Fields["resource"].Field;

            var param = Connector.Current.ParameterBuilder.CreateReferenceParameter("@id", arg.Id, t.PrimaryKey);

            return new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2}".FormatWith(t.Name, f.Name.SqlEscape(), param.ParameterName), new List<DbParameter> { param });
        }

        A IManualAuth<K, A>.GetAllowed(Lite<RoleEntity> role, K key)
        {
            R resource = ToEntity(key);

            ManualResourceCache miniCache = new ManualResourceCache(key, resource, merger, coercer.GetCoerceValueManual(key));

            return miniCache.GetAllowed(role);
        }

        void IManualAuth<K, A>.SetAllowed(Lite<RoleEntity> role, K key, A allowed)
        {
            R resource = ToEntity(key);

            var keyCoercer = coercer.GetCoerceValueManual(key);

            ManualResourceCache miniCache = new ManualResourceCache(key, resource, merger, keyCoercer);

            allowed = keyCoercer(role, allowed);

            if (miniCache.GetAllowed(role).Equals(allowed))
                return;

            IQueryable<RT> query = Database.Query<RT>().Where(a => a.Resource == resource && a.Role == role);
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

            public ManualResourceCache(K key, R resource, IMerger<K, A> merger, Func<Lite<RoleEntity>, A, A> coercer)
            {
                this.key = key;

                var list = (from r in Database.Query<RT>()
                            where r.Resource == resource
                            select new { r.Role, r.Allowed }).ToList();

                specificRules = list.ToDictionary(a => a.Role, a => a.Allowed);

                this.coercer = coercer;
                this.merger = merger;
            }

            public A GetAllowed(Lite<RoleEntity> role)
            {
                A result;
                if (specificRules.TryGetValue(role, out result))
                    return coercer(role, result);

                return GetAllowedBase(role);
            }

            public A GetAllowedBase(Lite<RoleEntity> role)
            {
                var result = merger.Merge(key, role, AuthLogic.RelatedTo(role).Select(r => KVP.Create(r, GetAllowed(r))));

                return coercer(role, result);
            }
        }

        Dictionary<Lite<RoleEntity>, RoleAllowedCache> NewCache()
        {
            List<Lite<RoleEntity>> roles = AuthLogic.RolesInOrder().ToList();

            Dictionary<Lite<RoleEntity>, Dictionary<K, A>> realRules =
               Database.Query<RT>()
               .Select(a => new { a.Role, a.Allowed, a.Resource })
                  .AgGroupToDictionary(ru => ru.Role, gr => gr
                      .ToDictionary(ru => ToKey(ru.Resource), ru => ru.Allowed));

            Dictionary<Lite<RoleEntity>, RoleAllowedCache> newRules = new Dictionary<Lite<RoleEntity>, RoleAllowedCache>();
            foreach (var role in roles)
            {
                var related = AuthLogic.RelatedTo(role);

                newRules.Add(role, new RoleAllowedCache(
                    role,
                    merger,
                    related.Select(r => newRules[r]).ToList(),
                    realRules.TryGetC(role),
                    coercer.GetCoerceValue(role)));
            }

            return newRules;
        }

        internal void GetRules(BaseRulePack<AR> rules, IEnumerable<R> resources)
        {
            RoleAllowedCache cache = runtimeRules.Value[rules.Role];

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
            return runtimeRules.Value[role].GetAllowed(key);
        }

        internal DefaultDictionary<K, A> GetDefaultDictionary()
        {
            return runtimeRules.Value[RoleEntity.Current.ToLite()].DefaultDictionary();
        }

        public class RoleAllowedCache
        {
            readonly Lite<RoleEntity> role;
            readonly IMerger<K, A> merger;
            readonly Func<K, A, A> coercer;

            readonly DefaultDictionary<K, A> rules;
            readonly List<RoleAllowedCache> baseCaches;


            public RoleAllowedCache(Lite<RoleEntity> role, IMerger<K, A> merger, List<RoleAllowedCache> baseCaches, Dictionary<K, A> newValues, Func<K, A, A> coercer)
            {
                this.role = role;

                this.merger = merger;
                this.coercer = coercer;

                this.baseCaches = baseCaches;

                Func<K, A> defaultAllowed = merger.MergeDefault(role);

                Func<K, A> baseAllowed = k => merger.Merge(k, role, baseCaches.Select(b => KVP.Create(b.role, b.GetAllowed(k))));

                var keys = baseCaches
                    .Where(b => b.rules.OverrideDictionary != null)
                    .SelectMany(a => a.rules.OverrideDictionary.Keys)
                    .ToHashSet();

                Dictionary<K, A> tmpRules = keys.ToDictionary(k => k, baseAllowed);
                if (newValues != null)
                    tmpRules.SetRange(newValues);

                tmpRules = Simplify(tmpRules, defaultAllowed, baseAllowed);

                rules = new DefaultDictionary<K, A>(defaultAllowed, tmpRules);
            }

            internal Dictionary<K, A> Simplify(Dictionary<K, A> dictionary, Func<K, A> defaultAllowed, Func<K, A> baseAllowed)
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
                var raw = merger.Merge(key, role, baseCaches.Select(b => KVP.Create(b.role, b.GetAllowed(key))));

                return coercer(key, raw);
            }

            internal DefaultDictionary<K, A> DefaultDictionary()
            {
                return this.rules;
            }
        }

        internal XElement ExportXml(XName rootName, XName elementName, Func<K, string> resourceToString, Func<A, string> allowedToString, List<K> allKeys)
        {
            var rules = runtimeRules.Value;

            return new XElement(rootName,
                (from r in AuthLogic.RolesInOrder()
                 let rac = rules[r]
                 select new XElement("Role",
                     new XAttribute("Name", r.ToString()),
                         from k in allKeys ?? rac.DefaultDictionary().OverrideDictionary.Try(dic => dic.Keys).EmptyIfNull()
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


        internal SqlPreCommand ImportXml(XElement element, XName rootName, XName elementName, Dictionary<string, Lite<RoleEntity>> roles,
            Func<string, R> toResource, Func<string, A> parseAllowed)
        {
            var current = Database.RetrieveAll<RT>().GroupToDictionary(a => a.Role);
            var xRoles = element.Element(rootName).Try(e => e.Elements("Role")).EmptyIfNull();
            var should = xRoles.ToDictionary(x => roles.GetOrThrow(x.Attribute("Name").Value));

            Table table = Schema.Current.Table(typeof(RT));

            return Synchronizer.SynchronizeScript(should, current,
                (role, x) =>
                {
                    var dic = (from xr in x.Elements(elementName)
                               let r = toResource(xr.Attribute("Resource").Value)
                               where r != null
                               select KVP.Create(r, parseAllowed(xr.Attribute("Allowed").Value)))
                               .ToDictionary("{0} rules for {1}".FormatWith(typeof(R).NiceName(), role));

                    SqlPreCommand restSql = dic.Select(kvp => table.InsertSqlSync(new RT
                    {
                        Resource = kvp.Key,
                        Role = role,
                        Allowed = kvp.Value
                    }, comment: Comment(role, kvp.Key, kvp.Value))).Combine(Spacing.Simple).TryDo(p => p.GoBefore = true);

                    return restSql;
                },
                (role, list) => list.Select(rt => table.DeleteSqlSync(rt)).Combine(Spacing.Simple).TryDo(p => p.GoBefore = true),
                (role, x, list) =>
                {
                    var def = list.SingleOrDefaultEx(a => a.Resource == null);

                    var dic = (from xr in x.Elements(elementName)
                               let r = toResource(xr.Attribute("Resource").Value)
                               where r != null
                               select KVP.Create(r, xr))
                               .ToDictionary("{0} rules for {1}".FormatWith(typeof(R).NiceName(), role));

                    SqlPreCommand restSql = Synchronizer.SynchronizeScript(
                        dic,
                        list.Where(a => a.Resource != null).ToDictionary(a => a.Resource),
                        (r, xr) =>
                        {
                            var a = parseAllowed(xr.Attribute("Allowed").Value);
                            return table.InsertSqlSync(new RT { Resource = r, Role = role, Allowed = a }, comment: Comment(role, r, a));
                        },
                        (r, rt) => table.DeleteSqlSync(rt, Comment(role, r, rt.Allowed)),
                        (r, xr, rt) =>
                        {
                            var oldA = rt.Allowed;
                            rt.Allowed = parseAllowed(xr.Attribute("Allowed").Value);
                            return table.UpdateSqlSync(rt, comment: Comment(role, r, oldA, rt.Allowed));
                        }, Spacing.Simple).TryDo(p => p.GoBefore = true);

                    return restSql;
                },
                Spacing.Double);
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
