using Signum.Engine.Linq;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Utilities.Reflection;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Signum.Engine.Authorization;

public static partial class TypeAuthLogic
{
    static readonly Variable<bool> queryFilterDisabled = Statics.ThreadVariable<bool>("queryFilterDisabled");
    public static IDisposable? DisableQueryFilter()
    {
        if (queryFilterDisabled.Value) return null;
        queryFilterDisabled.Value = true;
        return new Disposable(() => queryFilterDisabled.Value = false);
    }

    public static bool InSave => inSave.Value;//Available for Type Condition definition
    static readonly Variable<bool> inSave = Statics.ThreadVariable<bool>("inSave");
    static IDisposable? OnInSave()
    {
        if (inSave.Value) return null;
        inSave.Value = true;
        return new Disposable(() => inSave.Value = false);
    }

    public static Type? IsDelete => isDelete.Value;
    static readonly Variable<Type?> isDelete = Statics.ThreadVariable<Type?>("isDelete");
    static IDisposable? OnIsDelete(Type type)
    {
        var oldType = isDelete.Value;
        isDelete.Value = type;
        return new Disposable(() => isDelete.Value = type);
    }

    const string CreatedKey = "Created";
    const string ModifiedKey = "Modified";

    static void Schema_Saving_Instance(Entity ident)
    {
        if (ident.IsNew)
        {
            var created = (List<Entity>)Transaction.UserData.GetOrCreate(CreatedKey, () => new List<Entity>());
            if (created.Contains(ident))
                return;

            created.Add(ident);
        }
        else
        {
            if (IsCreatedOrModified(Transaction.TopParentUserData(), ident) ||
               IsCreatedOrModified(Transaction.UserData, ident))
                return;

            var modified = (List<Entity>)Transaction.UserData.GetOrCreate(ModifiedKey, () => new List<Entity>());

            modified.Add(ident);
        }

        Transaction.PreRealCommit -= Transaction_PreRealCommit;
        Transaction.PreRealCommit += Transaction_PreRealCommit;
    }

    private static bool IsCreatedOrModified(Dictionary<string, object> dictionary, Entity ident)
    {
        var modified = (List<Entity>?)dictionary.TryGetC(ModifiedKey);
        if (modified != null && modified.Contains(ident))
            return true;

        var created = (List<Entity>?)dictionary.TryGetC(CreatedKey);
        if (created != null && created.Contains(ident))
            return true;

        return false;
    }

    public static void RemovePreRealCommitChecking(Entity entity)
    {
        var created = (List<Entity>?)Transaction.UserData.TryGetC(CreatedKey);
        if (created != null && created.Contains(entity))
            created.Remove(entity);

        var modified = (List<Entity>?)Transaction.UserData.TryGetC(ModifiedKey);
        if (modified != null && modified.Contains(entity))
            modified.Remove(entity);
    }

    static void Transaction_PreRealCommit(Dictionary<string, object> dic)
    {
        using (OnInSave())
        {
            var modified = (List<Entity>?)dic.TryGetC(ModifiedKey);

            if (modified.HasItems())
            {
                var groups = modified.GroupBy(e => e.GetType(), e => e.Id);

                //Assert before
                using (var tr = Transaction.ForceNew())
                {
                    foreach (var gr in groups)
                        miAssertAllowed.GetInvoker(gr.Key)(gr.ToArray(), TypeAllowedBasic.Write);

                    tr.Commit();
                }

                //Assert after
                foreach (var gr in groups)
                {
                    miAssertAllowed.GetInvoker(gr.Key)(gr.ToArray(), TypeAllowedBasic.Write);
                }
            }

            var created = (List<Entity>?)Transaction.UserData.TryGetC(CreatedKey);

            if (created.HasItems())
            {
                var groups = created.GroupBy(e => e.GetType(), e => e.Id);

                //Assert after
                foreach (var gr in groups)
                    miAssertAllowed.GetInvoker(gr.Key)(gr.ToArray(), TypeAllowedBasic.Write);
            }
        }
    }


