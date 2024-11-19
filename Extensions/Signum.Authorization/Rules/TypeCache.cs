using Microsoft.VisualBasic;
using Signum.Basics;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Signum.Authorization.Rules;

class TypeCache : AuthCache<RuleTypeEntity, TypeAllowedRule, TypeEntity, Type, WithConditions<TypeAllowed>, WithConditionsModel<TypeAllowed>>
{
    public TypeCache(SchemaBuilder sb) : base(sb, invalidateWithTypes: true)
    {
    }

    protected override Expression<Func<TypeEntity, TypeEntity, bool>> IsEqual => (t1, t2) => t1.Is(t2);

    protected override Type ToKey(TypeEntity resource) => resource.ToType();
    protected override TypeEntity ToEntity(Type key) => key.ToTypeEntity();

    protected override WithConditions<TypeAllowed> GetRuleAllowed(RuleTypeEntity rule) => new WithConditions<TypeAllowed>(rule.Fallback,
            rule.ConditionRules.Select(c => new ConditionRule<TypeAllowed>(c.Conditions.ToFrozenSet(), c.Allowed)).ToReadOnly());

    protected override RuleTypeEntity SetRuleAllowed(RuleTypeEntity rule, WithConditions<TypeAllowed> allowed)
    {
        rule.Fallback = allowed.Fallback;
        rule.ConditionRules = allowed.ConditionRules.Select(a => new RuleTypeConditionEntity
        {
            Allowed = a.Allowed,
            Conditions = a.TypeConditions.ToMList()
        }).ToMList();
        return rule;
    }

    protected override WithConditions<TypeAllowed> ToAllowed(WithConditionsModel<TypeAllowed> allowedModel) => allowedModel.ToImmutable();
    protected override WithConditionsModel<TypeAllowed> ToAllowedModel(WithConditions<TypeAllowed> allowed) => allowed.ToModel();

    protected override TypeAllowedRule ToAllowedRule(TypeEntity resource, RoleAllowedCache ruleCache)
    {
        var r =  base.ToAllowedRule(resource, ruleCache);

        Type type = r.Resource.ToType();

        if (OperationAuthLogic.IsStarted)
            r.Operations = OperationAuthLogic.GetAllowedThumbnail(ruleCache.Role, type);

        if (PropertyAuthLogic.IsStarted)
            r.Properties = PropertyAuthLogic.GetAllowedThumbnail(ruleCache.Role, type, r.Allowed.ToImmutable());

        if (QueryAuthLogic.IsStarted)
            r.Queries = QueryAuthLogic.GetAllowedThumbnail(ruleCache.Role, type);

        r.AvailableConditions = TypeConditionLogic.ConditionsFor(type).ToList();

        return r;
    }
    protected override WithConditions<TypeAllowed> Merge(Type key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, WithConditions<TypeAllowed>>> baseValues)
    {
        var strategy = AuthLogic.GetMergeStrategy(role);
        if (strategy == MergeStrategy.Union)
            return ConditionMerger<TypeAllowed>.MergeBase(strategy, baseValues.Select(a => a.Value).ToList(), MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);
        else
            return ConditionMerger<TypeAllowed>.MergeBase(strategy, baseValues.Select(a => a.Value).ToList(), MinTypeAllowed, TypeAllowed.None, TypeAllowed.Write);
    }

    internal static TypeAllowed MinTypeAllowed(IEnumerable<TypeAllowed> collection)
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

    internal static TypeAllowed MaxTypeAllowed(IEnumerable<TypeAllowed> collection)
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


    protected override Func<Type, WithConditions<TypeAllowed>> MergeDefault(Lite<RoleEntity> role)
    {
        var allowed = AuthLogic.GetDefaultAllowed(role) ? TypeAllowed.Write : TypeAllowed.None;

        return type =>
        {
            if (EnumEntity.Extract(type) != null)
                return new WithConditions<TypeAllowed>(TypeAllowed.Read);

            return new WithConditions<TypeAllowed>(allowed);
        };
    }



