using Signum.API.Filters;
using Signum.Engine.Maps;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Filters;
using Signum.DynamicQuery.Tokens;
using Microsoft.AspNetCore.Mvc;
using Signum.API.Json;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;
using Signum.DynamicQuery;

namespace Signum.API.Controllers;

[ValidateModelFilter]
public class QueryController : ControllerBase
{
    public static Action<QueryRequest>? AssertQuery;

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

        foreach (var type in implementations.Types)
        {
            if (EntityKindCache.GetEntityData(type) == EntityData.Transactional)
                throw new ArgumentNullException($"{type} is a Transactional entity");
        }

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

        var tokens = request.tokens.Select(token => QueryUtils.Parse(token, qd, SubTokensOptions.All)).ToList();

        return tokens.Select(qt => new QueryTokenTS(qt, withParents: true)).ToList();
    }

    public class ParseTokensRequest
    {
        public required string queryKey;
        public required List<string> tokens;
    }

    [HttpPost("api/query/subTokens")]
    public List<QueryTokenTS> SubTokens([Required, FromBody]SubTokensRequest request)
    {
        var qn = QueryLogic.ToQueryName(request.queryKey);
        var qd = QueryLogic.Queries.QueryDescription(qn);

        var token = request.token == null ? null: QueryUtils.Parse(request.token, qd, SubTokensOptions.All);

        var tokens = QueryUtils.SubTokens(token, qd, SubTokensOptions.All);

        return tokens.Select(qt => QueryTokenTS.WithAutoExpand(qt, qd)).ToList();
    }

    public class SubTokensRequest
    {
        public required string queryKey;
        public string? token;
    }

    [HttpPost("api/query/executeQuery/{queryKey}"), ProfilerActionSplitter("queryKey")]
    public async Task<ResultTable> ExecuteQuery(string queryKey, [Required, FromBody]QueryRequestTS request, CancellationToken token)
    {
        var qr = request.ToQueryRequest(queryKey, SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer);
        AssertQuery?.Invoke(qr);
        var result = await QueryLogic.Queries.ExecuteQueryAsync(qr, token);
        return result;
    }

    [HttpPost("api/query/lites/{queryKey}"), ProfilerActionSplitter("queryKey")]
    public async Task<List<Lite<Entity>>> GetLites(string queryKey, [Required, FromBody]QueryEntitiesRequestTS request, CancellationToken token)
    {
        return await QueryLogic.Queries.GetEntitiesLite(request.ToQueryEntitiesRequest(queryKey, SignumServer.JsonSerializerOptions)).ToListAsync(token);
    }

    [HttpPost("api/query/entities/{queryKey}"), ProfilerActionSplitter("queryKey")]
    public async Task<List<Entity>> GetEntities(string queryKey, [Required, FromBody]QueryEntitiesRequestTS request, CancellationToken token)
    {
        return await QueryLogic.Queries.GetEntitiesFull(request.ToQueryEntitiesRequest(queryKey, SignumServer.JsonSerializerOptions)).ToListAsync(token);
    }

    [HttpPost("api/query/queryValue/{queryKey}"), ProfilerActionSplitter("queryKey"), EmbeddedPropertyRouteAttribute<QueryValueResolver>]
    public async Task<object?> QueryValue(string queryKey, [Required, FromBody]QueryValueRequestTS request, CancellationToken token)
    {
        var qvRequest = request.ToQueryValueRequest(queryKey, SignumServer.JsonSerializerOptions);

        HttpContext.Items["queryValueRequests"] = qvRequest;
        return await QueryLogic.Queries.ExecuteQueryValueAsync(qvRequest, token);
    }

    class QueryValueResolver : IEmbeddedPropertyRouteResolver
    {
        PropertyRoute IEmbeddedPropertyRouteResolver.GetPropertyRoute(EmbeddedEntity embedded, FilterContext filterContext)
        {
            var qvr = (QueryValueRequest)filterContext.HttpContext.Items["queryValueRequests"]!;
            return qvr.ValueToken!.GetPropertyRoute()!;
        }
    }
}



public class QueryDescriptionTS
{
    public string queryKey;
    public Dictionary<string, QueryTokenTS> columns;

