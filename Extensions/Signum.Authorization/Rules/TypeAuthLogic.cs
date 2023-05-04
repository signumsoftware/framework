using Signum.Authorization.Rules;
using Signum.Utilities.Reflection;
using System.Collections.Immutable;

namespace Signum.Authorization;

public static partial class TypeAuthLogic
{
    static TypeAuthCache cache = null!;

    public static IManualAuth<Type, TypeAllowedAndConditions> Manual { get { return cache; } }

    public static bool IsStarted { get { return cache != null; } }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            TypeLogic.AssertStarted(sb);
            AuthLogic.AssertStarted(sb);
            TypeConditionLogic.Start(sb);

            sb.Schema.EntityEventsGlobal.Saving += Schema_Saving; //because we need Modifications propagated
            sb.Schema.EntityEventsGlobal.Retrieved += EntityEventsGlobal_Retrieved;
            sb.Schema.IsAllowedCallback += Schema_IsAllowedCallback;

            sb.Schema.SchemaCompleted += () =>
            {
                foreach (var type in TypeConditionLogic.Types)
                {
                    miRegister.GetInvoker(type)(Schema.Current);
                }
            };

            sb.Schema.Synchronizing += Schema_Synchronizing;

            sb.Schema.EntityEventsGlobal.PreUnsafeDelete += query =>
            {
                return TypeAuthLogic.OnIsDelete(query.ElementType);
            };

            cache = new TypeAuthCache(sb, merger: TypeAllowedMerger.Instance);

            AuthLogic.ExportToXml += exportAll => cache.ExportXml(exportAll ? TypeLogic.TypeToEntity.Keys.ToList() : null);
            AuthLogic.ImportFromXml += (x, roles, replacements) => cache.ImportXml(x, roles, replacements);

