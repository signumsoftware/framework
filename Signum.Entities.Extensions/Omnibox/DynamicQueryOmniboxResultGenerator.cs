using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using System.Text.RegularExpressions;
using Signum.Utilities.Reflection;
using Signum.Utilities.DataStructures;
using Signum.Entities.UserAssets;
using Newtonsoft.Json;

namespace Signum.Entities.Omnibox
{
    public class DynamicQueryOmniboxResultGenerator :OmniboxResultGenerator<DynamicQueryOmniboxResult>
    {
        private static List<FilterSyntax> SyntaxSequence(Match m)
        {
            return m.Groups["filter"].Captures().Select(filter => new FilterSyntax
            {
                Index = filter.Index,
                TokenLength = m.Groups["token"].Captures().Single(filter.Contains).Length,
                Length = filter.Length,
                Completion = m.Groups["val"].Captures().Any(filter.Contains) ? FilterSyntaxCompletion.Complete :
                             m.Groups["op"].Captures().Any(filter.Contains) ? FilterSyntaxCompletion.Operation : FilterSyntaxCompletion.Token,
            }).ToList();
        }

        Regex regex = new Regex(@"^I(?<filter>(?<token>I(\.I)*)(\.|((?<op>=)(?<val>[ENSIG])?))?)*$", RegexOptions.ExplicitCapture);

        public override IEnumerable<DynamicQueryOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            Match m = regex.Match(tokenPattern);

            if (!m.Success)
                yield break;

            string pattern = tokens[0].Value;

            bool isPascalCase = OmniboxUtils.IsPascalCasePattern(pattern);

            List<FilterSyntax>? syntaxSequence = null;

            foreach (var match in OmniboxUtils.Matches(OmniboxParser.Manager.GetQueries(), OmniboxParser.Manager.AllowedQuery, pattern, isPascalCase).OrderBy(ma => ma.Distance))
            {
                var queryName = match.Value;

                if (syntaxSequence == null)
                    syntaxSequence = SyntaxSequence(m);

                if (syntaxSequence.Any())
                {
                    QueryDescription description = OmniboxParser.Manager.GetDescription(match.Value);

                    IEnumerable<IEnumerable<OmniboxFilterResult>> bruteFilters = syntaxSequence.Select(a => GetFilterQueries(rawQuery, description, a, tokens));

                    foreach (var list in bruteFilters.CartesianProduct())
                    {
                        yield return new DynamicQueryOmniboxResult
                        {
                            QueryName = match.Value,
                            QueryNameMatch = match,
                            Distance = match.Distance + list.Average(a => a.Distance),
                            Filters = list.ToList(),
                        };
                    }
                }
                else
                {
                    if (match.Text == pattern && tokens.Count == 1 && tokens[0].Next(rawQuery) == ' ')
                    {
                        QueryDescription description = OmniboxParser.Manager.GetDescription(match.Value);

                        foreach (var qt in QueryUtils.SubTokens(null, description, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement))
                        {
                            yield return new DynamicQueryOmniboxResult
                            {
                                QueryName = match.Value,
                                QueryNameMatch = match,
                                Distance = match.Distance,
                                Filters = new List<OmniboxFilterResult> { new OmniboxFilterResult(0, null, qt, null) },
                            };
                        }
                    }
                    else
                    {
                        yield return new DynamicQueryOmniboxResult
                        {
                            QueryName = match.Value,
                            QueryNameMatch = match,
                            Distance = match.Distance,
                            Filters = new List<OmniboxFilterResult>()
                        };
                    }
                }
            }

        }

