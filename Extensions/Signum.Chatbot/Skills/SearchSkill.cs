using Azure;
using Azure.Core;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Signum.API;
using Signum.API.Controllers;
using Signum.API.Json;
using Signum.DynamicQuery.Tokens;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
                .ToString(a => $"* {QueryUtils.GetKey(a)}: {QueryUtils.GetNiceName(a)}", "\n")
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

    [McpServerTool, Description("Convert FindOptions json string to a Url")]
    public static string GetFindOptionsUrl(string findOptions)
    {
        FindOptions fo;
        try
        {
            fo = JsonSerializer.Deserialize<FindOptions>(findOptions, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                Converters = {
                    new JsonStringEnumConverter(),
                }
            })!;
        }
        catch(Exception e)
        {
            throw new McpException(e.Message, e);
        }

        var queryName = QueryLogic.ToQueryName(fo.QueryName);
        var qd = QueryLogic.Queries.QueryDescription(queryName);

        var error = fo.Validate(qd);
        if (error.HasText())
            throw new McpException(error);

        return FindOptionsEncoder.FindOptionsPath(fo);
    }
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
            ["groupResults"] = fo.GroupResults,
            ["idf"] = fo.IncludeDefaultFilters,
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

    public static void EncodeFilters(Dictionary<string, object?> query, IEnumerable<FilterOption>? filters, string? prefix = null)
    {
        if (filters == null) return;

        int i = 0;
        void Encode(FilterOption fo, int indent = 0)
        {
            string identSuffix = indent == 0 ? "" : "_" + indent;
            string keyBase = prefix ?? "";

            if(fo.GroupOperation != null)
            {
                query[keyBase + "filter" + i + identSuffix] = $"{fo.Token ?? ""}~{fo.GroupOperation}~{fo.Value}";
                i++;
                foreach (var f in fo.Filters)
                    Encode(f, indent + 1);
            }
            else if(fo.Operation != null)
            {
                query[keyBase + "filter" + i + identSuffix] = $"{fo.Token}~{fo.Operation?.ToString() ?? "EqualTo"}~{fo.Value}";
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

    private static string ToQueryString(Dictionary<string, object?> query)
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

    internal string? Validate(QueryDescription qd)
    {
        var agg = this.GroupResults == true ? SubTokensOptions.CanAggregate : 0;
        var jsonOptions = SignumServer.JsonSerializerOptions;
        var sb = new StringBuilder();
        //if (FilterOptions != null)
        //{
        //    int i = 0;
        //    foreach (var f in FilterOptions)
        //        f.Validate(sb, $"filterOptions[{i}]", qd, agg, jsonOptions);
        //}

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

            object? val;
            try
            {
                val = Value is JsonElement jtok ?
                     jtok.ToObject(expectedValueType, jsonOptions) :
                     Value;
            }
            catch (JsonException)
            {
                sb.AppendLine($"{path}.value (FilterCondition with token '{Token}'): {Value} is not a valid {expectedValueType.TypeName()}");
                return;
            }

            if (Value is DateTime dt)
            {
                var kind = qt.DateTimeKind;
                val = dt.ToKind(kind);
            }
            else if (Value is ObservableCollection<DateTime?> col)
            {
                var kind = qt.DateTimeKind;
                val = col.Select(dt => dt?.ToKind(kind)).ToObservableCollection();
            }

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
}


public class OrderOption
{
    public string Token { get; set; } = null!;
    public OrderType OrderType { get; set; }

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

    internal void Validate(StringBuilder sb, string path, QueryDescription qd, SubTokensOptions agg)
    {
        var options = SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | agg;

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