    static GenericInvoker<Action<PrimaryKey[], TypeAllowedBasic>> miAssertAllowed =
        new((a, tab) => AssertAllowed<Entity>(a, tab));
    static void AssertAllowed<T>(PrimaryKey[] requested, TypeAllowedBasic typeAllowed)
        where T : Entity
    {
        using (DisableQueryFilter())
        {
            var found = requested.Chunk(1000).SelectMany(gr => Database.Query<T>().Where(a => gr.Contains(a.Id)).Select(a => new
            {
                a.Id,
                Allowed = a.IsAllowedFor(typeAllowed, ExecutionMode.InUserInterface),
            })).ToArray();

            if (found.Length != requested.Length)
                throw new EntityNotFoundException(typeof(T), requested.Except(found.Select(a => a.Id)).ToArray());

            PrimaryKey[] notFound = found.Where(a => !a.Allowed).Select(a => a.Id).ToArray();
            if (notFound.Any())
            {
                List<DebugData> debugInfo = Database.Query<T>().Where(a => notFound.Contains(a.Id))
                    .Select(a => a.IsAllowedForDebug(typeAllowed, ExecutionMode.InUserInterface)).ToList();

                string details = 
                    debugInfo.Count == 1 ? debugInfo.SingleEx().ErrorMessage! : 
                    debugInfo.ToString(a => "  {0}: {1}".FormatWith(a.Lite, a.ErrorMessage), "\r\n");

                throw new UnauthorizedAccessException(AuthMessage.NotAuthorizedTo0The1WithId2.NiceToString().FormatWith(
                    typeAllowed.NiceToString(),
                    notFound.Length == 1 ? typeof(T).NiceName() : typeof(T).NicePluralName(), notFound.CommaAnd()) + "\r\n" + details);
            }
        }
    }

    public static void AssertAllowed(this IEntity ident, TypeAllowedBasic allowed)
    {
        AssertAllowed(ident, allowed, ExecutionMode.InUserInterface);
    }

    public static void AssertAllowed(this IEntity ident, TypeAllowedBasic allowed, bool inUserInterface)
    {
        if (!ident.IsAllowedFor(allowed, inUserInterface))
            throw new UnauthorizedAccessException(AuthMessage.NotAuthorizedTo0The1WithId2.NiceToString().FormatWith(allowed.NiceToString().ToLower(), ident.GetType().NiceName(), ident.Id));
    }

    public static void AssertAllowed(this Lite<IEntity> lite, TypeAllowedBasic allowed)
    {
        AssertAllowed(lite, allowed, ExecutionMode.InUserInterface);
    }

    public static void AssertAllowed(this Lite<IEntity> lite, TypeAllowedBasic allowed, bool inUserInterface)
    {
        if (lite.IdOrNull == null)
            AssertAllowed(lite.Entity, allowed, inUserInterface);

        if (!lite.IsAllowedFor(allowed, inUserInterface))
            throw new UnauthorizedAccessException(AuthMessage.NotAuthorizedTo0The1WithId2.NiceToString().FormatWith(allowed.NiceToString().ToLower(), lite.EntityType.NiceName(), lite.Id));
    }

    [MethodExpander(typeof(IsAllowedForExpander))]
    public static bool IsAllowedFor(this IEntity ident, TypeAllowedBasic allowed)
    {
        return IsAllowedFor(ident, allowed, ExecutionMode.InUserInterface);
    }

    [MethodExpander(typeof(IsAllowedForExpander))]
    public static bool IsAllowedFor(this IEntity ident, TypeAllowedBasic allowed, bool inUserInterface)
    {
        return miIsAllowedForEntity.GetInvoker(ident.GetType()).Invoke(ident, allowed, inUserInterface);
    }

