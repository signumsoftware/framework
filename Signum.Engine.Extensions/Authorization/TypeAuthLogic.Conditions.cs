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
    public static partial class TypeAuthLogic
    {
        static readonly Variable<bool> queryFilterDisabled = Statics.ThreadVariable<bool>("queryFilterDisabled");
        public static IDisposable DisableQueryFilter()
        {
            if (queryFilterDisabled.Value) return null;
            queryFilterDisabled.Value = true;
            return new Disposable(() => queryFilterDisabled.Value = false);
        }

        static readonly Variable<bool> saveDisabled = Statics.ThreadVariable<bool>("saveDisabled");
        public static IDisposable DisableSave()
        {
            if (saveDisabled.Value) return null;
            saveDisabled.Value = true;
            return new Disposable(() => saveDisabled.Value = false);
        }

        static IQueryable<T> TypeAuthLogic_FilterQuery<T>(IQueryable<T> query)
            where T : IdentifiableEntity
        {
            if (!queryFilterDisabled.Value)
                return WhereAllowed<T>(query);
            return query;
        }


        const string CreatedKey = "Created";
        const string ModifiedKey = "Modified";

        static void Schema_Saving_Instance(IdentifiableEntity ident)
        {
            if (ident.IsNew)
            {
                var created = (List<IdentifiableEntity>)Transaction.UserData.GetOrCreate(CreatedKey, () => new List<IdentifiableEntity>());
                if (created.Contains(ident))
                    return;

                created.Add(ident);
            }
            else
            {
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

        static void Transaction_PreRealCommit()
        {
            var modified = (List<IdentifiableEntity>)Transaction.UserData.TryGetC(ModifiedKey);

            if (modified != null)
            {
                var groups = modified.GroupBy(e => e.GetType(), e => e.Id);

                //Assert before
                using (Transaction tr = Transaction.ForceNew())
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
            using (DisableQueryFilter())
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

            using (DisableQueryFilter())
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

            using (DisableQueryFilter())
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

            using (DisableQueryFilter())
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

            using (DisableQueryFilter())
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

            var ce = body as ConstantExpression;

            if (ce != null)
            {
                if (((bool)ce.Value))
                    return query;
            }

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

        public static Expression IsAllowedExpression(Expression entity, TypeAllowedBasic requested, ExecutionContext executionContext)
        {
            bool userInterface = executionContext == ExecutionContext.UserInterface;
            
            Type type = entity.Type;
          
            TypeAllowedAndConditions tac = GetAllowed(type);

            Expression baseValue = Expression.Constant(tac.Fallback.Get(userInterface) >= requested);

            var expression = tac.Conditions.Aggregate(baseValue, (acum, tacRule) =>
            {
                var lambda = TypeConditionLogic.GetExpression(type, tacRule.ConditionName);

                var exp = (Expression)Expression.Invoke(lambda, entity);

                if (tacRule.Allowed.Get(userInterface) >= requested)
                    return Expression.Or(exp, acum);
                else
                    return Expression.And(Expression.Not(exp), acum);
            });

            return DbQueryProvider.Clean(expression, false, null);
        }


        static ConstructorInfo ciDebugData = ReflectionTools.GetConstuctorInfo(() => new DebugData(null, TypeAllowedBasic.Create, true, TypeAllowed.Create,  null));
        static ConstructorInfo ciGroupDebugData = ReflectionTools.GetConstuctorInfo(() => new ConditionDebugData(null, true, TypeAllowed.Create));
        static MethodInfo miToLite = ReflectionTools.GetMethodInfo((IdentifiableEntity a) => a.ToLite()).GetGenericMethodDefinition();

        internal static Expression IsAllowedExpressionDebug(Expression entity, TypeAllowedBasic requested, ExecutionContext executionContext)
        {
            bool userInterface = executionContext == ExecutionContext.UserInterface;

            Type type = entity.Type;

            TypeAllowedAndConditions tac = GetAllowed(type);

            Expression baseValue = Expression.Constant(tac.Fallback.Get(userInterface) >= requested);

            var list = (from line in tac.Conditions
                        select Expression.New(ciGroupDebugData, Expression.Constant(line.ConditionName, typeof(Enum)),
                        Expression.Invoke(TypeConditionLogic.GetExpression(type, line.ConditionName), entity),
                        Expression.Constant(line.Allowed))).ToArray();

            Expression newList = Expression.ListInit(Expression.New(typeof(List<ConditionDebugData>)), list);

            Expression liteEntity = Expression.Call(null, miToLite.MakeGenericMethod(entity.Type), entity);

            return Expression.New(ciDebugData, liteEntity, 
                Expression.Constant(requested), 
                Expression.Constant(userInterface), 
                Expression.Constant(tac.Fallback),
                newList);
        }

        public class DebugData
        {
            public DebugData(Lite lite, TypeAllowedBasic requested, bool userInterface, TypeAllowed fallback, List<ConditionDebugData> groups)
            {
                this.Lite = lite;
                this.Requested = requested;
                this.Fallback = fallback;
                this.UserInterface = userInterface;
                this.Conditions = groups;
            }
            
            public Lite Lite { get; private set; }
            public TypeAllowedBasic Requested { get; private set; }
            public TypeAllowed Fallback { get; private set; }
            public bool UserInterface { get; private set; }

            public List<ConditionDebugData> Conditions { get; private set; }

            public bool IsAllowed
            {
                get
                {
                    foreach (var item in Conditions.AsEnumerable().Reverse())
                    {
                        if(item.InGroup)
                            return Requested <= item.Allowed.Get(UserInterface);
                    }

                    return Requested <= Fallback.Get(UserInterface);
                }
            }

            public string Error 
            {
                get
                {
                    foreach (var cond in Conditions.AsEnumerable().Reverse())
                    {
                        if (cond.InGroup)
                            return Requested <= cond.Allowed.Get(UserInterface) ? null :
                                "{0} belongs to {1} that is {2} (less than {3})".Formato(Lite, cond.ConditionName, cond.Allowed.Get(UserInterface), Requested);
                    }

                    return Requested <= Fallback.Get(UserInterface) ? null :
                        "The base value for {0} is {1} (less than {2}) and {3} does not belong to any condition".Formato(Lite.RuntimeType.TypeName(), Fallback.Get(UserInterface), Requested, Lite);
                }
            }
        }

        public class ConditionDebugData
        {
            public Enum ConditionName { get; private set; }
            public bool InGroup { get; private set; }
            public TypeAllowed Allowed { get; private set; }

            internal ConditionDebugData(Enum conditionName, bool inGroup, TypeAllowed allowed)
            {
                this.ConditionName = conditionName;
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
                using (TypeAuthLogic.DisableQueryFilter())
                {
                    return base.ExecuteQuery(request);
                }
            }

            public override Lite ExecuteUniqueEntity(UniqueEntityRequest request)
            {
                using (TypeAuthLogic.DisableQueryFilter())
                {
                    return base.ExecuteUniqueEntity(request);
                }
            }

            public override int ExecuteQueryCount(QueryCountRequest request)
            {
                using (TypeAuthLogic.DisableQueryFilter())
                {
                    return base.ExecuteQueryCount(request);
                }
            }
        }

        public static RuleTypeDN ToRuleType(this TypeAllowedAndConditions allowed, Lite<RoleDN> role, TypeDN resource)
        {
            return new RuleTypeDN
            {
                Role = role,
                Resource = resource,
                Allowed = allowed.Fallback,
                Conditions = allowed.Conditions.Select(a => new RuleTypeConditionDN
                {
                    Allowed = a.Allowed,
                    Condition = MultiEnumLogic<TypeConditionNameDN>.ToEntity(a.ConditionName)
                }).ToMList()
            };
        }

        public static TypeAllowedAndConditions ToTypeAllowedAndConditions(this RuleTypeDN rule)
        {
            return new TypeAllowedAndConditions(rule.Allowed,
                rule.Conditions.Select(c => new TypeConditionRule(MultiEnumLogic<TypeConditionNameDN>.ToEnum(c.Condition), c.Allowed)).ToReadOnly());
        }
    }
}
