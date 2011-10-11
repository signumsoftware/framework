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
using Signum.Engine.Exceptions;
using System.Data.SqlClient;
using System.Xml.Linq;

namespace Signum.Engine.Authorization
{
    public static class EntityGroupAuthLogic
    {
        static Lazy<Dictionary<Lite<RoleDN>, List<EntityGroupLine>>> cache = Schema.GlobalLazy(() =>
        {
            var allGroups = EntityGroupLogic.Groups.And(null);

            var dic = Database.Query<RuleEntityGroupDN>().ToList().AgGroupToDictionary(a => a.Role, gr =>
            {
                return (from k in allGroups
                        let eg = k == null ? null : EnumLogic<EntityGroupDN>.ToEntity(k)
                        select new EntityGroupLine
                        {
                            Key = k,
                            Allowed = gr.SingleOrDefaultEx(sd => sd.Resource.Is(eg)).TryCS(r => r.Allowed) ?? TypeAllowed.Create,
                            Priority = gr.SingleOrDefaultEx(sd => sd.Resource.Is(eg)).TryCS(r => r.Priority) ?? 0
                        }).OrderByDescending(a => a.Priority).ToList();
            });

            var rolesGraph = AuthLogic.RolesGraph();

            foreach (var role in rolesGraph)
            {
                var list = dic.GetOrCreate(role, new Func<List<EntityGroupLine>>(() =>
                {
                    var roles = rolesGraph.RelatedTo(role);
                    if (roles.IsEmpty())
                        return allGroups.Select(k => new EntityGroupLine
                        {
                            Key = k,
                            Allowed = TypeAllowed.Create,
                            Priority = 0
                        }).ToList();
                    else
                        return roles.Select(r => dic[r]).SingleOrDefaultEx(() =>
                            "Ambigous entity group rule script for role {0} (inheriting {1})".Formato(role, roles.CommaAnd()));
                }));
            }
          
            return dic;
        }); 
        
        //static AuthCache<RuleEntityGroupDN, EntityGroupAllowedRule, EntityGroupDN, Enum, EntityGroupAllowedDN> cache;

        //public static IManualAuth<Enum, EntityGroupAllowedDN> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public static void Start(SchemaBuilder sb, bool registerEntitiesWithNoGroup)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                EntityGroupLogic.Start(sb);

                sb.Include<RuleEntityGroupDN>();

                sb.AddUniqueIndex<RuleEntityGroupDN>(r => new { r.Role, r.Resource }); 

                if (registerEntitiesWithNoGroup)
                {
                    PermissionAuthLogic.RegisterPermissions(BasicPermissions.EntitiesWithNoGroup);

                    Schema.Current.IsAllowedCallback += t => BasicPermissions.EntitiesWithNoGroup.IsAuthorized() || EntityGroupLogic.GroupsFor(t).Any() ? null :
                        "The entity '{0}' has no EntityGroups registered and the permission '{1}' is denied".Formato(t.NiceName(), BasicPermissions.EntitiesWithNoGroup.NiceToString());
                }

                sb.Schema.Initializing[InitLevel.Level0SyncEntities] += Schema_InitializingRegisterEvents;

                AuthLogic.ExportToXml += AuthLogic_ExportToXml;
                AuthLogic.ImportFromXml += AuthLogic_ImportFromXml;
         
                sb.Schema.Initializing[InitLevel.Level1SimpleEntities] += Schema_InitializingCache;
                sb.Schema.EntityEvents<RuleEntityGroupDN>().Saving += Schema_Saving;
                AuthLogic.RolesModified += InvalidateCache;

