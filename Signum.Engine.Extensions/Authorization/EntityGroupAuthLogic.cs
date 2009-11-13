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
using Signum.Engine.Linq; 

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
        public static IDisposable DisableQueries()
        {
            queriesDisabled = true;
            return new Disposable(() => queriesDisabled = false);
        }

        [ThreadStatic]
        static bool retrieveDisabled;
        public static IDisposable DisableRetrieve()
        {
            retrieveDisabled = true;
            return new Disposable(() => retrieveDisabled = false);
        }


        static IQueryable<T> EntityGroupAuthLogic_FilterQuery<T>(IQueryable<T> query)
            where T:IdentifiableEntity
        {
            if (!queriesDisabled)
                return WhereAllowed<T>(query);
            return query; 
        }

        static void EntityGroupAuthLogic_Retrieved<T>(T ident, bool isRoot)
            where T:IdentifiableEntity
        {
            if (!retrieveDisabled && isRoot)
                ident.AssertAllowed();
        }

        

        static void Schema_Saved(RuleEntityGroupDN rule, bool isRoot)
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

        public static void AssertAllowed(this IIdentifiable ident)
        {
            if (!ident.IsAllowed())
                throw new UnauthorizedAccessException("Not authorized to retrieve the {0} with Id {1}".Formato(ident.GetType().NiceName(), ident.Id));
        }

        public static bool IsAllowed(this IIdentifiable ident)
        {
            if(!AuthLogic.IsEnabled)
                return true;

            RoleDN role = RoleDN.Current;

            return EntityGroupLogic.GroupsFor(ident.GetType()).All(eg =>
                {
                    AllowedPair pair = GetAllowed(role, eg);
                    return ident.IsInGroup(eg) ? pair.InGroup : pair.OutGroup;
                });
        }

        public static IQueryable<T> WhereAllowed<T>(this IQueryable<T> query)
            where T : IdentifiableEntity
        {
            if (!AuthLogic.IsEnabled)
                return query;

            RoleDN role = RoleDN.Current;

            var pairs = (from eg in EntityGroupLogic.GroupsFor(typeof(T))
                         select new
                         {
                             Group = eg,
                             Allowed = GetAllowed(role, eg),
                             Expression = EntityGroupLogic.GetInGroupExpression<T>(eg),
                             Queryable = EntityGroupLogic.GetInGroupQueryable<T>(eg)
                         }).Where(p => !p.Allowed.InGroup || !p.Allowed.OutGroup).ToList();

            if (pairs.Count == 0)
                return query;

            if (pairs.Any(p => !p.Allowed.InGroup && !p.Allowed.OutGroup))
                return query.Where(p => false);

            var pair = pairs.FirstOrDefault(p => p.Allowed.InGroup && !p.Allowed.OutGroup && p.Queryable != null);
            if (pair != null && query.IsBase())
            {
                pairs.Remove(pair);
                query = pair.Queryable;
            }

            if (pairs.Count > 0)
            {
                ParameterExpression e = Expression.Parameter(typeof(T), "e");
                Expression body = pairs.Select(p =>
                                !p.Allowed.InGroup ? Expression.Not(Expression.Invoke(p.Expression, e)) :
                                                     (Expression)Expression.Invoke(p.Expression, e)).Aggregate((a, b) => Expression.And(a, b));

                body = ExpressionEvaluator.PartialEval(body); //Avoid recursive Expansions of Database.Query<T>()

                query = query.Where(Expression.Lambda<Func<T, bool>>(body, e));

            }

            return new Query<T>(DbQueryProvider.Single, QueryReplacer.Replace(query.Expression));
        }

        class QueryReplacer : ExpressionVisitor
        {
            public static Expression Replace(Expression exp)
            {
                return new QueryReplacer().Visit(exp);
            }

            MethodInfo mi = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>()).GetGenericMethodDefinition();
            
            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                if (m.Method.IsGenericMethod && m.Method.GetGenericMethodDefinition() == mi)
                {
                    Type type = typeof(Query<>).MakeGenericType(m.Method.GetGenericArguments());
                    IQueryable query = (IQueryable)Activator.CreateInstance(type, DbQueryProvider.Single);
                    return Expression.Constant(query);
                }

                return base.VisitMethodCall(m);
            }
        }

        class WhereAllowedExpander : IMethodExpander
        {
            static MethodInfo mi = ReflectionTools.GetMethodInfo(() => CallWhereAllowed<TypeDN>(null)).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                return (Expression)mi.MakeGenericMethod(typeArguments[0]).Invoke(null, new object[] { arguments[0] });
            }

            static Expression CallWhereAllowed<T>(Expression expression)
                where T : IdentifiableEntity
            {
                IQueryable<T> query = new Query<T>(DbQueryProvider.Single, expression);
                IQueryable<T> result = WhereAllowed(query);
                return result.Expression;
            }
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

        public static DynamicQuery<T> ToDynamicDisableAutoFilter<T>(this IQueryable<T> query)
        {
            return new AutoDynamicQueryNoFilter<T>(query);
        }

        internal class AutoDynamicQueryNoFilter<T> : AutoDynamicQuery<T>
        {
            public AutoDynamicQueryNoFilter(IQueryable<T> query)
                : base(query)
            { }

            public override QueryResult ExecuteQuery(List<Filter> filters, int? limit)
            {
                using (EntityGroupAuthLogic.DisableQueries())
                {
                    return base.ExecuteQuery(filters, limit);
                }
            }

            public override int ExecuteQueryCount(List<Filter> filters)
            {
                using (EntityGroupAuthLogic.DisableQueries())
                {
                    return base.ExecuteQueryCount(filters);
                }
            }
        }
    }
}
