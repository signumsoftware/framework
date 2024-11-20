using System.Xml.Linq;

namespace Signum.Authorization.Rules;

class PermissionCache : AuthCache<RulePermissionEntity, PermissionAllowedRule, PermissionSymbol, PermissionSymbol, bool, bool>
{
    public PermissionCache(SchemaBuilder sb) : base(sb, invalidateWithTypes: false)
    {
    }

    protected override Expression<Func<PermissionSymbol, PermissionSymbol, bool>> IsEqual => (a, b) => a.Is(b);

    protected override PermissionSymbol ToKey(PermissionSymbol resource) => resource;
    protected override PermissionSymbol ToEntity(PermissionSymbol key) => key;

    protected override bool GetRuleAllowed(RulePermissionEntity rule) => rule.Allowed;
    protected override RulePermissionEntity SetRuleAllowed(RulePermissionEntity rule, bool allowed)
    {
        rule.Allowed = allowed;
        return rule;
    }

    protected override bool ToAllowedModel(bool allowed) => allowed;
    protected override bool ToAllowed(bool allowedModel) => allowedModel;

    protected override bool Merge(PermissionSymbol key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, bool>> baseValues)
    {
        if (AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union)
            return baseValues.Any(a => a.Value);
        else
            return baseValues.All(a => a.Value);
    }

    protected override Func<PermissionSymbol, bool> GetDefaultValue(Lite<RoleEntity> role)
    {
        return new ConstantFunction<PermissionSymbol, bool>(AuthLogic.GetDefaultAllowed(role)).GetValue;
    }

    public override XElement ExportXml(bool exportAll)
    {
        return ExportXmlInternal("Permissions", "Permission", a => a.Key, b => [new XAttribute("Allowed", b.ToString())],
              exportAll ? PermissionLogic.RegisteredPermission.ToList() : null);
    }

    public override SqlPreCommand? ImportXml(XElement root, Dictionary<string, Lite<RoleEntity>> roles, Replacements replacements)
    {
        string replacementKey = "AuthRules:" + typeof(PermissionSymbol).Name;

        replacements.AskForReplacements(
            root.Element("Permissions")!.Elements("Role").SelectMany(r => r.Elements("Permission")).Select(p => p.Attribute("Resource")!.Value).ToHashSet(),
            SymbolLogic<PermissionSymbol>.Symbols.Select(s => s.Key).ToHashSet(),
            replacementKey);

        return ImportXmlInternal(root, "Permissions", "Permission", roles,
            toResource: s => SymbolLogic<PermissionSymbol>.TryToSymbol(replacements.Apply(replacementKey, s)), 
            parseAllowed: e => bool.Parse(e.Attribute("Allowed")!.Value));
    }


}