                sb.Schema.Table<RuleEntityGroupDN>().PreDeleteSqlSync += AuthCache_PreDeleteSqlSync;
            }
        }

        static SqlPreCommand AuthLogic_ImportFromXml(XElement element, Dictionary<string, Lite<RoleDN>> roles)
        {
            var current = Database.RetrieveAll<RuleEntityGroupDN>().GroupToDictionary(a => a.Role);
            var should = element.Element("EntityGroups").Elements("Role").ToDictionary(x => roles[x.Attribute("Name").Value]);

            Table table = Schema.Current.Table(typeof(RuleEntityGroupDN));

            return Synchronizer.SynchronizeScript(current, should,
              (role, listRules) => listRules.Select(rt => table.DeleteSqlSync(rt)).Combine(Spacing.Simple),
              (role, x) =>
              {
                  return (from xr in x.Elements("EntityGroup")
                          let r = EnumLogic<EntityGroupDN>.ToEntity(xr.Attribute("Resource").Value)
                          let a = xr.Attribute("Allowed").Value.ToEnum<TypeAllowed>()
                          select table.InsertSqlSync(new RuleEntityGroupDN
                          {
                              Resource = r,
                              Role = role,
                              Allowed = a
                          }, Comment(role, r, a))).Combine(Spacing.Simple);
              },
              (role, list, x) =>
              {
                  return Synchronizer.SynchronizeScript(
                      list.Where(a => a.Resource != null).ToDictionary(a => a.Resource),
                      x.Elements("EntityGroup").ToDictionary(a => EnumLogic<EntityGroupDN>.ToEntity(a.Attribute("Resource").Value)),
                      (r, rt) => table.DeleteSqlSync(rt, Comment(role, r, rt.Allowed)),
                      (r, xr) =>
                      {
                          var a = xr.Attribute("Allowed").Value.ToEnum<TypeAllowed>();
                          return table.InsertSqlSync(new RuleEntityGroupDN { Resource = r, Role = role, Allowed = a }, Comment(role, r, a));
                      },
                      (r, pr, xr) =>
                      {
                          var oldA = pr.Allowed;
                          pr.Allowed = xr.Attribute("Allowed").Value.ToEnum<TypeAllowed>();
                          return table.UpdateSqlSync(pr, Comment(role, r, oldA, pr.Allowed));
                      }, Spacing.Simple);
              }, Spacing.Double);

        }

        internal static string Comment(Lite<RoleDN> role, EntityGroupDN resource, TypeAllowed allowed)
        {
            return "{0} {1} for {2} ({3})".Formato(typeof(EntityGroupDN).NiceName(), resource.ToStr, role, allowed);
        }

        internal static string Comment(Lite<RoleDN> role, EntityGroupDN resource, TypeAllowed from, TypeAllowed to)
        {
            return "{0} {1} for {2} ({3} -> {4})".Formato(typeof(EntityGroupDN).NiceName(), resource.ToStr, role, from, to);
        }

        static XElement AuthLogic_ExportToXml()
        {
            var list = Database.RetrieveAll<RuleEntityGroupDN>();

            var specificRules = list.Where(a => a.Resource != null).AgGroupToDictionary(a => a.Role, gr => gr.ToDictionary(a => a.Resource, a => a.Allowed));

            return new XElement("EntityGroups",
                (from r in AuthLogic.RolesInOrder()
                 select new XElement("Role",
                     new XAttribute("Name", r.ToStr),
                     specificRules.TryGetC(r).TryCC(dic =>
                         from kvp in dic
                         let resource = kvp.Key.Key
                         let allowed = kvp.Value.ToString()
                         orderby resource
                         select new XElement("EntityGroup",
                            new XAttribute("Resource", resource),
                            new XAttribute("Allowed", allowed))
                     ))
                 ));
        }

        static SqlPreCommand AuthCache_PreDeleteSqlSync(IdentifiableEntity arg)
        {
            var t = Schema.Current.Table<RuleEntityGroupDN>();
            var f = (FieldReference)t.Fields["resource"].Field;

            var param = SqlParameterBuilder.CreateReferenceParameter("id", false, arg.Id);

            return new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2}".Formato(t.Name, f.Name, param.ParameterName), new List<SqlParameter> { param });
        }

        static void Schema_Saving(RuleEntityGroupDN rule)
        {
            Transaction.RealCommit += () => InvalidateCache();
        }

        static void Schema_InitializingCache()
        {
            cache.Load();
        }

        static void InvalidateCache()
        {
            cache.ResetPublicationOnly();
        }

    
        static void Schema_InitializingRegisterEvents()
        {
            cache.Load();

            foreach (var type in EntityGroupLogic.Types)
            {
                miRegister.GetInvoker(type)(Schema.Current);
            }
        }

        static GenericInvoker<Action<Schema>> miRegister = 
            new GenericInvoker<Action<Schema>>(s => EntityGroupAuthLogic.RegisterSchemaEvent<TypeDN>(s));
        static void RegisterSchemaEvent<T>(Schema sender)
             where T : IdentifiableEntity
        {
            sender.EntityEvents<T>().Saving += new SavingEventHandler<T>(EntityGroupAuthLogic_Saving);
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


        const string CreatedKey = "Created";
        const string ModifiedKey = "Modified";

        static void EntityGroupAuthLogic_Saving<T>(T ident)
            where T : IdentifiableEntity
        {
            if (!Schema.Current.InGlobalMode && AuthLogic.IsEnabled && !saveDisabled && ident.Modified.Value)
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

        private static bool IsAllwaysAllowed(Type type, TypeAllowedBasic allowed)
        {
            var mm = MinMaxPair(ExecutionContext.Current == ExecutionContext.UserInterface, type);

            return mm.Min >= allowed;
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


        static GenericInvoker<Action<int[], TypeAllowedBasic>> miAssertAllowed = 
            new GenericInvoker<Action<int[], TypeAllowedBasic>>((a, tab) => AssertAllowed<IdentifiableEntity>(a, tab));
        static void AssertAllowed<T>(int[] requested, TypeAllowedBasic typeAllowed)
            where T : IdentifiableEntity
        {
            using (DisableQueries())
            {
                var found = Database.Query<T>().Where(a => requested.Contains(a.Id)).Select(a => new
                {
                    a.Id,
                    Allowed = a.IsAllowedFor(typeAllowed, ExecutionContext.Current),
                }).ToArray();

                if (found.Length != requested.Length)
                    throw new EntityNotFoundException(typeof(T), requested.Except(found.Select(a => a.Id)).ToArray());

                int[] notFound = found.Where(a => !a.Allowed).Select(a => a.Id).ToArray();
                if (notFound.Any())
                {
                    List<DebugData> debugInfo = Database.Query<T>().Where(a => notFound.Contains(a.Id))
                        .Select(a => a.IsAllowedForDebug(typeAllowed, ExecutionContext.Current)).ToList();

                    string details = debugInfo.ToString(a => "  {0} because {1}".Formato(a.Lite, a.Error), "\r\n");

                    throw new UnauthorizedAccessException(Resources.NotAuthorizedTo0The1WithId2.Formato(
                        typeAllowed.NiceToString(),
                        notFound.Length == 1 ? typeof(T).NiceName() : typeof(T).NicePluralName(), notFound.CommaAnd()) + "\r\n" + details);
                }
            }
        }

        public static void AssertAllowed(this IIdentifiable ident, TypeAllowedBasic allowed)
        {
            AssertAllowed(ident, allowed, ExecutionContext.Current);
        }

        public static void AssertAllowed(this IIdentifiable ident, TypeAllowedBasic allowed, ExecutionContext executionContext)
        {
            if (!ident.IsAllowedFor(allowed, executionContext))
                throw new UnauthorizedAccessException(Resources.NotAuthorizedTo0The1WithId2.Formato(allowed.NiceToString().ToLower(), ident.GetType().NiceName(), ident.Id));
        }

        public static void AssertAllowed(this Lite lite, TypeAllowedBasic allowed)
        {
            AssertAllowed(lite, allowed, ExecutionContext.Current);
        }

        public static void AssertAllowed(this Lite lite, TypeAllowedBasic allowed, ExecutionContext executionContext)
        {
            if (lite.IdOrNull == null)
                AssertAllowed(lite.UntypedEntityOrNull, allowed, executionContext);

            if (!lite.IsAllowedFor(allowed, executionContext))
                throw new UnauthorizedAccessException(Resources.NotAuthorizedTo0The1WithId2.Formato(allowed.NiceToString().ToLower(), lite.RuntimeType.NiceName(), lite.Id));
        }

        [MethodExpander(typeof(IsAllowedForExpander))]
        public static bool IsAllowedFor(this IIdentifiable ident, TypeAllowedBasic allowed)
        {
            return IsAllowedFor(ident, allowed, ExecutionContext.Current);
        }

        [MethodExpander(typeof(IsAllowedForExpander))]
        public static bool IsAllowedFor(this IIdentifiable ident, TypeAllowedBasic allowed, ExecutionContext executionContext)
        {
            return miIsAllowedForEntity.GetInvoker(ident.GetType()).Invoke(ident, allowed, executionContext);
        }

        static GenericInvoker<Func<IIdentifiable, TypeAllowedBasic, ExecutionContext, bool>> miIsAllowedForEntity
            = new GenericInvoker<Func<IIdentifiable, TypeAllowedBasic, ExecutionContext, bool>>((ie, tab, ec) => IsAllowedFor<IdentifiableEntity>((IdentifiableEntity)ie, tab, ec));
        [MethodExpander(typeof(IsAllowedForExpander))]
        static bool IsAllowedFor<T>(this T entity, TypeAllowedBasic allowed, ExecutionContext executionContext)
            where T : IdentifiableEntity
        {
            if (!AuthLogic.IsEnabled)
                return true;

            if (entity.IsNew)
                throw new InvalidOperationException("The entity {0} is new".Formato(entity));

            using (DisableQueries())
                return entity.InDB().WhereIsAllowedFor(allowed, executionContext).Any();
        }

        [MethodExpander(typeof(IsAllowedForExpander))]
        public static bool IsAllowedFor(this Lite lite, TypeAllowedBasic allowed)
        {
            return IsAllowedFor(lite, allowed, ExecutionContext.Current);
        }

        [MethodExpander(typeof(IsAllowedForExpander))]
        public static bool IsAllowedFor(this Lite lite, TypeAllowedBasic allowed, ExecutionContext executionContext)
        {
            return miIsAllowedForLite.GetInvoker(lite.RuntimeType).Invoke(lite, allowed, executionContext);
        }

        static GenericInvoker<Func<Lite, TypeAllowedBasic, ExecutionContext, bool>> miIsAllowedForLite =
            new GenericInvoker<Func<Lite, TypeAllowedBasic, ExecutionContext, bool>>((l, tab, ec) => IsAllowedFor<IdentifiableEntity>(l, tab, ec));
        [MethodExpander(typeof(IsAllowedForExpander))]
        static bool IsAllowedFor<T>(this Lite lite, TypeAllowedBasic allowed, ExecutionContext executionContext)
            where T : IdentifiableEntity
        {
            if (!AuthLogic.IsEnabled)
                return true;

            using (DisableQueries())
                return lite.ToLite<T>().InDB().WhereIsAllowedFor(allowed, executionContext).Any();
        }

        class IsAllowedForExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                TypeAllowedBasic allowed = (TypeAllowedBasic)ExpressionEvaluator.Eval(arguments[1]);

                ExecutionContext executionContext = arguments.Length == 3 ? (ExecutionContext)ExpressionEvaluator.Eval(arguments[2]) :
                    ExecutionContext.Current;

                Expression exp = arguments[0].Type.IsLite() ? Expression.Property(arguments[0], "Entity") : arguments[0];

                return IsAllowedExpression(exp, allowed, executionContext) ?? Expression.Constant(true);
            }
        }

        [MethodExpander(typeof(IsAllowedForDebugExpander))]
        public static DebugData IsAllowedForDebug(this IIdentifiable ident, TypeAllowedBasic allowed, ExecutionContext executionContext)
        {
            return miIsAllowedForDebugEntity.GetInvoker(ident.GetType()).Invoke((IdentifiableEntity)ident, allowed, executionContext);
        }

        static GenericInvoker<Func<IIdentifiable, TypeAllowedBasic, ExecutionContext, DebugData>> miIsAllowedForDebugEntity =
            new GenericInvoker<Func<IIdentifiable, TypeAllowedBasic, ExecutionContext, DebugData>>((ii, tab, ec) => IsAllowedForDebug<IdentifiableEntity>((IdentifiableEntity)ii, tab, ec));
        [MethodExpander(typeof(IsAllowedForDebugExpander))]
        static DebugData IsAllowedForDebug<T>(this T entity, TypeAllowedBasic allowed, ExecutionContext executionContext)
            where T : IdentifiableEntity
        {
            if (!AuthLogic.IsEnabled)
                return null;

            if (entity.IsNew)
                throw new InvalidOperationException("The entity {0} is new".Formato(entity));

            using (DisableQueries())
                return entity.InDB().Select(e => e.IsAllowedForDebug(allowed, executionContext)).SingleEx();
        } 

        [MethodExpander(typeof(IsAllowedForDebugExpander))]
        public static DebugData IsAllowedForDebug(this Lite lite, TypeAllowedBasic allowed, ExecutionContext executionContext)
        {
            return miIsAllowedForDebugLite.GetInvoker(lite.RuntimeType).Invoke(lite, allowed, executionContext);
        }

        static GenericInvoker<Func<Lite, TypeAllowedBasic, ExecutionContext, DebugData>> miIsAllowedForDebugLite =
            new GenericInvoker<Func<Lite, TypeAllowedBasic, ExecutionContext, DebugData>>((l, tab, ec) => IsAllowedForDebug<IdentifiableEntity>(l, tab, ec));
        [MethodExpander(typeof(IsAllowedForDebugExpander))]
        static DebugData IsAllowedForDebug<T>(this Lite lite, TypeAllowedBasic allowed, ExecutionContext executionContext)
             where T : IdentifiableEntity
        {
            if (!AuthLogic.IsEnabled)
                return null;

            using (DisableQueries())
                return lite.ToLite<T>().InDB().Select(a => a.IsAllowedForDebug(allowed, executionContext)).SingleEx();
        }

        class IsAllowedForDebugExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                TypeAllowedBasic allowed = (TypeAllowedBasic)ExpressionEvaluator.Eval(arguments[1]);

                ExecutionContext executionContext = arguments.Length == 3 ? (ExecutionContext)ExpressionEvaluator.Eval(arguments[2]) :
                    ExecutionContext.Current;

                Expression exp = arguments[0].Type.IsLite() ? Expression.Property(arguments[0], "Entity") : arguments[0];

                return IsAllowedExpressionDebug(exp, allowed, executionContext);
            }
        }


        [MethodExpander(typeof(WhereAllowedExpander))]
        public static IQueryable<T> WhereAllowed<T>(this IQueryable<T> query)
            where T : IdentifiableEntity
        {
            if (Schema.Current.InGlobalMode || !AuthLogic.IsEnabled)
                return query;

            return WhereIsAllowedFor<T>(query, TypeAllowedBasic.Read, ExecutionContext.Current);
        }


        [MethodExpander(typeof(WhereIsAllowedForExpander))]
        public static IQueryable<T> WhereIsAllowedFor<T>(this IQueryable<T> query, TypeAllowedBasic allowed, ExecutionContext executionContext)
            where T : IdentifiableEntity
        {
            ParameterExpression e = Expression.Parameter(typeof(T), "e");

            Expression body = IsAllowedExpression(e, allowed, executionContext);

            if (body == null)
                return query;

            IQueryable<T> result = query.Where(Expression.Lambda<Func<T, bool>>(body, e));

            return result;
        }

        class WhereAllowedExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                return miCallWhereAllowed.GetInvoker(mi.GetGenericArguments()).Invoke(arguments[0]);
            }

            static GenericInvoker<Func<Expression, Expression>> miCallWhereAllowed = new GenericInvoker<Func<Expression, Expression>>(exp => CallWhereAllowed<TypeDN>(exp));
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
            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                TypeAllowedBasic allowed = (TypeAllowedBasic)ExpressionEvaluator.Eval(arguments[1]);
                ExecutionContext context = (ExecutionContext)ExpressionEvaluator.Eval(arguments[2]);

                return miCallWhereIsAllowedFor.GetInvoker(mi.GetGenericArguments())(arguments[0], allowed, context);
            }

            static GenericInvoker<Func<Expression, TypeAllowedBasic, ExecutionContext, Expression>> miCallWhereIsAllowedFor = new GenericInvoker<Func<Expression, TypeAllowedBasic, ExecutionContext, Expression>>((ex, tab, ec) => CallWhereIsAllowedFor<TypeDN>(ex, tab, ec));
            static Expression CallWhereIsAllowedFor<T>(Expression expression, TypeAllowedBasic allowed, ExecutionContext executionContext)
                where T : IdentifiableEntity
            {
                IQueryable<T> query = new Query<T>(DbQueryProvider.Single, expression);
                IQueryable<T> result = WhereIsAllowedFor(query, allowed, executionContext);
                return result.Expression;
            }
        }

        public static Expression IsAllowedExpression(Expression entity, TypeAllowedBasic allowed, ExecutionContext executionContext)
        {
            bool userInterface = executionContext == ExecutionContext.UserInterface;

            Expression True = Expression.Constant(true);

            var expression = cache.Value[RoleDN.Current.ToLite()].AsEnumerable().Reverse().Aggregate(True, (acum, line)=>
            {
                var inv = GetExpression(line.Key, entity);

                if (inv == null)
                    return acum;

                if (line.Allowed.Get(userInterface) >= allowed)
                    return SmartOr(inv, acum);
                else
                    return SmartAnd(SmartNot(inv), acum);
            });

            return DbQueryProvider.Clean(expression, false);
        }

        private static Expression GetExpression(Enum key, Expression entity)
        {
            if (key == null)
                return Expression.Constant(true);

            var lambda = EntityGroupLogic.TryEntityGroupExpression(key, entity.Type);

            if (lambda == null)
                return null;

            if (lambda.Body.NodeType == ExpressionType.Constant)
                return lambda.Body;

            return (Expression)Expression.Invoke(lambda, entity); 
        }

        static Expression SmartAnd(Expression a, Expression b)
        {
            var valA = a.SimpleValue(); 
            if(valA == true)
                return b;

            var valB = b.SimpleValue(); 
            if(valB == true)
                return a;

            if(valA == false || valB == false)
                 return Expression.Constant(false);

            return Expression.And(a, b); 
        }

        static Expression SmartOr(Expression a, Expression b)
        {
            var valA = a.SimpleValue();
            if (valA == false)
                return b;

            var valB = b.SimpleValue();
            if (valB == false)
                return a;

            if (valA == true || valB == true)
                return Expression.Constant(true);

            return Expression.Or(a, b);
        }

        static Expression SmartNot(Expression a)
        {
            var val = a.SimpleValue();
            if (val.HasValue)
                return Expression.Constant(!val.Value);

            return Expression.Not(a);
        }

        static ConstructorInfo ciDebugData = ReflectionTools.GetConstuctorInfo(() => new DebugData(null, TypeAllowed.Create, true,  null));
        static ConstructorInfo ciGroupDebugData = ReflectionTools.GetConstuctorInfo(() => new GroupDebugData(null, true, TypeAllowed.Create));
        static MethodInfo miToLite = ReflectionTools.GetMethodInfo((IdentifiableEntity a) => a.ToLite()).GetGenericMethodDefinition();

        internal static Expression IsAllowedExpressionDebug(Expression entity, TypeAllowedBasic allowed, ExecutionContext executionContext)
        {
            bool userInterface = executionContext == ExecutionContext.UserInterface;

            Type type = entity.Type;

            var list = (from line in cache.Value[RoleDN.Current.ToLite()]
                        let exp = GetExpression(line.Key, entity)
                        where exp != null
                        select Expression.New(ciGroupDebugData, Expression.Constant(line.Key), exp, Expression.Constant( line.Allowed))).ToArray();

            Expression newList = Expression.ListInit(Expression.New(typeof(List<GroupDebugData>)), list);



            Expression liteEntity = Expression.Call(null, miToLite.MakeGenericMethod(entity.Type), entity);

            return Expression.New(ciDebugData, liteEntity, Expression.Constant(allowed), Expression.Constant(userInterface), newList);
        }

        public class DebugData
        {
            public DebugData(Lite lite, TypeAllowed requested, bool userInterface, List<GroupDebugData> groups)
            {
                this.Lite = lite;
                this.Requested = requested;
                this.UserInterface = userInterface;
                this.Groups = groups;
            }
            
            public Lite Lite { get; private set; }
            public TypeAllowed Requested { get; private set; }
            public bool UserInterface { get; private set; }

            public List<GroupDebugData> Groups { get; private set; }

            public bool Allowed
            {
                get
                {
                    foreach (var item in Groups.AsEnumerable())
                    {
                        if(item.InGroup)
                            return Requested.Get(UserInterface)<= item.Allowed.Get(UserInterface);
                    }

                    return true;
                }
            }

            public string Error 
            {
                get
                {
                    var item = Groups.FirstOrDefault(a => a.InGroup);

                    if (item == null || Requested.Get(UserInterface) <= item.Allowed.Get(UserInterface))
                        return null;

                    return "{0} belongs to {1} that is {2} (less than {3})".Formato(Lite, item.Key, item.Allowed, item.Allowed.Get(UserInterface), Requested.Get(UserInterface));
                }
            }
        }

        public class GroupDebugData
        {
            public Enum Key { get; private set; }
            public bool InGroup { get; private set; }
            public TypeAllowed Allowed { get; private set; }

            internal GroupDebugData(Enum key, bool inGroup, TypeAllowed allowed)
            {
                this.Key = key;
                this.InGroup = inGroup;
                this.Allowed = allowed;
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
            return new EntityGroupRulePack(roleLite)
            {
                Rules = cache.Value[roleLite].Select(a => new EntityGroupAllowedRule
                {
                    Allowed = a.Allowed,
                    Resource = a.Key,
                    Priority = a.Priority,
                }).ToMList(),
            };
        }

        public static void SetEntityGroupRules(EntityGroupRulePack rules)
        {
            using (Transaction tr = new Transaction(true))
            {
                Database.Query<RuleEntityGroupDN>().Where(r => r.Role == rules.Role).UnsafeDelete();

                rules.Rules.Select(r => new RuleEntityGroupDN
                {
                    Resource = r.Resource == null ? null : EnumLogic<EntityGroupDN>.ToEntity(r.Resource),
                    Role = rules.Role,
                    Priority = r.Priority,
                    Allowed = r.Allowed,
                }).SaveList();

                tr.Commit();
            }
        }

        public static Dictionary<Type, MinMax<TypeAllowedBasic>> GetEntityGroupTypesAllowed(bool userInterface)
        {
            return EntityGroupLogic.Types.ToDictionary(t => t,
                t => MinMaxPair(userInterface, t));
        }

        private static MinMax<TypeAllowedBasic> MinMaxPair(bool userInterface, Type t)
        {
            var collection = from line in cache.Value[RoleDN.Current.ToLite()]
                             let exp = line.Key == null ? null : EntityGroupLogic.TryEntityGroupExpression(line.Key, t)
                             where line.Key == null || exp != null
                             select new { Allowed = line.Allowed.Get(userInterface), InGroup = line.Key == null ? true : exp.SimpleValue() };

            return collection.Where(a => a.InGroup != false).TakeWhile(a => a.InGroup != true).Select(a => a.Allowed).WithMinMaxPair(a => (int)a);
        }
    }

    public class EntityGroupLine
    {
        public Enum Key;
        public TypeAllowed Allowed;
        public int Priority;
    }
}
