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

namespace Signum.React.ApiControllers
{
    public class QueryController : ApiController
    {
        [Route("api/query/findLiteLike"), HttpGet]
        public List<Lite<Entity>> FindLiteLike([FromUri]string types, [FromUri]string subString, [FromUri]int count)
        {
            var implementations = Implementations.By(types.Split(',').Select(TypeLogic.GetType).ToArray());

            return AutocompleteUtils.FindLiteLike(implementations, subString, count);
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
        public ResultTableTS Search(QueryRequestTS request)
        {
            var resultTable = DynamicQueryManager.Current.ExecuteQuery(request.ToQueryRequest());

            return new ResultTableTS(resultTable);
        }
    }

    public class ResultTableTS
    {
        public string entityColumn;
        public List<string> columns;
        public PaginationTS pagination;
        public int? totalElements;
        public List<ResultRowTS> rows;

        public ResultTableTS(ResultTable resultTable)
        {
            this.pagination = new PaginationTS(resultTable.Pagination);
            this.entityColumn = "Entity";
            this.columns = resultTable.Columns.Select(c => c.Column.Token.FullKey()).ToList();
            this.rows = resultTable.Rows.Select(r => new ResultRowTS
            {
                entity = (Lite<IEntity>)r[resultTable.entityColumn],
                columns = resultTable.Columns.Select(c=>r[c]).ToList(),
            }).ToList();
            this.totalElements = resultTable.TotalElements;
        }
    }

    public class ResultRowTS
    {
        public Lite<IEntity> entity;
        public List<object> columns;
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
                Filters = this.filters.EmptyIfNull().Select(f => f.ToFilter(qd)).ToList(),
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

        internal Filter ToFilter(QueryDescription qd)
        {
            return new Filter(QueryUtils.Parse(token, qd, SubTokensOptions.CanElement), operation, value);
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

        internal Pagination ToPagination()
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
            this.columns = queryDescription.Columns.ToDictionary(a => a.Name, a => new ColumnDescriptionTS(a));
        }
    }

    public class ColumnDescriptionTS
    {
        public string name;
        public TypeReferenceTS type;
        public FilterType filterType;
        public string unit;
        public string format;
        public string displayName; 

        public ColumnDescriptionTS(ColumnDescription a)
        {
            this.name = a.Name;
            this.type = new TypeReferenceTS(a.Type, a.Implementations);
            this.filterType = QueryUtils.GetFilterType(a.Type);
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
            this.format = qt.Format;
            this.unit = qt.Unit;
            if (recursive && qt.Parent != null)
                this.parent = new QueryTokenTS(qt.Parent, recursive);
        }

        public string toString;
        public string niceName;
        public string key;
        public string fullKey;
        public TypeReferenceTS type; 
        public string format;
        public string unit;
        public QueryTokenTS parent; 
    }
}