            AuthLogic.HasRuleOverridesEvent += role => cache.HasRealOverrides(role);
            TypeConditionLogic.Register(UserTypeCondition.DeactivatedUsers, (UserEntity u) => u.State == UserState.Deactivated);
        }
    }

    static GenericInvoker<Action<Schema>> miRegister =
        new(s => RegisterSchemaEvent<TypeEntity>(s));
    static void RegisterSchemaEvent<T>(Schema sender)
         where T : Entity
    {
        sender.EntityEvents<T>().FilterQuery += new FilterQueryEventHandler<T>(TypeAuthLogic_FilterQuery<T>);
    }

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => TypeAuthLogic.Start(null!)));
    }

    static string? Schema_IsAllowedCallback(Type type, bool inUserInterface)
    {
        var allowed = GetAllowed(type);

        if (allowed.Max(inUserInterface) == TypeAllowedBasic.None)
            return "Type '{0}' is set to None".FormatWith(type.NiceName());

        return null;
    }

    static void Schema_Saving(Entity ident)
    {
        if (ident.IsGraphModified && !inSave.Value)
        {
            TypeAllowedAndConditions access = GetAllowed(ident.GetType());

            var requested = TypeAllowedBasic.Write;

            var min = access.MinDB();
            var max = access.MaxDB();
            if (requested <= min)
                return;

            if (max < requested)
                throw new UnauthorizedAccessException(AuthMessage.NotAuthorizedTo0The1WithId2.NiceToString().FormatWith(requested.NiceToString(), ident.GetType().NiceName(), ident.IdOrNull));

            Schema_Saving_Instance(ident);
        }
    }


    static void EntityEventsGlobal_Retrieved(Entity ident, PostRetrievingContext ctx)
    {
        Type type = ident.GetType();
        TypeAllowedBasic access = GetAllowed(type).MaxDB();
        if (access < TypeAllowedBasic.Read)
            throw new UnauthorizedAccessException(AuthMessage.NotAuthorizedToRetrieve0.NiceToString().FormatWith(type.NicePluralName()));
    }

    public static TypeRulePack GetTypeRules(Lite<RoleEntity> roleLite)
    {
        var result = new TypeRulePack { Role = roleLite };
        Schema s = Schema.Current;
        cache.GetRules(result, TypeLogic.TypeToEntity.Where(t => !t.Key.IsEnumEntity() && s.IsAllowed(t.Key, false) == null).Select(a => a.Value));

        foreach (TypeAllowedRule r in result.Rules)
        {
            Type type = TypeLogic.EntityToType[r.Resource];

            if (OperationAuthLogic.IsStarted)
                r.Operations = OperationAuthLogic.GetAllowedThumbnail(roleLite, type);

            if (PropertyAuthLogic.IsStarted)
                r.Properties = PropertyAuthLogic.GetAllowedThumbnail(roleLite, type);

            if (QueryAuthLogic.IsStarted)
                r.Queries = QueryAuthLogic.GetAllowedThumbnail(roleLite, type);
        }

        return result;

    }

    public static void SetTypeRules(TypeRulePack rules)
    {
        cache.SetRules(rules);
    }

    public static TypeAllowedAndConditions GetAllowed(Type type)
    {
        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return new TypeAllowedAndConditions(TypeAllowed.Write);

        if (!TypeLogic.TypeToEntity.ContainsKey(type))
            return new TypeAllowedAndConditions(TypeAllowed.Write);

        if (EnumEntity.Extract(type) != null)
            return new TypeAllowedAndConditions(TypeAllowed.Read);

        var allowed = cache.GetAllowed(RoleEntity.Current, type);

        var overrideTypeAllowed = TypeAuthLogic.GetOverrideTypeAllowed(type);
        if (overrideTypeAllowed != null)
            return overrideTypeAllowed(allowed);

        return allowed;
    }

    public static TypeAllowedAndConditions GetAllowed(Lite<RoleEntity> role, Type type)
    {
        return cache.GetAllowed(role, type);
    }

    public static TypeAllowedAndConditions GetAllowedBase(Lite<RoleEntity> role, Type type)
    {
        return cache.GetAllowedBase(role, type);
    }

    public static DefaultDictionary<Type, TypeAllowedAndConditions> AuthorizedTypes()
    {
        return cache.GetDefaultDictionary();
    }

    static readonly Variable<ImmutableStack<(Type type, Func<TypeAllowedAndConditions, TypeAllowedAndConditions> typeAllowedOverride)>> tempAllowed =
        Statics.ThreadVariable<ImmutableStack<(Type type, Func<TypeAllowedAndConditions, TypeAllowedAndConditions> typeAllowedOverride)>>("temporallyAllowed");

    public static IDisposable OverrideTypeAllowed<T>(Func<TypeAllowedAndConditions, TypeAllowedAndConditions> typeAllowedOverride)
        where T : Entity
    {
        var old = tempAllowed.Value;

        tempAllowed.Value = (old ?? ImmutableStack<(Type type, Func<TypeAllowedAndConditions, TypeAllowedAndConditions> typeAllowedOverride)>.Empty).Push((typeof(T), typeAllowedOverride));

        return new Disposable(() => tempAllowed.Value = tempAllowed.Value.Pop());
    }

    internal static Func<TypeAllowedAndConditions, TypeAllowedAndConditions>? GetOverrideTypeAllowed(Type type)
    {
        var ta = tempAllowed.Value;
        if (ta == null || ta.IsEmpty)
            return null;

        var pair = ta.FirstOrDefault(a => a.type == type);

        if (pair.type == null)
            return null;

        return pair.typeAllowedOverride;
    }
}

class TypeAllowedMerger : IMerger<Type, TypeAllowedAndConditions>
{
    public static readonly TypeAllowedMerger Instance = new TypeAllowedMerger();

    TypeAllowedMerger() { }

    public TypeAllowedAndConditions Merge(Type key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, TypeAllowedAndConditions>> baseValues)
    {
        if (AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union)
            return MergeBase(baseValues.Select(a => a.Value), MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);
        else
            return MergeBase(baseValues.Select(a => a.Value), MinTypeAllowed, TypeAllowed.None, TypeAllowed.Write);
    }

    static TypeAllowed MinTypeAllowed(IEnumerable<TypeAllowed> collection)
    {
        TypeAllowed result = TypeAllowed.Write;

        foreach (var item in collection)
        {
            if (item < result)
                result = item;

            if (result == TypeAllowed.None)
                return result;

        }
        return result;
    }

    static TypeAllowed MaxTypeAllowed(IEnumerable<TypeAllowed> collection)
    {
        TypeAllowed result = TypeAllowed.None;

        foreach (var item in collection)
        {
            if (item > result)
                result = item;

            if (result == TypeAllowed.Write)
                return result;

        }
        return result;
    }

