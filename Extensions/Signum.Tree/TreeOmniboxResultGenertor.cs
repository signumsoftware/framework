using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.Omnibox;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Signum.Tree;

public class TreeOmniboxResultGenerator : OmniboxResultGenerator<TreeOmniboxResult>
{
    Regex regex = new Regex(@"^II?$", RegexOptions.ExplicitCapture);
    public override IEnumerable<TreeOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
    {
        if (tokenPattern != "I")
            yield break;
        
        string pattern = tokens[0].Value;

        bool isPascalCase = OmniboxUtils.IsPascalCasePattern(pattern);

        foreach (var match in OmniboxUtils.Matches(OmniboxParser.Manager.Types(), t => typeof(TreeEntity).IsAssignableFrom(t) && QueryLogic.Queries.QueryAllowed(t, fullScreen: true), pattern, isPascalCase).OrderBy(ma => ma.Distance))
        {
            var type = match.Value;

            yield return new TreeOmniboxResult
            {
                Distance = match.Distance - 0.1f,
                Type = (Type)match.Value,
                TypeMatch = match
            };
        }

    }

    public override List<HelpOmniboxResult> GetHelp()
    {
        var resultType = typeof(TreeOmniboxResult);
        return new List<HelpOmniboxResult>
        {
            new HelpOmniboxResult
            {
                Text = TreeMessage.TreeType.NiceToString(),
                ReferencedType = resultType
            }
        };
    }
}

public class TreeTypeJsonConverter : JsonConverter<object>
{
    public override bool CanConvert(Type objectType)
    {
        return true;
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize<object?>(writer, value == null ? null : (object?)QueryNameJsonConverter.GetQueryKey(value));
    }

    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class TreeOmniboxResult : OmniboxResult
{
    [JsonConverter(typeof(TreeTypeJsonConverter))]
    public Type Type { get; set; }
    public OmniboxMatch TypeMatch { get; set; }

    public override string ToString()
    {
        return Type.NiceName().ToOmniboxPascal();
    }
}
