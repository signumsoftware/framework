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
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
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
    }

    static ResetLazy<ConcurrentDictionary<(Lite<RoleEntity> role, Type type), bool>> TypeConditionsPerType;
    static bool RequiresTypeConditionForOperations(Type type)
    {
        var role = RoleEntity.Current;
        return TypeConditionsPerType.Value.GetOrAdd((role, type), e =>
        {
            var taac = TypeAuthLogic.GetAllowed(e.type);
            if (taac.ConditionRules.IsEmpty())
                return false;

            var def = taac.ToOperationAllowed();

            return OperationLogic.GetAllOperationInfos(type).Any(op => {

                var paac = cache.GetAllowed(e.role, (op.OperationSymbol, type));

                if (paac.Equals(def))
                    return false;

                return paac.CandidatesAssuming(taac).Distinct().Count() > 1; //If for all the type rules that are visible the property has the same value, we don't need the type conditions
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
        var ta = GetOperationAllowed(operationKey, entityType);

        if (entity == null)
            return ta.Max().ToBoolean(inUserInterface);

        return giAllowOperation.GetInvoker(entity.GetType())(entity, ta, inUserInterface);
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
        var wcps = OperationLogic.GetAllOperationInfos(entityType).Select(op => cache.GetAllowed(role, (op.OperationSymbol, entityType))).ToList();

        return new WithConditionsModel<AuthThumbnail>(
            wcps.Select(a => a.Fallback).Collapse()!.Value,
            typeAllowedModel.ConditionRules.Select(crt =>
            {
                var thumbnail = wcps.Select(a => a.ConditionRules.Single(cr => crt.TypeConditions.SequenceEqual(cr.TypeConditions)).Allowed).Collapse()!.Value;

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

    public static OperationAllowed InferredOperationAllowed((OperationSymbol operation, Type type) operationType, Func<Type, WithConditions<TypeAllowed>> allowed)
    {
        Func<Type, TypeAllowedBasic, OperationAllowed> operationAllowed = (t, checkFor) =>
        {
            if (!TypeLogic.TypeToEntity.ContainsKey(t))
                return OperationAllowed.Allow;

            var ta = allowed(t);

            return checkFor <= ta.MaxUI() ? OperationAllowed.Allow :
                checkFor <= ta.MaxDB() ? OperationAllowed.DBOnly :
                OperationAllowed.None;
        };

        var operation = OperationLogic.FindOperation(operationType.type ?? /*Temp*/  OperationLogic.FindTypes(operationType.operation).First(), operationType.operation);

        switch (operation.OperationType)
        {
            case OperationType.Execute:
                var defaultAllowed = ((IExecuteOperation)operation).ForReadonlyEntity ? TypeAllowedBasic.Read : TypeAllowedBasic.Write;
                return operationAllowed(operation.OverridenType, defaultAllowed);

            case OperationType.Delete:
                return operationAllowed(operation.OverridenType, TypeAllowedBasic.Write);

            case OperationType.Constructor:
                return operationAllowed(operation.ReturnType!, TypeAllowedBasic.Write);

            case OperationType.ConstructorFrom:
            case OperationType.ConstructorFromMany:
                {
                    var fromTypeAllowed = operationAllowed(operation.OverridenType, TypeAllowedBasic.Read);
                    var returnTypeAllowed = operationAllowed(operation.ReturnType!, TypeAllowedBasic.Write);

                    return returnTypeAllowed < fromTypeAllowed ? returnTypeAllowed : fromTypeAllowed;
                }

            default:
                throw new UnexpectedValueException(operation.OperationType);
        }
    }
}
