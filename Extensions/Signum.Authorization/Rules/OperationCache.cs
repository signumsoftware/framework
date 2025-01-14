using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Linq;
using System.Xml.Linq;

namespace Signum.Authorization.Rules;

class OperationCache : AuthCache<RuleOperationEntity, OperationAllowedRule, OperationTypeEmbedded, (OperationSymbol operation, Type type), WithConditions<OperationAllowed>, WithConditionsModel<OperationAllowed>>
{
    public OperationCache(SchemaBuilder sb) : base(sb, invalidateWithTypes: true)
    {
    }

    protected override Expression<Func<OperationTypeEmbedded, OperationTypeEmbedded, bool>> IsEqual => (o1, o2) => o1.Operation.Is(o2.Operation) && o1.Type.Is(o2.Type);

    protected override (OperationSymbol operation, Type type) ToKey(OperationTypeEmbedded resource) => (operation: resource.Operation, type: resource.Type.ToType());

    protected override OperationTypeEmbedded ToEntity((OperationSymbol operation, Type type) key) => new OperationTypeEmbedded { Operation = key.operation, Type = key.type.ToTypeEntity() };

    protected override WithConditions<OperationAllowed> GetRuleAllowed(RuleOperationEntity rule) => new WithConditions<OperationAllowed>(rule.Fallback, rule.ConditionRules.Select(c => new ConditionRule<OperationAllowed>(c.Conditions.ToFrozenSet(), c.Allowed)).ToReadOnly());

    protected override RuleOperationEntity SetRuleAllowed(RuleOperationEntity rule, WithConditions<OperationAllowed> allowed)
    {
        rule.Fallback = allowed.Fallback;
        var oldConditions = rule.ConditionRules.Select(a => new ConditionRule<OperationAllowed>(a.Conditions.ToFrozenSet(), a.Allowed)).ToReadOnly();
        if (!oldConditions.SequenceEqual(allowed.ConditionRules))
            rule.ConditionRules = allowed.ConditionRules.Select(a => new RuleOperationConditionEntity
            {
                Allowed = a.Allowed,
                Conditions = a.TypeConditions.ToMList()
            }).ToMList();
        return rule;
    }

    protected override WithConditions<OperationAllowed> ToAllowed(WithConditionsModel<OperationAllowed> allowedModel) => allowedModel.ToImmutable();
    protected override WithConditionsModel<OperationAllowed> ToAllowedModel(WithConditions<OperationAllowed> allowed) => allowed.ToModel();

    static ConcurrentDictionary<(WithConditions<OperationAllowed> allowed, WithConditions<TypeAllowed> taac), WithConditions<OperationAllowed>> coerceCache =
    new ConcurrentDictionary<(WithConditions<OperationAllowed> allowed, WithConditions<TypeAllowed> taac), WithConditions<OperationAllowed>>();

    public override WithConditions<OperationAllowed> CoerceValue(Lite<RoleEntity> role, (OperationSymbol operation, Type type) key, WithConditions<OperationAllowed> allowed, bool manual = false)
    {
        if (!TypeLogic.TypeToEntity.ContainsKey(key.type))
            return WithConditions<OperationAllowed>.Simple(OperationAllowed.Allow);

        var taac = manual ?
            TypeAuthLogic.Manual.GetAllowed(role, key.type) :
            TypeAuthLogic.GetAllowed(role, key.type);

        return coerceCache.GetOrAdd((allowed, taac), p =>
        {
            var ptaac = p.taac.ToOperationAllowed();

            var adjusted = AdjustShape(values: p.allowed, shape: ptaac);

            var coerced = CoerceSimilar(adjusted, ptaac);

            return coerced;
        });
    }

