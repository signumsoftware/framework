using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.React.Facades;
using Signum.Utilities;
using Signum.Entities;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Signum.Entities.Basics;
using Signum.Engine;
using Signum.React.Filters;
using System.Collections.ObjectModel;
using Signum.Engine.Maps;
using System.Threading;
using System.Threading.Tasks;
using Signum.Utilities.ExpressionTrees;

namespace Signum.React.ApiControllers
{
    public class QueryController : ApiController
    {
        [Route("api/query/findLiteLike"), HttpGet, ProfilerActionSplitter("types")]
        public async Task<List<Lite<Entity>>> FindLiteLike(string types, string subString, int count, CancellationToken token)
        {
            Implementations implementations = ParseImplementations(types);

            return await AutocompleteUtils.FindLiteLikeAsync(implementations, subString, count, token);
        }

        [Route("api/query/findLiteLikeWithFilters"), HttpPost, ProfilerActionSplitter("types")]
        public async Task<List<Lite<Entity>>> FindLiteLikeWithFilters(AutocompleteQueryRequestTS request, CancellationToken token)
        {
            var qn = QueryLogic.ToQueryName(request.queryKey);
            var qd = DynamicQueryManager.Current.QueryDescription(qn);

            var entitiesQuery = DynamicQueryManager.Current.GetEntities(new QueryEntitiesRequest
            {
                QueryName = qn,
                Filters = request.filters.EmptyIfNull().Select(a => a.ToFilter(qd, false)).ToList(),
                Orders = request.orders.EmptyIfNull().Select(a=>a.ToOrder(qd, false)).ToList()
            });
            var entityType = qd.Columns.Single(a => a.IsEntity).Implementations.Value.Types.SingleEx();

            return await entitiesQuery.AutocompleteUntypedAsync(request.subString, request.count, entityType, token);
        }

        [Route("api/query/allLites"), HttpGet, ProfilerActionSplitter("types")]
        public async Task<List<Lite<Entity>>> FetchAllLites(string types, CancellationToken token)
        {
            Implementations implementations = ParseImplementations(types);

            return await AutocompleteUtils.FindAllLiteAsync(implementations, token);
        }

        private static Implementations ParseImplementations(string types)
        {
            return Implementations.By(types.Split(',').Select(a => TypeLogic.GetType(a.Trim())).ToArray());
        }

        [Route("api/query/description/{queryName}"), ProfilerActionSplitter("queryName")]
        public QueryDescriptionTS GetQueryDescription(string queryName)
        {
            var qn = QueryLogic.ToQueryName(queryName);
            return new QueryDescriptionTS(DynamicQueryManager.Current.QueryDescription(qn));
        }

        [Route("api/query/queryEntity/{queryName}"), ProfilerActionSplitter("queryName")]
        public QueryEntity GetQueryEntity(string queryName)
        {
            var qn = QueryLogic.ToQueryName(queryName);
            return QueryLogic.GetQueryEntity(qn);
        }

        [Route("api/query/parseTokens"), HttpPost]
        public List<QueryTokenTS> ParseTokens(ParseTokensRequest request)
        {
            var qn = QueryLogic.ToQueryName(request.queryKey);
            var qd = DynamicQueryManager.Current.QueryDescription(qn);

            var tokens = request.tokens.Select(tr => QueryUtils.Parse(tr.token, qd, tr.options)).ToList();

            return tokens.Select(qt => new QueryTokenTS(qt, recursive: true)).ToList();
        }

        public class TokenRequest
        {
            public string token;
            public SubTokensOptions options;
        }

        public class ParseTokensRequest
        {
            public string queryKey;
            public List<TokenRequest> tokens;

        }

        [Route("api/query/subTokens"), HttpPost]
        public List<QueryTokenTS> SubTokens(SubTokensRequest request)
        {
            var qn = QueryLogic.ToQueryName(request.queryKey);
            var qd = DynamicQueryManager.Current.QueryDescription(qn);

            var token = request.token == null ? null: QueryUtils.Parse(request.token, qd, request.options);


            var tokens = QueryUtils.SubTokens(token, qd, request.options);

            return tokens.Select(qt => new QueryTokenTS(qt, recursive: false)).ToList();
        }

