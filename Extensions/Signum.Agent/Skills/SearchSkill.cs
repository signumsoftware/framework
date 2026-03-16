using Azure;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Signum.API;
using Signum.API.Controllers;
using Signum.Chart;
using Signum.DynamicQuery.Tokens;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace Signum.Agent.Skills;

public class SearchSkill : AgentSkill
{
    public static Func<object, bool> InlineQueryName = q => false; 

    public SearchSkill()
    {
        ShortDescription = "Explores the database schema and queries any information in the database";
        IsAllowed = () => true;
        Replacements = new Dictionary<string, Func<object?, string>>()
        {
            {
                "<LIST_ROOT_QUERIES>",
                obj => QueryLogic.Queries.GetAllowedQueryNames(fullScreen: true)
                .GroupBy(a => a is Type t? t.Namespace : a is Enum e ? e.GetType().Namespace : "Unknown")
                .ToString(gr =>
                {
                    var inlineQueries = gr.Where(InlineQueryName).ToString(qn =>
                    {
                        var imp = QueryLogic.Queries.GetEntityImplementations(qn);
                        var impStr = imp.Types.Only() == qn as Type ? "" : $" (ImplementedBy {imp.Types.ToString(t => t.Name, ", ")})";

                            return $"* {QueryUtils.GetKey(qn)}{impStr}";
                    }, "\n");

                    if(inlineQueries.HasText())
                        return "* Module " + gr.Key + $": {gr.Count()} queries including...\n" + inlineQueries.Indent("2");

                    return "* Module " + gr.Key + $": {gr.Count()} queries";
                } , "\n")
            }
        };
    }

    [McpServerTool, Description("List all query names in a namespace")]
    public static List<string> ListQueryNames(string namespaceName)
    {
        return QueryLogic.Queries.GetAllowedQueryNames(fullScreen: true)
            .Where(qn => (qn is Type t ? t.Namespace : qn is Enum e ? e.GetType().Namespace : "Unknown") == namespaceName)
            .Select(qn => QueryUtils.GetKey(qn))
            .ToList();
    }

    [McpServerTool, Description("Gets query description")]
    public static QueryDescriptionTS QueryDescription(string queryKey)
    {
        try
        {
            var qn = QueryLogic.ToQueryName(queryKey);
            var description = QueryLogic.Queries.QueryDescription(qn);
            var result = new QueryDescriptionTS(description);
            return result;
        }
        catch (Exception e)
        {
            AddQueryKeyHint(e, queryKey);
            throw;
        }
    }

    [McpServerTool, Description("Returns the Sub-Tokens of a QueryToken")]
    public static List<QueryTokenTS> SubTokens(string queryKey, string token)
    {
        try
        {
            var qn = QueryLogic.ToQueryName(queryKey);
            var qd = QueryLogic.Queries.QueryDescription(qn);

            var t = QueryUtils.Parse(token, qd, SubTokensOptions.All);

            var tokens = QueryUtils.SubTokens(t, qd, SubTokensOptions.All);

            return tokens.Select(qt => QueryTokenTS.WithAutoExpand(qt, qd)).ToList();
        }
        catch (Exception e)
        {
            AddQueryKeyHint(e, queryKey);
            throw;
        }
    }

