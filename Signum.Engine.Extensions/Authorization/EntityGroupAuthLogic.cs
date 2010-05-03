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
using Signum.Engine.Extensions.Properties;
using Signum.Entities.Operations; 

namespace Signum.Engine.Authorization
{
    public static class EntityGroupAuthLogic
    {
        static AuthCache<RuleEntityGroupDN, EntityGroupAllowedRule, EntityGroupDN, Enum, EntityGroupAllowed> cache; 

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertIsStarted(sb);
                EntityGroupLogic.Start(sb);

                cache = new AuthCache<RuleEntityGroupDN, EntityGroupAllowedRule, EntityGroupDN, Enum, EntityGroupAllowed>(sb,
                     EnumLogic<EntityGroupDN>.ToEnum,
                     EnumLogic<EntityGroupDN>.ToEntity,
                     MaxEntityGroupAllowed, EntityGroupAllowed.CreateCreate);

                sb.Schema.Initializing(InitLevel.Level0SyncEntities, Schema_InitializingRegisterEvents);
            }
        }

        static EntityGroupAllowed MaxEntityGroupAllowed(this IEnumerable<EntityGroupAllowed> collection)
        {
            return collection.Aggregate(EntityGroupAllowed.NoneNone, (a, b) => a | b);
        }

        static void Schema_InitializingRegisterEvents(Schema sender)
        {
            MethodInfo miRegister = ReflectionTools.GetMethodInfo(() => 
                EntityGroupAuthLogic.RegisterSchemaEvent<TypeDN>(null)).GetGenericMethodDefinition();

            foreach (var type in EntityGroupLogic.Types)
            {
                miRegister.GenericInvoke(new[] { type }, null, new object[] { sender });
            }
        }

        static void RegisterSchemaEvent<T>(Schema sender)
             where T : IdentifiableEntity
        {
            sender.EntityEvents<T>().Retrieved += new EntityEventHandler<T>(EntityGroupAuthLogic_Retrieved);
            sender.EntityEvents<T>().Saving += new EntityEventHandler<T>(EntityGroupAuthLogic_Saving);
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

        [ThreadStatic]
        static bool saveDisabled;
        public static IDisposable DisableSave()
        {
            saveDisabled = true;
            return new Disposable(() => saveDisabled = false);
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
                ident.AssertAllowed(TypeAllowed.Read);
        }

        static void EntityGroupAuthLogic_Saving<T>(T ident, bool isRoot)
            where T : IdentifiableEntity
        {
            if (!saveDisabled && ident.Modified)
            {
                if (ident.IsNew)
                    ident.AssertAllowed(TypeAllowed.Create);
                else
                    ident.AssertAllowed(TypeAllowed.None);
            }
        }

        public static void AssertAllowed(this IIdentifiable ident, TypeAllowed allowed)
        {
            if (!ident.IsAllowedFor(allowed))
                throw new UnauthorizedAccessException(Resources.NotAuthorizedToRetrieveThe0WithId1.Formato(ident.GetType().NiceName(), ident.Id));
        }

        public static bool IsAllowedFor(this IIdentifiable ident, TypeAllowed allowed)
        {
            if(!AuthLogic.IsEnabled)
                return true;

            RoleDN role = RoleDN.Current;

            return EntityGroupLogic.GroupsFor(ident.GetType()).All(eg =>
                {
                    EntityGroupAllowed access = cache.GetAllowed(role, eg);
                    TypeAllowed inAllowed = EntityGroupAllowedUtils.In(access);
                    TypeAllowed outAllowed = EntityGroupAllowedUtils.Out(access);
                    if (inAllowed >= allowed && outAllowed >= allowed)
                        return true;

                    if (((IdentifiableEntity)ident).IsInGroup(eg))
                        return inAllowed >= allowed;
                    else
                        return outAllowed >= allowed;
                });
        }

        [MethodExpander(typeof(WhereAllowedExpander))]
        public static IQueryable<T> WhereAllowed<T>(this IQueryable<T> query)
            where T : IdentifiableEntity
        {
            if (!AuthLogic.IsEnabled)
                return query;

            RoleDN role = RoleDN.Current;

            var pairs = (from eg in EntityGroupLogic.GroupsFor(typeof(T))
                         let allowed = cache.GetAllowed(role, eg)
                         select new
                         {
                             Group = eg,
                             AllowedIn = EntityGroupAllowedUtils.In(allowed) != TypeAllowed.None,
                             AllowedOut = EntityGroupAllowedUtils.Out(allowed) != TypeAllowed.None,
                             Expression = EntityGroupLogic.GetInGroupExpression<T>(eg),
                         }).Where(p => !p.AllowedIn || !p.AllowedOut).ToList();

            if (pairs.Count == 0)
                return query;

            if (pairs.Any(p => !p.AllowedIn && !p.AllowedOut))
                return query.Where(p => false);

            ParameterExpression e = Expression.Parameter(typeof(T), "e");
            Expression body = pairs.Select(p =>
                            p.AllowedIn ? (Expression)Expression.Invoke(p.Expression, e) :
                            Expression.Not(Expression.Invoke(p.Expression, e))).Aggregate((a, b) => Expression.And(a, b));

            Expression cleanBody = DbQueryProvider.Clean(body);

            IQueryable<T> result = query.Where(Expression.Lambda<Func<T, bool>>(cleanBody, e));

            return result;
        }

        class WhereAllowedExpander : IMethodExpander
        {
            static MethodInfo mi = ReflectionTools.GetMethodInfo(() => CallWhereAllowed<TypeDN>(null)).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                return (Expression)mi.GenericInvoke(typeArguments, null, new object[] { arguments[0] });
            }

            static Expression CallWhereAllowed<T>(Expression expression)
                where T : IdentifiableEntity
            {
                IQueryable<T> query = new Query<T>(DbQueryProvider.Single, expression);
                IQueryable<T> result = WhereAllowed(query);
                return result.Expression;
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

            public override ResultTable ExecuteQuery(List<UserColumn> userColumns, List<Filter> filters, List<Order> orders, int? limit)
            {
                using (EntityGroupAuthLogic.DisableQueries())
                {
                    return base.ExecuteQuery(userColumns, filters, orders, limit);
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

        public static EntityGroupRulePack GetEntityGroupRules(Lite<RoleDN> roleLite)
        {
            return new EntityGroupRulePack
            {
                Role = roleLite,
                Rules = cache.GetRules(roleLite, EnumLogic<EntityGroupDN>.AllEntities()).ToMList()
            };
        }

        public static void SetEntityGroupRules(EntityGroupRulePack rules)
        {
            cache.SetRules(rules, r => true); 
        }

        public static void SetEntityGroupAllowed(Lite<RoleDN> role, Enum entityGroupKey, EntityGroupAllowed allowed)
        {
            cache.SetAllowed(role, entityGroupKey, allowed); 
        }
    }
}