        protected IEnumerable<OmniboxFilterResult> GetFilterQueries(string rawQuery, QueryDescription queryDescription, FilterSyntax syntax, List<OmniboxToken> tokens)
        {
            List<OmniboxFilterResult> result = new List<OmniboxFilterResult>();

            int operatorIndex = syntax.Index + syntax.TokenLength;

            List<(QueryToken token, ImmutableStack<OmniboxMatch> stack)> ambiguousTokens = GetAmbiguousTokens(null, ImmutableStack<OmniboxMatch>.Empty,
                queryDescription, tokens, syntax.Index, operatorIndex).ToList();

            foreach ((QueryToken token, ImmutableStack<OmniboxMatch> stack) pair in ambiguousTokens)
            {
                var distance = pair.stack.Sum(a => a.Distance);
                var tokenMatches = pair.stack.Reverse().ToArray();
                var token = pair.token;

                if (syntax.Completion == FilterSyntaxCompletion.Token)
                {
                    if (tokens[operatorIndex - 1].Next(rawQuery) == '.' && pair.stack.All(a => ((QueryToken)a.Value).ToString().ToOmniboxPascal() == a.Text))
                    {
                        foreach (var qt in QueryUtils.SubTokens(pair.token, queryDescription, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement))
                        {
                            result.Add(new OmniboxFilterResult(distance, syntax, qt, tokenMatches));
                        }
                    }
                    else
                    {
                        result.Add(new OmniboxFilterResult(distance, syntax, token, tokenMatches));
                    }
                }
                else
                {
                    string? canFilter = QueryUtils.CanFilter(pair.token);

                    if (canFilter.HasText())
                    {
                        result.Add(new OmniboxFilterResult(distance, syntax, token, tokenMatches)
                        {
                            CanFilter = canFilter,
                        });
                    }
                    else
                    {
                        FilterOperation operation = FilterValueConverter.ParseOperation(tokens[operatorIndex].Value);

                        if (syntax.Completion == FilterSyntaxCompletion.Operation)
                        {
                            var suggested = SugestedValues(pair.token);

                            if (suggested == null)
                            {
                                result.Add(new OmniboxFilterResult(distance, syntax, token, tokenMatches)
                                {
                                    Operation = operation,
                                });
                            }
                            else
                            {
                                foreach (var item in suggested)
                                {
                                    result.Add(new OmniboxFilterResult(distance, syntax, token, tokenMatches)
                                    {
                                        Operation = operation,
                                        Value = item.Value
                                    });
                                }
                            }
                        }
                        else
                        {
                            var values = GetValues(pair.token, tokens[operatorIndex + 1]);

                            foreach (var value in values)
                            {
                                result.Add(new OmniboxFilterResult(distance, syntax, token, tokenMatches)
                                {
                                    Operation = operation,
                                    Value = value.Value,
                                    ValueMatch = value.Match,
                                });
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static readonly string UnknownValue = "??UNKNOWN??";

        public struct ValueTuple
        {
            public object? Value;
            public OmniboxMatch? Match;
        }

        protected virtual ValueTuple[]? SugestedValues(QueryToken queryToken)
        {
            var ft = QueryUtils.GetFilterType(queryToken.Type);
            switch (ft)
            {
                case FilterType.Integer:
                case FilterType.Decimal: return new[] { new ValueTuple { Value = Activator.CreateInstance(queryToken.Type.UnNullify()), Match = null } };
                case FilterType.String: return new[] { new ValueTuple { Value = "", Match = null } };
                case FilterType.DateTime: return new[] { new ValueTuple { Value = DateTime.Today, Match = null } };
                case FilterType.Lite:
                case FilterType.Embedded: break;
                case FilterType.Boolean: return new[] { new ValueTuple { Value = true, Match = null }, new ValueTuple { Value = false, Match = null } };
                case FilterType.Enum: return EnumEntity.GetValues(queryToken.Type.UnNullify()).Select(e => new ValueTuple { Value = e, Match = null }).ToArray();
                case FilterType.Guid: break;
            }

            return null;
        }

        public int AutoCompleteLimit = 5;

        protected virtual ValueTuple[] GetValues(QueryToken queryToken, OmniboxToken omniboxToken)
        {
            if (omniboxToken.IsNull())
                return new[] { new ValueTuple { Value = null, Match = null } };

            var ft = QueryUtils.GetFilterType(queryToken.Type);
            switch (ft)
            {
                case FilterType.Integer:
                case FilterType.Decimal:
                    if (omniboxToken.Type == OmniboxTokenType.Number)
                    {
                        if (ReflectionTools.TryParse(omniboxToken.Value, queryToken.Type, out object? result))
                            return new[] { new ValueTuple { Value = result, Match = null } };
                    }
                    break;
                case FilterType.String:
                    if (omniboxToken.Type == OmniboxTokenType.String)
                        return new[] { new ValueTuple { Value = OmniboxUtils.CleanCommas(omniboxToken.Value), Match = null } };
                    break;
                case FilterType.DateTime:
                    if (omniboxToken.Type == OmniboxTokenType.String)
                    {
                        var str = OmniboxUtils.CleanCommas(omniboxToken.Value);
                        if (ReflectionTools.TryParse(str, queryToken.Type, out object? result))
                            return new[] { new ValueTuple { Value = result, Match = null } };
                    }
                    break;
                case FilterType.Lite:
                    if (omniboxToken.Type == OmniboxTokenType.String)
                    {
                        var patten = OmniboxUtils.CleanCommas(omniboxToken.Value);

                        var result = OmniboxParser.Manager.Autocomplete(queryToken.GetImplementations()!.Value, patten, AutoCompleteLimit);

                        return result.Select(lite => new ValueTuple { Value = lite, Match = OmniboxUtils.Contains(lite, lite.ToString()!, patten) }).ToArray();
                    }
                    else if (omniboxToken.Type == OmniboxTokenType.Entity)
                    {
                        var error = Lite.TryParseLite(omniboxToken.Value, out Lite<Entity>? lite);
                        if (string.IsNullOrEmpty(error))
                            return new []{new ValueTuple { Value = lite }};
                    }
                    else if (omniboxToken.Type == OmniboxTokenType.Number)
                    {
                        var imp = queryToken.GetImplementations()!.Value;

                        if (!imp.IsByAll)
                        {
                            return imp.Types.Select(t => CreateLite(t, omniboxToken.Value))
                                .NotNull().Select(t => new ValueTuple { Value = t }).ToArray();
                        }
                    }break;
                case FilterType.Embedded:
                case FilterType.Boolean:
                    bool? boolean = ParseBool(omniboxToken.Value);
                    if (boolean.HasValue)
                        return new []{ new ValueTuple{ Value = boolean.Value} };
                    break;
                case FilterType.Enum:
                    if (omniboxToken.Type == OmniboxTokenType.String ||
                        omniboxToken.Type == OmniboxTokenType.Identifier)
                    {
                        string value = omniboxToken.Type == OmniboxTokenType.Identifier ? omniboxToken.Value : OmniboxUtils.CleanCommas(omniboxToken.Value);
                        bool isPascalValue = OmniboxUtils.IsPascalCasePattern(value);
                        Type enumType = queryToken.Type.UnNullify();
                        var dic = EnumEntity.GetValues(enumType).ToOmniboxPascalDictionary(a => a.NiceToString(), a => (object)a);

                        var result = OmniboxUtils.Matches(dic, e => true, value, isPascalValue)
                            .Select(m => new ValueTuple { Value = m.Value, Match = m })
                            .ToArray();

                        return result;
                    }
                    break;
                case FilterType.Guid:
                    if (omniboxToken.Type == OmniboxTokenType.Guid)
                    {
                        if (Guid.TryParse(omniboxToken.Value, out Guid result))
                            return new[] { new ValueTuple { Value = result, Match = null } };
                    }
                    else if (omniboxToken.Type == OmniboxTokenType.String)
                    {
                        var str = OmniboxUtils.CleanCommas(omniboxToken.Value);
                        if (Guid.TryParse(str, out Guid result))
                            return new[] { new ValueTuple { Value = result, Match = null } };
                    }
                    break;
                default:
                    break;
            }

            return new[] { new ValueTuple { Value = UnknownValue, Match = null } };
        }

        Lite<Entity>? CreateLite(Type type, string value)
        {
            if (PrimaryKey.TryParse(value, type, out PrimaryKey id))
                return Lite.Create(type, id, "{0} {1}".FormatWith(type.NiceName(), id));

            return null;
        }

        bool? ParseBool(string val)
        {
            val = val.ToLower().RemoveDiacritics();

            if (val == "true" || val == "t" || val == "yes" || val == "y" || val == OmniboxMessage.Yes.NiceToString())
                return true;

            if (val == "false" || val == "f" || val == "no" || val == "n" || val == OmniboxMessage.No.NiceToString())
                return false;

            return null;
        }

        protected virtual IEnumerable<(QueryToken token, ImmutableStack<OmniboxMatch> stack)> GetAmbiguousTokens(QueryToken? queryToken, ImmutableStack<OmniboxMatch> distancePack,
            QueryDescription queryDescription, List<OmniboxToken> omniboxTokens, int index, int operatorIndex)
        {
            OmniboxToken omniboxToken = omniboxTokens[index];

            bool isPascal = OmniboxUtils.IsPascalCasePattern(omniboxToken.Value);

            var dic = QueryUtils.SubTokens(queryToken, queryDescription, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement).ToOmniboxPascalDictionary(qt => qt.ToString(), qt => qt);
            var matches = OmniboxUtils.Matches(dic, qt => qt.IsAllowed() == null, omniboxToken.Value, isPascal);

            if (index == operatorIndex - 1)
            {
                foreach (var m in matches)
                {
                    var token = (QueryToken)m.Value;
                    yield return (token: token, stack: distancePack.Push(m));
                }
            }
            else
            {
                foreach (var m in matches)
                    foreach (var newPair in GetAmbiguousTokens((QueryToken)m.Value, distancePack.Push(m), queryDescription, omniboxTokens, index + 2, operatorIndex))
                        yield return newPair;
            }
        }

        public static string ToStringValue(object? p)
        {
            if (p == null)
                return "null";


            switch (QueryUtils.GetFilterType(p.GetType()))
            {
                case FilterType.Integer:
                case FilterType.Decimal: return p.ToString()!;

                case FilterType.String: return "\"" + p.ToString() + "\"";
                case FilterType.DateTime: return "'" + p.ToString() + "'";
                case FilterType.Lite: return ((Lite<Entity>)p).Key();
                case FilterType.Embedded: throw new InvalidOperationException("Impossible to translate not null Embedded entity to string");
                case FilterType.Boolean: return p.ToString()!;
                case FilterType.Enum: return ((Enum)p).NiceToString().SpacePascal();
                case FilterType.Guid: return "\"" + p.ToString() + "\"";
            }

            throw new InvalidOperationException("Unexpected value type {0}".FormatWith(p.GetType()));
        }

        public override List<HelpOmniboxResult> GetHelp()
        {
            var resultType = typeof(DynamicQueryOmniboxResult);

            var queryName = OmniboxMessage.Omnibox_Query.NiceToString();
            var field = OmniboxMessage.Omnibox_Field.NiceToString();
            var value = OmniboxMessage.Omnibox_Value.NiceToString();

            return new List<HelpOmniboxResult>
            {
                new HelpOmniboxResult { Text = "{0}".FormatWith(queryName), ReferencedType = resultType },
                new HelpOmniboxResult { Text = "{0} {1}='{2}'".FormatWith(queryName, field, value), ReferencedType = resultType },
                new HelpOmniboxResult { Text = "{0} {1}1='{2}1' {1}2='{2}2'".FormatWith(queryName, field, value), ReferencedType = resultType },
            };
        }
    }



    public class DynamicQueryOmniboxResult : OmniboxResult
    {
        [JsonConverter(typeof(QueryNameJsonConverter))]
        public object QueryName { get; set; }
        public OmniboxMatch QueryNameMatch { get; set; }
        public List<OmniboxFilterResult> Filters { get; set; }

        public override string ToString()
        {
            string queryName = QueryUtils.GetNiceName(QueryName).ToOmniboxPascal();

            string filters = Filters.ToString(" ");

            if (string.IsNullOrEmpty(filters))
                return queryName;
            else
                return queryName + " " + filters;
        }

    }

    public class OmniboxFilterResult
    {
        public OmniboxFilterResult(float distance, FilterSyntax? syntax, DynamicQuery.QueryToken queryToken, OmniboxMatch[]? omniboxMatch)
        {
            this.Distance = distance;
            this.Syntax = syntax;
            this.QueryToken = queryToken;
            this.QueryTokenMatches = omniboxMatch;
        }

        public float Distance { get; set; }
        public FilterSyntax? Syntax  {get; set;}

        [JsonConverter(typeof(QueryTokenJsonConverter))]
        public QueryToken QueryToken { get; set; }
        public string? QueryTokenOmniboxPascal => QueryToken?.Follow(a => a.Parent).Reverse().ToString(a => a.ToString().ToOmniboxPascal(), ".");

        public OmniboxMatch[]? QueryTokenMatches { get; set; }
        public FilterOperation? Operation { get; set; }
        public string? OperationToString => this.Operation == null ? null : FilterValueConverter.ToStringOperation(this.Operation.Value);


        public string? ValueToString => this.Value == null ? null : DynamicQueryOmniboxResultGenerator.ToStringValue(this.Value);
        public object? Value { get; set; }
        public OmniboxMatch? ValueMatch { get; set; }

        public string CanFilter { get; set; }

        public override string ToString()
        {
            string token = QueryToken.Follow(q => q.Parent).Reverse().Select(a => a.ToString().ToOmniboxPascal()).ToString(".");

            if (Syntax == null || Syntax.Completion == FilterSyntaxCompletion.Token || CanFilter.HasText())
                return token;

            string oper = FilterValueConverter.ToStringOperation(Operation!.Value);

            if ((Syntax.Completion == FilterSyntaxCompletion.Operation && Value == null) ||
                (Value as string == DynamicQueryOmniboxResultGenerator.UnknownValue))
                return token + oper;

            return token + oper + DynamicQueryOmniboxResultGenerator.ToStringValue(Value);
        }
    }

    public class FilterSyntax
    {
        public int Index;
        public int TokenLength;
        public int Length;
        public FilterSyntaxCompletion Completion;
    }

    public enum FilterSyntaxCompletion
    {
        Token,
        Operation,
        Complete,
    }

    //User 2
    //ped cus.per.add.cit=="London" fj>'2012'

    //FVL N="hola"


    public class QueryNameJsonConverter : JsonConverter
    {
        public static Func<object, string> GetQueryKey;
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override bool CanWrite => true;
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer.WriteValue(GetQueryKey(value!));
        }

        public override bool CanRead => false;
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class QueryTokenJsonConverter : JsonConverter
    {
        public static Func<QueryToken, object> GetQueryTokenTS;
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override bool CanWrite => true;
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, GetQueryTokenTS((QueryToken)value!));
        }

        public override bool CanRead => false;
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
