using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using System.Text.RegularExpressions;
using Signum.Utilities.Reflection;
using Signum.Entities.Extensions.Properties;
using Signum.Entities.Reflection;
using Signum.Utilities.DataStructures;
using Signum.Entities.Basics;

namespace Signum.Entities.Omnibox
{
    public abstract class DynamicQueryOmniboxProvider :OmniboxProvider
    {
        static Dictionary<string, object> queries;

        public DynamicQueryOmniboxProvider(IEnumerable<object> queryNames)
        {
            queries = queryNames.ToDictionary(qn => qn is Type ? Reflector.CleanTypeName((Type)qn) : qn.ToString());
        }

        protected abstract bool Allowed(object queryName);
        protected abstract QueryDescription GetDescription(object queryName);
        protected abstract List<Lite> AutoComplete(Type cleanType, Implementations implementations, string subString);

        public override void AddResults(List<OmniboxResult> results, string rawQuery, List<OmniboxToken> tokens)
        {
            if(tokens.Count >= 1 && tokens[0].Type == OmniboxTokenType.Identifier)
            {
                string pattern = tokens[0].Value;

                bool isPascalCase = OmniboxUtils.IsPascalCasePattern(pattern);

                List<FilterSyntax> syntaxSequence = null;

                foreach (var match in OmniboxUtils.Matches(queries,QueryUtils.GetNiceName, pattern, isPascalCase))
                {
                    var queryName = match.Value;
                    if (Allowed(queryName))
                    {
                        if (syntaxSequence == null)
                            syntaxSequence = GetSyntaxSequence(tokens);

                        if (syntaxSequence.Any())
                        {
                            QueryDescription description = GetDescription(match.Value);

                            IEnumerable<IEnumerable<FilterQuery>> bruteFilters = syntaxSequence.Select(a => GetFilterQueries(rawQuery, description, a, tokens));

                            foreach (var list in bruteFilters.CartesianProduct())
                            {
                                results.Add(new DynamicQueryOmniboxResult
                                {
                                    QueryName = match.Value,
                                    QueryNameMatch = match,
                                    Distance = match.Distance + list.Average(a => a.Distance),
                                    Filters = list.ToList(),
                                });
                            }
                        }
                        else
                        {
                            if (match.Text == pattern && tokens.Count == 1 && tokens[0].Next(rawQuery) == ' ')
                            {
                                QueryDescription description = GetDescription(match.Value);

                                foreach (var qt in QueryUtils.SubTokens(null, description.Columns))
                                {
                                    results.Add(new DynamicQueryOmniboxResult
                                    {
                                        QueryName = match.Value,
                                        QueryNameMatch = match,
                                        Distance = match.Distance,
                                        Filters = new List<FilterQuery> { new FilterQuery(0, null, qt, null) },
                                    });
                                }
                            }
                            else
                            {
                                results.Add(new DynamicQueryOmniboxResult
                                {
                                    QueryName = match.Value,
                                    QueryNameMatch = match,
                                    Distance = match.Distance,
                                    Filters = new List<FilterQuery>()
                                });
                            }
                        }
                    }
                }
            }
        }

