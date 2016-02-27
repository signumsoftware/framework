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

namespace Signum.React.ApiControllers
{
    public class QueryController : ApiController
    {
        [Route("api/query/findLiteLike"), HttpGet]
        public List<Lite<Entity>> FindLiteLike([FromUri]string types, [FromUri]string subString, [FromUri]int count)
        {
            Implementations implementations = ParseImplementations(types);

            return AutocompleteUtils.FindLiteLike(implementations, subString, count);
        }

        [Route("api/query/findAllLites"), HttpGet]
        public List<Lite<Entity>> FindAllLites([FromUri]string types)
        {
            Implementations implementations = ParseImplementations(types);

            return AutocompleteUtils.FindAllLite(implementations);
        }

        private static Implementations ParseImplementations(string types)
        {
            return Implementations.By(types.Split(',').Select(a => TypeLogic.GetType(a.Trim())).ToArray());
        }

        [Route("api/query/description/{queryName}")]
        public QueryDescriptionTS GetQueryDescription(string queryName)
        {
            var qn = QueryLogic.ToQueryName(queryName);
            return new QueryDescriptionTS(DynamicQueryManager.Current.QueryDescription(qn));
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

        [Route("api/query/search"), HttpPost]
        public ResultTable Search(QueryRequestTS request)
        {
            var resultTable = DynamicQueryManager.Current.ExecuteQuery(request.ToQueryRequest());

            return resultTable;
        }
    }

    public class QueryRequestTS
    {
        public string queryKey;
        public List<FilterTS> filters;
        public List<OrderTS> orders;
        public List<ColumnTS> columns;
        public PaginationTS pagination;

        public QueryRequest ToQueryRequest()
        {
            var qn = QueryLogic.ToQueryName(this.queryKey);
            var qd = DynamicQueryManager.Current.QueryDescription(qn);

            return new QueryRequest
            {
                QueryName = qn,
                Filters = this.filters.EmptyIfNull().Select(f => f.ToFilter(qd, canAggregate: false)).ToList(),
                Orders = this.orders.EmptyIfNull().Select(f => f.ToOrder(qd)).ToList(),
                Columns = this.columns.EmptyIfNull().Select(f => f.ToColumn(qd)).ToList(),
                Pagination = this.pagination.ToPagination()
            };
        }
    }

    public class OrderTS
    {
        public string token;
        public OrderType orderType;

        public Order ToOrder(QueryDescription qd)
        {
            return new Order(QueryUtils.Parse(this.token, qd, SubTokensOptions.CanElement), orderType);
        }
    }

    public class FilterTS
    {
        public string token;
        public FilterOperation operation;
        public object value;

        internal Filter ToFilter(QueryDescription qd, bool canAggregate)
        {
            var options = SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | (canAggregate ? SubTokensOptions.CanAggregate : 0);
            var parsedToken = QueryUtils.Parse(token, qd, options);
            var expectedValueType = operation.IsList() ? typeof(IEnumerable<>).MakeGenericType(parsedToken.Type.Nullify()) : parsedToken.Type;
            
            var val = value is JToken ?
                 ((JToken)value).ToObject(expectedValueType, JsonSerializer.Create(GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings)) :
                 value;

            return new Filter(parsedToken, operation, val);
        }
    }

    public class ColumnTS
    {
        public string token;
        public string dispayName;

        internal Column ToColumn(QueryDescription qd)
        {
            return new Column(QueryUtils.Parse(token, qd, SubTokensOptions.CanElement), dispayName);
        }
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

    public class QueryDescriptionTS 
    {
        public string queryKey;
        public Dictionary<string, ColumnDescriptionTS> columns;

        public QueryDescriptionTS(QueryDescription queryDescription)
        {
            this.queryKey = QueryUtils.GetKey(queryDescription.QueryName);
            this.columns = queryDescription.Columns.ToDictionary(a => a.Name, a => new ColumnDescriptionTS(a, queryDescription.QueryName));
        }
    }

    public class ColumnDescriptionTS
    {
        public string name;
        public TypeReferenceTS type;
        public string typeColor; 
        public string niceTypeName; 
        public FilterType filterType;
        public string unit;
        public string format;
        public string displayName; 

        public ColumnDescriptionTS(ColumnDescription a, object queryName)
        {
            var token = new ColumnToken(a, queryName);

            this.name = a.Name;
            this.type = new TypeReferenceTS(a.Type, a.Implementations);
            this.filterType = QueryUtils.GetFilterType(a.Type);
            this.typeColor = token.TypeColor;
            this.niceTypeName = token.NiceTypeName;
            this.unit = a.Unit;
            this.format = a.Format;
            this.displayName = a.DisplayName;
        }
    }
    
    public class QueryTokenTS
    {
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
            if (recursive && qt.Parent != null)
                this.parent = new QueryTokenTS(qt.Parent, recursive);
        }

        private QueryTokenType? GetQueryTokenType(QueryToken qt)
        {
            if (qt is AggregateToken)
                return QueryTokenType.Aggregate;

            var ce = qt as CollectionElementToken;
            if (ce != null)
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
        public QueryTokenTS parent; 
    }

    public enum QueryTokenType
    {
        Aggregate,
        Element,
        AnyOrAll, 
    }
}