using System.Xml.Linq;

namespace Signum.Authorization.Rules;

public class QueryCache : AuthCache<RuleQueryEntity, QueryAllowedRule, QueryEntity, object, QueryAllowed, QueryAllowed>
{
    public QueryCache(SchemaBuilder sb): base(sb, invalidateWithTypes: true)
    {
    }

    protected override Expression<Func<QueryEntity, QueryEntity, bool>> IsEqual => (q1, q2) => q1.Is(q2);

    protected override object ToKey(QueryEntity resource) => QueryLogic.ToQueryName(resource.Key);
    protected override QueryEntity ToEntity(object key) => QueryLogic.GetQueryEntity(key);

    protected override QueryAllowed GetRuleAllowed(RuleQueryEntity rule) => rule.Allowed;

    protected override RuleQueryEntity SetRuleAllowed(RuleQueryEntity rule, QueryAllowed allowed) 
    {
        rule.Allowed = allowed;
        return rule;
    }

    protected override QueryAllowed ToAllowed(QueryAllowed allowedModel) => allowedModel;
    protected override QueryAllowed ToAllowedModel(QueryAllowed allowed) => allowed;

    protected override QueryAllowed Merge(object key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, QueryAllowed>> baseValues)
    {
        QueryAllowed best = AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union ?
           Max(baseValues.Select(a => a.Value)) :
           Min(baseValues.Select(a => a.Value));

        var maxUp = QueryAuthLogic.MaxAutomaticUpgrade.TryGetS(key);

        if (maxUp.HasValue && maxUp <= best)
            return best;

        if (!PermissionAuthLogic.IsAuthorized(BasicPermission.AutomaticUpgradeOfQueries, role))
            return best;

        if (baseValues.Where(a => a.Value.Equals(best)).All(a => GetDefault(key, a.Key).Equals(a.Value)))
        {
            var def = GetDefault(key, role);

            return maxUp.HasValue && maxUp <= def ? maxUp.Value : def;
        }

        return best;
    }

    static QueryAllowed Max(IEnumerable<QueryAllowed> baseValues)
    {
        QueryAllowed result = QueryAllowed.None;

        foreach (var item in baseValues)
        {
            if (item > result)
                result = item;

            if (result == QueryAllowed.Allow)
                return result;
        }
        return result;
    }

    static QueryAllowed Min(IEnumerable<QueryAllowed> baseValues)
    {
        QueryAllowed result = QueryAllowed.Allow;

        foreach (var item in baseValues)
        {
            if (item < result)
                result = item;

            if (result == QueryAllowed.None)
                return result;
        }
        return result;
    }

    QueryAllowed GetDefault(object key, Lite<RoleEntity> role)
    {
        return QueryLogic.Queries.GetEntityImplementations(key).AllCanRead(t => TypeAuthLogic.GetAllowed(role, t)) ? QueryAllowed.Allow : QueryAllowed.None;
    }

    protected override Func<object, QueryAllowed> GetDefaultValue(Lite<RoleEntity> role)
    {
        return key =>
        {
            if (AuthLogic.GetDefaultAllowed(role))
                return QueryAllowed.Allow;

            if (!PermissionAuthLogic.IsAuthorized(BasicPermission.AutomaticUpgradeOfQueries, role))
                return QueryAllowed.None;

            var maxUp = QueryAuthLogic.MaxAutomaticUpgrade.TryGetS(key);

            var def = GetDefault(key, role);

            return maxUp.HasValue && maxUp <= def ? maxUp.Value : def;
        };
    }

    public override QueryAllowed CoerceValue(Lite<RoleEntity> role, object key, QueryAllowed allowed, bool manual = false)
    {
        if (QueryAuthLogic.AvoidCoerce.Contains(key))
            return allowed;

        if (allowed == QueryAllowed.None)
            return allowed;

        var implementations = QueryLogic.Queries.GetEntityImplementations(key);

        return manual ?
            implementations.AllCanRead(t => TypeAuthLogic.Manual.GetAllowed(role, t)) ? allowed : QueryAllowed.None :
            implementations.AllCanRead(t => TypeAuthLogic.GetAllowed(role, t)) ? allowed : QueryAllowed.None;
    }

    public override XElement ExportXml(bool exportAll)
    {
        return ExportXmlInternal("Queries", "Query", 
            resourceToString: QueryUtils.GetKey, 
            allowedToXml: qa => [new XAttribute("Allowed", qa.ToString())],
            allKeys: exportAll ? QueryLogic.QueryNames.Values.ToList() : null);
    }

    public override SqlPreCommand? ImportXml(XElement root, Dictionary<string, Lite<RoleEntity>> roles, Replacements replacements)
    {
        string replacementKey = "AuthRules:" + typeof(QueryEntity).Name;

        replacements.AskForReplacements(
            root.Element("Queries")!.Elements("Role").SelectMany(r => r.Elements("Query")).Select(p => p.Attribute("Resource")!.Value).ToHashSet(),
            QueryLogic.QueryNames.Keys.ToHashSet(),
            replacementKey);

        return ImportXmlInternal(root, "Queries", "Query", roles,
            toResource: s =>
            {
                var qn = QueryLogic.TryToQueryName(replacements.Apply(replacementKey, s));

                if (qn == null)
                    return null;

                return QueryLogic.GetQueryEntity(qn);
            },
            parseAllowed: e =>
            {
                var allowed = e.Attribute("Allowed")!.Value;
                if (Enum.TryParse<QueryAllowed>(allowed, out var result))
                    return result;

                var bResult = bool.Parse(allowed); //For backwards compatibilityS
                return bResult ? QueryAllowed.Allow : QueryAllowed.None;
            });
    }

   
}
