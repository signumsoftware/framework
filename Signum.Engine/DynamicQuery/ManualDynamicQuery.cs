using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Entities;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Threading.Tasks;
using System.Threading;

namespace Signum.Engine.DynamicQuery
{
    public class ManualDynamicQueryCore<T> : DynamicQueryCore<T>
    {
        public Func<QueryRequest, QueryDescription, CancellationToken, Task<DEnumerableCount<T>>> Execute { get; private set; }


        public ManualDynamicQueryCore(Func<QueryRequest, QueryDescription, CancellationToken, Task<DEnumerableCount<T>>> execute)
        {
            this.Execute = execute ?? throw new ArgumentNullException("execute");

            this.StaticColumns = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
              .Select((e, i) => new ColumnDescriptionFactory(i, e.MemberInfo, null)).ToArray();
        }


        public override ResultTable ExecuteQuery(QueryRequest request) => Task.Run(() => ExecuteQueryAsync(request, CancellationToken.None)).Result;
        public override async Task<ResultTable> ExecuteQueryAsync(QueryRequest request, CancellationToken cancellationToken)
        {
            request.Columns.Insert(0, new _EntityColumn(EntityColumnFactory().BuildColumnDescription(), QueryName));

            DEnumerableCount<T> manualResult = await Execute(request, GetQueryDescription(), cancellationToken);

            return manualResult.ToResultTable(request); 
        }

        public override ResultTable ExecuteQueryGroup(QueryRequest request) => Task.Run(() => ExecuteQueryGroupAsync(request, CancellationToken.None)).Result;
        public override async Task<ResultTable> ExecuteQueryGroupAsync(QueryRequest request, CancellationToken cancellationToken)
        {
            var simpleFilters = request.Filters.Where(f => !(f.Token is AggregateToken)).ToList();
            var aggregateFilters = request.Filters.Where(f => f.Token is AggregateToken).ToList();

            var keys = request.Columns.Select(t => t.Token).Where(t => !(t is AggregateToken)).ToHashSet();

            var allAggregates = request.AllTokens().OfType<AggregateToken>().ToHashSet();

            var qr = new QueryRequest
            {
                Columns = keys.Concat(allAggregates.Select(at => at.Parent).NotNull()).Distinct().Select(t => new Column(t, t.NiceName())).ToList(),
                Orders = new List<Order>(),
                Filters = simpleFilters,
                QueryName = request.QueryName,
                Pagination = new Pagination.All(),
            };

            DEnumerableCount<T> plainCollection = await Execute(qr, GetQueryDescription(), cancellationToken);

            var groupCollection = plainCollection
                     .GroupBy(keys, allAggregates)
                     .Where(aggregateFilters)
                     .OrderBy(request.Orders);

            var cols = groupCollection.TryPaginate(request.Pagination);

            return cols.ToResultTable(request);
        }

        public override object ExecuteQueryValue(QueryValueRequest request) => Task.Run(() => ExecuteQueryValueAsync(request, CancellationToken.None));
        public override async Task<object> ExecuteQueryValueAsync(QueryValueRequest request, CancellationToken cancellationToken)
        {
            var req = new QueryRequest
            {
                QueryName = request.QueryName,
                Filters = request.Filters,
                Columns = new List<Column>(),
                Orders = new List<Order>(),
                Pagination = new Pagination.All(),
            };

            if (request.ValueToken == null || request.ValueToken is AggregateToken && ((AggregateToken)request.ValueToken).AggregateFunction == AggregateFunction.Count)
            {
                req.Pagination = new Pagination.Paginate(1, 1);
                req.Columns.Add(new Column(this.EntityColumnFactory().BuildColumnDescription(), QueryName));
                var result = await Execute(req, GetQueryDescription(), cancellationToken);
                return result.TotalElements.Value;
            }

            else if (request.ValueToken is AggregateToken)
            {
                var parent = request.ValueToken.Parent;
                req.Columns.Add(new Column(parent, parent.NiceName()));
                var result = await Execute(req, GetQueryDescription(), cancellationToken);
                return result.SimpleAggregate((AggregateToken)request.ValueToken);
            }
            else
            {
                req.Columns.Add(new Column(request.ValueToken, request.ValueToken.NiceName()));
                var result = await Execute(req, GetQueryDescription(), cancellationToken);
                return result.SelectOne(request.ValueToken).Unique(UniqueType.Single);
            }
        }

    

        public override Lite<Entity> ExecuteUniqueEntity(UniqueEntityRequest request) => Task.Run(() => ExecuteUniqueEntityAsync(request, CancellationToken.None)).Result;
        public override async Task<Lite<Entity>> ExecuteUniqueEntityAsync(UniqueEntityRequest request, CancellationToken cancellationToken)
        {
            var req = new QueryRequest
            {
                QueryName = request.QueryName,
                Filters = request.Filters,
                Orders = request.Orders,
                Columns = new List<Column> { new Column(this.EntityColumnFactory().BuildColumnDescription(), QueryName) },
                Pagination = new Pagination.Firsts(2),
            };

            DEnumerable<T> mr = await Execute(req, GetQueryDescription(), cancellationToken);

            return (Lite<Entity>)mr.Collection.Select(entitySelector.Value).Unique(request.UniqueType);
        }

        static readonly Lazy<Func<object, Lite<IEntity>>> entitySelector = new Lazy<Func<object, Lite<IEntity>>>(() =>
        {
            ParameterExpression pe = Expression.Parameter(typeof(object), "p");
            return  Expression.Lambda<Func<object, Lite<IEntity>>>(TupleReflection.TupleChainProperty(pe, 0), pe).Compile();
        }, true);
        
        public override IQueryable<Lite<Entity>> GetEntities(QueryEntitiesRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
