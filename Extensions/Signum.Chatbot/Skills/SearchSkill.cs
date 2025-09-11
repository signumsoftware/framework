using Azure;
using Azure.Core;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Signum.API;
using Signum.API.Controllers;
using Signum.API.Json;
using Signum.Chart;
using Signum.DynamicQuery.Tokens;
using Signum.Utilities.Reflection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace Signum.Chatbot.Agents;

public class SearchSkill : ChatbotSkill
{
    public SearchSkill()
    {
        ShortDescription = "Explores the database schema to search any information in the database";
        IsAllowed = () => true;
        Replacements = new Dictionary<string, Func<object?, string>>()
        {
            {
                "<LIST_ROOT_QUERIES>",
                obj => QueryLogic.Queries.GetAllowedQueryNames(fullScreen: true)
                .ToString(a =>
                {
                    var imp = QueryLogic.Queries.GetEntityImplementations(a);

                    var impStr = imp.Types.Only() == a ? "" : $" (ImplementedBy {imp.Types.ToString(t => t.Name, ", ")})";

                    return $"* {QueryUtils.GetKey(a)}{impStr}: {QueryUtils.GetNiceName(a)}";
                } , "\n")
            }
        };
    }

    [McpServerTool, Description("Gets query description")]
    public static QueryDescriptionTS QueryDescription(string queryKey)
    {
        var qn = QueryLogic.ToQueryName(queryKey);
        var description = QueryLogic.Queries.QueryDescription(qn);
        var result = new QueryDescriptionTS(description);
        return result;
    }

    [McpServerTool, Description("Returns the Sub-Tokens of a QueryToken")]
    public static List<QueryTokenTS> SubTokens(string queryKey, string token)
    {
        var qn = QueryLogic.ToQueryName(queryKey);
        var qd = QueryLogic.Queries.QueryDescription(qn);

        var t = QueryUtils.Parse(token, qd, SubTokensOptions.All);

        var tokens = QueryUtils.SubTokens(t, qd, SubTokensOptions.All);

        return tokens.Select(qt => QueryTokenTS.WithAutoExpand(qt, qd)).ToList();
    }



    [McpServerTool, Description("Convert FindOptions to a url")]
    public static string GetFindOptionsUrl(string findOptionsJson)
    {
        FindOptions fo = ParseFindOptions(findOptionsJson);

        return FindOptionsEncoder.FindOptionsPath(fo);
    }

    public static FindOptions ParseFindOptions(string findOptionsJson)
    {
        FindOptions fo;
        try
        {
            fo = JsonSerializer.Deserialize<FindOptions>(findOptionsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                Converters = {
                    new JsonStringEnumConverter(),
                }
            })!;
        }
        catch (Exception e)
        {
            throw new McpException(e.Message, e);
        }

        var queryName = QueryLogic.ToQueryName(fo.QueryName);
        var qd = QueryLogic.Queries.QueryDescription(queryName);

        var error = fo.Validate(qd);
        if (error.HasText())
            throw new McpException(error);
        return fo;
    }

 
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
        var columns = this.ColumnOptions.EmptyIfNull();
        switch (this.ColumnOptionsMode ?? DynamicQuery.ColumnOptionsMode.Add)
        {
            case DynamicQuery.ColumnOptionsMode.Add: return qd.Columns.Where(cd => !cd.IsEntity).Select(cd => new Column(cd, qd.QueryName)).Concat(columns.Select(co => co.ToColumn(qd, aggregates))).ToList();
            case DynamicQuery.ColumnOptionsMode.Remove: return qd.Columns.Where(cd => !cd.IsEntity && !columns.Any(co => co.Token == cd.Name)).Select(cd => new Column(cd, qd.QueryName)).ToList();
            case DynamicQuery.ColumnOptionsMode.ReplaceAll: return columns.Select(co => co.ToColumn(qd,  aggregates)).ToList();
            case DynamicQuery.ColumnOptionsMode.ReplaceOrAdd:
                {
                    var original = qd.Columns.Where(cd => !cd.IsEntity).Select(cd => new Column(cd, qd.QueryName)).ToList();
                    var toReplaceOrAdd = columns.Select(co => co.ToColumn(qd, aggregates)).ToList();
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
            var qt = QueryUtils.TryParse(Token!, qd, SubTokensOptions.CanElement | aggregate | SubTokensOptions.CanAnyAll, out string? error);
            if (qt == null)
            {
                sb.AppendLine($"{path}.token (FilterCondition with token '{Token}'): {error}");
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
                var qt = QueryUtils.TryParse(Token, qd, SubTokensOptions.CanElement | aggregate | SubTokensOptions.CanAnyAll, out string? error);
                sb.AppendLine($"{path}.token (FilterGroup with token '{Token}'): {error}");
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
            var token = QueryUtils.Parse(Token!, qd, SubTokensOptions.CanElement | SubTokensOptions.All | aggregate);
            return new FilterCondition(token, Operation ?? FilterOperation.EqualTo, Value);
        }

        if(GroupOperation != null)
        {
            var token = Token.HasText() ? null : QueryUtils.Parse(Token!, qd, SubTokensOptions.CanElement | SubTokensOptions.All | aggregate);

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
        var parsedToken = QueryUtils.Parse(Token, qd, agg);

        return new Order(parsedToken, OrderType);
    }

    internal void Validate(StringBuilder sb, string path, QueryDescription qd, SubTokensOptions agg)
    {
        var options = SubTokensOptions.CanElement | agg;

        var parsedToken = QueryUtils.TryParse(Token, qd, options, out var error);
        if (error.HasText())
        {
            sb.AppendLine($"{path}.token (Order with token '{Token}'): {error}");
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

        var parsedToken = QueryUtils.TryParse(Token, qd, options, out var error);
        if (error.HasText())
        {
            sb.AppendLine($"{path}.token (Column with token '{Token}'): {error}");
            return;
        }

        if (SummaryToken.HasText())
        {
            var summaryToken = QueryUtils.TryParse(SummaryToken, qd, options | SubTokensOptions.CanAggregate, out var errorSummary);
            if (errorSummary.HasText())
            {
                sb.AppendLine($"{path}.summaryToken (Column with summaryToken '{SummaryToken}'): {errorSummary}");
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