    public static WithConditions<OperationAllowed> CoerceSimilar(WithConditions<OperationAllowed> value, WithConditions<OperationAllowed> maxValue)
    {
        if (AlreadyCoerced(value, maxValue))
            return value;

        OperationAllowed Min(OperationAllowed a, OperationAllowed b) => a < b ? a : b;

        return new WithConditions<OperationAllowed>(Min(value.Fallback, maxValue.Fallback),
            value.ConditionRules.Select((c, i) => new ConditionRule<OperationAllowed>(c.TypeConditions, Min(c.Allowed, maxValue.ConditionRules[i].Allowed))).ToReadOnly()
            ).Intern();
    }

    private static bool AlreadyCoerced(WithConditions<OperationAllowed> value, WithConditions<OperationAllowed> maxValue)
    {
        if (value.Fallback > maxValue.Fallback)
            return false;

        for (int i = 0; i < value.ConditionRules.Count; i++)
        {
            if (value.ConditionRules[i].Allowed > maxValue.ConditionRules[i].Allowed)
                return false;
        }

        return true;
    }

    static ConcurrentDictionary<(WithConditions<OperationAllowed> shape, WithConditions<OperationAllowed> values), WithConditions<OperationAllowed>> cacheAdjustShape = new();
    WithConditions<OperationAllowed> AdjustShape(WithConditions<OperationAllowed> values, WithConditions<OperationAllowed> shape)
    {
        if (shape.ConditionRules.Count == values.ConditionRules.Count &&
            shape.ConditionRules.Select(a => a.TypeConditions).SequenceEqual(values.ConditionRules.Select(a => a.TypeConditions)))
            return values;

        return cacheAdjustShape.GetOrAdd((shape, values), p => new WithConditions<OperationAllowed>(p.values.Fallback,
            p.shape.ConditionRules.Select(cr =>
            {
                var rule = values.ConditionRules.LastOrDefault(cr2 => cr2.TypeConditions.All(c => cr.TypeConditions.Contains(c)));
                return new ConditionRule<OperationAllowed>(cr.TypeConditions,
                    rule.TypeConditions != null ? rule.Allowed : values.Fallback);
            }).ToReadOnly()).Intern());
    }

    //static ConcurrentDictionary<(WithConditions<TypeAllowed> shape, WithConditions<TypeAllowed> values), WithConditions<TypeAllowed>> cacheAdjustShapeType = new();
    //WithConditions<TypeAllowed> AdjustShapeType(WithConditions<TypeAllowed> shape, WithConditions<TypeAllowed> values)
    //{
    //    if (shape.ConditionRules.Count == values.ConditionRules.Count &&
    //        shape.ConditionRules.Select(a => a.TypeConditions).SequenceEqual(values.ConditionRules.Select(a => a.TypeConditions)))
    //        return values;

    //    return cacheAdjustShapeType.GetOrAdd((shape, values), p => new WithConditions<TypeAllowed>(
    //        p.values.Fallback,
    //        p.shape.ConditionRules.Select(cr =>
    //        {
    //            var rule = values.ConditionRules.LastOrDefault(cr2 => cr2.TypeConditions.All(c => cr.TypeConditions.Contains(c)));
    //            return new ConditionRule<TypeAllowed>(cr.TypeConditions,
    //                rule.TypeConditions == null ? cr.Allowed : values.Fallback);
    //        }).ToReadOnly()).Intern());
    //}

    static ConcurrentDictionary<(WithConditions<OperationAllowed> taac, OperationAllowed pa), WithConditions<OperationAllowed>> cacheWithShape = new();
    WithConditions<OperationAllowed> WithShape(WithConditions<OperationAllowed> taac, OperationAllowed pa)
    {
        return cacheWithShape.GetOrAdd((taac, pa), p => new WithConditions<OperationAllowed>(pa, p.taac.ConditionRules.Select(cr => new ConditionRule<OperationAllowed>(cr.TypeConditions, pa)).ToReadOnly()).Intern());
    }

