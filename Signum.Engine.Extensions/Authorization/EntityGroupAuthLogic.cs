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

                AuthLogic.ExportToXml += () => cache.ExportXml("EntityGroups", "EntityGroup", p => p.Key, b => b.ToString());
                AuthLogic.ImportFromXml += (x, roles) => cache.ImportXml(x, "EntityGroups", "EntityGroup", roles, EnumLogic<EntityGroupDN>.ToEntity, EntityGroupAllowedDN.Parse);
            }
        }

        static void Schema_InitializingRegisterEvents(Schema sender)
        {
            foreach (var type in EntityGroupLogic.Types)
            {
                miRegister.GetInvoker(type)(sender);
            }
        }

        static GenericInvoker miRegister = GenericInvoker.Create(() => EntityGroupAuthLogic.RegisterSchemaEvent<TypeDN>(null));
        static void RegisterSchemaEvent<T>(Schema sender)
             where T : IdentifiableEntity
        {
            sender.EntityEvents<T>().Saving += new EntityEventHandler<T>(EntityGroupAuthLogic_Saving);
            sender.EntityEvents<T>().Retrieving += new RetrivingEntitiesEventHandler(EntityGroupAuthLogic_Retrieving);
            sender.EntityEvents<T>().FilterQuery += new FilterQueryEventHandler<T>(EntityGroupAuthLogic_FilterQuery);
        }

        [ThreadStatic]
        static bool queriesDisabled;
        public static IDisposable DisableQueries()
        {
            bool oldQueriesDisabled = queriesDisabled;
            queriesDisabled = true;
            return new Disposable(() => queriesDisabled = oldQueriesDisabled);
        }

        [ThreadStatic]
        static bool retrieveDisabled;
        public static IDisposable DisableRetrieve()
        {
            bool oldRetrieveDisabled = retrieveDisabled;
            retrieveDisabled = true;
            return new Disposable(() => retrieveDisabled = oldRetrieveDisabled);
        }

        [ThreadStatic]
        static bool saveDisabled;
        public static IDisposable DisableSave()
        {
            bool oldSaveDisabled = saveDisabled; 
            saveDisabled = true;
            return new Disposable(() => saveDisabled = oldSaveDisabled);
        }

        static IQueryable<T> EntityGroupAuthLogic_FilterQuery<T>(IQueryable<T> query)
            where T : IdentifiableEntity
        {
            if (!queriesDisabled)
                return WhereAllowed<T>(query);
            return query;
        }

        static void EntityGroupAuthLogic_Retrieving(Type type, int[] ids, bool inQuery)
        {
            if (AuthLogic.IsEnabled && !retrieveDisabled && (!inQuery || queriesDisabled)  && !IsAllwaysAllowed(type, TypeAllowedBasic.Read))
            {
                miAssertAllowed.GetInvoker(type)(ids, TypeAllowedBasic.Read); 
            }
        }

        const string CreatedKey = "Created";
        const string ModifiedKey = "Modified"; 

        static void EntityGroupAuthLogic_Saving<T>(T ident, bool isRoot)
            where T : IdentifiableEntity
        {
            if (AuthLogic.IsEnabled && !saveDisabled && ident.Modified.Value)
            {
                if (ident.IsNew)
                {
                    if (IsAllwaysAllowed(typeof(T), TypeAllowedBasic.Create))
                        return;

                    var created = (List<IdentifiableEntity>)Transaction.UserData.GetOrCreate(CreatedKey, () => new List<IdentifiableEntity>());
                    if (created.Contains(ident))
                        return;

                    created.Add(ident);
                }
                else
                {
                    if (IsAllwaysAllowed(typeof(T), TypeAllowedBasic.Modify))
                        return;

                    var modified = (List<IdentifiableEntity>)Transaction.UserData.GetOrCreate(ModifiedKey, () => new List<IdentifiableEntity>());
                    if (modified.Contains(ident))
                        return;

                    var created = (List<IdentifiableEntity>)Transaction.UserData.TryGetC(CreatedKey);
                    if (created != null && created.Contains(ident))
                        return;

                    modified.Add(ident);
                }

                Transaction.PreRealCommit -= Transaction_PreRealCommit;
                Transaction.PreRealCommit += Transaction_PreRealCommit;
            }
        }


        static void Transaction_PreRealCommit()
        {
            var modified = (List<IdentifiableEntity>)Transaction.UserData.TryGetC(ModifiedKey);

            if (modified != null)
            {
                var groups = modified.GroupBy(e => e.GetType(), e => e.Id);

                //Assert before
                using (Transaction tr = new Transaction(true))
                {
                    foreach (var gr in groups)
                        miAssertAllowed.GetInvoker(gr.Key)(gr.ToArray(), TypeAllowedBasic.Modify);

                    tr.Commit();
                }

                //Assert after
                foreach (var gr in groups)
                {
                    miAssertAllowed.GetInvoker(gr.Key)(gr.ToArray(), TypeAllowedBasic.Modify);
                }
            }

            var created = (List<IdentifiableEntity>)Transaction.UserData.TryGetC(CreatedKey);

            if (created != null)
            {
                var groups = created.GroupBy(e => e.GetType(), e => e.Id);

                //Assert after
                foreach (var gr in groups)
                    miAssertAllowed.GetInvoker(gr.Key)(gr.ToArray(), TypeAllowedBasic.Create);
            }
        }


        static GenericInvoker miAssertAllowed = GenericInvoker.Create(() => AssertAllowed<IdentifiableEntity>(null, TypeAllowedBasic.Create));
        static void AssertAllowed<T>(int[] requested, TypeAllowedBasic typeAllowed)
            where T : IdentifiableEntity
        {
            using (DisableQueries())
            {
                int[] found = Database.Query<T>().Where(a => requested.Contains(a.Id)).WhereIsAllowedFor(typeAllowed, Database.UserInterface).Select(a => a.Id).ToArray();

                if (found.Length != requested.Length)
                {
                    int[] notFound = requested.Except(found).ToArray();

                    throw new UnauthorizedAccessException(Resources.NotAuthorizedTo0The1WithId2.Formato(
                        typeAllowed.NiceToString(),
                        notFound.Length == 1 ? typeof(T).NiceName() : typeof(T).NicePluralName(),
                        notFound.Length == 1 ? notFound[0].ToString() : notFound.CommaAnd()));
                }
            }
        }

        public static void AssertAllowed(this IIdentifiable ident, TypeAllowedBasic allowed)
        {
            AssertAllowed(ident, allowed, Database.UserInterface); 
        }

        public static void AssertAllowed(this IIdentifiable ident, TypeAllowedBasic allowed, bool userInterface)
        {
            if (!ident.IsAllowedFor(allowed, userInterface))
                throw new UnauthorizedAccessException(Resources.NotAuthorizedTo0The1WithId2.Formato(allowed.NiceToString().ToLower(), ident.GetType().NiceName(), ident.Id));
        }

        [MethodExpander(typeof(IsAllowedForExpander))]
        public static bool IsAllowedFor(this IIdentifiable ident, TypeAllowedBasic allowed)
        {
            return IsAllowedFor(ident, allowed, Database.UserInterface); 
        }

        [MethodExpander(typeof(IsAllowedForExpander))]
        public static bool IsAllowedFor(this IIdentifiable ident, TypeAllowedBasic allowed, bool userInterface)
        {
            return (bool)miIsAllowedForEntity.GetInvoker(ident.GetType()).Invoke(ident, allowed, userInterface);
        }

        static GenericInvoker miIsAllowedForEntity = GenericInvoker.Create(() => IsAllowedFor<IdentifiableEntity>((IdentifiableEntity)null, TypeAllowedBasic.Create, true));
        static bool IsAllowedFor<T>(this T entity, TypeAllowedBasic allowed, bool userInterface)
            where T : IdentifiableEntity
        {
            if (!AuthLogic.IsEnabled)
                return true;

            if (entity.IsNew)
                throw new InvalidOperationException("The entity {0} is new");

            using (DisableQueries())
                return entity.InDB().WhereIsAllowedFor(allowed, userInterface).Any();
        }

        public static void AssertAllowed(this Lite lite, TypeAllowedBasic allowed)
        {
            AssertAllowed(lite, allowed, Database.UserInterface);
        }

        public static void AssertAllowed(this Lite lite, TypeAllowedBasic allowed, bool userInterface)
        {
            if (lite.IdOrNull == null)
                AssertAllowed(lite.UntypedEntityOrNull, allowed, userInterface);

            if (!lite.IsAllowedFor(allowed, userInterface))
                throw new UnauthorizedAccessException(Resources.NotAuthorizedTo0The1WithId2.Formato(allowed.NiceToString().ToLower(), lite.RuntimeType.NiceName(), lite.Id));
        }

        [MethodExpander(typeof(IsAllowedForExpander))]
        public static bool IsAllowedFor(this Lite lite, TypeAllowedBasic allowed)
        {
            return IsAllowedFor(lite, allowed, Database.UserInterface); 
        }

        [MethodExpander(typeof(IsAllowedForExpander))]
        public static bool IsAllowedFor(this Lite lite, TypeAllowedBasic allowed, bool userInterface)
        {
            return (bool)miIsAllowedForLite.GetInvoker(lite.RuntimeType).Invoke(lite, allowed, userInterface);
        }

        static GenericInvoker miIsAllowedForLite = GenericInvoker.Create(() => IsAllowedFor<IdentifiableEntity>((Lite)null, TypeAllowedBasic.Create, true));

        static bool IsAllowedFor<T>(this Lite lite, TypeAllowedBasic allowed, bool userInterface)
            where T : IdentifiableEntity
        {
            if (!AuthLogic.IsEnabled)
                return true;

            using (DisableQueries())
                return lite.ToLite<T>().InDB().WhereIsAllowedFor(allowed, userInterface).Any();
        }

        class IsAllowedForExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                TypeAllowedBasic allowed = (TypeAllowedBasic)ExpressionEvaluator.Eval(arguments[1]);

                bool userInterface = arguments.Length == 3 ? (bool)ExpressionEvaluator.Eval(arguments[2]) :
                    Database.UserInterface; 

                Expression exp = arguments[0].Type.IsLite() ? Expression.Property(arguments[0], "Entity") : arguments[0];

                return IsAllowedExpression(exp, allowed, userInterface);
            }
        }


        [MethodExpander(typeof(WhereAllowedExpander))]
        public static IQueryable<T> WhereAllowed<T>(this IQueryable<T> query)
            where T : IdentifiableEntity
        {
            if (!AuthLogic.IsEnabled)
                return query;

            return WhereIsAllowedFor<T>(query, TypeAllowedBasic.Read, Database.UserInterface);
        }


        [MethodExpander(typeof(WhereIsAllowedForExpander))]
        public static IQueryable<T> WhereIsAllowedFor<T>(this IQueryable<T> query, TypeAllowedBasic allowed, bool userInterface)
            where T : IdentifiableEntity
        {
            ParameterExpression e = Expression.Parameter(typeof(T), "e");

            Expression body = IsAllowedExpression(e, allowed, userInterface);

            if (body == null)
                return query;

            IQueryable<T> result = query.Where(Expression.Lambda<Func<T, bool>>(body, e));

            return result;
        }
        
        class EntityGroupTuple
        {
            public Enum Key;
            public bool AllowedIn;
            public bool AllowedOut;
            public IEntityGroupInfo EntityGroup;
        }

        private static List<EntityGroupTuple> GetPairs(Type type, TypeAllowedBasic allowed, bool userInterface)
        {
            if (!Schema.Current.Tables.ContainsKey(type))
                throw new InvalidOperationException("{0} is not included in the schema".Formato(type));

            var pairs = (from eg in EntityGroupLogic.GroupsFor(type)
                         let allowedDN = cache.GetAllowed(eg)
                         let entityGroup = EntityGroupLogic.GetEntityGroupInfo(eg, type)
                         select new EntityGroupTuple
                         {
                             Key = eg,
                             AllowedIn = allowedDN.InGroup.Get(userInterface) >= allowed,
                             AllowedOut = allowedDN.OutGroup.Get(userInterface) >= allowed,
                             EntityGroup = entityGroup,
                         }).ToList();

            pairs.RemoveAll(p => p.AllowedIn && p.AllowedOut);
            pairs.RemoveAll(p => p.EntityGroup.IsApplicable != null && p.EntityGroup.IsApplicable.Resume == false);
            return pairs;
        }

        static bool IsAllwaysAllowed(Type type, TypeAllowedBasic allowed)
        {
            return GetPairs(type, allowed, Database.UserInterface).Empty(); 
        }

        internal static Expression IsAllowedExpression(Expression entity, TypeAllowedBasic allowed, bool userInterface)
        {
            var pairs = GetPairs(entity.Type, allowed, userInterface);

            if (pairs.Count == 0)
                return null;

            if (pairs.Any(p => (p.EntityGroup.IsApplicable == null || p.EntityGroup.IsApplicable.Resume == true) && !p.AllowedIn && !p.AllowedOut))
                return Expression.Constant(false);

            Expression body = pairs.Select(p =>
            {
                if (p.EntityGroup.IsApplicable == null || p.EntityGroup.IsApplicable.Resume == true)
                {
                    return p.AllowedIn ? p.EntityGroup.IsInGroup.UntypedExpression.InvokeLambda(entity) :
                                        Expression.Not(p.EntityGroup.IsInGroup.UntypedExpression.InvokeLambda(entity));
                }
                else
                {
                    var notApplicable = (Expression)Expression.Not(p.EntityGroup.IsApplicable.UntypedExpression.InvokeLambda(entity));

                    return p.AllowedIn ? Expression.Or(notApplicable, p.EntityGroup.IsInGroup.UntypedExpression.InvokeLambda(entity)) :
                           p.AllowedOut ? Expression.Or(notApplicable, Expression.Not(p.EntityGroup.IsInGroup.UntypedExpression.InvokeLambda(entity))) :
                           notApplicable;
                }
            }).Aggregate((a, b) => Expression.And(a, b));

            Expression cleanBody = DbQueryProvider.Clean(body);

            return cleanBody;
        }

        

        static Expression InvokeLambda(this LambdaExpression lambda, Expression p)
        {
            return (Expression)Expression.Invoke(lambda, p);
        }

        class WhereAllowedExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                return (Expression)miCallWhereAllowed.GetInvoker(typeArguments).Invoke(arguments[0]);
            }

            static GenericInvoker miCallWhereAllowed = GenericInvoker.Create(() => CallWhereAllowed<TypeDN>(null));
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

            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                TypeAllowedBasic allowed = (TypeAllowedBasic)ExpressionEvaluator.Eval(arguments[1]);
                bool userInterface = (bool)ExpressionEvaluator.Eval(arguments[2]);

                return (Expression)miCallWhereIsAllowedFor.GetInvoker(typeArguments)(arguments[0], allowed, userInterface);
            }

            static GenericInvoker miCallWhereIsAllowedFor = GenericInvoker.Create(() => CallWhereIsAllowedFor<TypeDN>(null, TypeAllowedBasic.Create, true));
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

            if (egi.IsApplicable != null)
            {
                if (egi.IsApplicable.Resume == false)
                {
                    yield return null;
                    yield break;
                }
                else if (egi.IsApplicable.Resume == null)
                    yield return null;
            }

            EntityGroupAllowedDN access = cache.GetAllowed(entityGroup);
            if ((egi.IsInGroup.Resume ?? true) == true)
                yield return access.InGroup.Get(userInterface);
            if ((egi.IsInGroup.Resume ?? false) == false)
                yield return access.OutGroup.Get(userInterface);
        }
    }

    //public enum DisableSaveOptions
    //{
    //    Origin = 1,
    //    Destiny = 2,
    //    Both = Origin | Destiny,
    //    ReEnabled = 0, 
    //}
}
