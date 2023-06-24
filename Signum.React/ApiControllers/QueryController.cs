using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.React.Facades;
using Signum.Entities.Basics;
using Signum.React.Filters;
using System.Collections.ObjectModel;
using Signum.Engine.Maps;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;
using Signum.Engine.Json;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Filters;
using Azure.Core;

namespace Signum.React.ApiControllers;

[ValidateModelFilter]
public class QueryController : ControllerBase
{
    [HttpGet("api/query/findLiteLike"), ProfilerActionSplitter("types")]
    public async Task<List<Lite<Entity>>> FindLiteLike(string types, string subString, int count, CancellationToken token)
    {
        Implementations implementations = ParseImplementations(types);

        return await AutocompleteUtils.FindLiteLikeAsync(implementations, subString, count, token);
    }

    [HttpGet("api/query/allLites"), ProfilerActionSplitter("types")]
    public async Task<List<Lite<Entity>>> FetchAllLites(string types, CancellationToken token)
    {
        Implementations implementations = ParseImplementations(types);

        return await AutocompleteUtils.FindAllLiteAsync(implementations, token);
    }

    private static Implementations ParseImplementations(string types)
    {
        return Implementations.By(types.Split(',').Select(a => TypeLogic.GetType(a.Trim())).ToArray());
    }

    [HttpGet("api/query/description/{queryName}"), ProfilerActionSplitter("queryName")]
    public QueryDescriptionTS GetQueryDescription(string queryName)
    {
        var qn = QueryLogic.ToQueryName(queryName);
        return new QueryDescriptionTS(QueryLogic.Queries.QueryDescription(qn));
    }

    [HttpGet("api/query/queryEntity/{queryName}"), ProfilerActionSplitter("queryName")]
    public QueryEntity GetQueryEntity(string queryName)
    {
        var qn = QueryLogic.ToQueryName(queryName);
        return QueryLogic.GetQueryEntity(qn);
    }

    [HttpPost("api/query/parseTokens")]
    public List<QueryTokenTS> ParseTokens([Required, FromBody]ParseTokensRequest request)
    {
        var qn = QueryLogic.ToQueryName(request.queryKey);
        var qd = QueryLogic.Queries.QueryDescription(qn);

        var tokens = request.tokens.Select(tr => QueryUtils.Parse(tr.token, qd, tr.options)).ToList();

        return tokens.Select(qt => new QueryTokenTS(qt, recursive: true)).ToList();
    }

    public class TokenRequest
    {
        public required string token;
        public SubTokensOptions options;

        public override string ToString() => $"{token} ({options})";
    }

    public class ParseTokensRequest
    {
        public required string queryKey;
        public required List<TokenRequest> tokens;
    }

    [HttpPost("api/query/subTokens")]
    public List<QueryTokenTS> SubTokens([Required, FromBody]SubTokensRequest request)
    {
        var qn = QueryLogic.ToQueryName(request.queryKey);
        var qd = QueryLogic.Queries.QueryDescription(qn);

        var token = request.token == null ? null: QueryUtils.Parse(request.token, qd, request.options);


        var tokens = QueryUtils.SubTokens(token, qd, request.options);

        return tokens.Select(qt => new QueryTokenTS(qt, recursive: false)).ToList();
    }

    public class SubTokensRequest
    {
        public required string queryKey;
        public string? token;
        public SubTokensOptions options;
    }

    [HttpPost("api/query/executeQuery"), ProfilerActionSplitter]
    public async Task<ResultTable> ExecuteQuery([Required, FromBody]QueryRequestTS request, CancellationToken token)
    {
        var result = await QueryLogic.Queries.ExecuteQueryAsync(request.ToQueryRequest(SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer), token);
        return result;
    }

    [HttpPost("api/query/lites"), ProfilerActionSplitter]
    public async Task<List<Lite<Entity>>> GetLites([Required, FromBody]QueryEntitiesRequestTS request, CancellationToken token)
    {
        return await QueryLogic.Queries.GetEntitiesLite(request.ToQueryEntitiesRequest(SignumServer.JsonSerializerOptions)).ToListAsync(token);
    }

    [HttpPost("api/query/entities"), ProfilerActionSplitter]
    public async Task<List<Entity>> GetEntities([Required, FromBody]QueryEntitiesRequestTS request, CancellationToken token)
    {
        return await QueryLogic.Queries.GetEntitiesFull(request.ToQueryEntitiesRequest(SignumServer.JsonSerializerOptions)).ToListAsync(token);
    }

    [HttpPost("api/query/queryValue"), ProfilerActionSplitter, EmbeddedPropertyRouteAttribute<QueryValueResolver>]
    public async Task<object?> QueryValue([Required, FromBody]QueryValueRequestTS request, CancellationToken token)
    {
        var qvRequest = request.ToQueryValueRequest(SignumServer.JsonSerializerOptions);

        HttpContext.Items["queryValueRequests"] = qvRequest;
        return await QueryLogic.Queries.ExecuteQueryValueAsync(qvRequest, token);
    }