    public override XElement ExportXml(bool exportAll)
    {
        return ExportXmlInternal("Types", "Type",
            resourceToString: TypeLogic.GetCleanName,
            allowedToXml: taac => [
                new XAttribute("Allowed", taac.Fallback.ToString()),
                taac.ConditionRules.Select(c => new XElement("Condition",
                   new XAttribute("Name", c.TypeConditions.ToString(", ")),
                   new XAttribute("Allowed", c.Allowed.ToString())))
                ],
            allKeys: exportAll ? TypeLogic.TypeToEntity.Keys.ToList() : null);
    }

    internal static readonly string typeReplacementKey = "AuthRules:" + typeof(TypeEntity).Name;
    internal static readonly string typeConditionReplacementKey = "AuthRules:" + typeof(TypeConditionSymbol).Name;

    public override SqlPreCommand? ImportXml(XElement root, Dictionary<string, Lite<RoleEntity>> roles, Replacements replacements)
    {
        var current = Database.RetrieveAll<RuleTypeEntity>().GroupToDictionary(a => a.Role);
        var xRoles = (root.Element("Types")?.Elements("Role")).EmptyIfNull();
        var should = xRoles.ToDictionary(x => roles.GetOrThrow(x.Attribute("Name")!.Value));

        Table table = Schema.Current.Table(typeof(RuleTypeEntity));
        Table conditionTable = Schema.Current.Table(typeof(RuleTypeConditionEntity));

        replacements.AskForReplacements(
            xRoles.SelectMany(x => x.Elements("Type")).Select(x => x.Attribute("Resource")!.Value).ToHashSet(),
            TypeLogic.NameToType.Where(a => !a.Value.IsEnumEntity()).Select(a => a.Key).ToHashSet(), typeReplacementKey);

        replacements.AskForReplacements(
            xRoles.SelectMany(x => x.Elements("Type")).SelectMany(t => t.Elements("Condition")).SelectMany(x => x.Attribute("Name")!.Value.SplitNoEmpty(",").Select(a => a.Trim()).ToList()).ToHashSet(),
            SymbolLogic<TypeConditionSymbol>.AllUniqueKeys(),
            typeConditionReplacementKey);

        return ImportXmlInternal(root, "Types", "Type", roles,
            toResource: s =>
            {
                Type? type = TypeLogic.NameToType.TryGetC(replacements.Apply(typeReplacementKey, s));

                if (type == null)
                    return null;

                return TypeLogic.TypeToEntity.GetOrThrow(type);
            },
            parseAllowed: e =>
            {
                return new WithConditions<TypeAllowed>(
                    fallback: e.Attribute("Allowed")!.Value.ToEnum<TypeAllowed>(),
                    conditionRules: e.Elements("Condition").Select(xc => new ConditionRule<TypeAllowed>(
                        typeConditions: xc.Attribute("Name")!.Value.SplitNoEmpty(",").Select(s => SymbolLogic<TypeConditionSymbol>.TryToSymbol(replacements.Apply(typeConditionReplacementKey, s.Trim()))).NotNull().ToFrozenSet(),
                        allowed: xc.Attribute("Allowed")!.Value.ToEnum<TypeAllowed>())).ToReadOnly());
            });
    }

    protected override string AllowedComment(WithConditions<TypeAllowed> allowed)
    {
        if(allowed.ConditionRules.Count == 0)
        return allowed.Fallback.ToString();

        return $"{allowed.Fallback} + {allowed.ConditionRules.Count} conditions";
    }
}



internal static class ConditionMerger<A> where A : struct, Enum
{
    static ConcurrentDictionary<(MergeStrategy strategy, StructureList<WithConditions<A>> tuple), WithConditions<A>> cache = new();

    public static WithConditions<A> MergeBase(MergeStrategy mergeStrage, List<WithConditions<A>> baseRules, Func<IEnumerable<A>, A> maxMerge, A max, A min)
    {
        return cache.GetOrAdd((mergeStrage, new StructureList<WithConditions<A>>(baseRules)), tuple => MergeBaseImplementations(baseRules, maxMerge, max, min));
    }