    public Func<Type, TypeAllowedAndConditions> MergeDefault(Lite<RoleEntity> role)
    {
        var taac = new TypeAllowedAndConditions(AuthLogic.GetDefaultAllowed(role) ? TypeAllowed.Write : TypeAllowed.None);
        return new ConstantFunctionButEnums(taac).GetValue;
    }

    public static TypeAllowedAndConditions MergeBase(IEnumerable<TypeAllowedAndConditions> baseRules, Func<IEnumerable<TypeAllowed>, TypeAllowed> maxMerge, TypeAllowed max, TypeAllowed min)
    {
        TypeAllowedAndConditions? only = baseRules.Only();
        if (only != null)
            return only;

        if (baseRules.Any(a => a.Exactly(max)))
            return new TypeAllowedAndConditions(max);

        TypeAllowedAndConditions? onlyNotOposite = baseRules.Where(a => !a.Exactly(min)).Only();
        if (onlyNotOposite != null)
            return onlyNotOposite;

        if (baseRules.All(a => a.ConditionRules.Count == 0))
            return new TypeAllowedAndConditions(maxMerge(baseRules.Select(a => a.Fallback)));

        var conditions = baseRules.SelectMany(a => a.ConditionRules).SelectMany(a => a.TypeConditions).Distinct().OrderBy(a => a.ToString()).ToList();

        if (conditions.Count > 31)
            throw new InvalidOperationException("You can not merge more than 31 type conditions");

        var conditionDictionary = conditions.Select((tc, i) => KeyValuePair.Create(tc, 1 << i)).ToDictionaryEx();

        int numCells = 1 << conditionDictionary.Count;

        var matrixes = baseRules.Select(tac => GetMatrix(tac, numCells, conditionDictionary)).ToList();

        var maxMatrix = 0.To(numCells).Select(i => maxMerge(matrixes.Select(m => m[i]))).ToArray();

        return GetRules(maxMatrix, numCells, conditionDictionary);
    }

    static string Debug(int cell, Dictionary<TypeConditionSymbol, int> conditionDictionary)
    {
        return conditionDictionary.ToString(kvp => ((kvp.Value & cell) == kvp.Value ? " " : "!") + kvp.Key.ToString().After("."), " & ");
    }

    static string Debug(TypeAllowed[] matrix, Dictionary<TypeConditionSymbol, int> conditionDictionary)
    {
        return matrix.Select((ta, i) => Debug(i, conditionDictionary) + " => " + ta).ToString("\n");
    }

    static string Debug(TypeAllowed?[] matrix, Dictionary<TypeConditionSymbol, int> conditionDictionary)
    {
        return matrix.Select((ta, i) => Debug(i, conditionDictionary) + " => " + ta).ToString("\n");
    }

    static TypeAllowed[] GetMatrix(TypeAllowedAndConditions tac, int numCells, Dictionary<TypeConditionSymbol, int> conditionDictionary)
    {
        var matrix = 0.To(numCells).Select(a => tac.Fallback).ToArray();

        foreach (var rule in tac.ConditionRules)
        {
            var mask = rule.TypeConditions.Select(tc => conditionDictionary.GetOrThrow(tc)).Aggregate((a, b) => a | b);

            for (int i = 0; i < numCells; i++)
            {
                if ((i & mask) == mask)
                {
                    matrix[i] = rule.Allowed;
                }
            }
        }

        return matrix;
    }