    class QueryValueResolver : IEmbeddedPropertyRouteResolver
    {
        public PropertyRoute GetPropertyRoute(EmbeddedEntity embedde, FilterContext filterContext)
        {
            var qvr = (QueryValueRequest)filterContext.HttpContext.Items["queryValueRequests"]!;
            return qvr.ValueToken!.GetPropertyRoute()!;
        }
    }
}



public class QueryDescriptionTS
{
    public string queryKey;
    public Dictionary<string, ColumnDescriptionTS> columns;

    public QueryDescriptionTS(QueryDescription queryDescription)
    {
        this.queryKey = QueryUtils.GetKey(queryDescription.QueryName);
        this.columns = queryDescription.Columns.ToDictionary(a => a.Name, a => new ColumnDescriptionTS(a, queryDescription.QueryName));

        foreach (var action in AddExtension.GetInvocationListTyped())
        {
            action(this);
        }
    }

    [JsonExtensionData]
    public Dictionary<string, object> Extension { get; set; } = new Dictionary<string, object>();

    public static Action<QueryDescriptionTS>? AddExtension;
}

public class ColumnDescriptionTS
{
    public string name;
    public TypeReferenceTS type;
    public string typeColor;
    public string niceTypeName;
    public FilterType? filterType;
    public string? unit;
    public string? format;
    public string displayName;
    public bool isGroupable;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool hasOrderAdapter;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool preferEquals;
    public string? propertyRoute;

    public ColumnDescriptionTS(ColumnDescription a, object queryName)
    {
        var token = new ColumnToken(a, queryName);

        this.name = a.Name;
        this.type = new TypeReferenceTS(a.Type, a.Implementations);
        this.filterType = QueryUtils.TryGetFilterType(a.Type);
        this.typeColor = token.TypeColor;
        this.niceTypeName = token.NiceTypeName;
        this.isGroupable = token.IsGroupable;
        this.hasOrderAdapter = QueryUtils.OrderAdapters.Any(a => a(token) != null);
        this.preferEquals = token.Type == typeof(string) &&
            token.GetPropertyRoute() is PropertyRoute pr &&
            typeof(Entity).IsAssignableFrom(pr.RootType) &&
            Schema.Current.HasSomeIndex(pr);
        this.unit = UnitAttribute.GetTranslation(a.Unit);
        this.format = a.Format;
        this.displayName = a.DisplayName;
        this.propertyRoute = token.GetPropertyRoute()?.ToString();
    }
}

public class QueryTokenTS
{

    QueryTokenTS() { }
    [SetsRequiredMembers]
    public QueryTokenTS(QueryToken qt, bool recursive)
    {
        this.toStr = qt.ToString();
        this.niceName = qt.NiceName();
        this.key = qt.Key;
        this.fullKey = qt.FullKey();
        this.type = new TypeReferenceTS(qt.Type, qt.GetImplementations());
        this.filterType = QueryUtils.TryGetFilterType(qt.Type);
        this.format = qt.Format;
        this.unit = UnitAttribute.GetTranslation(qt.Unit);
        this.typeColor = qt.TypeColor;
        this.niceTypeName = qt.NiceTypeName;
        this.queryTokenType = GetQueryTokenType(qt);
        this.isGroupable = qt.IsGroupable;
        this.hasOrderAdapter = QueryUtils.OrderAdapters.Any(a => a(qt) != null);

        this.preferEquals = qt.Type == typeof(string) &&
            qt.GetPropertyRoute() is PropertyRoute pr &&
            typeof(Entity).IsAssignableFrom(pr.RootType) &&
            Schema.Current.HasSomeIndex(pr);

        this.propertyRoute = qt.GetPropertyRoute()?.ToString();
        if (recursive && qt.Parent != null)
            this.parent = new QueryTokenTS(qt.Parent, recursive);
    }

    private static QueryTokenType? GetQueryTokenType(QueryToken qt)
    {
        if (qt is AggregateToken)
            return QueryTokenType.Aggregate;

        if (qt is CollectionElementToken ce)
            return QueryTokenType.Element;

        if (qt is CollectionAnyAllToken caat)
            return QueryTokenType.AnyOrAll;

        if (qt is CollectionToArrayToken)
            return QueryTokenType.ToArray;

        if (qt is OperationsToken ot)
            return QueryTokenType.Operation;
        
        return null;
    }

    public required string toStr;
    public required string niceName;
    public required string key;
    public required string fullKey;
    public required string typeColor;
    public required string niceTypeName;
    public QueryTokenType? queryTokenType;
    public required TypeReferenceTS type;
    public FilterType? filterType;
    public string? format;
    public string? unit;
    public bool isGroupable;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool hasOrderAdapter;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool preferEquals;
    public QueryTokenTS? parent;
    public string? propertyRoute;
}

public enum QueryTokenType
{
    Aggregate,
    Element,
    AnyOrAll,
    Operation,
    ToArray
}