    static void AddQueryKeyHint(Exception e, string queryKey)
    {
        var similar = QueryLogic.Queries.GetAllowedQueryNames(fullScreen: true)
            .Select(q => QueryUtils.GetKey(q))
            .Where(k => k.Contains(queryKey, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        if (similar.Any())
            e.Data["Hint"] = $"Similar query names: {similar.ToString(", ")}";
    }



    [McpServerTool, Description("Convert FindOptions to a url")]
    public static string GetFindOptionsUrl(FindOptions findOptions)
    {
        FindOptions fo = ParseFindOptions(findOptions);

        return CurrentServerContextSkill.UrlLeft?.Invoke() + FindOptionsEncoder.FindOptionsPath(fo);
    }

    public static FindOptions ParseFindOptions(FindOptions findOptions)
    {
        var queryName = QueryLogic.ToQueryName(findOptions.QueryName);
        var qd = QueryLogic.Queries.QueryDescription(queryName);

        var error = findOptions.Validate(qd);
        if (error.HasText())
            throw new McpException(error);
        return findOptions;
    }


    [McpServerTool, Description("Executes a FindOptions and returns a dynamic ResultTable")]
    public static ResultTableSimple GetResultTable(FindOptions findOptions)
    {
        FindOptions fo = ParseFindOptions(findOptions);

        var qr = fo.ToQueryRequest();

        var rt = QueryLogic.Queries.ExecuteQuery(qr);

        return new ResultTableSimple
        {
            Columns = rt.Columns.Select((a, i) => KeyValuePair.Create("c" + i, a.Token.FullKey())).ToDictionary(),
            Rows = rt.Rows.Select(r =>
            {
                var dic = new Dictionary<string, object?>();
                if (!qr.GroupResults)
                    dic.Add("Entity", r.Entity);

                for (int i = 0; i < rt.Columns.Length; i++)
                {
                    var rc = rt.Columns[i];
                    dic.Add("c" + i, r[rc]);
                }
                return dic;
            }).ToList(),
        };
    }
}

public class ResultTableSimple
{
    public Dictionary<string, string> Columns { get; internal set; }

    public List<Dictionary<string, object?>> Rows { get; internal set; }

}

public class SimpleChatScript
{
    public string Key { get; set; }
    public List<ChartScriptColumn> Columns { get; set; }
}

public class FindOptionsEncoder
{
    public static string FindOptionsPath(FindOptions fo)
    {
        var query = FindOptionsPathQuery(fo);
        var strQuery = ToQueryString(query);
        return "/find/" + fo.QueryName + (string.IsNullOrEmpty(strQuery) ? "" : "?" + strQuery);
    }

 
    public static Dictionary<string, object?> FindOptionsPathQuery(FindOptions fo, object? extra = null)
    {
        var query = new Dictionary<string, object?>
        {
            ["groupResults"] = fo.GroupResults?.ToString().ToLower(),
            ["idf"] = fo.IncludeDefaultFilters?.ToString().ToLower(),
            ["columnMode"] = fo.ColumnOptionsMode.HasValue && fo.ColumnOptionsMode != ColumnOptionsMode.Add
                ? fo.ColumnOptionsMode.ToString()
                : null,
            ["paginationMode"] = fo.Pagination?.Mode.ToString(),
            ["elementsPerPage"] = fo.Pagination?.ElementsPerPage,
            ["currentPage"] = fo.Pagination?.CurrentPage
        };

        EncodeFilters(query, fo.FilterOptions);
        EncodeOrders(query, fo.OrderOptions);
        EncodeColumns(query, fo.ColumnOptions);

        return query;
    }

    static string EncodeValue(object? value) => value switch
    {
        Lite<Entity> lite => lite.Key(),
        DateTime dt => dt.ToIsoString(),
        DateOnly date => date.ToIsoString(),
        bool b => b.ToString().ToLower(),
        IFormattable f => f?.ToString(null, CultureInfo.InvariantCulture) ?? "",
        var o => o?.ToString() ?? "",
    };

    public static void EncodeFilters(Dictionary<string, object?> query, IEnumerable<FilterOption>? filters, string? prefix = null)
    {
        if (filters == null) 
            return;

        int i = 0;
        void Encode(FilterOption fo, int indent = 0)
        {
            string identSuffix = indent == 0 ? "" : "_" + indent;
            string keyBase = prefix ?? "";

            if(fo.GroupOperation != null)
            {
                query[keyBase + "filter" + i + identSuffix] = $"{fo.Token ?? ""}~{fo.GroupOperation}~{EncodeValue(fo.Value)}";
                i++;
                foreach (var f in fo.Filters)
                    Encode(f, indent + 1);
            }
            else if(fo.Operation != null)
            {
                query[keyBase + "filter" + i + identSuffix] = $"{fo.Token}~{fo.Operation?.ToString() ?? "EqualTo"}~{EncodeValue(fo.Value)}";
                i++;
            }
            else 
                throw new InvalidOperationException("Should be either a FilterCondition or a FilterGroup");
        }

        foreach (var fo in filters) Encode(fo);
    }

    public static void EncodeOrders(Dictionary<string, object?> query, IEnumerable<OrderOption>? orders, string? prefix = null)
    {
        if (orders == null) return;
        int i = 0;
        foreach (var o in orders)
            query[(prefix ?? "") + "order" + i++] = (o.OrderType == OrderType.Descending ? "-" : "") + o.Token;
    }

    public static void EncodeColumns(Dictionary<string, object?> query, IEnumerable<ColumnOption>? columns, string? prefix = null)
    {
        if (columns == null) return;
        int i = 0;
        foreach (var co in columns)
        {
            string keyBase = prefix ?? "";
            string? displayName = co.DisplayName != null ? EscapeTilde(co.DisplayName) : null;

            query[keyBase + "column" + i] = co.Token + (displayName != null ? "~" + displayName : "");
            if (!string.IsNullOrEmpty(co.SummaryToken)) query[keyBase + "summary" + i] = co.SummaryToken;

            i++;
        }
    }

    public static string ToQueryString(Dictionary<string, object?> query)
    {
        return string.Join("&", query
            .Where(kv => kv.Value != null)
            .Select(kv => $"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(kv.Value?.ToString())}"));
    }

    private static string EscapeTilde(string input) => input.Replace("~", "~~");

}

public class FindOptions
{
    public string QueryName { get; set; } = null!;
    public bool? GroupResults { get; set; }
    public bool? IncludeDefaultFilters { get; set; }
    public List<FilterOption>? FilterOptions { get; set; }
    public List<OrderOption>? OrderOptions { get; set; }
    public ColumnOptionsMode? ColumnOptionsMode { get; set; }
    public List<ColumnOption>? ColumnOptions { get; set; }
    public Pagination? Pagination { get; set; }

    internal QueryRequest ToQueryRequest()
    {

        var queryName = QueryLogic.ToQueryName(QueryName);
        var qd = QueryLogic.Queries.QueryDescription(queryName);
        var aggregate = GroupResults == true ? SubTokensOptions.CanAggregate : (SubTokensOptions)0;
        return new QueryRequest
        {
            QueryName = queryName,
            GroupResults = GroupResults ?? false,
            Filters = FilterOptions.EmptyIfNull().Select(a => a.ToFilter(qd, aggregate)).ToList(),
            Columns = MergeColumns(qd, aggregate),
            Orders = this.OrderOptions.EmptyIfNull().Select(a => a.ToOrder(qd, aggregate)).ToList(),
            Pagination = Pagination?.ToPagination() ?? new DynamicQuery.Pagination.Firsts(20)
        };
    }

    List<Column> MergeColumns(QueryDescription qd, SubTokensOptions aggregates)
    {
        var options = SubTokensOptions.CanElement | SubTokensOptions.CanToArray |  aggregates;

        var columns = this.ColumnOptions.EmptyIfNull();
        switch (this.ColumnOptionsMode ?? DynamicQuery.ColumnOptionsMode.ReplaceAll)
        {
            case DynamicQuery.ColumnOptionsMode.Add: return qd.Columns.Where(cd => !cd.IsEntity).Select(cd => new Column(cd, qd.QueryName)).Concat(columns.Select(co => co.ToColumn(qd, options))).ToList();
            case DynamicQuery.ColumnOptionsMode.Remove: return qd.Columns.Where(cd => !cd.IsEntity && !columns.Any(co => co.Token == cd.Name)).Select(cd => new Column(cd, qd.QueryName)).ToList();
            case DynamicQuery.ColumnOptionsMode.ReplaceAll: return columns.Select(co => co.ToColumn(qd, options)).ToList();
            case DynamicQuery.ColumnOptionsMode.ReplaceOrAdd:
                {
                    var original = qd.Columns.Where(cd => !cd.IsEntity).Select(cd => new Column(cd, qd.QueryName)).ToList();
                    var toReplaceOrAdd = columns.Select(co => co.ToColumn(qd, options)).ToList();
                    foreach (var item in toReplaceOrAdd)
                    {
                        var index = original.FindIndex(o => o.Token.Equals(item.Token));
                        if (index != -1)
                            original[index] = item;
                        else
                            original.Add(item);
                    }
                    return original;
                }
            default: throw new InvalidOperationException("{0} is not a valid ColumnOptionMode".FormatWith(ColumnOptionsMode));
        }
    }

    internal string? Validate(QueryDescription qd)
    {
        var agg = this.GroupResults == true ? SubTokensOptions.CanAggregate : 0;
        var jsonOptions = SignumServer.JsonSerializerOptions;
        var sb = new StringBuilder();
        if (FilterOptions != null)
        {
            int i = 0;
            foreach (var f in FilterOptions)
                f.Validate(sb, $"filterOptions[{i}]", qd, agg, jsonOptions);
        }

        if (ColumnOptions != null)
        {
            int i = 0;
            foreach (var f in ColumnOptions)
                f.Validate(sb, $"columnOptions[{i}]", qd, agg);
        }

        if (OrderOptions != null)
        {
            int i = 0;
            foreach (var f in OrderOptions)
                f.Validate(sb, $"orderOptions[{i}]", qd, agg);
        }

        if(Pagination != null)
        {
            this.Pagination.Validate(sb, "pagination");
        }

        if (sb.Length == 0)
            return null;

        return sb.ToString();
    }
}


public class FilterOption
{
    public string? Token { get; set; } = null!;
    public FilterOperation? Operation { get; set; }
    public object? Value { get; set; }
    public FilterGroupOperation? GroupOperation { get; set; }
    public List<FilterOption> Filters { get; set; } = new();

    public void Validate(StringBuilder sb, string path, QueryDescription qd, SubTokensOptions aggregate, JsonSerializerOptions jsonOptions)
    {
        if (Operation != null)
        {
            var options = SubTokensOptions.CanElement | aggregate | SubTokensOptions.CanAnyAll;
            var qt = QueryUtils.TryParse(Token!, qd, options, out string? error, out var partial);
            if (qt == null)
            {
                sb.AppendLine($"{path}.token (FilterCondition with token '{Token}'): {error}");
                sb.AppendLine(" Possible next tokens: " + qd.NextAlternatives(options, partial));
                return;
            }

            var ft = QueryUtils.GetFilterType(qt.Type);

            var op = Operation ?? FilterOperation.EqualTo;
            if (!QueryUtils.GetFilterOperations(qt).Contains(op))
            {
                sb.AppendLine($"{path}.condition (FilterCondition with token '{Token}'): Operation '{op}' is not valid for type '{qt.Type.Name}'");
                return;
            }

            var expectedValueType = FilterCondition.GetValueType(qt, op);

            object? val = null;
            try
            {
                val = ((object?)Value) is JsonElement jtok ?
                   jtok.ToObject(expectedValueType, jsonOptions) :
                   Value;
            }
            catch (JsonException)
            {
                sb.AppendLine($"{path}.value (FilterCondition with token '{Token}'): {Value} is not a valid {expectedValueType.TypeName()}");
                return;
            }

            if (val is DateTime dt)
            {
                var kind = qt.DateTimeKind;
                val = dt.ToKind(kind);
            }
            else if (val is ObservableCollection<DateTime?> col)
            {
                var kind = qt.DateTimeKind;
                val = col.Select(dt => dt?.ToKind(kind)).ToObservableCollection();
            }

            Value = val;
        }
        else if (GroupOperation != null)
        {
            if (Token.HasText())
            {
                var options = SubTokensOptions.CanElement | aggregate | SubTokensOptions.CanAnyAll;
                var qt = QueryUtils.TryParse(Token, qd, options, out string? error, out QueryToken? partial);
                sb.AppendLine($"{path}.token (FilterGroup with token '{Token}'): {error}");
                sb.AppendLine(" Possible next tokens: " + qd.NextAlternatives(options, partial));
                return;
            }

            int i = 0;
            foreach (var f in Filters)
            {
                f.Validate(sb, $"{path}.filters[{i}]", qd, aggregate, jsonOptions);
            }
        }
        else
        {
            sb.AppendLine($"{path}: Should be either a FilterCondition or a FilterGroup");
        }
    }

  

    internal Filter ToFilter(QueryDescription qd, SubTokensOptions aggregate)
    {
        if (Operation != null)
        {
            var token = QueryUtils.Parse(Token!, qd, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | aggregate);
            return new FilterCondition(token, Operation ?? FilterOperation.EqualTo, Value);
        }

        if(GroupOperation != null)
        {
            var token = Token.HasText() ? null : QueryUtils.Parse(Token!, qd, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | aggregate);

            return new FilterGroup(GroupOperation.Value, token,
                this.Filters.Select(a => a.ToFilter(qd, aggregate)).ToList()
                );
        }

        throw new InvalidOperationException("Unexpected filter type");
    }
}


public class OrderOption
{
    public string Token { get; set; } = null!;
    public OrderType OrderType { get; set; }

    internal Order ToOrder(QueryDescription qd, SubTokensOptions agg)
    {
        var parsedToken = QueryUtils.Parse(Token, qd, SubTokensOptions.CanElement | agg);

        return new Order(parsedToken, OrderType);
    }

    internal void Validate(StringBuilder sb, string path, QueryDescription qd, SubTokensOptions agg)
    {
        var options = SubTokensOptions.CanElement | agg;

        var parsedToken = QueryUtils.TryParse(Token, qd, options, out var error, out var partial);
        if (error.HasText())
        {
            sb.AppendLine($"{path}.token (Order with token '{Token}'): {error}");
            sb.AppendLine(" Possible next tokens: " + qd.NextAlternatives(options, partial));
            return;
        }
    }
}


public class ColumnOption
{
    public string Token { get; set; }
    public string? DisplayName { get; set; }
    public string? SummaryToken { get; set; }
    public bool? HiddenColumn { get; set; }

    public Column ToColumn(QueryDescription qd, SubTokensOptions aggregates)
    {
        var parsedToken = QueryUtils.Parse(Token, qd, aggregates);

        return new DynamicQuery.Column(parsedToken, DisplayName);
    }

    internal void Validate(StringBuilder sb, string path, QueryDescription qd, SubTokensOptions agg)
    {
        var options = SubTokensOptions.CanElement | agg;

        var parsedToken = QueryUtils.TryParse(Token, qd, options, out var error, out var partial);
        if (error.HasText())
        {
            sb.AppendLine($"{path}.token (Column with token '{Token}'): {error}");
            sb.AppendLine(" Possible next tokens: " + qd.NextAlternatives(options, partial));
            return;
        }

        if (SummaryToken.HasText())
        {
            var summaryToken = QueryUtils.TryParse(SummaryToken, qd, options | SubTokensOptions.CanAggregate, out var errorSummary, out var partialSummary);
            if (errorSummary.HasText())
            {
                sb.AppendLine($"{path}.summaryToken (Column with summaryToken '{SummaryToken}'): {errorSummary}");
                sb.AppendLine(" Possible next tokens: " + qd.NextAlternatives(options | SubTokensOptions.CanAggregate, partialSummary));
                return;
            }

            if(summaryToken != null && summaryToken is not AggregateToken)
            {
                sb.AppendLine($"{path}.summaryToken (Column with summaryToken '{SummaryToken}'): is not and aggregate");
                return;
            }
        }
    }
}


public class Pagination
{
    public PaginationMode Mode { get; set; }
    public int? ElementsPerPage { get; set; }
    public int? CurrentPage { get; set; }

    internal DynamicQuery.Pagination ToPagination() => Mode switch
    {
        PaginationMode.All => new DynamicQuery.Pagination.All(),
        PaginationMode.Firsts => new DynamicQuery.Pagination.Firsts(ElementsPerPage ?? 20),
        PaginationMode.Paginate => new DynamicQuery.Pagination.Paginate(ElementsPerPage ?? 20, CurrentPage ?? 1),
        _ => throw new UnexpectedValueException(Mode)
    };

    internal void Validate(StringBuilder sb, string path)
    {
        if(Mode == PaginationMode.All)
        {   
        }

        if(Mode == PaginationMode.Firsts)
        {
            if(ElementsPerPage == null || ElementsPerPage <= 0)
                sb.AppendLine($"{path}.elementsPerPage: Should be a positive number when paginationMode is 'Firsts'");
        }

        if (Mode == PaginationMode.Paginate)
        {
            if (ElementsPerPage == null || ElementsPerPage <= 0)
                sb.AppendLine($"{path}.elementsPerPage: Should be a positive number when paginationMode is 'Paginate'");
            if (CurrentPage == null || CurrentPage < 0)
                sb.AppendLine($"{path}.currentPage: Should be a positive number when paginationMode is 'Paginate'");
        }
    }
}
