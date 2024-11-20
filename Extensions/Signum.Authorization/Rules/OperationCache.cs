using System.Xml.Linq;

namespace Signum.Authorization.Rules;

class OperationCache : AuthCache<RuleOperationEntity, OperationAllowedRule, OperationTypeEmbedded, (OperationSymbol operation, Type type), OperationAllowed, OperationAllowed>
{
    public OperationCache(SchemaBuilder sb) : base(sb, invalidateWithTypes: true)
    {
    }

    protected override Expression<Func<OperationTypeEmbedded, OperationTypeEmbedded, bool>> IsEqual => (o1, o2) => o1.Operation.Is(o2.Operation) && o1.Type.Is(o2.Type);

    protected override (OperationSymbol operation, Type type) ToKey(OperationTypeEmbedded resource) => (operation: resource.Operation, type: resource.Type.ToType());

    protected override OperationTypeEmbedded ToEntity((OperationSymbol operation, Type type) key) => new OperationTypeEmbedded { Operation = key.operation, Type = key.type.ToTypeEntity() };

    protected override OperationAllowed GetRuleAllowed(RuleOperationEntity rule) => rule.Allowed;

    protected override RuleOperationEntity SetRuleAllowed(RuleOperationEntity rule, OperationAllowed allowed)
    {
        rule.Allowed = allowed;
        return rule;
    }

    protected override OperationAllowed ToAllowed(OperationAllowed allowedModel) => allowedModel;
    protected override OperationAllowed ToAllowedModel(OperationAllowed allowed) => allowed;

    protected override OperationAllowed Merge((OperationSymbol operation, Type type) key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, OperationAllowed>> baseValues)
    {
        OperationAllowed best = AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union ?
           Max(baseValues.Select(a => a.Value)) :
           Min(baseValues.Select(a => a.Value));

        if (!PermissionAuthLogic.IsAuthorized(BasicPermission.AutomaticUpgradeOfOperations, role))
            return best;

        var maxUp = OperationAuthLogic.MaxAutomaticUpgrade.TryGetS(key.operation);

        if (maxUp.HasValue && maxUp <= best)
            return best;

        if (baseValues.Where(a => a.Value.Equals(best)).All(a => GetDefault(key, a.Key).Equals(a.Value)))
        {
            var def = GetDefault(key, role);

            return maxUp.HasValue && maxUp <= def ? maxUp.Value : def;
        }

        return best;
    }

    static OperationAllowed GetDefault((OperationSymbol operation, Type type) operationType, Lite<RoleEntity> role)
    {
        return OperationAuthLogic.InferredOperationAllowed(operationType, t => TypeAuthLogic.GetAllowed(role, t));
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


    protected override Func<(OperationSymbol operation, Type type), OperationAllowed> GetDefaultValue(Lite<RoleEntity> role)
    {
        return key =>
        {
            if (AuthLogic.GetDefaultAllowed(role))
                return OperationAllowed.Allow;

            if (!PermissionAuthLogic.IsAuthorized(BasicPermission.AutomaticUpgradeOfOperations, role))
                return OperationAllowed.None;

            var maxUp = OperationAuthLogic.MaxAutomaticUpgrade.TryGetS(key.operation);

            var def = GetDefault(key, role);

            return maxUp.HasValue && maxUp <= def ? maxUp.Value : def;
        };
    }

    public override OperationAllowed CoerceValue(Lite<RoleEntity> role, (OperationSymbol operation, Type type) key, OperationAllowed allowed, bool manual = false)
    {
        var required = manual ? 
            OperationAuthLogic.InferredOperationAllowed(key, t => TypeAuthLogic.Manual.GetAllowed(role, t)): 
            OperationAuthLogic.InferredOperationAllowed(key, t => TypeAuthLogic.GetAllowed(role, t));

        return allowed < required ? allowed : required;
    }

    public override XElement ExportXml(bool exportAll)
    {
        return ExportXmlInternal("Operations", "Operation",
            resourceToString: s => s.operation.Key + "/" + s.type?.ToTypeEntity().CleanName,
            allowedToXml: b => [new XAttribute("Allowed", b.ToString())],
            allKeys: exportAll ? OperationAuthLogic.AllOperationTypes() : null);
    }

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
            parseAllowed: elem =>
            {
                var allowed = elem.Attribute("Allowed")!.Value;

                return EnumExtensions.ToEnum<OperationAllowed>(allowed);
            });
    }
}
