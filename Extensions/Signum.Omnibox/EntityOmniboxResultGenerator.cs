using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Signum.Omnibox;

public class EntityOmniboxResultGenenerator : OmniboxResultGenerator<EntityOmniboxResult>
{
    public int AutoCompleteLimit = 5;

    Regex regex = new Regex(@"^I((?<id>N)|(?<id>G)|(?<toStr>S))?$", RegexOptions.ExplicitCapture);

    public override IEnumerable<EntityOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
    {
        Match m = regex.Match(tokenPattern);

        if (!m.Success)
            yield break;

        string ident = tokens[0].Value;

        bool isPascalCase = OmniboxUtils.IsPascalCasePattern(ident);


        var matches = OmniboxUtils.Matches(OmniboxParser.Manager.Types(), filter: type => Schema.Current.IsAllowed(type, inUserInterface: true) == null, ident, isPascalCase);

        if (tokens.Count == 1)
        {
            yield break;
        }
        else if (tokens[1].Type == OmniboxTokenType.Number || tokens[1].Type == OmniboxTokenType.Guid)
        {
            foreach (var match in matches.OrderBy(ma => ma.Distance))
            {
                Type type = (Type)match.Value;

                if (PrimaryKey.TryParse(tokens[1].Value, type, out PrimaryKey id))
                {
                    Lite<Entity>? lite = Database.TryRetrieveLite(type, id);

                    yield return new EntityOmniboxResult
                    {
                        Type = (Type)match.Value,
                        TypeMatch = match,
                        Id = id,
                        Lite = lite,
                        Distance = match.Distance,
                    };
                }
            }
        }
        else if (tokens[1].Type == OmniboxTokenType.String)
        {
            string pattern = OmniboxUtils.CleanCommas(tokens[1].Value);

            foreach (var match in matches.OrderBy(ma => ma.Distance))
            {
                var autoComplete = OmniboxParser.Manager.Autocomplete((Type)match.Value, pattern, AutoCompleteLimit);

                if (autoComplete.Any())
                {
                    foreach (Lite<Entity> lite in autoComplete)
                    {
                        OmniboxMatch? distance = OmniboxUtils.Contains(lite, lite.ToString() ?? "", pattern);

                        if (distance != null)
                            yield return new EntityOmniboxResult
                            {
                                Type = (Type)match.Value,
                                TypeMatch = match,
                                ToStr = pattern,
                                Lite = lite,
                                Distance = match.Distance + distance.Distance,
                                ToStrMatch = distance,
                            };
                    }
                }
                else
                {
                    yield return new EntityOmniboxResult
                    {
                        Type = (Type)match.Value,
                        TypeMatch = match,
                        Distance = match.Distance + 100,
                        ToStr = pattern,
                    };
                }
            }

        }
    }

    public override List<HelpOmniboxResult> GetHelp()
    {
        var resultType = typeof(EntityOmniboxResult);
        var entityTypeName = OmniboxMessage.Omnibox_Type.NiceToString();

        return new List<HelpOmniboxResult>
        {
            new HelpOmniboxResult { Text = "{0} Id".FormatWith(entityTypeName), ReferencedType = resultType },
            new HelpOmniboxResult { Text = "{0} 'ToStr'".FormatWith(entityTypeName), ReferencedType = resultType }
        };
    }
}

public class PrimaryKeyJsonConverter : JsonConverter<object>
{
    public override bool CanConvert(Type objectType)
    {
        return true;
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize<object?>(writer, value == null ? null : (object?)((PrimaryKey)value).Object);
    }

    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class EntityOmniboxResult : OmniboxResult
{
    [JsonIgnore]
    public Type Type { get; set; }
    public OmniboxMatch TypeMatch { get; set; }

    [JsonConverter(typeof(PrimaryKeyJsonConverter))]
    public PrimaryKey? Id { get; set; }

    public string ToStr { get; set; }
    public OmniboxMatch ToStrMatch { get; set; }

    public Lite<Entity>? Lite { get; set; }

    public override string ToString()
    {
        if (Id.HasValue)
            return "{0} {1}".FormatWith(Type.NicePluralName().ToOmniboxPascal(), Id);

        if (ToStr != null)
            return "{0} \"{1}\"".FormatWith(Type.NicePluralName().ToOmniboxPascal(), ToStr);

        return Type.NicePluralName().ToOmniboxPascal();
    }
}
