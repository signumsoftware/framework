using System.Collections.Concurrent;
using System.Collections.Immutable;
using Signum.Authorization;
using Signum.Operations;
using Signum.Utilities.Reflection;

namespace Signum.Authorization.Rules;


public static class OperationAuthLogic
{
    static OperationCache cache = null!;

    public static IManualAuth<(OperationSymbol operation, Type type), WithConditions<OperationAllowed>> Manual { get { return cache; } }

    public static bool IsStarted { get { return cache != null; } }

    public static readonly Dictionary<OperationSymbol, OperationAllowed> MaxAutomaticUpgrade = new Dictionary<OperationSymbol, OperationAllowed>();

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        AuthLogic.AssertStarted(sb);
        OperationLogic.AssertStarted(sb);

        OperationLogic.AllowOperation += OperationLogic_AllowOperation;

        sb.Include<RuleOperationEntity>()
            .WithUniqueIndex(rt => new { rt.Resource.Operation, rt.Resource.Type, rt.Role })
            .WithVirtualMList(rt => rt.ConditionRules, c => c.RuleOperation);            

        cache = new OperationCache(sb);

        TypeConditionsPerType = sb.GlobalLazy(() => new ConcurrentDictionary<(Lite<RoleEntity> role, Type type), bool>(),
            new InvalidateWith(typeof(RuleOperationEntity), typeof(RuleOperationConditionEntity)));

        sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query =>
        {
            Database.Query<RuleOperationEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete();
            return null;
        };

        AuthLogic.ExportToXml += cache.ExportXml;
        AuthLogic.ImportFromXml += cache.ImportXml;

        AuthLogic.HasRuleOverridesEvent += role => cache.HasRealOverrides(role);

        sb.Schema.Synchronizing += rep => TypeConditionRuleSync.NotDefinedTypeCondition<RuleOperationConditionEntity>(rep, rt => rt.Conditions, rtc => rtc.RuleOperation.Entity.Resource.Type, rtc => rtc.RuleOperation.Entity.Role);
        sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query => { Database.Query<RuleOperationEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete(); return null; };
        sb.Schema.EntityEvents<OperationSymbol>().PreDeleteSqlSync += op => Administrator.UnsafeDeletePreCommandVirtualMList(Database.Query<RuleOperationEntity>().Where(a => a.Resource.Operation.Is(op)));
        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += t => Administrator.UnsafeDeletePreCommandVirtualMList(Database.Query<RuleOperationEntity>().Where(a => a.Resource.Type.Is(t)));
        sb.Schema.EntityEvents<TypeConditionSymbol>().PreDeleteSqlSync += condition => TypeConditionRuleSync.DeletedTypeCondition<RuleOperationConditionEntity>(rt => rt.Conditions, mle => mle.Element.Is(condition));


