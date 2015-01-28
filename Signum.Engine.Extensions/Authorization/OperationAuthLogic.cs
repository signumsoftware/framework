using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System.Threading;
using Signum.Entities;
using Signum.Engine.Operations;
using System.Reflection;
using Signum.Entities.DynamicQuery;

namespace Signum.Engine.Authorization
{
    public static class OperationAuthLogic
    {
        static AuthCache<RuleOperationEntity, OperationAllowedRule, OperationSymbol, OperationSymbol, OperationAllowed> cache;

        public static IManualAuth<OperationSymbol, OperationAllowed> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public static readonly HashSet<OperationSymbol> AvoidAutomaticUpgradeCollection = new HashSet<OperationSymbol>();

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                OperationLogic.AssertStarted(sb);

                OperationLogic.AllowOperation += new AllowOperationHandler(OperationLogic_AllowOperation);

                cache = new AuthCache<RuleOperationEntity, OperationAllowedRule, OperationSymbol, OperationSymbol, OperationAllowed>(sb,
                     s=>s,
                     s=>s,
                     merger: new OperationMerger(),
                     invalidateWithTypes: true,
                     coercer:  OperationCoercer.Instance);

                AuthLogic.ExportToXml += exportAll => cache.ExportXml("Operations", "Operation", s => s.Key, b => b.ToString(),
                    exportAll ? OperationLogic.RegisteredOperations.ToList() : null);
                AuthLogic.ImportFromXml += (x, roles, replacements) =>
                {
                    string replacementKey = "AuthRules:" + typeof(OperationSymbol).Name;

                    replacements.AskForReplacements(
                        x.Element("Operations").Elements("Role").SelectMany(r => r.Elements("Operation")).Select(p => p.Attribute("Resource").Value).ToHashSet(),
                        SymbolLogic<OperationSymbol>.AllUniqueKeys(),
                        replacementKey);

                    return cache.ImportXml(x, "Operations", "Operation", roles,
                        s => SymbolLogic<OperationSymbol>.TryToSymbol(replacements.Apply(replacementKey, s)), EnumExtensions.ToEnum<OperationAllowed>);
                };
            }
        }

        public static T AvoidAutomaticUpgrade<T>(this T operation) where T : IOperation
        {
            AvoidAutomaticUpgradeCollection.Add(operation.OperationSymbol);

            return operation;
        }

        static bool OperationLogic_AllowOperation(OperationSymbol operationKey, bool inUserInterface)
        {
            return GetOperationAllowed(operationKey, inUserInterface);
        }

        public static OperationRulePack GetOperationRules(Lite<RoleEntity> role, TypeEntity typeEntity)
        {
            var resources = OperationLogic.GetAllOperationInfos(TypeLogic.DnToType[typeEntity]).Select(a => a.OperationSymbol);
            var result = new OperationRulePack { Role = role, Type = typeEntity, };

            cache.GetRules(result, resources);

            var coercer = OperationCoercer.Instance.GetCoerceValue(role);
            result.Rules.ForEach(r => r.CoercedValues = EnumExtensions.GetValues<OperationAllowed>().Where(a => !coercer(r.Resource, a).Equals(a)).ToArray());
            
            return result;
        }

        public static void SetOperationRules(OperationRulePack rules)
        {
            var keys = OperationLogic.GetAllOperationInfos(TypeLogic.DnToType[rules.Type])
                .Select(a => a.OperationSymbol).ToHashSet();

            cache.SetRules(rules, r => keys.Contains(r));
        }

        public static bool GetOperationAllowed(Lite<RoleEntity> role, OperationSymbol operationKey, bool inUserInterface)
        {
            OperationAllowed allowed = GetOperationAllowed(role, operationKey);

            return allowed == OperationAllowed.Allow || allowed == OperationAllowed.DBOnly && !inUserInterface;
        }

        public static OperationAllowed GetOperationAllowed(Lite<RoleEntity> role, OperationSymbol operationKey)
        {
            return cache.GetAllowed(role, operationKey);
        }

        public static bool GetOperationAllowed(OperationSymbol operationKey, bool inUserInterface)
        {
            if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
                return true;

            if (GetTemporallyAllowed(operationKey))
                return true;

            OperationAllowed allowed =cache.GetAllowed(RoleEntity.Current.ToLite(), operationKey);

            return allowed == OperationAllowed.Allow || allowed == OperationAllowed.DBOnly && !inUserInterface;
        }

        public static AuthThumbnail? GetAllowedThumbnail(Lite<RoleEntity> role, Type entityType)
        {
            return OperationLogic.GetAllOperationInfos(entityType).Select(oi => cache.GetAllowed(role, oi.OperationSymbol)).Collapse();
        }

        public static Dictionary<OperationSymbol, OperationAllowed> AllowedOperations()
        {
            return OperationLogic.AllSymbols().ToDictionary(k => k, k => cache.GetAllowed(RoleEntity.Current.ToLite(), k));
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

        public static OperationAllowed MaxTypePermission(OperationSymbol operationKey, TypeAllowedBasic minimum,  Func<Type, TypeAllowedAndConditions> allowed)
        {
            Func<Type, OperationAllowed> operationAllowed = t =>
            {
                if (!TypeLogic.TypeToEntity.ContainsKey(t))
                    return OperationAllowed.Allow;
                
                var ta = allowed(t);

                return minimum <= ta.MaxUI() ? OperationAllowed.Allow :
                    minimum <= ta.MaxDB() ? OperationAllowed.DBOnly :
                    OperationAllowed.None;
            };

            return OperationLogic.FindTypes(operationKey).Max(t =>
            {
                var operation = OperationLogic.FindOperation(t, operationKey);

                Type resultType = operation.OperationType == OperationType.ConstructorFrom ||
                    operation.OperationType == OperationType.ConstructorFromMany ? operation.ReturnType : operation.OverridenType;

                var result = operationAllowed(resultType);

                if (result == OperationAllowed.None)
                    return result;

                Type fromType = operation.OperationType == OperationType.ConstructorFrom ||
                    operation.OperationType == OperationType.ConstructorFromMany ? operation.OverridenType : null;

                if (fromType == null)
                    return result;

                var fromTypeAllowed = operationAllowed(fromType);

                return result < fromTypeAllowed ? result : fromTypeAllowed;
            });
        }
    }

    class OperationMerger : IMerger<OperationSymbol, OperationAllowed>
    {
        public OperationAllowed Merge(OperationSymbol key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, OperationAllowed>> baseValues)
        {   
            OperationAllowed best = AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union ? 
                Max(baseValues.Select(a => a.Value)):
                Min(baseValues.Select(a => a.Value));

            if (!BasicPermission.AutomaticUpgradeOfOperations.IsAuthorized(role) || OperationAuthLogic.AvoidAutomaticUpgradeCollection.Contains(key))
               return best;

            if (baseValues.Where(a => a.Value.Equals(best)).All(a => GetDefault(key, a.Key).Equals(a.Value)))
                return GetDefault(key, role);

            return best; 
        }

        static OperationAllowed GetDefault(OperationSymbol key, Lite<RoleEntity> role)
        {
            return OperationAuthLogic.MaxTypePermission(key, TypeAllowedBasic.Modify, t => TypeAuthLogic.GetAllowed(role, t));
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

        public Func<OperationSymbol, OperationAllowed> MergeDefault(Lite<RoleEntity> role)
        {
            return key => 
            {
                if (!BasicPermission.AutomaticUpgradeOfOperations.IsAuthorized(role) || OperationAuthLogic.AvoidAutomaticUpgradeCollection.Contains(key))
                    return AuthLogic.GetDefaultAllowed(role) ? OperationAllowed.Allow : OperationAllowed.None;

                return GetDefault(key, role);
            };
        }

    }

    class OperationCoercer : Coercer<OperationAllowed, OperationSymbol>
    {
        public static readonly OperationCoercer Instance = new OperationCoercer();

        private OperationCoercer()
        {
        }

        public override Func<OperationSymbol, OperationAllowed, OperationAllowed> GetCoerceValue(Lite<RoleEntity> role)
        {
            return (operationKey, allowed) =>
            {
                var required = OperationAuthLogic.MaxTypePermission(operationKey, TypeAllowedBasic.Read, t => TypeAuthLogic.GetAllowed(role, t));

                return allowed < required ? allowed : required;
            };
        }

        public override Func<Lite<RoleEntity>, OperationAllowed, OperationAllowed> GetCoerceValueManual(OperationSymbol operationKey)
        {
            return (role, allowed) =>
            {
                var required = OperationAuthLogic.MaxTypePermission(operationKey, TypeAllowedBasic.Read, t => TypeAuthLogic.Manual.GetAllowed(role, t));

                return allowed < required ? allowed : required;
            };
        }
    }
}
