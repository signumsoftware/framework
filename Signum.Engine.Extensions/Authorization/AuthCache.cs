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
    class AuthCache<RT, AR, R, K, A>
        where RT : RuleDN<R, A>, new()
        where AR : AllowedRule<R, A>, new()
        where R : IdentifiableEntity
        where A : struct
    {
        Dictionary<Lite<RoleDN>, Dictionary<K, A>> _runtimeRules;
        Dictionary<Lite<RoleDN>, Dictionary<K, A>> RuntimeRules
        {
            get { return Sync.Initialize(ref _runtimeRules, () => NewCache()); }
        }

        Func<R, K> ToKey;
        Func<K, R> ToEntity;
        Func<IEnumerable<A>, A> Max;
        A MaxValue;

        public AuthCache(SchemaBuilder sb, Func<R, K> toKey, Func<K, R> toEntity, Func<IEnumerable<A>, A> max, A maxValue)
        {
            this.ToKey = toKey;
            this.ToEntity = toEntity;
            this.Max = max;
            this.MaxValue = maxValue;

            sb.Include<RT>();

            sb.Schema.Initializing(InitLevel.Level1SimpleEntities, Schema_InitializingCache);
            sb.Schema.EntityEvents<RT>().Saved += Schema_Saved;
            AuthLogic.RolesModified += UserAndRoleLogic_RolesModified;
        }

        void Schema_InitializingCache(Schema sender)
        {
            _runtimeRules = NewCache();
        }

        void InvalidateCache()
        {
            _runtimeRules = null; 
        }

        internal List<AR> GetRules(Lite<RoleDN> role, IEnumerable<R> resources)
        {
            return (from r in resources
                    let k = ToKey(r)
                    select new AR()
                    {
                        Resource = r,
                        AllowedBase = GetBaseAllowed(role, k),
                        Allowed = GetAllowed(role, k)
                    }).ToList();
        }

        internal void SetRules(BaseRulePack<AR> rules, Expression<Func<R,bool>> filterResources)
        {
            var current = Database.Query<RT>().Where(r => r.Role == rules.Role && filterResources.Invoke(r.Resource)).ToDictionary(a => a.Resource);
            var should = rules.Rules.Where(a => a.Overriden).ToDictionary(r => r.Resource);

            Synchronizer.Synchronize(current, should,
                (p, pr) => pr.Delete(),
                (p, ar) => new RT { Resource = p, Role = rules.Role, Allowed = ar.Allowed }.Save(),
                (p, pr, ar) => { pr.Allowed = ar.Allowed; pr.Save(); });

            InvalidateCache();
        }

        internal void SetAllowed(Lite<RoleDN> role, K key, A allowed)
        {
            if (GetAllowed(role, key).Equals(allowed))
                return;

            R resource = ToEntity(key);
            IQueryable<RT> query = Database.Query<RT>().Where(a => a.Resource == resource && a.Role == role);
            if (GetBaseAllowed(role, key).Equals(allowed))
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

        Dictionary<Lite<RoleDN>, Dictionary<K, A>> NewCache()
        {
            using (AuthLogic.Disable())
            using (new EntityCache(true))
            {
                List<Lite<RoleDN>> roles = AuthLogic.RolesInOrder().ToList();

                Dictionary<Lite<RoleDN>, Dictionary<K, A>> realRules =
                    Database.RetrieveAll<RT>()
                      .AgGroupToDictionary(ru => ru.Role, gr => gr
                          .ToDictionary(ru => ToKey(ru.Resource), ru => ru.Allowed));

                Dictionary<Lite<RoleDN>, Dictionary<K, A>> newRules = new Dictionary<Lite<RoleDN>, Dictionary<K, A>>();
                foreach (var role in roles)
                {
                    var related = AuthLogic.RelatedTo(role);
                    var permissions = related.Any() ? related.
                        Select(r => newRules.TryGetC(r)).OuterCollapseDictionariesS(vals => Max(vals.Select(a => a ?? MaxValue))) : null;

                    permissions = permissions.Override(realRules.TryGetC(role)).Simplify(a => a.Equals(MaxValue));

                    if (permissions != null)
                        newRules.Add(role, permissions);
                }

                return newRules;
            }
        }

        void Schema_Saved(RT rule, bool isRoot)
        {
            Transaction.RealCommit += () => InvalidateCache();
        }

        void UserAndRoleLogic_RolesModified()
        {
            Transaction.RealCommit += () => InvalidateCache();
        }



        internal A GetAllowed(RoleDN role, K key)
        {
            return RuntimeRules.TryGetC(role.ToLite()).TryGetS(key) ??  MaxValue;
        }

        internal A GetAllowed(Lite<RoleDN> role, K key)
        {
            return RuntimeRules.TryGetC(role).TryGetS(key) ?? MaxValue;
        }

        A GetBaseAllowed(Lite<RoleDN> role, K key)
        {
            var related = AuthLogic.RelatedTo(role);

            return related.Any()? Max(related.Select(r => GetAllowed(r, key))): MaxValue ;
        }

        internal Dictionary<K, A> GetCleanDictionary()
        {
            return RuntimeRules.TryGetC(RoleDN.Current.ToLite()) ?? new Dictionary<K, A>();
        }
    }
}