        TypeAuthLogic.HasTypeConditionInOperations = RequiresTypeConditionForOperations;
    }

    static ResetLazy<ConcurrentDictionary<(Lite<RoleEntity> role, Type type), bool>> TypeConditionsPerType;
    static bool RequiresTypeConditionForOperations(Lite<RoleEntity> role, Type type)
    {
        return TypeConditionsPerType.Value.GetOrAdd((role, type), e =>
        {
            var taac = TypeAuthLogic.GetAllowed(e.role, e.type);
            if (taac.ConditionRules.IsEmpty())
                return false;

            return OperationLogic.GetAllOperationInfos(type).Any(op => {

                var paac = cache.GetAllowed(e.role, (op.OperationSymbol, type));

                return paac.CandidatesAssuming(taac, inUserInterface: false).Distinct().Count() > 1; //If for all the type rules that are visible the property has the same value, we don't need the type conditions
            });
        });
    }

    public static T SetMaxAutomaticUpgrade<T>(this T operation, OperationAllowed allowed) where T : IOperation
    {
        MaxAutomaticUpgrade.Add(operation.OperationSymbol, allowed);

        return operation;
    }

    static GenericInvoker<Func<Entity, WithConditions<OperationAllowed>, bool, bool>> giAllowOperation = new((e, ta, inUI) => AllowOperation<Entity>((Entity)e, ta, inUI));
    static bool AllowOperation<T>(T entity, WithConditions<OperationAllowed> ta, bool inUserInterface)
        where T : Entity
    {
        foreach (var c in ta.ConditionRules.Reverse())
        {
            if (c.TypeConditions.All(a => entity.InTypeCondition(a)))
                return c.Allowed.ToBoolean(inUserInterface);
        }

        return ta.Fallback.ToBoolean(inUserInterface);
    }

    static bool OperationLogic_AllowOperation(OperationSymbol operationKey, Type entityType, bool inUserInterface, Entity? entity)
    {
        var oa = GetOperationAllowed(operationKey, entityType);

        if (entity == null || entity.IsNew)
            return oa.Max().ToBoolean(inUserInterface);

        if(!RequiresTypeConditionForOperations(RoleEntity.Current, entityType))
        {
            var ta = TypeAuthLogic.GetAllowed(entityType);

            var single = oa.CandidatesAssuming(ta, inUserInterface).Distinct().SingleEx();

            return single.ToBoolean(inUserInterface);
        }

        return giAllowOperation.GetInvoker(entity.GetType())(entity, oa, inUserInterface);
    }

    public static OperationRulePack GetOperationRules(Lite<RoleEntity> role, TypeEntity typeEntity)
    {
        var entityType = typeEntity.ToType();

        var resources = OperationLogic.GetAllOperationInfos(entityType).Select(a => new OperationTypeEmbedded { Operation = a.OperationSymbol, Type = typeEntity });
        var result = new OperationRulePack { Role = role, Type = typeEntity, };

        cache.GetRules(result, resources);

        result.Rules.ForEach(r =>
        {
            var operationType = (operation: r.Resource.Operation, type: r.Resource.Type.ToType());
            r.Coerced = cache.CoerceValue(role, operationType, WithConditions<OperationAllowed>.Simple(OperationAllowed.Allow)).ToModel();
        });

        Type type = typeEntity.ToType();
        result.AvailableTypeConditions = TypeAuthLogic.GetAllowed(role, type).ConditionRules.Select(a => a.TypeConditions.ToList()).ToList();

        return result;
    }

    public static void SetOperationRules(OperationRulePack rules)
    {
        cache.SetRules(rules, r => r.Type.Is(rules.Type));
    }

    public static WithConditions<OperationAllowed> GetOperationAllowed(Lite<RoleEntity> role, OperationSymbol operation, Type entityType)
    {
        return cache.GetAllowed(role, (operation, entityType));
    }

    public static WithConditions<OperationAllowed> GetOperationAllowed(OperationSymbol operation, Type type)
    {
        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return WithConditions<OperationAllowed>.Simple(OperationAllowed.Allow);

        if (GetTemporallyAllowed(operation))
            return WithConditions<OperationAllowed>.Simple(OperationAllowed.Allow);

        return cache.GetAllowed(RoleEntity.Current, (operation, type));
    }

    public static WithConditionsModel<AuthThumbnail>? GetAllowedThumbnail(Lite<RoleEntity> role, Type entityType, WithConditions<TypeAllowed> typeAllowedModel)
    {
        var ops = OperationLogic.GetAllOperationInfos(entityType);

        if(ops.IsEmpty())
            return null;    

        var wcps = ops.Select(op => cache.GetAllowed(role, (op.OperationSymbol, entityType))).ToList();

        return new WithConditionsModel<AuthThumbnail>(
            wcps.Select(a => a.Fallback).Collapse()!.Value,
            typeAllowedModel.ConditionRules.Select(crt =>
            {
                var thumbnail = wcps.Where(a => a.ConditionRules.Count > 0 /*Constructor*/)
                .Select(a => a.ConditionRules.Single(cr => crt.TypeConditions.SequenceEqual(cr.TypeConditions)).Allowed)
                .Collapse() ?? AuthThumbnail.None;

                return new ConditionRuleModel<AuthThumbnail>(crt.TypeConditions, thumbnail);
            }));
    }

    public static Dictionary<(OperationSymbol operation, Type type), WithConditions<OperationAllowed>> AllowedOperations()
    {
        return AllOperationTypes().ToDictionary(a => a, a => cache.GetAllowed(RoleEntity.Current, a));
    }

    internal static List<(OperationSymbol operation, Type type)> AllOperationTypes()
    {
        return (from type in Schema.Current.Tables.Keys
                from o in OperationLogic.TypeOperations(type)
                select (operation: o.OperationSymbol, type: type))
                .ToList();
    }

    static readonly Variable<ImmutableStack<OperationSymbol>> tempAllowed = Statics.ThreadVariable<ImmutableStack<OperationSymbol>>("authTempOperationsAllowed");

    public static IDisposable AllowTemporally(OperationSymbol operationKey)
    {
        tempAllowed.Value = (tempAllowed.Value ?? ImmutableStack<OperationSymbol>.Empty).Push(operationKey);

        return new Disposable(() => tempAllowed.Value = tempAllowed.Value.Pop());
    }

    internal static bool GetTemporallyAllowed(OperationSymbol operationKey)
    {
        var ta = tempAllowed.Value;
        if (ta == null || ta.IsEmpty)
            return false;

        return ta.Contains(operationKey);
    }


    static OperationAllowed ToOperationAllowed(TypeAllowed allowed, TypeAllowedBasic checkFor)
    {
        return checkFor <= allowed.GetUI() ? OperationAllowed.Allow :
                 checkFor <= allowed.GetDB() ? OperationAllowed.DBOnly :
                 OperationAllowed.None;
    }

    static ConcurrentDictionary<(WithConditions<TypeAllowed> allowed, TypeAllowedBasic checkFor), WithConditions<OperationAllowed>> toOperationAllowedCache = new ();
    static ConcurrentDictionary<(WithConditions<OperationAllowed> allowed, OperationAllowed limit), WithConditions<OperationAllowed>> limitCache = new ();

    public static WithConditions<OperationAllowed> ToOperationAllowed(this WithConditions<TypeAllowed> allowed, IOperation operation, TypeAllowed? maxReturnAllowed)
    {
        if (operation.OperationType == OperationType.Constructor)
            return WithConditions<OperationAllowed>.Simple(ToOperationAllowed(allowed.MaxCombined(), TypeAllowedBasic.Write));

        TypeAllowedBasic checkFor = operation.OperationType switch
        {
            OperationType.ConstructorFrom => TypeAllowedBasic.Read,
            OperationType.ConstructorFromMany => TypeAllowedBasic.Read,
            OperationType.Execute => ((IExecuteOperation)operation).ForReadonlyEntity ? TypeAllowedBasic.Read : TypeAllowedBasic.Write,
            OperationType.Delete => TypeAllowedBasic.Write,
            _ => throw new UnexpectedValueException(operation.OperationType),
        };

        var result = toOperationAllowedCache.GetOrAdd((allowed, checkFor), tuple => tuple.allowed.MapWithConditions(a => ToOperationAllowed(a, tuple.checkFor)));
        if (operation.ReturnType == null)
            return result;

        var limit = ToOperationAllowed(maxReturnAllowed!.Value, TypeAllowedBasic.Write);

        return limitCache.GetOrAdd((result, limit), tuple => tuple.allowed.MapWithConditions(a => a < limit ? a : limit));
    }

}