    static TypeAllowedAndConditions GetRules(TypeAllowed[] matrix, int numCells, Dictionary<TypeConditionSymbol, int> conditionDictionary)
    {
        var array = matrix.Select(ta => (TypeAllowed?)ta).ToArray();

        var conditionRules = new List<TypeConditionRuleModel>();

        var availableTypeConditions = conditionDictionary.Keys.ToList();

        while (true)
        {
            { //0 Conditions
                var ta = OnlyOneValue(0);

                if (ta != null)
                    return new TypeAllowedAndConditions(ta.Value, conditionRules.AsEnumerable().Reverse().ToArray());
            }

            { //1 Condition
                foreach (var tc in availableTypeConditions)
                {
                    var mask = conditionDictionary[tc];

                    var ta = OnlyOneValue(mask);

                    if (ta.HasValue)
                    {
                        conditionRules.Add(new TypeConditionRuleModel(new[] { tc }, ta.Value));
                        availableTypeConditions.Remove(tc);

                        ClearArray(mask);

                        goto next;
                    }
                }

            }

            //>= 2 Conditions
            for (int numConditions = 2; numConditions <= availableTypeConditions.Count; numConditions++)
            {
                foreach (var mask in GetMasksOf(numConditions, availableTypeConditions))
                {
                    var ta = OnlyOneValue(mask);

                    if (ta.HasValue)
                    {
                        conditionRules.Add(new TypeConditionRuleModel(availableTypeConditions.Where(tc => (conditionDictionary[tc] & mask) == conditionDictionary[tc]).ToArray(), ta.Value));

                        ClearArray(mask);

                        goto next;
                    }
                }
            }

        next: continue;
        }


        TypeAllowed? OnlyOneValue(int mask)
        {
            TypeAllowed? currentValue = null;

            for (int i = 0; i < numCells; i++)
            {
                if ((i & mask) == mask)
                {
                    var v = array![i];
                    if (v != null)
                    {
                        if (currentValue == null)
                            currentValue = v;
                        else if (currentValue != v)
                            return null;
                    }
                }
            }

            if (currentValue == null)
                throw new InvalidOperationException("Array is empty!");

            return currentValue;
        }

        void ClearArray(int mask)
        {
            for (int i = 0; i < numCells; i++)
            {
                if ((i & mask) == mask)
                {
                    array![i] = null;
                }
            }
        }


        IEnumerable<int> GetMasksOf(int numConditions, List<TypeConditionSymbol> availableTypeConditions, int skip = 0)
        {
            if (numConditions == 1)
            {
                for (int i = skip; i < availableTypeConditions.Count; i++)
                {
                    yield return conditionDictionary[availableTypeConditions[i]];
                }
            }
            else
            {
                for (int i = skip; i < availableTypeConditions.Count; i++)
                {
                    var val = conditionDictionary[availableTypeConditions[i]];

                    foreach (var item in GetMasksOf(numConditions -1, availableTypeConditions, skip + 1))
                    {
                        yield return item | val;
                    }
                }
            }
        }
    }
}

public static class AuthThumbnailExtensions
{
    public static AuthThumbnail? Collapse(this IEnumerable<bool> values)
    {
        bool? acum = null;
        foreach (var item in values)
        {
            if (acum == null)
                acum = item;
            else if (acum.Value != item)
                return AuthThumbnail.Mix;
        }

        if (acum == null)
            return null;

        return acum.Value ? AuthThumbnail.All : AuthThumbnail.None;
    }

    public static AuthThumbnail? Collapse(this IEnumerable<QueryAllowed> values)
    {
        QueryAllowed? acum = null;
        foreach (var item in values)
        {
            if (acum == null)
                acum = item;
            else if (acum.Value != item)
                return AuthThumbnail.Mix;
        }

        if (acum == null)
            return null;

        return
           acum.Value == QueryAllowed.None ? AuthThumbnail.None :
           acum.Value == QueryAllowed.EmbeddedOnly ? AuthThumbnail.Mix : AuthThumbnail.All;
    }

    public static AuthThumbnail? Collapse(this IEnumerable<OperationAllowed> values)
    {
        OperationAllowed? acum = null;
        foreach (var item in values)
        {
            if (acum == null)
                acum = item;
            else if (acum.Value != item)
                return AuthThumbnail.Mix;
        }

        if (acum == null)
            return null;

        return
           acum.Value == OperationAllowed.None ? AuthThumbnail.None :
           acum.Value == OperationAllowed.DBOnly ? AuthThumbnail.Mix : AuthThumbnail.All;
    }

    public static AuthThumbnail? Collapse(this IEnumerable<PropertyAllowed> values)
    {
        PropertyAllowed? acum = null;
        foreach (var item in values)
        {
            if (acum == null)
                acum = item;
            else if (acum.Value != item || acum.Value == PropertyAllowed.Read)
                return AuthThumbnail.Mix;
        }

        if (acum == null)
            return null;

        return
            acum.Value == PropertyAllowed.None ? AuthThumbnail.None :
            acum.Value == PropertyAllowed.Read ? AuthThumbnail.Mix : AuthThumbnail.All;

    }
}
