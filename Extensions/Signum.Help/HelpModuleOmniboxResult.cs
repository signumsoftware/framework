using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Signum.Omnibox;
using Signum.Authorization;
using Signum.Authorization.Rules;

namespace Signum.Help;

public class HelpModuleOmniboxResultGenerator : OmniboxResultGenerator<HelpModuleOmniboxResult>
{
    public Func<string> NiceName = () => OmniboxMessage.Omnibox_Help.NiceToString();

    static readonly Regex regex = new Regex("^I[IS]?$");

    public override IEnumerable<HelpModuleOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
    {
        if (!PermissionLogic.IsAuthorized(HelpPermissions.ViewHelp))
            yield break;

        if (tokens.Count == 0 || !regex.IsMatch(tokenPattern))
            yield break;

        string key = tokens[0].Value;

        var keyMatch = OmniboxUtils.Contains(NiceName(), NiceName(), key) ?? OmniboxUtils.Contains("help", "help", key);

        if (keyMatch == null)
            yield break;

        if (tokenPattern == "I")
        {
            yield return new HelpModuleOmniboxResult { Distance = keyMatch.Distance, KeywordMatch = keyMatch, SecondMatch = null };
            yield break;
        }

        if(tokens.Count != 2)
            yield break;

        if (tokens[1].Type == OmniboxTokenType.String)
        {
            yield return new HelpModuleOmniboxResult { Distance = keyMatch.Distance, KeywordMatch = keyMatch, SearchString = tokens[1].Value.Trim('\'', '"') };
            yield break;
        }

        string pattern = tokens[1].Value;

        bool isPascalCase = OmniboxUtils.IsPascalCasePattern(pattern);

        foreach (var match in OmniboxUtils.Matches(OmniboxParser.Manager.Types(), t => TypeAuthLogic.GetAllowed(t).MinUI() > TypeAllowedBasic.None, pattern, isPascalCase).OrderBy(ma => ma.Distance))
        {
            var type = (Type)match.Value;
            yield return new HelpModuleOmniboxResult { Distance = keyMatch.Distance + match.Distance, KeywordMatch = keyMatch, Type = type, SecondMatch = match };
        }
    }

    public override List<HelpOmniboxResult> GetHelp()
    {
        var resultType = typeof(HelpModuleOmniboxResult);
        return new List<HelpOmniboxResult>
        {
            new HelpOmniboxResult
            {
                Text =  NiceName() + " " + typeof(TypeEntity).NiceName(),
                ReferencedType = resultType
            },
            new HelpOmniboxResult
            {
                Text =  NiceName() + " '" + HelpMessage.SearchText.NiceToString()  + "'",
                ReferencedType = resultType
            },
        };
    }
}

public class HelpModuleOmniboxResult : OmniboxResult
{
    public OmniboxMatch KeywordMatch { get; set; }

    [JsonIgnore]
    public Type? Type { get; set; }
    public string? TypeName { get { return this.Type == null ? null : QueryNameJsonConverter.GetQueryKey(this.Type); } }

    public string? SearchString { get; set; }
    public OmniboxMatch? SecondMatch { get; set; }

    public override string ToString()
    {
        if (Type == null && !SearchString.HasText())
            return KeywordMatch.Value.ToString() + " ";

        return "{0} {1}".FormatWith(KeywordMatch.Value,
            Type != null ? Type.NiceName().ToOmniboxPascal() :
            ("'" + SearchString + "'"));
    }
}

[AutoInit]
public static class HelpPermissions
{
    public static PermissionSymbol ViewHelp;
    public static PermissionSymbol ExportHelp;
}
