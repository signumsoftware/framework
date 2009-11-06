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
using System.Linq.Expressions;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees; 

namespace Signum.Engine.Authorization
{
    public static class EntityGroupAuthLogic
    {
        class AllowedPair
        {
            public static readonly AllowedPair True = new AllowedPair(true, true);

            public readonly bool InGroup;
            public readonly bool OutGroup;

            public AllowedPair(bool inGroup, bool outGroup)
            {
                this.InGroup = inGroup;
                this.OutGroup = outGroup;
            }

            public static AllowedPair Max(AllowedPair ap1, AllowedPair ap2)
            {
                return new AllowedPair(
                    ap1.InGroup || ap1.InGroup,
                    ap2.InGroup || ap2.InGroup);
            }

            public static AllowedPair Min(AllowedPair ap1, AllowedPair ap2)
            {
                return new AllowedPair(
                    ap1.InGroup && ap1.InGroup,
                    ap2.InGroup && ap2.InGroup);
            }

            public bool IsTrue()
            {
                return InGroup && OutGroup;
            }
        }

        static Dictionary<RoleDN, Dictionary<Enum, AllowedPair>> _runtimeRules;
        static Dictionary<RoleDN, Dictionary<Enum, AllowedPair>> RuntimeRules
        {
            get { return Sync.Initialize(ref _runtimeRules, () => NewCache()); }
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertIsStarted(sb);
                EntityGroupLogic.Start(sb);
                sb.Include<RuleEntityGroupDN>();
                sb.Schema.Initializing(InitLevel.Level0SyncEntities, Schema_InitializingRegisterEvents);
                sb.Schema.Initializing(InitLevel.Level1SimpleEntities, Schema_InitializingCache);
                sb.Schema.EntityEvents<RuleEntityGroupDN>().Saved += Schema_Saved;
                AuthLogic.RolesModified += UserAndRoleLogic_RolesModified;
            }
        }

        static AllowedPair MaxAllowedPair(this IEnumerable<AllowedPair> collection)
        {
            AllowedPair max = new AllowedPair(false, false);
            foreach (var item in collection)
            {
                max = AllowedPair.Max(max, item);
                if (max.IsTrue())
                    return max;
            }
            return max;
        }

        static void Schema_InitializingCache(Schema sender)
        {
            _runtimeRules = NewCache();
        }

        static void Schema_InitializingRegisterEvents(Schema sender)
        {
            MethodInfo miRegister = ReflectionTools.GetMethodInfo(() => 
                EntityGroupAuthLogic.RegisterSchemaEvent<TypeDN>(null)).GetGenericMethodDefinition();

            foreach (var type in EntityGroupLogic.Types)
            {
                miRegister.MakeGenericMethod(type).Invoke(null, new object[] { sender }); 
            }
        }

        static void RegisterSchemaEvent<T>(Schema sender)
             where T : IdentifiableEntity
        {
            sender.EntityEvents<T>().Retrieved += new EntityEventHandler<T>(EntityGroupAuthLogic_Retrieved);
            sender.EntityEvents<T>().FilterQuery +=new FilterQueryEventHandler<T>(EntityGroupAuthLogic_FilterQuery);
        }

        [ThreadStatic]
        static bool queriesDisabled;
        public static IDisposable DisableAutoFilterQueries()
        {
            queriesDisabled = true;
            return new Disposable(() => queriesDisabled = false);
        }

        static IQueryable<T> EntityGroupAuthLogic_FilterQuery<T>(IQueryable<T> query)
            where T:IdentifiableEntity
        {
            if (!queriesDisabled)
                return WhereGroupsAllowed<T>(query);
            return query; 
        }

        static IQueryable sender_FilterQuery(IQueryable arg)
        {
            throw new NotImplementedException();
        }

        static void EntityGroupAuthLogic_Retrieved<T>(T ident)
            where T:IdentifiableEntity
        {
            if (!GroupsAllowed(ident))
                throw new UnauthorizedAccessException("Not authorized to retrieve the {0} with Id {1}".Formato(typeof(T).NiceName(), ident.Id));
        }

        static void Schema_Saved(RuleEntityGroupDN rule)
        {
            Transaction.RealCommit += () => _runtimeRules = null;
        }

        static void UserAndRoleLogic_RolesModified()
        {
            Transaction.RealCommit += () => _runtimeRules = null;
        }