    static GenericInvoker<Func<IEntity, TypeAllowedBasic, bool, bool>> miIsAllowedForEntity
        = new((ie, tab, ec) => IsAllowedFor<Entity>((Entity)ie, tab, ec));
    [MethodExpander(typeof(IsAllowedForExpander))]
    static bool IsAllowedFor<T>(this T entity, TypeAllowedBasic allowed, bool inUserInterface)
        where T : Entity
    {
        if (!AuthLogic.IsEnabled)
            return true;

        var tac = GetAllowed(entity.GetType());

        var min = inUserInterface ? tac.MinUI() : tac.MinDB();

        if (allowed <= min)
            return true;

        var max = inUserInterface ? tac.MaxUI() : tac.MaxDB();

        if (max < allowed)
            return false;

        var inMemoryCodition = IsAllowedInMemory<T>(tac, allowed, inUserInterface);
        if (inMemoryCodition != null)
            return inMemoryCodition(entity);

        using (DisableQueryFilter())
            return entity.InDB().WhereIsAllowedFor(allowed, inUserInterface).Any();
    }

    private static Func<T, bool>? IsAllowedInMemory<T>(TypeAllowedAndConditions tac, TypeAllowedBasic allowed, bool inUserInterface) where T : Entity
    {
        if (tac.ConditionRules.SelectMany(c => c.TypeConditions).Any(tc => TypeConditionLogic.GetInMemoryCondition<T>(tc) == null))
            return null;

        return entity =>
        {
            foreach (var cond in tac.ConditionRules.Reverse())
            {
                if (cond.TypeConditions.All(tc =>
                {
                    var func = TypeConditionLogic.GetInMemoryCondition<T>(tc)!;
                    return func(entity);
                }))
                {
                    return cond.Allowed.Get(inUserInterface) >= allowed;
                }
            }

            return tac.Fallback.Get(inUserInterface) >= allowed;
        };
    }

    [MethodExpander(typeof(IsAllowedForExpander))]
    public static bool IsAllowedFor(this Lite<IEntity> lite, TypeAllowedBasic allowed)
    {
        return IsAllowedFor(lite, allowed, ExecutionMode.InUserInterface);
    }

    [MethodExpander(typeof(IsAllowedForExpander))]
    public static bool IsAllowedFor(this Lite<IEntity> lite, TypeAllowedBasic allowed, bool inUserInterface)
    {
        return miIsAllowedForLite.GetInvoker(lite.EntityType).Invoke(lite, allowed, inUserInterface);
    }

    static GenericInvoker<Func<Lite<IEntity>, TypeAllowedBasic, bool, bool>> miIsAllowedForLite =
        new((l, tab, ec) => IsAllowedFor<Entity>(l, tab, ec));
    [MethodExpander(typeof(IsAllowedForExpander))]
    static bool IsAllowedFor<T>(this Lite<IEntity> lite, TypeAllowedBasic allowed, bool inUserInterface)
        where T : Entity
    {
        if (!AuthLogic.IsEnabled)
            return true;

        using (DisableQueryFilter())
            return ((Lite<T>)lite).InDB().WhereIsAllowedFor(allowed, inUserInterface).Any();
    }

    class IsAllowedForExpander : IMethodExpander
    {
        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            TypeAllowedBasic allowed = (TypeAllowedBasic)ExpressionEvaluator.Eval(arguments[1])!;

            bool inUserInterface = arguments.Length == 3 ? (bool)ExpressionEvaluator.Eval(arguments[2])! : ExecutionMode.InUserInterface;

            Expression exp = arguments[0].Type.IsLite() ? Expression.Property(arguments[0], "Entity") : arguments[0];

