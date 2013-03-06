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
    class DefaultBehaviour<A>
    {
        public A BaseAllowed { get; private set;  }
        public Func<IEnumerable<A>, A> MergeAllowed;

        public DefaultBehaviour(A baseAllowed, Func<IEnumerable<A>, A> merge)
        {
            this.BaseAllowed = baseAllowed;
            this.MergeAllowed = merge;
        }

        public override string ToString()
        {
            return "DefaulBehaviour {0}".Formato(BaseAllowed);
        }
    }

    public interface IManualAuth<K, A>
    {
        DefaultRule GetDefaultRule(Lite<RoleDN> role);
        void SetDefaultRule(Lite<RoleDN> role, DefaultRule behaviour);
        A GetAllowed(Lite<RoleDN> role, K key);
        void SetAllowed(Lite<RoleDN> role, K key, A allowed);
    }

    class AuthCache<RT, AR, R, K, A>: IManualAuth<K, A> 
        where RT : RuleDN<R, A>, new()
        where AR : AllowedRule<R, A>, new()
        where R : IdentifiableEntity
    {
        readonly ResetLazy<Dictionary<Lite<RoleDN>, RoleAllowedCache>> runtimeRules; 

        Func<R, K> ToKey;
        Func<K, R> ToEntity;
        DefaultBehaviour<A> Min;
        DefaultBehaviour<A> Max;

        public AuthCache(SchemaBuilder sb, Func<R, K> toKey, Func<K, R> toEntity, DefaultBehaviour<A> max, DefaultBehaviour<A> min)
        {
            this.ToKey = toKey;
            this.ToEntity = toEntity;
            this.Max = max;
            this.Min = min;

            sb.Include<RT>();

            runtimeRules = sb.GlobalLazy(this.NewCache,
              new InvalidateWith(typeof(RT), typeof(RoleDN)));

            sb.AddUniqueIndex<RT>(rt => new { rt.Resource, rt.Role });

            sb.Schema.Table<R>().PreDeleteSqlSync += new Func<IdentifiableEntity, SqlPreCommand>(AuthCache_PreDeleteSqlSync);
        }

        SqlPreCommand AuthCache_PreDeleteSqlSync(IdentifiableEntity arg)
        {
            var t = Schema.Current.Table<RT>();
            var f = (FieldReference)t.Fields["resource"].Field;

            var param = Connector.Current.ParameterBuilder.CreateReferenceParameter("@id", false, arg.Id);

            return new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2}".Formato(t.Name, f.Name.SqlScape(), param.ParameterName), new List<DbParameter> { param });
        }



        DefaultRule IManualAuth<K, A>.GetDefaultRule(Lite<RoleDN> role)
        {
            var allowed = Database.Query<RT>().Where(a => a.Resource == null && a.Role == role).Select(a=>a.Allowed).ToList();

            return allowed.IsEmpty() || allowed[0].Equals(Max.BaseAllowed) ? DefaultRule.Max : DefaultRule.Min; 
        }

        void IManualAuth<K, A>.SetDefaultRule(Lite<RoleDN> role, DefaultRule behaviour)
        {
            if (((IManualAuth<K, A>)this).GetDefaultRule(role) == behaviour)
                return;

            IQueryable<RT> query = Database.Query<RT>().Where(a => a.Resource == null && a.Role == role);
            if (behaviour == DefaultRule.Max)
            {
                if (query.UnsafeDelete() == 0)
                    throw new InvalidOperationException("Inconsistency in the data");
            }
            else
            {
                if (query.UnsafeUpdate(a => new RT { Allowed = Min.BaseAllowed }) == 0)
                    new RT
                    {
                        Role = role,
                        Resource = null,
                        Allowed = Min.BaseAllowed,
                    }.Save();
            }
        }

        A IManualAuth<K, A>.GetAllowed(Lite<RoleDN> role, K key)
        {
            R resource = ToEntity(key);

            ManualResourceCache miniCache = new ManualResourceCache(resource, Min, Max);

            return miniCache.GetAllowed(role);
        }

        void IManualAuth<K, A>.SetAllowed(Lite<RoleDN> role, K key, A allowed)
        {
            R resource = ToEntity(key);

            ManualResourceCache miniCache = new ManualResourceCache(resource, Min, Max); 

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
                if (query.UnsafeUpdate(a => new RT { Allowed = allowed }) == 0)
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
            readonly Dictionary<Lite<RoleDN>, A> defaultRules;
            readonly Dictionary<Lite<RoleDN>, A> specificRules;

            readonly DefaultBehaviour<A> Min;
            readonly DefaultBehaviour<A> Max;

            public ManualResourceCache(R resource, DefaultBehaviour<A> min, DefaultBehaviour<A> max)
            {
                var list = (from r in Database.Query<RT>()
                            where r.Resource == resource || r.Resource == null
                            select new { Default = r.Resource == null, r.Role, r.Allowed }).ToList();

                defaultRules = list.Where(a => a.Default).ToDictionary(a => a.Role, a => a.Allowed);
                specificRules = list.Where(a => !a.Default).ToDictionary(a => a.Role, a => a.Allowed);

                this.Min = min;
                this.Max = max;
            }

            public A GetAllowed(Lite<RoleDN> role)
            {
                A result;
                if (specificRules.TryGetValue(role, out result))
                    return result;

                return GetAllowedBase(role);
            }

            DefaultBehaviour<A> GetBehaviour(Lite<RoleDN> role)
            {
                return defaultRules.TryGet(role, Max.BaseAllowed).Equals(Max.BaseAllowed) ? Max : Min;
            }

            public A GetAllowedBase(Lite<RoleDN> role)
            {
                var behaviour = GetBehaviour(role);
                var related = AuthLogic.RelatedTo(role);
                if (related.IsEmpty())
                    return behaviour.BaseAllowed;
                else
                    return behaviour.MergeAllowed(related.Select(r => GetAllowed(r)));
            }
        }

        Dictionary<Lite<RoleDN>, RoleAllowedCache> NewCache()
        {
            List<Lite<RoleDN>> roles = AuthLogic.RolesInOrder().ToList();

            Dictionary<Lite<RoleDN>, A> defaultBehaviours =
                Database.Query<RT>().Where(a => a.Resource == null)
                .Select(a => new { a.Role, a.Allowed }).ToDictionary(a => a.Role, a => a.Allowed);

            Dictionary<Lite<RoleDN>, Dictionary<K, A>> realRules =
               Database.Query<RT>().Where(a => a.Resource != null)
               .Select(a => new { a.Role, a.Allowed, a.Resource })
                  .AgGroupToDictionary(ru => ru.Role, gr => gr
                      .ToDictionary(ru => ToKey(ru.Resource), ru => ru.Allowed));

            Dictionary<Lite<RoleDN>, RoleAllowedCache> newRules = new Dictionary<Lite<RoleDN>, RoleAllowedCache>();
            foreach (var role in roles)
            {
                var related = AuthLogic.RelatedTo(role);

                var behaviour = defaultBehaviours.TryGet(role, Max.BaseAllowed).Equals(Max.BaseAllowed) ? Max : Min;

                newRules.Add(role, new RoleAllowedCache(behaviour, related.Select(r => newRules[r]).ToList(), realRules.TryGetC(role)));
            }

            return newRules;
        }

        internal void GetRules(BaseRulePack<AR> rules, IEnumerable<R> resources)
        {
            RoleAllowedCache cache = runtimeRules.Value[rules.Role];

            rules.SubRoles = AuthLogic.RelatedTo(rules.Role).ToMList();
            rules.DefaultRule = GetDefaultRule(rules.Role);
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
                if (rules.DefaultRule != GetDefaultRule(rules.Role))
                {
                    ((IManualAuth<K, A>)this).SetDefaultRule(rules.Role, rules.DefaultRule);
                    Database.Query<RT>().Where(r => r.Role == rules.Role && r.Resource != null).UnsafeDelete();
                    return;
                }

                var current = Database.Query<RT>().Where(r => r.Role == rules.Role && r.Resource != null && filterResources.Evaluate(r.Resource)).ToDictionary(a => a.Resource);
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

        public DefaultRule GetDefaultRule(Lite<RoleDN> role)
        {
            return runtimeRules.Value[role].GetDefaultRule(Max);
        }

        internal A GetAllowed(Lite<RoleDN> role, K key)
        {
            return runtimeRules.Value[role].GetAllowed(key);
        }
      
        internal DefaultDictionary<K, A> GetDefaultDictionary()
        {
            return runtimeRules.Value[RoleDN.Current.ToLite()].DefaultDictionary();
        }

        public class RoleAllowedCache
        {
            readonly DefaultBehaviour<A> behaviour;
            readonly DefaultDictionary<K, A> rules; 
            readonly List<RoleAllowedCache> baseCaches;

            public RoleAllowedCache(DefaultBehaviour<A> behaviour, List<RoleAllowedCache> baseCaches, Dictionary<K, A> newValues)
            {
                this.behaviour = behaviour;

                this.baseCaches = baseCaches;

                A defaultAllowed;
                Dictionary<K, A> tmpRules; 

                if(baseCaches.IsEmpty())
                {
                    defaultAllowed = behaviour.BaseAllowed;

                    tmpRules = newValues; 
                }
                else
                {
                    defaultAllowed = behaviour.MergeAllowed(baseCaches.Select(a => a.rules.DefaultAllowed));

                    var keys = baseCaches.Where(b => b.rules.DefaultAllowed.Equals(defaultAllowed) && b.rules != null).SelectMany(a => a.rules.ExplicitKeys).ToHashSet();

                    if (keys != null)
                    {
                        tmpRules = keys.ToDictionary(k => k, k => behaviour.MergeAllowed(baseCaches.Select(b => b.GetAllowed(k))));
                        if (newValues != null)
                            tmpRules.SetRange(newValues);
                    }
                    else
                    {
                        tmpRules = newValues; 
                    }
                }

                tmpRules = Simplify(tmpRules, defaultAllowed);

                rules = new DefaultDictionary<K, A>(defaultAllowed, tmpRules);
            }

            internal static Dictionary<K, A> Simplify(Dictionary<K, A> dictionary, A defaultAllowed)
            {
                if (dictionary == null || dictionary.Count == 0)
                    return null;

                dictionary.RemoveRange(dictionary.Where(p => p.Value.Equals(defaultAllowed)).Select(p => p.Key).ToList());

                if (dictionary.Count == 0)
                    return null;

                return dictionary;
            }

            public A GetAllowed(K k)
            {
                return rules.GetAllowed(k);
            }

            public A GetAllowedBase(K k)
            {
                return baseCaches.IsEmpty() ? rules.DefaultAllowed :
                       behaviour.MergeAllowed(baseCaches.Select(b => b.GetAllowed(k)));
            }

            public DefaultRule GetDefaultRule(DefaultBehaviour<A> max)
            {
                return behaviour == max ? DefaultRule.Max : DefaultRule.Min;
            }

            internal DefaultDictionary<K, A> DefaultDictionary()
            {
                return this.rules;
            }
        }

        internal XElement ExportXml(XName rootName, XName elementName, Func<R, string> resourceToString, Func<A, string> allowedToString)
        {
            var list = Database.RetrieveAll<RT>();

            var defaultRules = list.Where(a => a.Resource == null).ToDictionary(a => a.Role, a => a.Allowed);
            var specificRules = list.Where(a => a.Resource != null).AgGroupToDictionary(a => a.Role, gr => gr.ToDictionary(a => a.Resource, a => a.Allowed));

            return new XElement(rootName,
                (from r in AuthLogic.RolesInOrder()
                 let max = defaultRules.TryGet(r, Max.BaseAllowed).Equals(Max.BaseAllowed)
                 select new XElement("Role",
                     new XAttribute("Name", r.ToString()),
                     max ? null : new XAttribute("Default", "Min"),
                     specificRules.TryGetC(r).TryCC(dic =>
                         from kvp in dic
                         let resource = resourceToString(kvp.Key)
                         let allowed = allowedToString(kvp.Value)
                         orderby resource
                         select new XElement(elementName,
                            new XAttribute("Resource", resource),
                            new XAttribute("Allowed", allowed))
                     ))
                 ));
        }


        internal SqlPreCommand ImportXml(XElement element, XName rootName, XName elementName, Dictionary<string, Lite<RoleDN>> roles,
            Func<string, R> toResource, Func<string, A> parseAllowed)
        {
            var current = Database.RetrieveAll<RT>().GroupToDictionary(a => a.Role);
            var should = element.Element(rootName).Elements("Role").ToDictionary(x => roles[x.Attribute("Name").Value]);

            Table table = Schema.Current.Table(typeof(RT));
            
            return Synchronizer.SynchronizeScript(should, current, 
                (role, x) =>
                {
                    var max = x.Attribute("Default") == null || x.Attribute("Default").Value != "Min";
                    SqlPreCommand defSql = SetDefault(table, null, max, role);

                    var dic =  (from xr in x.Elements(elementName)
                               let r = toResource(xr.Attribute("Resource").Value)
                               where r != null
                               select KVP.Create(r, parseAllowed(xr.Attribute("Allowed").Value)))
                               .ToDictionary("{0} rules for {1}".Formato(typeof(R).NiceName(), role));

                    SqlPreCommand restSql = dic.Select(kvp => table.InsertSqlSync(new RT
                    {
                        Resource = kvp.Key,
                        Role = role,
                        Allowed = kvp.Value
                    }, Comment(role, kvp.Key, kvp.Value))).Combine(Spacing.Simple);

                    return SqlPreCommand.Combine(Spacing.Simple, defSql, restSql);
                }, 
                (role, list) => list.Select(rt => table.DeleteSqlSync(rt)).Combine(Spacing.Simple),
                (role, x, list) =>
                {
                    var def = list.SingleOrDefaultEx(a => a.Resource == null);
                    var max = x.Attribute("Default") == null || x.Attribute("Default").Value != "Min";
                    SqlPreCommand defSql = SetDefault(table, def, max, role);

                    var dic = (from xr in x.Elements(elementName)
                               let r = toResource(xr.Attribute("Resource").Value)
                               where r != null
                               select KVP.Create(r, xr))
                               .ToDictionary("{0} rules for {1}".Formato(typeof(R).NiceName(), role));

                    SqlPreCommand restSql = Synchronizer.SynchronizeScript(
                        dic, 
                        list.Where(a => a.Resource != null).ToDictionary(a => a.Resource), 
                        (r, xr) =>
                        {
                            var a = parseAllowed(xr.Attribute("Allowed").Value);
                            return table.InsertSqlSync(new RT { Resource = r, Role = role, Allowed = a }, Comment(role, r, a));
                        }, 
                        (r, rt) => table.DeleteSqlSync(rt, Comment(role, r, rt.Allowed)), 
                        (r, xr, rt) =>
                        {
                            var oldA = rt.Allowed;
                            rt.Allowed = parseAllowed(xr.Attribute("Allowed").Value);
                            return table.UpdateSqlSync(rt, Comment(role, r, oldA, rt.Allowed));
                        }, Spacing.Simple);

                    return SqlPreCommand.Combine(Spacing.Simple, defSql, restSql);
                }, 
                Spacing.Double);
        }


        internal static string Comment(Lite<RoleDN> role, R resource, A allowed)
        {
            return "{0} {1} for {2} ({3})".Formato(typeof(R).NiceName(), resource.ToString(), role, allowed);
        }

        internal static string Comment(Lite<RoleDN> role, R resource, A from, A to)
        {
            return "{0} {1} for {2} ({3} -> {4})".Formato(typeof(R).NiceName(), resource.ToString(), role, from, to);
        }

        private SqlPreCommand SetDefault(Table table, RT def, bool max, Lite<RoleDN> role)
        {
            string comment = "Default {0} for {1}".Formato(typeof(R).NiceName(), role);

            if (max)
            {
                if (def != null)
                    return table.DeleteSqlSync(def, comment + " ({0})".Formato(def.Allowed));

                return null;
            }
            else
            {
                if (def == null)
                {
                    return table.InsertSqlSync(new RT()
                    {
                        Role = role,
                        Resource = null,
                        Allowed = Min.BaseAllowed
                    }, comment + " ({0})".Formato(Min.BaseAllowed));
                }
                else if (!def.Allowed.Equals(Min.BaseAllowed))
                {
                    var old = def.Allowed;
                    def.Allowed = Min.BaseAllowed;
                    return table.UpdateSqlSync(def, comment + "({0} -> {1})".Formato(old, Min.BaseAllowed));
                }

                return null;
            }
        }
    }
}