    public QueryDescriptionTS(QueryDescription qd)
    {
        this.queryKey = QueryUtils.GetKey(qd.QueryName);
        columns = new Dictionary<string, QueryTokenTS>();
        var count = new AggregateToken(AggregateFunction.Count, qd.QueryName);
        this.columns.Add(count.Key, QueryTokenTS.WithAutoExpand(count, qd));

        var timeSeries = new TimeSeriesToken(qd.QueryName);
        this.columns.Add(timeSeries.Key, QueryTokenTS.WithAutoExpand(timeSeries, qd));

        this.columns.AddRange(qd.Columns, a => a.Name, cd =>
        {
            var token = new ColumnToken(cd, qd.QueryName);
            return QueryTokenTS.WithAutoExpand(token, qd);
        });

        foreach (var action in AddExtension.GetInvocationListTyped())
        {
            action(this);
        }
    }

    [JsonExtensionData]
    public Dictionary<string, object> Extension { get; set; } = new Dictionary<string, object>();

    public static Action<QueryDescriptionTS>? AddExtension;
}

public class QueryTokenTS
{
    QueryTokenTS() { }
    [SetsRequiredMembers]
    public QueryTokenTS(QueryToken qt, bool withParents)
    {
        this.toStr = qt.ToString();
        this.niceName = qt.NiceName();
        this.key = qt.Key;
        this.fullKey = qt.FullKey();
        this.type = new TypeReferenceTS(qt.Type, qt.GetImplementations());
        this.filterType = QueryUtils.TryGetFilterType(qt.Type);
        this.format = qt.Format;
        this.unit = UnitAttribute.GetTranslation(qt.Unit);
        this.niceTypeName = qt.NiceTypeName;
        this.queryTokenType = GetQueryTokenType(qt);
        this.isGroupable = qt.IsGroupable;
        this.autoExpand = qt.AutoExpand;
        this.hideInAutoExpand = qt.HideInAutoExpand;
        this.hasOrderAdapter = QueryUtils.OrderAdapters.Any(a => a(qt) != null);
        this.tsVectorFor = qt is PgTsVectorColumnToken tsqt ? tsqt.GetColumnsRoutes().Select(a => a.ToString()).ToList() : null;

        this.preferEquals = qt.Type == typeof(string) &&
            qt.GetPropertyRoute() is PropertyRoute pr &&
            typeof(Entity).IsAssignableFrom(pr.RootType) &&
            Schema.Current.HasSomeIndex(pr);

        this.propertyRoute = qt.GetPropertyRoute()?.ToString();
        if (withParents && qt.Parent != null)
            this.parent = new QueryTokenTS(qt.Parent, withParents);
    }

    public static QueryTokenTS WithAutoExpand(QueryToken qt, QueryDescription qd)
    {
        var qt2 = new QueryTokenTS(qt, withParents: false);
        if (qt.AutoExpand)
            qt2.subTokens = QueryUtils.SubTokens(qt, qd, SubTokensOptions.All).Select(st => WithAutoExpand(st, qd)).ToDictionaryEx(a => a.key);

        return qt2;
    }

    private static QueryTokenType? GetQueryTokenType(QueryToken qt)
    {
        if (qt is AggregateToken)
            return QueryTokenType.Aggregate;

        if (qt is CollectionElementToken)
            return QueryTokenType.Element;

        if (qt is CollectionNestedToken)
            return QueryTokenType.Nested;

        if (qt is CollectionAnyAllToken)
            return QueryTokenType.AnyOrAll;

        if (qt is CollectionToArrayToken)
            return QueryTokenType.ToArray;

        if (qt is OperationsContainerToken)
            return QueryTokenType.OperationContainer;
        
        if (qt is ManualContainerToken or ManualToken)
            return QueryTokenType.Manual;

        if (qt is StringSnippetToken)
            return QueryTokenType.Snippet;

        if (qt is TimeSeriesToken)
            return QueryTokenType.TimeSeries;

        if (qt is IndexerContainerToken)
            return QueryTokenType.IndexerContainer;

        return null;
    }

    public required string fullKey;
    public required string key;
    public required string toStr;
    public required string niceName;
    public required string niceTypeName;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public QueryTokenType? queryTokenType;
    public required TypeReferenceTS type;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FilterType? filterType;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? format;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? unit;
    public bool isGroupable;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool hasOrderAdapter;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool preferEquals;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyList<string>? tsVectorFor;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public QueryTokenTS? parent;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? propertyRoute;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool autoExpand;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool hideInAutoExpand;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, QueryTokenTS>? subTokens;
}

public enum QueryTokenType
{
    Aggregate,
    Element,
    AnyOrAll,
    OperationContainer,
    ToArray,
    Manual,
    Nested,
    Snippet,
    TimeSeries,
    IndexerContainer,
}