            return IsAllowedExpression(exp, allowed, inUserInterface);
        }
    }

    [MethodExpander(typeof(IsAllowedForDebugExpander))]
    public static DebugData IsAllowedForDebug(this IEntity ident, TypeAllowedBasic allowed, bool inUserInterface)
    {
        return miIsAllowedForDebugEntity.GetInvoker(ident.GetType()).Invoke((Entity)ident, allowed, inUserInterface);
    }

    [MethodExpander(typeof(IsAllowedForDebugExpander))]
    public static string? CanBeModified(this IEntity ident)
    {
        var taac = TypeAuthLogic.GetAllowed(ident.GetType());

        if (taac.ConditionRules.IsEmpty())
            return taac.Fallback.GetDB() >= TypeAllowedBasic.Write ? null : AuthAdminMessage.CanNotBeModified.NiceToString();

        if (ident.IsNew)
            return null;

        return IsAllowedForDebug(ident, TypeAllowedBasic.Write, false)?.ErrorMessage;
    }

    static GenericInvoker<Func<IEntity, TypeAllowedBasic, bool, DebugData>> miIsAllowedForDebugEntity =
        new((ii, tab, ec) => IsAllowedForDebug<Entity>((Entity)ii, tab, ec));
    [MethodExpander(typeof(IsAllowedForDebugExpander))]
    static DebugData IsAllowedForDebug<T>(this T entity, TypeAllowedBasic allowed, bool inUserInterface)
        where T : Entity
    {
        if (!AuthLogic.IsEnabled)
            throw new InvalidOperationException("AuthLogic.IsEnabled is false");

        if (entity.IsNew)
            throw new InvalidOperationException("The entity {0} is new".FormatWith(entity));

        using (DisableQueryFilter())
            return entity.InDB().Select(e => e.IsAllowedForDebug(allowed, inUserInterface)).SingleEx();
    }

    [MethodExpander(typeof(IsAllowedForDebugExpander))]
    public static DebugData IsAllowedForDebug(this Lite<IEntity> lite, TypeAllowedBasic allowed, bool inUserInterface)
    {
        return miIsAllowedForDebugLite.GetInvoker(lite.EntityType).Invoke(lite, allowed, inUserInterface);
    }

    static GenericInvoker<Func<Lite<IEntity>, TypeAllowedBasic, bool, DebugData>> miIsAllowedForDebugLite =
        new((l, tab, ec) => IsAllowedForDebug<Entity>(l, tab, ec));
    [MethodExpander(typeof(IsAllowedForDebugExpander))]
    static DebugData IsAllowedForDebug<T>(this Lite<IEntity> lite, TypeAllowedBasic allowed, bool inUserInterface)
         where T : Entity
    {
        if (!AuthLogic.IsEnabled)
            throw new InvalidOperationException("AuthLogic.IsEnabled is false");

        using (DisableQueryFilter())
            return ((Lite<T>)lite).InDB().Select(a => a.IsAllowedForDebug(allowed, inUserInterface)).SingleEx();
    }

    class IsAllowedForDebugExpander : IMethodExpander
    {
        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            TypeAllowedBasic allowed = (TypeAllowedBasic)ExpressionEvaluator.Eval(arguments[1])!;

            bool inUserInterface = arguments.Length == 3 ? (bool)ExpressionEvaluator.Eval(arguments[2])! : ExecutionMode.InUserInterface;

            Expression exp = arguments[0].Type.IsLite() ? Expression.Property(arguments[0], "Entity") : arguments[0];

            return IsAllowedExpressionDebug(exp, allowed, inUserInterface);
        }
    }


    static FilterQueryResult<T>? TypeAuthLogic_FilterQuery<T>()
      where T : Entity
    {
        if (queryFilterDisabled.Value)
            return null;

        if (ExecutionMode.InGlobal || !AuthLogic.IsEnabled)
            return null;

        var ui = ExecutionMode.InUserInterface;
        AssertMinimum<T>(ui);

        ParameterExpression e = Expression.Parameter(typeof(T), "e");

        var tab = typeof(T) == IsDelete ? TypeAllowedBasic.Write : TypeAllowedBasic.Read;

        Expression body = IsAllowedExpression(e, tab, ui);

        if (body is ConstantExpression ce)
        {
            if (((bool)ce.Value!))
                return null;
        }

        Func<T, bool>? func = IsAllowedInMemory<T>(GetAllowed(typeof(T)), tab, ui);

        return new FilterQueryResult<T>(Expression.Lambda<Func<T, bool>>(body, e), func);
    }


    [MethodExpander(typeof(WhereAllowedExpander))]
    public static IQueryable<T> WhereAllowed<T>(this IQueryable<T> query)
        where T : Entity
    {
        if (ExecutionMode.InGlobal || !AuthLogic.IsEnabled)
            return query;

        var ui = ExecutionMode.InUserInterface;

        AssertMinimum<T>(ui);

        return WhereIsAllowedFor<T>(query, TypeAllowedBasic.Read, ui);
    }

    private static void AssertMinimum<T>(bool ui) where T : Entity
    {
        var allowed = GetAllowed(typeof(T));
        var max = ui ? allowed.MaxUI() : allowed.MaxDB();
        if (max < TypeAllowedBasic.Read)
            throw new UnauthorizedAccessException("Type {0} is not authorized{1}{2}".FormatWith(typeof(T).Name,
                ui ? " in user interface" : null,
                allowed.ConditionRules.Any() ? " for any condition" : null));
    }


    [MethodExpander(typeof(WhereIsAllowedForExpander))]
    public static IQueryable<T> WhereIsAllowedFor<T>(this IQueryable<T> query, TypeAllowedBasic allowed, bool inUserInterface)
        where T : Entity
    {
        ParameterExpression e = Expression.Parameter(typeof(T), "e");

        Expression body = IsAllowedExpression(e, allowed, inUserInterface);


        if (body is ConstantExpression ce)
        {
            if (((bool)ce.Value!))
                return query;
        }

        IQueryable<T> result = query.Where(Expression.Lambda<Func<T, bool>>(body, e));

        return result;
    }

    class WhereAllowedExpander : IMethodExpander
    {
        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            return miCallWhereAllowed.GetInvoker(mi.GetGenericArguments()).Invoke(arguments[0]);
        }

        static GenericInvoker<Func<Expression, Expression>> miCallWhereAllowed = new(exp => CallWhereAllowed<TypeEntity>(exp));
        static Expression CallWhereAllowed<T>(Expression expression)
            where T : Entity
        {
            IQueryable<T> query = new Query<T>(DbQueryProvider.Single, expression);
            IQueryable<T> result = WhereAllowed(query);
            return result.Expression;
        }
    }

    class WhereIsAllowedForExpander : IMethodExpander
    {
        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            TypeAllowedBasic allowed = (TypeAllowedBasic)ExpressionEvaluator.Eval(arguments[1])!;
            bool inUserInterface = (bool)ExpressionEvaluator.Eval(arguments[2])!;

            return miCallWhereIsAllowedFor.GetInvoker(mi.GetGenericArguments())(arguments[0], allowed, inUserInterface);
        }

        static GenericInvoker<Func<Expression, TypeAllowedBasic, bool, Expression>> miCallWhereIsAllowedFor =
            new((ex, tab, ui) => CallWhereIsAllowedFor<TypeEntity>(ex, tab, ui));
        static Expression CallWhereIsAllowedFor<T>(Expression expression, TypeAllowedBasic allowed, bool inUserInterface)
            where T : Entity
        {
            IQueryable<T> query = new Query<T>(DbQueryProvider.Single, expression);
            IQueryable<T> result = WhereIsAllowedFor(query, allowed, inUserInterface);
            return result.Expression;
        }
    }

    public static Expression IsAllowedExpression(Expression entity, TypeAllowedBasic requested, bool inUserInterface)
    {
        Type type = entity.Type;

        TypeAllowedAndConditions tac = GetAllowed(type);

        var node = tac.ToTypeConditionNode(requested, inUserInterface);

        var simpleNode = node.Simplify();

        var expression = simpleNode.ToExpression(entity);

        return expression;
    }


    static ConstructorInfo ciDebugData = ReflectionTools.GetConstuctorInfo(() => new DebugData(null!, TypeAllowedBasic.Write, true, TypeAllowed.Write, null!));
    static ConstructorInfo ciConditionRuleDebugData = ReflectionTools.GetConstuctorInfo(() => new ConditionRuleDebugData(null!, TypeAllowed.Write));
    static ConstructorInfo ciConditionDebugData = ReflectionTools.GetConstuctorInfo(() => new ConditionDebugData(null!, true));
    static MethodInfo miToLite = ReflectionTools.GetMethodInfo((Entity a) => a.ToLite()).GetGenericMethodDefinition();

    internal static Expression IsAllowedExpressionDebug(Expression entity, TypeAllowedBasic requested, bool inUserInterface)
    {
        Type type = entity.Type;

        TypeAllowedAndConditions tac = GetAllowed(type);

        Expression baseValue = Expression.Constant(tac.Fallback.Get(inUserInterface) >= requested);

        var list = (from line in tac.ConditionRules
                    select Expression.New(ciConditionRuleDebugData,

                    Expression.ListInit(Expression.New(typeof(List<ConditionDebugData>)),
                            line.TypeConditions.Select(tc => Expression.New(ciConditionDebugData,
                                Expression.Constant(tc, typeof(TypeConditionSymbol)),
                                Expression.Invoke(TypeConditionLogic.GetCondition(type, tc), entity))).ToArray()),

                    Expression.Constant(line.Allowed)))
                .ToArray();

        Expression newList = Expression.ListInit(Expression.New(typeof(List<ConditionRuleDebugData>)), list);

        Expression liteEntity = Expression.Call(null, miToLite.MakeGenericMethod(entity.Type), entity);

        return Expression.New(ciDebugData, liteEntity,
            Expression.Constant(requested),
            Expression.Constant(inUserInterface),
            Expression.Constant(tac.Fallback),
            newList);
    }

    public class DebugData
    {
        public DebugData(Lite<IEntity> lite, TypeAllowedBasic requested, bool userInterface, TypeAllowed fallback, List<ConditionRuleDebugData> rules)
        {
            this.Lite = lite;
            this.Requested = requested;
            this.Fallback = fallback;
            this.UserInterface = userInterface;
            this.Rules = rules;
        }

        public Lite<IEntity> Lite { get; private set; }
        public TypeAllowedBasic Requested { get; private set; }
        public TypeAllowed Fallback { get; private set; }
        public bool UserInterface { get; private set; }

        public List<ConditionRuleDebugData> Rules { get; private set; }

        public bool IsAllowed
        {
            get
            {
                foreach (var rule in Rules.AsEnumerable().Reverse())
                {
                    if (rule.Conditions.All(c => c.InGroup))
                        return Requested <= rule.Allowed.Get(UserInterface);
                }

                return Requested <= Fallback.Get(UserInterface);
            }
        }

        public string? ErrorMessage
        {
            get
            {
                foreach (var rule in Rules.AsEnumerable().Reverse())
                {
                    if (rule.Conditions.All(c => c.InGroup))
                        return Requested <= rule.Allowed.Get(UserInterface) ? null :
                            (Requested == TypeAllowedBasic.Read ?
                            AuthAdminMessage.CanNotBeReadBecauseIsInCondition0 :
                            AuthAdminMessage.CanNotBeModifiedBecauseIsInCondition0)
                            .NiceToString(rule.Conditions.CommaAnd(c => "'" + c.TypeCondition.NiceToString() + "'"));
                }

                return Requested <= Fallback.Get(UserInterface) ? null :
                    (Requested == TypeAllowedBasic.Read ?
                    AuthAdminMessage.CanNotBeReadBecauseIsNotInCondition0 :
                    AuthAdminMessage.CanNotBeModifiedBecauseIsNotInCondition0)
                    .NiceToString(Rules.Where(cond => Requested <= cond.Allowed.Get(UserInterface)).AsEnumerable().Reverse()
                    .CommaOr(rule => rule.Conditions.Where(a => !a.InGroup).CommaAnd(c => "'" + c.TypeCondition.NiceToString() + "'")));
            }
        }
    }

    public class ConditionDebugData
    {
        internal ConditionDebugData(TypeConditionSymbol typeCondition, bool inGroup)
        {
            TypeCondition = typeCondition;
            InGroup = inGroup;
        }

        public TypeConditionSymbol TypeCondition { get; private set; }
        public bool InGroup { get; private set; }
    }

    public class ConditionRuleDebugData
    {
        public List<ConditionDebugData> Conditions { get; private set; }
        public TypeAllowed Allowed { get; private set; }

        internal ConditionRuleDebugData(List<ConditionDebugData> conditions, TypeAllowed allowed)
        {
            this.Conditions = conditions;
            this.Allowed = allowed;
        }
    }

    public static DynamicQueryCore<T> ToDynamicDisableAuth<T>(this IQueryable<T> query, bool disableQueryFilter = false, bool authDisable = true)
    {
        return new AutoDynamicQueryNoFilterCore<T>(query)
        {
            DisableQueryFilter = disableQueryFilter,
            AuthDisable = authDisable,
        };
    }

    internal class AutoDynamicQueryNoFilterCore<T> : AutoDynamicQueryCore<T>
    {
        public bool DisableQueryFilter { get; internal set; }
        public bool AuthDisable { get; internal set; }

        public AutoDynamicQueryNoFilterCore(IQueryable<T> query)
            : base(query)
        { }

        public override async Task<ResultTable> ExecuteQueryAsync(QueryRequest request, CancellationToken token)
        {
            using (this.AuthDisable ? AuthLogic.Disable() : null)
            using (this.DisableQueryFilter ? TypeAuthLogic.DisableQueryFilter() : null)
            {
                return await base.ExecuteQueryAsync(request, token);
            }
        }

        public override async Task<Lite<Entity>?> ExecuteUniqueEntityAsync(UniqueEntityRequest request, CancellationToken token)
        {
            using (this.AuthDisable ? AuthLogic.Disable() : null)
            using (this.DisableQueryFilter ? TypeAuthLogic.DisableQueryFilter() : null)
            {
                return await base.ExecuteUniqueEntityAsync(request, token);
            }
        }

        public override async Task<object?> ExecuteQueryValueAsync(QueryValueRequest request, CancellationToken token)
        {
            using (this.AuthDisable ? AuthLogic.Disable() : null)
            using (this.DisableQueryFilter ? TypeAuthLogic.DisableQueryFilter() : null)
            {
                return await base.ExecuteQueryValueAsync(request, token);
            }
        }
    }

    public static RuleTypeEntity ToRuleType(this TypeAllowedAndConditions allowed, Lite<RoleEntity> role, TypeEntity resource)
    {
        return new RuleTypeEntity
        {
            Role = role,
            Resource = resource,
            Allowed = allowed.Fallback,
            ConditionRules = allowed.ConditionRules.Select(a => new RuleTypeConditionEntity
            {
                Allowed = a.Allowed,
                Conditions = a.TypeConditions.ToMList()
            }).ToMList()
        };
    }

    public static TypeAllowedAndConditions ToTypeAllowedAndConditions(this RuleTypeEntity rule)
    {
        return new TypeAllowedAndConditions(rule.Allowed,
            rule.ConditionRules.Select(c => new TypeConditionRuleModel(c.Conditions, c.Allowed)));
    }

    static SqlPreCommand? Schema_Synchronizing(Replacements rep)
    {
        var conds = (from rt in Database.Query<RuleTypeEntity>()
                     from c in rt.ConditionRules
                     from s in c.Conditions
                     select new { rt.Resource, s, rt.Role }).ToList();

        var errors = conds.GroupBy(a => new { a.Resource, a.s}, a => a.Role)
            .Where(gr =>
            {
                if (gr.Key.s.FieldInfo == null) /*CSBUG*/
                {
                    var replacedName = rep.TryGetC(typeof(TypeConditionSymbol).Name)?.TryGetC(gr.Key.s.Key);
                    if (replacedName == null)
                        return false; // Other Syncronizer will do it

                    return !TypeConditionLogic.ConditionsFor(gr.Key.Resource!.ToType()).Any(a => a.Key == replacedName);
                }

                return !TypeConditionLogic.IsDefined(gr.Key.Resource!.ToType(), gr.Key.s);
            })
            .ToList();

        using (rep.WithReplacedDatabaseName())
            return errors.Select(a =>
            {
                return Administrator.UnsafeDeletePreCommand(Database.Query<RuleTypeConditionEntity>()
                    .Where(rule => rule.Conditions.Contains(a.Key.s) && rule.RuleType.Entity.Resource.Is(a.Key.Resource)))!
                    .AddComment("TypeCondition {0} not defined for {1} (roles {2})".FormatWith(a.Key.s, a.Key.Resource, a.ToString(", ")));
            }).Combine(Spacing.Double);
    }
}