    internal static WithConditions<A> MergeBaseImplementations(List<WithConditions<A>> baseRules, Func<IEnumerable<A>, A> maxMerge, A max, A min)
    {
        WithConditions<A>? only = baseRules.Only();
        if (only != null)
            return only;

        if (baseRules.Any(a => a.Exactly(max)))
            return new WithConditions<A>(max);

        WithConditions<A>? onlyNotMin = baseRules.Where(a => !a.Exactly(min)).Only();
        if (onlyNotMin != null)
            return onlyNotMin;

        if (baseRules.All(a => a.ConditionRules.Count == 0))
            return new WithConditions<A>(maxMerge(baseRules.Select(a => a.Fallback)));

        var conditions = baseRules.SelectMany(a => a.ConditionRules).SelectMany(a => a.TypeConditions).Distinct().OrderBy(a => a.ToString()).ToList();

        if (conditions.Count > 31)
            throw new InvalidOperationException("You can not merge more than 31 type conditions");

        var conditionDictionary = conditions.Select((tc, i) => KeyValuePair.Create(tc, 1 << i)).ToDictionaryEx();

        int numCells = 1 << conditionDictionary.Count;

        var matrixes = baseRules.Select(tac => GetMatrix(tac, numCells, conditionDictionary)).ToList();

        var maxMatrix = 0.To(numCells).Select(i => maxMerge(matrixes.Select(m => m[i]))).ToArray();

        return GetRules(maxMatrix, numCells, conditionDictionary);
    }


    static A[] GetMatrix(WithConditions<A> tac, int numCells, Dictionary<TypeConditionSymbol, int> conditionDictionary)
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

    static WithConditions<A> GetRules(A[] matrix, int numCells, Dictionary<TypeConditionSymbol, int> conditionDictionary)
    {
        var array = matrix.Select(ta => (A?)ta).ToArray();

        var conditionRules = new List<ConditionRule<A>>();

        var availableTypeConditions = conditionDictionary.Keys.ToList();

        while (true)
        {
            { //0 Conditions
                var ta = OnlyOneValue(0);

                if (ta != null)
                    return new WithConditions<A>(ta.Value, conditionRules.AsEnumerable().Reverse().ToReadOnly());
            }

            { //1 Condition
                foreach (var tc in availableTypeConditions)
                {
                    var mask = conditionDictionary[tc];

                    var ta = OnlyOneValue(mask);

                    if (ta.HasValue)
                    {
                        conditionRules.Add(new ConditionRule<A>(new[] { tc }.ToFrozenSet(), ta.Value));
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
                        conditionRules.Add(new ConditionRule<A>(availableTypeConditions.Where(tc => (conditionDictionary[tc] & mask) == conditionDictionary[tc]).ToFrozenSet(), ta.Value));

                        ClearArray(mask);

                        goto next;
                    }
                }
            }

        next: continue;
        }


        A? OnlyOneValue(int mask)
        {
            A? currentValue = null;

            for (int i = 0; i < numCells; i++)
            {
                if ((i & mask) == mask)
                {
                    var v = array![i];
                    if (v != null)
                    {
                        if (currentValue == null)
                            currentValue = v;
                        else if (!currentValue.Equals(v))
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

                    foreach (var item in GetMasksOf(numConditions - 1, availableTypeConditions, skip + 1))
                    {
                        yield return item | val;
                    }
                }
            }
        }
    }
}


public class StructureList<T> : ReadOnlyCollection<T>
{
    public StructureList(IList<T> list) : base(list) { }

    public override bool Equals(object? obj)
    {
        if (obj is StructureList<T> otherList)
        {
            // Check if both lists have the same count and contents in the same order
            return this.Count == otherList.Count && this.SequenceEqual(otherList);
        }
        return false;
    }

    public override int GetHashCode()
    {
        // Aggregate the hash codes of all elements in the list
        unchecked
        {
            int hash = 17;
            foreach (var item in this)
            {
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
}
