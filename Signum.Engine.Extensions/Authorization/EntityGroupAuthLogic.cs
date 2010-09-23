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
        static AuthCache<RuleEntityGroupDN, EntityGroupAllowedRule, EntityGroupDN, Enum, EntityGroupAllowedDN> cache;

        public static IManualAuth<Enum, EntityGroupAllowedDN> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public static void Start(SchemaBuilder sb, bool registerEntitiesWithNoGroup)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                EntityGroupLogic.Start(sb);
                if (registerEntitiesWithNoGroup)
                {
                    PermissionAuthLogic.RegisterPermissions(BasicPermissions.EntitiesWithNoGroup);

                    Schema.Current.IsAllowedCallback += t => BasicPermissions.EntitiesWithNoGroup.IsAuthorized() || EntityGroupLogic.GroupsFor(t).Any() ? null :
                        "The entity '{0}' has no EntityGroups registered and the permission '{1}' is denied".Formato(t.NiceName(), BasicPermissions.EntitiesWithNoGroup.NiceToString());
                }

                cache = new AuthCache<RuleEntityGroupDN, EntityGroupAllowedRule, EntityGroupDN, Enum, EntityGroupAllowedDN>(sb,
                     EnumLogic<EntityGroupDN>.ToEnum,
                     EnumLogic<EntityGroupDN>.ToEntity,
                     AuthUtils.MaxEntityGroup,
                     AuthUtils.MinEntityGroup);

                sb.Schema.Initializing(InitLevel.Level0SyncEntities, Schema_InitializingRegisterEvents);
            }
        }


        static MethodInfo miRegister = ReflectionTools.GetMethodInfo(() =>EntityGroupAuthLogic.RegisterSchemaEvent<TypeDN>(null)).GetGenericMethodDefinition();

        static void Schema_InitializingRegisterEvents(Schema sender)
        {
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
            sender.EntityEvents<T>().FilterQuery += new FilterQueryEventHandler<T>(EntityGroupAuthLogic_FilterQuery);
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
        static DisableSaveOptions saveDisabled = 0;
        public static IDisposable DisableSave(DisableSaveOptions mode)
        {
            DisableSaveOptions saveDisabledOld = saveDisabled;
            saveDisabled = mode;
            return new Disposable(() => saveDisabled = saveDisabledOld);
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
                ident.AssertAllowed(TypeAllowedBasic.Read, false);
        }

        static void EntityGroupAuthLogic_Saving<T>(T ident, bool isRoot)
            where T : IdentifiableEntity
        {
            if (AuthLogic.IsEnabled)
            {
                if (!saveDisabled.HasFlag(DisableSaveOptions.Origin))
                {
                    if (!ident.IsNew && !ident.InDB().WhereAllowed().Any()) //Previous state in the database
                        throw new UnauthorizedAccessException(Resources.NotAuthorizedTo0The1WithId2.Formato(TypeAllowedBasic.Modify.NiceToString().ToLower(), ident.GetType().NiceName(), ident.Id));
                }

                if (!saveDisabled.HasFlag(DisableSaveOptions.Destiny))
                {
                    if (ident.IsNew)
                        Transaction.PreRealCommit += () => ident.AssertAllowed(TypeAllowedBasic.Create, false);
                    else
                        Transaction.PreRealCommit += () => ident.AssertAllowed(TypeAllowedBasic.Modify, false);
                }
            }
        }

        public static void AssertAllowed(this IIdentifiable ident, TypeAllowedBasic allowed, bool userInterface)
        {
            if (!ident.IsAllowedFor(allowed, userInterface))
                throw new UnauthorizedAccessException(Resources.NotAuthorizedTo0The1WithId2.Formato(allowed.NiceToString().ToLower(), ident.GetType().NiceName(), ident.Id));
        }

        public static bool IsAllowedFor(this IIdentifiable ident, TypeAllowedBasic allowed, bool userInterface)
        {
            if(!AuthLogic.IsEnabled)
                return true;

            IdentifiableEntity ent = (IdentifiableEntity)ident; 

            return EntityGroupLogic.GroupsFor(ident.GetType()).All(eg =>
                {
                    EntityGroupAllowedDN access = cache.GetAllowed(eg);
                    bool allowedIn = access.InGroup.Get(userInterface) >= allowed;
                    bool allowedOut = access.OutGroup.Get(userInterface) >= allowed;
                    var egi = EntityGroupLogic.GetEntityGroupInfo(eg, ident.GetType());

                    if (egi.NeverApplicable)
                        return true;
                    
                    if (allowedIn && allowedOut)
                        return true;

                    if (!ent.IsApplicable(eg))
                        return true; 

                    if (!allowedIn && !allowedOut)
                        return false;

                    if (ent.IsInGroup(eg))
                        return allowedIn;
                    else
                        return allowedOut;
                });
        }

        public static void AssertAllowed(this Lite lite, TypeAllowedBasic allowed, bool userInterface)
        {
            if (lite.IdOrNull == null)
                AssertAllowed(lite.UntypedEntityOrNull, allowed, userInterface);

            if (!lite.IsAllowedFor(allowed, userInterface))
                throw new UnauthorizedAccessException(Resources.NotAuthorizedTo0The1WithId2.Formato(allowed.NiceToString().ToLower(), lite.RuntimeType.NiceName(), lite.Id));
        }

        public static bool IsAllowedFor(this Lite lite, TypeAllowedBasic allowed, bool userInterface)
        {
            return (bool)miIsAllowedFor.GenericInvoke(new[] { lite.RuntimeType }, null, new object[] { lite, allowed, userInterface });
        }

        static MethodInfo miIsAllowedFor = ReflectionTools.GetMethodInfo(() => IsAllowedFor<IdentifiableEntity>(null, TypeAllowedBasic.Create, true)).GetGenericMethodDefinition();

        static bool IsAllowedFor<T>(this Lite lite, TypeAllowedBasic allowed, bool userInterface)
            where T: IdentifiableEntity
        {
            if (!AuthLogic.IsEnabled)
                return true;

            return lite.ToLite<T>().InDB().WhereIsAllowedFor(allowed, userInterface).Any(); 
        }


        [MethodExpander(typeof(WhereAllowedExpander))]
        public static IQueryable<T> WhereAllowed<T>(this IQueryable<T> query)
            where T : IdentifiableEntity
        {
            if (!AuthLogic.IsEnabled)
                return query;

            return WhereIsAllowedFor<T>(query, TypeAllowedBasic.Read, DbQueryProvider.UserInterface);
        }


        [MethodExpander(typeof(WhereAllowedExpander))]
        private static IQueryable<T> WhereIsAllowedFor<T>(this IQueryable<T> query, TypeAllowedBasic allowed, bool userInterface) 
            where T : IdentifiableEntity
        {
            if (!Schema.Current.Tables.ContainsKey(typeof(T)))
                throw new InvalidOperationException("{0} is not included in the schema".Formato(typeof(T)));

            var pairs = (from eg in EntityGroupLogic.GroupsFor(typeof(T))
                         let allowedDN = cache.GetAllowed(eg)
                         let entityGroup = EntityGroupLogic.GetEntityGroupInfo(eg, typeof(T))
                         where !entityGroup.NeverApplicable
                         select new
                         {
                             Key = eg,
                             AllowedIn = allowedDN.InGroup.Get(userInterface) >= allowed,
                             AllowedOut = allowedDN.OutGroup.Get(userInterface) >= allowed,
                             EntityGroup = (EntityGroupLogic.EntityGroupInfo<T>)entityGroup,
                         }).ToList();

            pairs.RemoveAll(p=>p.AllowedIn && p.AllowedOut);

            if (pairs.Count == 0)
                return query;

            if(pairs.Any(p=>p.EntityGroup.IsApplicableExpression == null && !p.AllowedIn && !p.AllowedOut))
                return query.Where(_ => false);

            ParameterExpression e = Expression.Parameter(typeof(T), "e");
            Expression body = pairs.Select(p => 
                {
                    if (p.EntityGroup.IsApplicableExpression == null)
                    {
                        return p.AllowedIn? p.EntityGroup.IsInGroupExpression.InvokeLambda(e):
                                            Expression.Not(p.EntityGroup.IsInGroupExpression.InvokeLambda(e));
                    }
                    else
                    {
                        var notApplicable = (Expression)Expression.Not(p.EntityGroup.IsApplicableExpression.InvokeLambda(e));

                        return p.AllowedIn ? Expression.Or(notApplicable, p.EntityGroup.IsInGroupExpression.InvokeLambda(e)) :
                               p.AllowedOut ? Expression.Or(notApplicable, Expression.Not(p.EntityGroup.IsInGroupExpression.InvokeLambda(e))) :
                               notApplicable;
                    }
                }).Aggregate((a, b) => Expression.And(a, b));

            Expression cleanBody = DbQueryProvider.Clean(body);

            IQueryable<T> result = query.Where(Expression.Lambda<Func<T, bool>>(cleanBody, e));

            return result;
        }

        static Expression InvokeLambda(this LambdaExpression lambda, ParameterExpression p)
        {
            return (Expression)Expression.Invoke(lambda, p);
        }

        class WhereAllowedExpander : IMethodExpander
        {
            static MethodInfo miCallWhereAllowed = ReflectionTools.GetMethodInfo(() => CallWhereAllowed<TypeDN>(null)).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                return (Expression)miCallWhereAllowed.GenericInvoke(typeArguments, null, new object[] { arguments[0] });
            }

            static Expression CallWhereAllowed<T>(Expression expression)
                where T : IdentifiableEntity
            {
                IQueryable<T> query = new Query<T>(DbQueryProvider.Single, expression);
                IQueryable<T> result = WhereAllowed(query);
                return result.Expression;
            }
        }

        class WhereIsAllowedForExpander : IMethodExpander
        {
            static MethodInfo miCallWhereIsAllowedFor = ReflectionTools.GetMethodInfo(() => CallWhereIsAllowedFor<TypeDN>(null, TypeAllowedBasic.Create, true)).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                TypeAllowedBasic allowed = (TypeAllowedBasic)ExpressionEvaluator.Eval(arguments[1]);
                bool userInterface = (bool)ExpressionEvaluator.Eval(arguments[2]);

                return (Expression)miCallWhereIsAllowedFor.GenericInvoke(typeArguments, null, new object[] { arguments[0], allowed, userInterface });
            }

            static Expression CallWhereIsAllowedFor<T>(Expression expression, TypeAllowedBasic allowed, bool userInterface)
                where T : IdentifiableEntity
            {
                IQueryable<T> query = new Query<T>(DbQueryProvider.Single, expression);
                IQueryable<T> result = WhereIsAllowedFor(query, allowed, userInterface);
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

            public override ResultTable ExecuteQuery(QueryRequest request)
            {
                using (EntityGroupAuthLogic.DisableQueries())
                {
                    return base.ExecuteQuery(request);
                }
            }

            public override Lite ExecuteUniqueEntity(UniqueEntityRequest request)
            {
                using (EntityGroupAuthLogic.DisableQueries())
                {
                    return base.ExecuteUniqueEntity(request);
                }
            }

            public override int ExecuteQueryCount(QueryCountRequest request)
            {
                using (EntityGroupAuthLogic.DisableQueries())
                {
                    return base.ExecuteQueryCount(request);
                }
            }
        }

        public static EntityGroupRulePack GetEntityGroupRules(Lite<RoleDN> roleLite)
        {
            EntityGroupRulePack result = new EntityGroupRulePack { Role = roleLite };
            cache.GetRules(result, EnumLogic<EntityGroupDN>.AllEntities());
            return result;
        }

        public static void SetEntityGroupRules(EntityGroupRulePack rules)
        {
            cache.SetRules(rules, r => true); 
        }

        public static EntityGroupAllowedDN GetEntityGroupAllowed(Lite<RoleDN> role, Enum entityGroupKey)
        {
            return cache.GetAllowed(role, entityGroupKey);
        }

        public static EntityGroupAllowedDN GetEntityGroupAllowed(Enum entityGroupKey)
        {
            return cache.GetAllowed(entityGroupKey);
        }

        public static Dictionary<Type, MinMax<TypeAllowedBasic>> GetEntityGroupTypesAllowed(bool userInterface)
        {
            return EntityGroupLogic.Types.ToDictionary(t => t,
                t => EntityGroupLogic.GroupsFor(t)
                    .Select(eg => Possibilities(t, eg, userInterface))
                    .CartesianProduct()
                    .Select(col => col.MinAllowed())
                    .WithMinMaxPair(a => (int)a));
        }

        //returns Create insted of extension when not null
        static TypeAllowedBasic MinAllowed(this IEnumerable<TypeAllowedBasic?> collection)
        {
            TypeAllowedBasic min = TypeAllowedBasic.Create;

            foreach (var item in collection.NotNull())
            {
                if (item < min)
                    min = item;
            }

            return min;
        }

        static IEnumerable<TypeAllowedBasic?> Possibilities(Type type, Enum entityGroup, bool userInterface)
        {
            IEntityGroupInfo egi = EntityGroupLogic.GetEntityGroupInfo(entityGroup, type);

            if(egi.NeverApplicable)
            {
                yield return null;
                yield break; 
            }

            EntityGroupAllowedDN access = cache.GetAllowed(entityGroup);
            yield return access.InGroup.Get(userInterface);
            yield return access.OutGroup.Get(userInterface);

            if (egi.IsApplicableUntypedExpression != null)
                yield return null;
        }
    }

    public enum DisableSaveOptions
    {
        Origin = 1,
        Destiny = 2,
        Both = Origin | Destiny,
        ReEnabled = 0, 
    }
}
