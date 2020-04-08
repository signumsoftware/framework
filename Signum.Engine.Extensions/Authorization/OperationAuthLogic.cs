using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using Signum.Engine.Operations;
using System.Reflection;
using System.Xml.Linq;

namespace Signum.Engine.Authorization
{
    public static class OperationAuthLogic
    {
        static AuthCache<RuleOperationEntity, OperationAllowedRule, OperationTypeEmbedded, (OperationSymbol operation, Type type), OperationAllowed> cache = null!;

        public static HashSet<OperationSymbol> AvoidCoerce = new HashSet<OperationSymbol>();

        public static IManualAuth<(OperationSymbol operation, Type type), OperationAllowed> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public static readonly Dictionary<OperationSymbol, OperationAllowed> MaxAutomaticUpgrade = new Dictionary<OperationSymbol, OperationAllowed>();

        internal static readonly string operationReplacementKey = "AuthRules:" + typeof(OperationSymbol).Name;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                OperationLogic.AssertStarted(sb);

                OperationLogic.AllowOperation += OperationLogic_AllowOperation;

                sb.Include<RuleOperationEntity>()
                     .WithUniqueIndex(rt => new { rt.Resource.Operation, rt.Resource.Type, rt.Role });

                cache = new AuthCache<RuleOperationEntity, OperationAllowedRule, OperationTypeEmbedded, (OperationSymbol operation, Type type), OperationAllowed>(sb,
                     toKey: s => (operation: s.Operation, type: s.Type.ToType()),
                     toEntity: s => new OperationTypeEmbedded { Operation = s.operation, Type = s.type.ToTypeEntity() },
                     isEquals: (o1, o2) => o1.Operation == o2.Operation && o1.Type == o2.Type,
                     merger: new OperationMerger(),
                     invalidateWithTypes: true,
                     coercer:  OperationCoercer.Instance);

                sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query =>
                {
                    Database.Query<RuleOperationEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete();
                    return null;
                };

                AuthLogic.ExportToXml += exportAll => cache.ExportXml("Operations", "Operation", s => s.operation.Key + "/" + s.type?.ToTypeEntity().CleanName, b => b.ToString(),
                    exportAll ? AllOperationTypes() : null);

                AuthLogic.ImportFromXml += (x, roles, replacements) =>
                {
                    var allResources = x.Element("Operations").Elements("Role").SelectMany(r => r.Elements("Operation")).Select(p => p.Attribute("Resource").Value).ToHashSet();

                    replacements.AskForReplacements(
                      allResources.Select(a => a.TryBefore("/") ?? a).ToHashSet(),
                      SymbolLogic<OperationSymbol>.AllUniqueKeys(),
                      operationReplacementKey);

                    string typeReplacementKey = "AuthRules:" + typeof(OperationSymbol).Name;
                    replacements.AskForReplacements(
                       allResources.Select(a => a.After("/")).ToHashSet(),
                       TypeLogic.NameToType.Keys.ToHashSet(),
                       TypeAuthCache.typeReplacementKey);

                    return cache.ImportXml(x, "Operations", "Operation", roles,
                        s => {
                            var operation = SymbolLogic<OperationSymbol>.TryToSymbol(replacements.Apply(operationReplacementKey, s.Before("/")));
                            var type = TypeLogic.TryGetType(replacements.Apply(TypeAuthCache.typeReplacementKey, s.After("/")));

                            if (operation == null || type == null || !OperationLogic.IsDefined(type, operation))
                                return null;

                            return new OperationTypeEmbedded { Operation = operation, Type = type.ToTypeEntity() };
                        }, EnumExtensions.ToEnum<OperationAllowed>);
                };

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

            var coercer = OperationCoercer.Instance.GetCoerceValue(role);
            result.Rules.ForEach(r =>
            {
                var operationType = (operation: r.Resource.Operation, type: r.Resource.Type.ToType());
                r.CoercedValues = EnumExtensions.GetValues<OperationAllowed>().Where(a => !coercer(operationType, a).Equals(a)).ToArray();
            });

            return result;
        }