        public class SubTokensRequest
        {
            public string queryKey;
            public string token;
            public SubTokensOptions options;
        }

        [Route("api/query/executeQuery"), HttpPost, ProfilerActionSplitter]
        public async Task<ResultTable> ExecuteQuery(QueryRequestTS request, CancellationToken token)
        {
            var result = await DynamicQueryManager.Current.ExecuteQueryAsync(request.ToQueryRequest(), token);
            return result;
        }

        [Route("api/query/entitiesWithFilter"), HttpPost, ProfilerActionSplitter]
        public async Task<List<Lite<Entity>>> GetEntitiesWithFilter(QueryEntitiesRequestTS request, CancellationToken token)
        {
            var qn = QueryLogic.ToQueryName(request.queryKey);
            var qd = DynamicQueryManager.Current.QueryDescription(qn);
            
            return await DynamicQueryManager.Current.GetEntities(new QueryEntitiesRequest
            {
                QueryName = qn,
                Count = request.count,
                Filters = request.filters.EmptyIfNull().Select(f => f.ToFilter(qd, canAggregate: false)).ToList(),
                Orders = request.orders.EmptyIfNull().Select(f => f.ToOrder(qd, canAggregate: false)).ToList(),
            }).ToListAsync();
        }

        [Route("api/query/queryCount"), HttpPost, ProfilerActionSplitter]
        public async Task<object> QueryCount(QueryValueRequestTS request, CancellationToken token)
        {
            return await DynamicQueryManager.Current.ExecuteQueryCountAsync(request.ToQueryCountRequest(), token);
        }
    }

    public class QueryValueRequestTS
    {
        public string querykey;
        public List<FilterTS> filters;
        public string valueToken;

        public QueryValueRequest ToQueryCountRequest()
        {
            var qn = QueryLogic.ToQueryName(this.querykey);
            var qd = DynamicQueryManager.Current.QueryDescription(qn);

            var value = valueToken.HasText() ? QueryUtils.Parse(valueToken, qd, SubTokensOptions.CanAggregate | SubTokensOptions.CanElement) : null;

            return new QueryValueRequest
            {
                QueryName = qn,
                Filters = this.filters.EmptyIfNull().Select(f => f.ToFilter(qd, canAggregate: false)).ToList(),
                ValueToken = value,
            };
        }

        public override string ToString() => querykey;
    }

    public class AutocompleteQueryRequestTS
    {
        public string queryKey;
        public List<FilterTS> filters;
        public List<OrderTS> orders;
        public string subString;
        public int count;
    }


    public class QueryRequestTS
    {
        public string queryKey;
        public bool groupResults;
        public List<FilterTS> filters;
        public List<OrderTS> orders;
        public List<ColumnTS> columns;
        public PaginationTS pagination;
        public SystemTimeTS/*?*/ systemTime;

        public QueryRequest ToQueryRequest()
        {
            var qn = QueryLogic.ToQueryName(this.queryKey);
            var qd = DynamicQueryManager.Current.QueryDescription(qn);

            return new QueryRequest
            {
                QueryName = qn,
                GroupResults = groupResults,
                Filters = this.filters.EmptyIfNull().Select(f => f.ToFilter(qd, canAggregate: groupResults)).ToList(),
                Orders = this.orders.EmptyIfNull().Select(f => f.ToOrder(qd, canAggregate: groupResults)).ToList(),
                Columns = this.columns.EmptyIfNull().Select(f => f.ToColumn(qd, canAggregate: groupResults)).ToList(),
                Pagination = this.pagination.ToPagination(),
                SystemTime = this.systemTime?.ToSystemTime(),
            };
        }


        public override string ToString() => queryKey;
    }

    public class QueryEntitiesRequestTS
    {
        public string queryKey;
        public List<FilterTS> filters;
        public List<OrderTS> orders;
        public int count;