//public static class AndOrSimplifierVisitor
//{
//    class HashSetComparer<T> : IEqualityComparer<HashSet<T>>
//    {
//        public bool Equals(HashSet<T>? x, HashSet<T>? y)
//        {
//            return x != null && y != null && x.SetEquals(y);
//        }

//        public int GetHashCode([DisallowNull] HashSet<T> obj)
//        {
//            return obj.Count;
//        }
//    }

//    static IEqualityComparer<Expression> Comparer = ExpressionComparer.GetComparer<Expression>(false);
//    static IEqualityComparer<HashSet<Expression>> HSetComparer = new HashSetComparer<Expression>();

//    public static Expression SimplifyOrs(Expression expr)
//    {
//        if (expr is BinaryExpression b && (b.NodeType == ExpressionType.Or || b.NodeType == ExpressionType.OrElse))
//        {
//            var orGroups = OrAndList(b);

//            var newOrGroups = orGroups.Where(og => !orGroups.Any(og2 => og2 != og && og2.IsMoreSimpleAndGeneralThan(og))).ToList();

//            return newOrGroups.Select(andGroup => andGroup.Aggregate(Expression.AndAlso)).Aggregate(Expression.OrElse);
//        }

//        return expr;
//    }

//    static HashSet<HashSet<Expression>> OrAndList(Expression expression)
//    {
//        if (expression is BinaryExpression b && (b.NodeType == ExpressionType.Or || b.NodeType == ExpressionType.OrElse))
//        {
//            return OrAndList(b.Left).Concat(OrAndList(b.Right)).ToHashSet(HSetComparer);
//        }
//        else
//        {
//            var ands = AndList(expression);
//            return new HashSet<HashSet<Expression>>(HSetComparer) { ands };
//        }
//    }

//    static HashSet<Expression> AndList(Expression expression)
//    {
//        if (expression is BinaryExpression b && (b.NodeType == ExpressionType.And || b.NodeType == ExpressionType.AndAlso))
//            return AndList(b.Left).Concat(AndList(b.Right)).ToHashSet(Comparer);
//        else
//            return new HashSet<Expression>(Comparer) { expression };
//    }

//    static bool IsMoreSimpleAndGeneralThan(this HashSet<Expression> simple, HashSet<Expression> complex)
//    {
//        return simple.All(a => complex.Contains(a));
//    }
//}
