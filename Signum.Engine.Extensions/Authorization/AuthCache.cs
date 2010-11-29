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
  
        Dictionary<Lite<RoleDN>, RoleAllowedCache> _runtimeRules;
        Dictionary<Lite<RoleDN>, RoleAllowedCache> RuntimeRules
        {
            get { return Sync.Initialize(ref _runtimeRules, () => NewCache()); }
        }

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

            sb.Schema.Initializing(InitLevel.Level1SimpleEntities, Schema_InitializingCache);
            sb.Schema.EntityEvents<RT>().Saving += Schema_Saving;
            AuthLogic.RolesModified += InvalidateCache;
        }

        void Schema_InitializingCache(Schema sender)
        {
            _runtimeRules = NewCache();
        }

        void InvalidateCache()
        {
            _runtimeRules = null; 
        }

        DefaultRule IManualAuth<K, A>.GetDefaultRule(Lite<RoleDN> role)
        {
            var allowed = Database.Query<RT>().Where(a => a.Resource == null && a.Role == role).Select(a=>a.Allowed).ToList();

            return allowed.Empty() || allowed[0].Equals(Max.BaseAllowed) ? DefaultRule.Max : DefaultRule.Min; 
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

            InvalidateCache();
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

            InvalidateCache();
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
                if (related.Empty())
                    return behaviour.BaseAllowed;
                else
                    return behaviour.MergeAllowed(related.Select(r => GetAllowed(r)));
            }
        }

        Dictionary<Lite<RoleDN>, RoleAllowedCache> NewCache()
        {
            using (AuthLogic.Disable())
            using (new EntityCache(true))
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
        }

        void Schema_Saving(RT rule, bool isRoot)
        {
            Transaction.RealCommit += () => InvalidateCache();
        }

        internal void GetRules(BaseRulePack<AR> rules, IEnumerable<R> resources)
        {
            RoleAllowedCache cache = RuntimeRules[rules.Role];

            rules.SubRoles = AuthLogic.RelatedTo(rules.Role).ToList();
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
            if (rules.DefaultRule != GetDefaultRule(rules.Role))
            {
                ((IManualAuth<K, A>)this).SetDefaultRule(rules.Role, rules.DefaultRule);
                Database.Query<RT>().Where(r => r.Role == rules.Role && r.Resource != null).UnsafeDelete();  
                return;
            }

            var current = Database.Query<RT>().Where(r => r.Role == rules.Role && r.Resource != null && filterResources.Invoke(r.Resource)).ToDictionary(a => a.Resource);
            var should = rules.Rules.Where(a => a.Overriden).ToDictionary(r => r.Resource);

            Synchronizer.Synchronize(current, should,
                (p, pr) => pr.Delete(),
                (p, ar) => new RT { Resource = p, Role = rules.Role, Allowed = ar.Allowed }.Save(),
                (p, pr, ar) => { pr.Allowed = ar.Allowed; pr.Save(); });

            InvalidateCache();
        }

        private DefaultRule GetDefaultRule(Lite<RoleDN> role)
        {
            return RuntimeRules[role].GetDefaultRule(Max);
        }

        internal A GetAllowed(K key)
        {
            if (!AuthLogic.IsEnabled)
                return Max.BaseAllowed;

            return RuntimeRules[RoleDN.Current.ToLite()].GetAllowed(key);
        }

        internal A GetAllowed(Lite<RoleDN> role, K key)
        {
            return RuntimeRules[role].GetAllowed(key);
        }
      
        internal DefaultDictionary<K, A> GetCleanDictionary()
        {
            return RuntimeRules[RoleDN.Current.ToLite()].CleanDictionary();
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

                if(baseCaches.Empty())
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
                return baseCaches.Empty() ? rules.DefaultAllowed :
                       behaviour.MergeAllowed(baseCaches.Select(b => b.GetAllowed(k)));
            }

            public DefaultRule GetDefaultRule(DefaultBehaviour<A> max)
            {
                return behaviour == max ? DefaultRule.Max : DefaultRule.Min;
            }

            internal DefaultDictionary<K, A> CleanDictionary()
            {
                return this.rules;
            }
        }
    }
}