        protected IEnumerable<FilterQuery> GetFilterQueries(string rawQuery, QueryDescription queryDescription, FilterSyntax syntax, List<OmniboxToken> tokens)
        {
            List<FilterQuery> result = new List<FilterQuery>();

            int operatorIndex = syntax.Index + syntax.TokenLength;

            List<Tuple<QueryToken, ImmutableStack<OmniboxMatch>>> ambiguousTokens = GetAmbiguousTokens(null, ImmutableStack<OmniboxMatch>.Empty, 
                queryDescription, tokens, syntax.Index, operatorIndex).ToList();

            foreach (Tuple<QueryToken, ImmutableStack<OmniboxMatch>> pair in ambiguousTokens)
            {
                var distance = pair.Item2.Sum(a => a.Distance);
                var tokenMatches = pair.Item2.Reverse().ToArray();
                var token = pair.Item1;

                if (syntax.Completion == FilterSyntaxCompletion.Token)
                {
                    if (tokens[operatorIndex - 1].Next(rawQuery) == '.' && pair.Item2.All(a => ((QueryToken)a.Value).Key == a.Text))
                    {
                        foreach (var qt in QueryUtils.SubTokens(pair.Item1, queryDescription.Columns))
                        {
                            result.Add(new FilterQuery(distance, syntax, qt, tokenMatches));
                        }
                    }
                    else
                    {
                        result.Add(new FilterQuery(distance, syntax, token, tokenMatches));
                    }
                }
                else
                {
                    string canFilter = QueryUtils.CanFilter(pair.Item1);

                    if (canFilter.HasText())
                    {
                        result.Add(new FilterQuery(distance, syntax, token, tokenMatches)
                        {
                            CanFilter = canFilter,
                        });
                    }
                    else
                    {

                        FilterOperation operation = ParseOperation(tokens[operatorIndex]);

                        if (syntax.Completion == FilterSyntaxCompletion.Operation)
                        {
                            var suggested = SugestedValues(pair.Item1);

                            if (suggested == null)
                            {
                                result.Add(new FilterQuery(distance, syntax, token, tokenMatches)
                                {
                                    Operation = operation,
                                });
                            }
                            else
                            {
                                foreach (var item in suggested)
                                {
                                    result.Add(new FilterQuery(distance, syntax, token, tokenMatches)
                                    {
                                        Operation = operation,
                                        Value = item.Value
                                    });
                                }
                            }
                        }
                        else
                        {
                            var values = GetValues(pair.Item1, tokens[operatorIndex + 1]);

                            foreach (var value in values)
                            {
                                result.Add(new FilterQuery(distance, syntax, token, tokenMatches)
                                {
                                    Operation = operation,
                                    Value = value.Value,
                                    ValuePack = value.Match,
                                });
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static readonly object UnknownValue = new object();

        public struct ValueTuple
        {
            public object Value;
            public OmniboxMatch Match;
        }

        protected virtual ValueTuple[] SugestedValues(QueryToken queryToken)
        {
            var ft = QueryUtils.GetFilterType(queryToken.Type);
            switch (ft)
            {
                case FilterType.Number:
                case FilterType.DecimalNumber: return new[] { new ValueTuple { Value = 0, Match = null } };
                case FilterType.String: return new[] { new ValueTuple { Value = "", Match = null } };
                case FilterType.DateTime: return new[] { new ValueTuple { Value = DateTime.Today, Match = null } };
                case FilterType.Lite:
                case FilterType.Embedded: break;
                case FilterType.Boolean: return new[] { new ValueTuple { Value = true, Match = null }, new ValueTuple { Value = false, Match = null } };
                case FilterType.Enum: return EnumProxy.GetValues(queryToken.Type.UnNullify()).Select(e => new ValueTuple { Value = e, Match = null }).ToArray();
                case FilterType.Guid: break;
            }

            return null;
        }

        protected virtual ValueTuple[] GetValues(QueryToken queryToken, OmniboxToken omniboxToken)
        {
            if (omniboxToken.IsNull())
                return new[] { new ValueTuple { Value = null, Match = null } };

            var ft = QueryUtils.GetFilterType(queryToken.Type);
            switch (ft)
            {
                case FilterType.Number:
                case FilterType.DecimalNumber:
                    if (omniboxToken.Type == OmniboxTokenType.Number)
                    {
                        object result;
                        if (ReflectionTools.TryParse(omniboxToken.Value, queryToken.Type, out result))
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
                        object result;
                        if (ReflectionTools.TryParse(str, queryToken.Type, out result))
                            return new[] { new ValueTuple { Value = result, Match = null } };
                    }
                    break;
                case FilterType.Lite:
                    if (omniboxToken.Type == OmniboxTokenType.String)
                    {
                        var patten = OmniboxUtils.CleanCommas(omniboxToken.Value);

                        var result = AutoComplete(queryToken.Type.CleanType(), queryToken.Implementations(), patten);

                        return result.Select(lite => new ValueTuple { Value = lite, Match = OmniboxUtils.Contains(lite, lite.ToString(), patten) }).ToArray();  
                    }
                    else if (omniboxToken.Type == OmniboxTokenType.Number)
                    {
                        int id;
                        if (int.TryParse(omniboxToken.Value, out id))
                        {
                            var imp = queryToken.Implementations();

                            if(imp == null)
                                return new []{ new ValueTuple { Value = CreateLite(queryToken.Type.CleanType(), id), Match = null}};

                            if (!imp.IsByAll)
                            {
                                return ((ImplementedByAttribute)imp).ImplementedTypes.Select(t=>
                                    new ValueTuple { Value =  CreateLite(t, id) }).ToArray();
                            }
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
                        var dic = EnumProxy.GetValues(queryToken.Type.UnNullify()).ToDictionary(a => a.ToString(), a => (object)a);

                        var result = OmniboxUtils.Matches(dic, e => ((Enum)e).NiceToString(), value, isPascalValue)
                            .Select(m => new ValueTuple { Value = m.Value, Match = m })
                            .ToArray();

                        return result;
                    }
                    break;
                case FilterType.Guid:
                    if (omniboxToken.Type == OmniboxTokenType.String)
                    {
                        var str = OmniboxUtils.CleanCommas(omniboxToken.Value);
                        Guid result;
                        if (Guid.TryParse(str, out result))
                            return new []{new ValueTuple{ Value = result, Match = null}};
                    }break;
                default:
                    break;
            }

            return new[] { new ValueTuple { Value = UnknownValue, Match = null } };
        }

        Lite CreateLite(Type type, int id)
        {
            return Lite.Create(type, id, type, "{0} {1}".Formato(type.NiceName(), id));
        }

        bool? ParseBool(string val)
        {
            val = val.ToLower().RemoveDiacritics();

            if (val == "true" || val == "t" || val == "yes" || val == "y" || val == Resources.Yes)
                return true;

            if (val == "false" || val == "f" || val == "no" || val == "n" || val == Resources.No)
                return false;

            return null;
        }

        public static FilterOperation ParseOperation(OmniboxToken omniboxToken)
        {
            switch (omniboxToken.Value)
            {
                case "=":
                case "==": return FilterOperation.EqualTo;
                case "<=": return FilterOperation.LessThanOrEqual;
                case ">=": return FilterOperation.GreaterThanOrEqual;
                case "<": return FilterOperation.LessThan;
                case ">": return FilterOperation.GreaterThan;
                case "^=": return FilterOperation.StartsWith;
                case "$=": return FilterOperation.EndsWith;
                case "*=": return FilterOperation.Contains;
                case "%=": return FilterOperation.Like;

                case "!=": return FilterOperation.DistinctTo;
                case "!^=": return FilterOperation.NotStartsWith;
                case "!$=": return FilterOperation.NotEndsWith;
                case "!*=": return FilterOperation.NotContains;
                case "!%=": return FilterOperation.NotLike;
            }

            throw new InvalidOperationException("Unexpected Filter {0}".Formato(omniboxToken.Value));
        }

        public static string ToStringOperation(FilterOperation operation)
        {
            switch (operation)
            {
                case FilterOperation.EqualTo: return "=";
                case FilterOperation.DistinctTo: return "!=";
                case FilterOperation.GreaterThan: return ">";
                case FilterOperation.GreaterThanOrEqual: return ">=";
                case FilterOperation.LessThan: return "<";
                case FilterOperation.LessThanOrEqual: return "<=";
                case FilterOperation.Contains: return "*=";
                case FilterOperation.StartsWith: return "^=";
                case FilterOperation.EndsWith: return "$=";
                case FilterOperation.Like: return "%=";
                case FilterOperation.NotContains: return "!*=";
                case FilterOperation.NotStartsWith: return "!^=";
                case FilterOperation.NotEndsWith: return "!$=";
                case FilterOperation.NotLike: return "!%=";
            }

            throw new InvalidOperationException("Unexpected Filter {0}".Formato(operation));
        }

        protected virtual IEnumerable<Tuple<QueryToken, ImmutableStack<OmniboxMatch>>> GetAmbiguousTokens(QueryToken queryToken, ImmutableStack<OmniboxMatch> distancePack,
            QueryDescription queryDescription, List<OmniboxToken> omniboxTokens, int index, int operatorIndex)
        {
            OmniboxToken omniboxToken = omniboxTokens[index];

            bool isPascal = OmniboxUtils.IsPascalCasePattern(omniboxToken.Value);

            var matches = OmniboxUtils.Matches(
                QueryUtils.SubTokens(queryToken, queryDescription.Columns).ToDictionary(qt => qt.Key),
                qt => qt.ToString(), omniboxToken.Value, isPascal);

            if (index == operatorIndex - 1)
            {
                foreach (var m in matches)
                    yield return Tuple.Create((QueryToken)m.Value, distancePack.Push(m));
            }
            else
            {
                foreach (var m in matches)
                    foreach (var newPair in GetAmbiguousTokens((QueryToken)m.Value, distancePack.Push(m), queryDescription, omniboxTokens, index + 2, operatorIndex))
                        yield return newPair;
            }
        }


        protected Regex regex = new Regex(@"(?<token>I(\.I)*)((?<op>=)(?<val>(I;N)|[NSI])?)?", RegexOptions.ExplicitCapture);
        protected virtual List<FilterSyntax> GetSyntaxSequence(List<OmniboxToken> tokens)
        {
            var tokenPattern = new string(tokens.Select(t => Char(t.Type)).ToArray());

            var matches = regex.Matches(tokenPattern, 1).Cast<Match>().ToList();

            var list = matches.Select(a => new FilterSyntax
            {
                Index = a.Index,
                TokenLength = a.Groups["token"].Length,
                Length = a.Length,
                Completion = a.Groups["val"].Success ? FilterSyntaxCompletion.Complete :
                             a.Groups["op"].Success ? FilterSyntaxCompletion.Operation : FilterSyntaxCompletion.Token,
            }).ToList();

            return list; 
        }
       
        static char Char(OmniboxTokenType omniboxTokenType)
        {
            switch (omniboxTokenType)
            {
                case OmniboxTokenType.Identifier: return 'I';
                case OmniboxTokenType.Dot: return '.';
                case OmniboxTokenType.Semicolon: return ';';
                case OmniboxTokenType.Comparer: return '=';
                case OmniboxTokenType.Number: return 'N';
                case OmniboxTokenType.String: return 'S';
                default: return '?';
            }
        }

        public static string ToStringValue(object p)
        {
            if (p == null)
                return "null";


            switch (QueryUtils.GetFilterType(p.GetType()))
            {
                case FilterType.Number:
                case FilterType.DecimalNumber: return p.ToString();

                case FilterType.String: return "\"" + p.ToString() + "\"";
                case FilterType.DateTime: return "'" + p.ToString() + "'";
                case FilterType.Lite: return ((Lite)p).Key();
                case FilterType.Embedded: throw new InvalidOperationException("Impossible to translate not null Embedded entity to string");
                case FilterType.Boolean: return p.ToString();
                case FilterType.Enum: return p.ToString();
                case FilterType.Guid: return "\"" + p.ToString() + "\"";
            }

            throw new InvalidOperationException("Unexpected value type {0}".Formato(p.GetType()));
        }
    }

    public class DynamicQueryOmniboxResult : OmniboxResult
    {
        public object QueryName { get; set; }
        public OmniboxMatch QueryNameMatch { get; set; }
        public List<FilterQuery> Filters { get; set; }

        public override string ToString()
        {
            string queryName = (QueryName is Type ? Reflector.CleanTypeName((Type)QueryName) : QueryName.ToString());

            string filters = Filters.ToString(" ");

            if (string.IsNullOrEmpty(filters))
                return queryName;
            else
                return queryName + " " + filters;
        }
    }

    public class FilterQuery
    {
        public FilterQuery(float distance, FilterSyntax syntax, DynamicQuery.QueryToken queryToken, OmniboxMatch[] omniboxMatch)
        {
            this.Distance = distance;
            this.Syntax = syntax;
            this.QueryToken = queryToken;
            this.QueryTokenPacks = omniboxMatch;
        }

        public float Distance { get; set; }
        public FilterSyntax Syntax  {get; set;}

        public QueryToken QueryToken { get; set; }
        public OmniboxMatch[] QueryTokenPacks { get; set; }
        public FilterOperation? Operation { get; set; }
        public object Value { get; set; }
        public OmniboxMatch ValuePack { get; set; }

        public string CanFilter { get; set; }

        public override string ToString()
        {
            string token = QueryToken.TryCC(q => q.FullKey());

            if (Syntax == null || Syntax.Completion == FilterSyntaxCompletion.Token)
                return token;

            string oper = DynamicQueryOmniboxProvider.ToStringOperation(Operation.Value);

            if (Syntax.Completion == FilterSyntaxCompletion.Operation && Value == null)
                return token + oper;

            return token + oper + DynamicQueryOmniboxProvider.ToStringValue(Value);
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
    //
}
