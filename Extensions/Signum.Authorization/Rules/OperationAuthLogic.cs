using System.Collections.Immutable;
using Signum.Authorization;
using Signum.Operations;

namespace Signum.Authorization.Rules;


public static class OperationAuthLogic
{
    static OperationCache cache = null!;

    public static IManualAuth<(OperationSymbol operation, Type type), OperationAllowed> Manual { get { return cache; } }

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
                 .WithUniqueIndex(rt => new { rt.Resource.Operation, rt.Resource.Type, rt.Role });

            cache = new OperationCache(sb);

            sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query =>
            {
                Database.Query<RuleOperationEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete();
                return null;
            };

            AuthLogic.ExportToXml += cache.ExportXml;
            AuthLogic.ImportFromXml += cache.ImportXml;

            AuthLogic.HasRuleOverridesEvent += role => cache.HasRealOverrides(role);

            sb.Schema.Table<OperationSymbol>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(AuthCache_PreDeleteOperationSqlSync);
            sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(AuthCache_PreDeleteTypeSqlSync);
        }
    }

    static SqlPreCommand AuthCache_PreDeleteOperationSqlSync(Entity arg)
    {
        return Administrator.DeleteWhereScript((RuleOperationEntity rt) => rt.Resource.Operation, (OperationSymbol)arg);
    }

    static SqlPreCommand AuthCache_PreDeleteTypeSqlSync(Entity arg)
    {
        return Administrator.DeleteWhereScript((RuleOperationEntity rt) => rt.Resource.Type, (TypeEntity)arg);
    }

    public static T SetMaxAutomaticUpgrade<T>(this T operation, OperationAllowed allowed) where T : IOperation
    {
        MaxAutomaticUpgrade.Add(operation.OperationSymbol, allowed);

        return operation;
    }

    static bool OperationLogic_AllowOperation(OperationSymbol operationKey, Type entityType, bool inUserInterface)
    {
        return GetOperationAllowed(operationKey, entityType, inUserInterface);
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

            r.Coerced = cache.CoerceValue(role, operationType, OperationAllowed.Allow);
        });

        return result;
    }

    public static void SetOperationRules(OperationRulePack rules)
    {
        cache.SetRules(rules, r => r.Type.Is(rules.Type));
    }

    public static bool GetOperationAllowed(Lite<RoleEntity> role, OperationSymbol operation, Type entityType, bool inUserInterface)
    {
        OperationAllowed allowed = GetOperationAllowed(role, operation, entityType);

        return allowed == OperationAllowed.Allow || allowed == OperationAllowed.DBOnly && !inUserInterface;
    }

    public static OperationAllowed GetOperationAllowed(Lite<RoleEntity> role, OperationSymbol operation, Type entityType)
    {
        return cache.GetAllowed(role, (operation, entityType));
    }

    public static bool GetOperationAllowed(OperationSymbol operation, Type type, bool inUserInterface)
    {
        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return true;

        if (GetTemporallyAllowed(operation))
            return true;

        OperationAllowed allowed = cache.GetAllowed(RoleEntity.Current, (operation, type));

        return allowed == OperationAllowed.Allow || allowed == OperationAllowed.DBOnly && !inUserInterface;
    }

    public static AuthThumbnail? GetAllowedThumbnail(Lite<RoleEntity> role, Type entityType)
    {
        return OperationLogic.GetAllOperationInfos(entityType).Select(oi => cache.GetAllowed(role, (oi.OperationSymbol, entityType))).Collapse();
    }

    public static Dictionary<(OperationSymbol operation, Type type), OperationAllowed> AllowedOperations()
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