        static AllowedPair GetAllowed(RoleDN role, Enum entityGroupKey)
        {
            return RuntimeRules.TryGetC(role).TryGetC(entityGroupKey) ?? AllowedPair.True;
        }

        static AllowedPair GetBaseAllowed(RoleDN role, Enum entityGroupKey)
        {
            return role.Roles.Count == 0 ? AllowedPair.True :
                  role.Roles.Select(r => GetAllowed(r, entityGroupKey)).MaxAllowedPair();
        }

        static bool GroupsAllowed(IdentifiableEntity entity)
        {
            if(!AuthLogic.IsEnabled)
                return true;

            RoleDN role = RoleDN.Current;

            return EntityGroupLogic.GroupsFor(entity.GetType()).All(eg=>
                {
                    AllowedPair pair = GetAllowed(role, eg); 
                    return entity.IsInGroup(eg)? pair.InGroup: pair.OutGroup;
                });
        }

        public static IQueryable<T> WhereGroupsAllowed<T>(this IQueryable<T> query)
            where T : IdentifiableEntity
        {
            if (!AuthLogic.IsEnabled)
                return query;

            RoleDN role = RoleDN.Current;
            foreach (Enum eg in EntityGroupLogic.GroupsFor(typeof(T)))
            {
                var pair = GetAllowed(role, eg);
                if (!pair.InGroup && !pair.OutGroup)
                    query = query.Where(p => false);
                else
                {
                    if (!pair.InGroup)
                        query = query.Where(e=>!e.IsInGroup(eg));
                    else if (!pair.OutGroup)
                        query = query.Where(e=>e.IsInGroup(eg));
                }
            }

            return query;
        }

        public static List<EntityGroupRule> GetEntityGroupRules(Lite<RoleDN> roleLite)
        {
            var role = roleLite.Retrieve();

            return EntityGroupLogic.Groups.Select(eg => 
                {
                    AllowedPair basePair = GetBaseAllowed(role, eg); 
                    AllowedPair pair = GetAllowed(role, eg);
                    return new EntityGroupRule(basePair.InGroup, basePair.OutGroup)
                    {
                        Group = EnumLogic<EntityGroupDN>.ToEntity(eg),
                        AllowedIn = pair.InGroup,
                        AllowedOut = pair.OutGroup
                    };
                }).ToList();
        }

        public static void SetEntityGroupRules(List<EntityGroupRule> rules, Lite<RoleDN> roleLite)
        {
            var role = roleLite.Retrieve();

            var current = Database.Query<RuleEntityGroupDN>().Where(r => r.Role == role).ToDictionary(a => a.Group);
            var should = rules.Where(a => a.Overriden).ToDictionary(r => r.Group);

            Synchronizer.Synchronize(current, should,
                (p, pr) => pr.Delete(),
                (p, ar) => new RuleEntityGroupDN { Group = p, Role = role, AllowedIn = ar.AllowedIn, AllowedOut = ar.AllowedOut}.Save(),
                (p, pr, ar) => { pr.AllowedIn = ar.AllowedIn; pr.AllowedOut = ar.AllowedOut; pr.Save(); });

            _runtimeRules = null;
        }

        static Dictionary<RoleDN, Dictionary<Enum, AllowedPair>> NewCache()
        {
            using (AuthLogic.Disable())
            using (new EntityCache(true))
            {
                List<RoleDN> roles = AuthLogic.RolesInOrder().ToList();

                Dictionary<RoleDN, Dictionary<Enum, AllowedPair>> realRules =
                    Database.RetrieveAll<RuleEntityGroupDN>()
                      .AgGroupToDictionary(ru => ru.Role, gr => gr
                          .ToDictionary(ru => EnumLogic<EntityGroupDN>.ToEnum(ru.Group), ru => new AllowedPair(ru.AllowedIn, ru.AllowedOut)));

                Dictionary<RoleDN, Dictionary<Enum, AllowedPair>> newRules = new Dictionary<RoleDN, Dictionary<Enum, AllowedPair>>();
                foreach (var role in roles)
                {
                    var permissions = (role.Roles.Count == 0 ?
                         null :
                         role.Roles.Select(r => newRules.TryGetC(r)).OuterCollapseDictionariesC(vals => vals.MaxAllowedPair()));

                    permissions = permissions.Override(realRules.TryGetC(role)).Simplify(a => a.IsTrue());

                    if (permissions != null)
                        newRules.Add(role, permissions);
                }

                return newRules;
            }
        }
    }
}