    protected override WithConditions<OperationAllowed> Merge((OperationSymbol operation, Type type) key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, WithConditions<OperationAllowed>>> baseValues)
    {
        var merge = AuthLogic.GetMergeStrategy(role);

        var tac = GetDefaultFromType(key, role);

        var baseAdjusted = baseValues.Select(a => AdjustShape(values: a.Value, shape: tac)).ToList();

        Func<IEnumerable<OperationAllowed>, OperationAllowed> collapse = merge == MergeStrategy.Union ? MaxOperationAllowed : MinOperationAllowed ;

        var best =
            baseAdjusted.Count == 0 ? WithShape(tac, merge == MergeStrategy.Union ? OperationAllowed.None : OperationAllowed.Allow) :
            baseAdjusted.Count == 1 ? baseAdjusted.SingleEx() :
            new WithConditions<OperationAllowed>(collapse(baseAdjusted.Select(a => a.Fallback)),
                tac.ConditionRules.Select((a, i) => new ConditionRule<OperationAllowed>(a.TypeConditions, collapse(baseAdjusted.Select(b => b.ConditionRules[i].Allowed)))).ToReadOnly());

        if (!PermissionAuthLogic.IsAuthorized(BasicPermission.AutomaticUpgradeOfOperations, role))
            return best;


        var maxUp = OperationAuthLogic.MaxAutomaticUpgrade.TryGetS(key.operation);
        OperationAllowed AutomaticUpgrade(OperationAllowed mergedValue, IEnumerable<(OperationAllowed baseValue, OperationAllowed defaultBaseValue)> bases, OperationAllowed defaultValue)
        {
            if (bases.Where(a => a.baseValue == mergedValue).All(a => mergedValue == a.defaultBaseValue))
            {
                var upgrade = maxUp.HasValue && maxUp <= defaultValue ? maxUp.Value : defaultValue;
                return upgrade;
            }

            return mergedValue;
        }

        var baseDefaults = baseValues.Select(a => AdjustShape(values: GetDefaultFromType(key, a.Key), shape: tac)).ToList();
        var result = new WithConditions<OperationAllowed>(AutomaticUpgrade(best.Fallback, baseAdjusted.Zip(baseDefaults, (b, d) => (b.Fallback, d.Fallback)), tac.Fallback),
            tac.ConditionRules.Select((cr, i) => new ConditionRule<OperationAllowed>(cr.TypeConditions,
             AutomaticUpgrade(best.ConditionRules[i].Allowed, baseAdjusted.Zip(baseDefaults, (b, d) => (b.ConditionRules[i].Allowed, d.ConditionRules[i].Allowed)), cr.Allowed)
            )).ToReadOnly());

        return result.Intern();
    }

    static OperationAllowed MaxOperationAllowed(IEnumerable<OperationAllowed> baseValues)
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

    static OperationAllowed MinOperationAllowed(IEnumerable<OperationAllowed> baseValues)
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

    protected override Func<(OperationSymbol operation, Type type), WithConditions<OperationAllowed>> GetDefaultValue(Lite<RoleEntity> role)
    {
        return key =>
        {
            var typeAllowed = TypeAuthLogic.GetAllowed(role, key.type).ToOperationAllowed();

            if (AuthLogic.GetDefaultAllowed(role))
                return typeAllowed;

            if (!PermissionAuthLogic.IsAuthorized(BasicPermission.AutomaticUpgradeOfOperations, role))
                return CoerceSimple(typeAllowed, OperationAllowed.None);

            var maxUp = OperationAuthLogic.MaxAutomaticUpgrade.TryGetS(key.operation);
            if (maxUp == null)
                return typeAllowed;

            return CoerceSimple(typeAllowed, maxUp.Value);
        };
    }

    WithConditions<OperationAllowed> GetDefaultFromType((OperationSymbol operation, Type type) key, Lite<RoleEntity> role)
    {
        return TypeAuthLogic.GetAllowed(role, key.type).ToOperationAllowed();
    }