        public override string ToString() => queryKey;
    }

    public class OrderTS
    {
        public string token;
        public OrderType orderType;

        public Order ToOrder(QueryDescription qd, bool canAggregate)
        {
            return new Order(QueryUtils.Parse(this.token, qd, SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0)), orderType);
        }

        public override string ToString() => $"{token} {orderType}";
    }

    public class FilterTS
    {
        public string token;
        public FilterOperation operation;
        public object value;

        public Filter ToFilter(QueryDescription qd, bool canAggregate)
        {
            var options = SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | (canAggregate ? SubTokensOptions.CanAggregate : 0);
            var parsedToken = QueryUtils.Parse(token, qd, options);
            var expectedValueType = operation.IsList() ? typeof(ObservableCollection<>).MakeGenericType(parsedToken.Type.Nullify()) : parsedToken.Type;
            
            var val = value is JToken ?
                 ((JToken)value).ToObject(expectedValueType, JsonSerializer.Create(GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings)) :
                 value;

            return new Filter(parsedToken, operation, val);
        }

        public override string ToString() => $"{token} {operation} {value}";
    }

    public class ColumnTS
    {
        public string token;
        public string displayName;

        public Column ToColumn(QueryDescription qd, bool canAggregate)
        {
            var queryToken = QueryUtils.Parse(token, qd, SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0));

            return new Column(queryToken, displayName ?? queryToken.NiceName());
        }

        public override string ToString() => $"{token} '{displayName}'";

    }

    public class PaginationTS
    {
        public PaginationMode mode;
        public int? elementsPerPage;
        public int? currentPage;

        public PaginationTS() { }

        public PaginationTS(Pagination pagination)
        {
            this.mode = pagination.GetMode();
            this.elementsPerPage = pagination.GetElementsPerPage();
            this.currentPage = (pagination as Pagination.Paginate)?.CurrentPage;
        }

        public override string ToString() => $"{mode} {elementsPerPage} {currentPage}";


        public Pagination ToPagination()
        {
            switch (mode)
            {
                case PaginationMode.All: return new Pagination.All();
                case PaginationMode.Firsts: return new Pagination.Firsts(this.elementsPerPage.Value);
                case PaginationMode.Paginate: return new Pagination.Paginate(this.elementsPerPage.Value, this.currentPage.Value);
                default:throw new InvalidOperationException($"Unexpected {mode}");
            }
        }
    }

    public class SystemTimeTS
    {
        public SystemTimeMode mode;
        public DateTime? startDate;
        public DateTime? endDate;

        public SystemTimeTS() { }

        public SystemTimeTS(SystemTime systemTime)
        {
            if (systemTime is SystemTime.AsOf asOf)
            {
                mode = SystemTimeMode.AsOf;
                startDate = asOf.DateTime;
            }
            else if (systemTime is SystemTime.Between between)
            {
                mode = SystemTimeMode.Between;
                startDate = between.StartDateTime;
                endDate = between.EndtDateTime;
            }
            else if (systemTime is SystemTime.FromTo fromTo)
            {
                mode = SystemTimeMode.Between; //Same
                startDate = fromTo.StartDateTime;
                endDate = fromTo.EndtDateTime;
            }
            else if (systemTime is SystemTime.ContainedIn containedIn)
            {
                mode = SystemTimeMode.Between; //Same
                startDate = containedIn.StartDateTime;
                endDate = containedIn.EndtDateTime;
            }
            else if (systemTime is SystemTime.All all)
            {
                mode = SystemTimeMode.All;
                startDate = null;
                endDate = null;
            }
            else
                throw new InvalidOperationException("Unexpected System Time");
        }

        public override string ToString() => $"{mode} {startDate} {endDate}";


        public SystemTime ToSystemTime()
        {
            switch (mode)
            {
                case SystemTimeMode.All: return new SystemTime.All();
                case SystemTimeMode.AsOf: return new SystemTime.AsOf(startDate.Value);
                case SystemTimeMode.Between: return new SystemTime.Between(startDate.Value, endDate.Value);
                case SystemTimeMode.ContainedIn: return new SystemTime.ContainedIn(startDate.Value, endDate.Value);
                default: throw new InvalidOperationException($"Unexpected {mode}");
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

        public static Action<QueryDescriptionTS> AddExtension;
    }

    public class ColumnDescriptionTS
    {
        public string name;
        public TypeReferenceTS type;
        public string typeColor; 
        public string niceTypeName; 
        public FilterType? filterType;
        public string unit;
        public string format;
        public string displayName;
        public bool isGroupable;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool hasOrderAdapter;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool preferEquals;
        public string propertyRoute;

        public ColumnDescriptionTS(ColumnDescription a, object queryName)
        {
            var token = new ColumnToken(a, queryName);

            this.name = a.Name;
            this.type = new TypeReferenceTS(a.Type, a.Implementations);
            this.filterType = QueryUtils.TryGetFilterType(a.Type);
            this.typeColor = token.TypeColor;
            this.niceTypeName = token.NiceTypeName;
            this.isGroupable = token.IsGroupable;
            this.hasOrderAdapter = QueryUtils.OrderAdapters.ContainsKey(token.Type);
            this.preferEquals = token.Type == typeof(string) && 
                token.GetPropertyRoute() is PropertyRoute pr &&
                typeof(Entity).IsAssignableFrom(pr.RootType) &&
                Schema.Current.HasSomeIndex(pr);
            this.unit = a.Unit;
            this.format = a.Format;
            this.displayName = a.DisplayName;
            this.propertyRoute = token.GetPropertyRoute()?.ToString();
        }
    }
    
    public class QueryTokenTS
    {
        public QueryTokenTS() { }
        public QueryTokenTS(QueryToken qt, bool recursive)
        {
            this.toString = qt.ToString();
            this.niceName = qt.NiceName();
            this.key = qt.Key;
            this.fullKey = qt.FullKey();
            this.type = new TypeReferenceTS(qt.Type, qt.GetImplementations());
            this.filterType = QueryUtils.TryGetFilterType(qt.Type);
            this.format = qt.Format;
            this.unit = qt.Unit;
            this.typeColor = qt.TypeColor;
            this.niceTypeName = qt.NiceTypeName;
            this.queryTokenType = GetQueryTokenType(qt);
            this.isGroupable = qt.IsGroupable;
            this.hasOrderAdapter = QueryUtils.OrderAdapters.ContainsKey(qt.Type);

            this.preferEquals = qt.Type == typeof(string) &&
                qt.GetPropertyRoute() is PropertyRoute pr &&
                typeof(Entity).IsAssignableFrom(pr.RootType) &&
                Schema.Current.HasSomeIndex(pr);

            this.propertyRoute = qt.GetPropertyRoute()?.ToString();
            if (recursive && qt.Parent != null)
                this.parent = new QueryTokenTS(qt.Parent, recursive);
        }

        private QueryTokenType? GetQueryTokenType(QueryToken qt)
        {
            if (qt is AggregateToken)
                return QueryTokenType.Aggregate;

            if (qt is CollectionElementToken ce)
            {
                switch (ce.CollectionElementType)
                {
                    case CollectionElementType.Element:
                    case CollectionElementType.Element2:
                    case CollectionElementType.Element3:
                        return QueryTokenType.Element;
                    default:
                        return QueryTokenType.AnyOrAll;
                }
            }

            return null;
        }

        public string toString;
        public string niceName;
        public string key;
        public string fullKey;
        public string typeColor;
        public string niceTypeName;
        public QueryTokenType? queryTokenType; 
        public TypeReferenceTS type;
        public FilterType? filterType;
        public string format;
        public string unit;
        public bool isGroupable;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool hasOrderAdapter;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool preferEquals;
        public QueryTokenTS parent;
        public string propertyRoute;
    }

    public enum QueryTokenType
    {
        Aggregate,
        Element,
        AnyOrAll, 
    }
}