        public static void SetOperationRules(OperationRulePack rules)
        {
            cache.SetRules(rules, r => r.Type == rules.Type);
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

        static List<(OperationSymbol operation, Type type)> AllOperationTypes()
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

        public static void RegisterAvoidCoerce(this IOperation operation)
        {
            SetAvoidCoerce(operation.OperationSymbol);
            operation.Register();
        }

        private static void SetAvoidCoerce(OperationSymbol operationSymbol)
        {
            AvoidCoerce.Add(operationSymbol);
        }

        public static OperationAllowed MaxTypePermission((OperationSymbol operation, Type type) operationType, TypeAllowedBasic checkFor, Func<Type, TypeAllowedAndConditions> allowed)
        {
            Func<Type, OperationAllowed> operationAllowed = t =>
            {
                if (!TypeLogic.TypeToEntity.ContainsKey(t))
                    return OperationAllowed.Allow;

                var ta = allowed(t);

                return checkFor <= ta.MaxUI() ? OperationAllowed.Allow :
                    checkFor <= ta.MaxDB() ? OperationAllowed.DBOnly :
                    OperationAllowed.None;
            };



            var operation = OperationLogic.FindOperation(operationType.type ?? /*Temp*/  OperationLogic.FindTypes(operationType.operation).First(), operationType.operation);

            Type resultType = operation.OperationType == OperationType.ConstructorFrom ||
                operation.OperationType == OperationType.ConstructorFromMany ? operation.ReturnType! : operation.OverridenType;

            var result = operationAllowed(resultType);

            if (result == OperationAllowed.None)
                return result;

            Type? fromType = operation.OperationType == OperationType.ConstructorFrom ||
                operation.OperationType == OperationType.ConstructorFromMany ? operation.OverridenType : null;

            if (fromType == null)
                return result;

            var fromTypeAllowed = operationAllowed(fromType);

            return result < fromTypeAllowed ? result : fromTypeAllowed;
        }
    }

    class OperationMerger : IMerger<(OperationSymbol operation, Type type), OperationAllowed>
    {
        public OperationAllowed Merge((OperationSymbol operation, Type type) operationType, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, OperationAllowed>> baseValues)
        {
            OperationAllowed best = AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union ?
                Max(baseValues.Select(a => a.Value)):
                Min(baseValues.Select(a => a.Value));

            if (!BasicPermission.AutomaticUpgradeOfOperations.IsAuthorized(role))
               return best;

            var maxUp = OperationAuthLogic.MaxAutomaticUpgrade.TryGetS(operationType.Item1);

            if (maxUp.HasValue && maxUp <= best)
                return best;

            if (baseValues.Where(a => a.Value.Equals(best)).All(a => GetDefault(operationType, a.Key).Equals(a.Value)))
            {
                var def = GetDefault(operationType, role);

                return maxUp.HasValue && maxUp <= def ? maxUp.Value : def;
            }

            return best;
        }

        static OperationAllowed GetDefault((OperationSymbol operation, Type type) operationType, Lite<RoleEntity> role)
        {
            return OperationAuthLogic.MaxTypePermission(operationType, TypeAllowedBasic.Write, t => TypeAuthLogic.GetAllowed(role, t));
        }

        static OperationAllowed Max(IEnumerable<OperationAllowed> baseValues)
        {
            OperationAllowed result = OperationAllowed.None;

            foreach (var item in baseValues)
            {
                if (item > result)
                    result = item;

                if (result == OperationAllowed.Allow)
                    return result;

            }
            return result;
        }

        static OperationAllowed Min(IEnumerable<OperationAllowed> baseValues)
        {
            OperationAllowed result = OperationAllowed.Allow;

            foreach (var item in baseValues)
            {
                if (item < result)
                    result = item;

                if (result == OperationAllowed.None)
                    return result;

            }
            return result;
        }

        public Func<(OperationSymbol operation, Type type), OperationAllowed> MergeDefault(Lite<RoleEntity> role)
        {
            return key =>
            {
                if (!BasicPermission.AutomaticUpgradeOfOperations.IsAuthorized(role))
                    return AuthLogic.GetDefaultAllowed(role) ? OperationAllowed.Allow : OperationAllowed.None;

                var maxUp = OperationAuthLogic.MaxAutomaticUpgrade.TryGetS(key.operation);

                var def = GetDefault(key, role);

                return maxUp.HasValue && maxUp <= def ? maxUp.Value : def;
            };
        }

    }

    class OperationCoercer : Coercer<OperationAllowed, (OperationSymbol symbol, Type type)>
    {
        public static readonly OperationCoercer Instance = new OperationCoercer();

        private OperationCoercer()
        {
        }

        public override Func<(OperationSymbol symbol, Type type), OperationAllowed, OperationAllowed> GetCoerceValue(Lite<RoleEntity> role)
        {
            return (operationType, allowed) =>
            {
                if (OperationAuthLogic.AvoidCoerce.Contains(operationType.symbol))
                    return allowed;

                var required = OperationAuthLogic.MaxTypePermission(operationType, TypeAllowedBasic.Read, t => TypeAuthLogic.GetAllowed(role, t));

                return allowed < required ? allowed : required;
            };
        }

        public override Func<Lite<RoleEntity>, OperationAllowed, OperationAllowed> GetCoerceValueManual((OperationSymbol symbol, Type type) operationType)
        {
            return (role, allowed) =>
            {
                if (OperationAuthLogic.AvoidCoerce.Contains(operationType.symbol))
                    return allowed;

                var required = OperationAuthLogic.MaxTypePermission(operationType, TypeAllowedBasic.Read, t => TypeAuthLogic.Manual.GetAllowed(role, t));

                return allowed < required ? allowed : required;
            };
        }
    }
}