    public static WithConditions<OperationAllowed> CoerceSimple(WithConditions<OperationAllowed> value, OperationAllowed maxValue)
    {
        if (value.Fallback <= maxValue && value.ConditionRules.All(a => a.Allowed <= maxValue))
            return value;

        OperationAllowed Min(OperationAllowed a) => a < maxValue ? a : maxValue;

        return new WithConditions<OperationAllowed>(Min(value.Fallback),
            value.ConditionRules.Select(c => new ConditionRule<OperationAllowed>(c.TypeConditions, Min(c.Allowed))).ToReadOnly()
            ).Intern();
    }

    public override XElement ExportXml(bool exportAll)
    {
        return ExportXmlInternal("Operations", "Operation",
            resourceToString: s => s.operation.Key + "/" + s.type?.ToTypeEntity().CleanName,
            allowedToXml: paac => [
                new XAttribute("Allowed", paac.Fallback.ToString()),
                paac.ConditionRules.Select(c => new XElement("Condition",
                   new XAttribute("Name", c.TypeConditions.ToString(", ")),
                   new XAttribute("Allowed", c.Allowed.ToString())))            
                ],
            allKeys: exportAll ? OperationAuthLogic.AllOperationTypes() : null);
    }

    internal static readonly string typeReplacementKey = "AuthRules:" + typeof(TypeEntity).Name;
    internal static readonly string typeConditionReplacementKey = "AuthRules:" + typeof(TypeConditionSymbol).Name;

    internal static readonly string operationReplacementKey = "AuthRules:" + typeof(OperationSymbol).Name;

    public override SqlPreCommand? ImportXml(XElement root, Dictionary<string, Lite<RoleEntity>> roles, Replacements replacements)
    {
        var allResources = root.Element("Operations")!.Elements("Role").SelectMany(r => r.Elements("Operation")).Select(p => p.Attribute("Resource")!.Value).ToHashSet();

        replacements.AskForReplacements(
          allResources.Select(a => a.TryBefore("/") ?? a).ToHashSet(),
          SymbolLogic<OperationSymbol>.AllUniqueKeys(),
          operationReplacementKey);

        string typeReplacementKey = "AuthRules:" + typeof(OperationSymbol).Name;
        replacements.AskForReplacements(
           allResources.Select(a => a.After("/")).ToHashSet(),
           TypeLogic.NameToType.Keys.ToHashSet(),
           TypeCache.typeReplacementKey);

        return ImportXmlInternal(root, "Operations", "Operation", roles, 
            toResource: s => {
                var operation = SymbolLogic<OperationSymbol>.TryToSymbol(replacements.Apply(operationReplacementKey, s.Before("/")));
                var type = TypeLogic.TryGetType(replacements.Apply(TypeCache.typeReplacementKey, s.After("/")));

                if (operation == null || type == null || !OperationLogic.IsDefined(type, operation))
                    return null;

                return new OperationTypeEmbedded { Operation = operation, Type = type.ToTypeEntity() };
            },
            parseAllowed: e =>
            {
                return new WithConditions<OperationAllowed>(fallback: e.Attribute("Allowed")!.Value.ToEnum<OperationAllowed>(),
                    conditionRules: e.Elements("Condition").Select(xc => new ConditionRule<OperationAllowed>(
                        typeConditions: xc.Attribute("Name")!.Value.SplitNoEmpty(",").Select(s => SymbolLogic<TypeConditionSymbol>.TryToSymbol(replacements.Apply(typeConditionReplacementKey, s.Trim()))).NotNull().ToFrozenSet(),
                        allowed: xc.Attribute("Allowed")!.Value.ToEnum<OperationAllowed>()))
                    .ToReadOnly());
            });
    }

    protected override string AllowedComment(WithConditions<OperationAllowed> allowed)
    {
        if (allowed.ConditionRules.Count == 0)
            return allowed.Fallback.ToString();

        return $"{allowed.Fallback} + {allowed.ConditionRules.Count} conditions";
    }
}
