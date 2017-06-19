using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Engine.DynamicQuery
{
    public class AutoDynamicQueryCore<T> : DynamicQueryCore<T> 
    {
        public IQueryable<T> Query { get; private set; }
        
        Dictionary<string, Meta> metas;

        public AutoDynamicQueryCore(IQueryable<T> query)
        {
            this.Query = query;

            metas = DynamicQueryCore.QueryMetadata(Query);

            StaticColumns = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
              .Select((e, i) => new ColumnDescriptionFactory(i, e.MemberInfo, metas[e.MemberInfo.Name])).ToArray();
        }

        public override ResultTable ExecuteQuery(QueryRequest request)
        {
            request.Columns.Insert(0, new _EntityColumn(EntityColumnFactory().BuildColumnDescription(), QueryName));

            DQueryable<T> query = Query
                .ToDQueryable(GetQueryDescription())
                .SelectMany(request.Multiplications)
                .Where(request.Filters)
                .OrderBy(request.Orders)
                .Select(request.Columns);

            var result = query.TryPaginate(request.Pagination);

            return result.ToResultTable(request);
        }

        public override async Task<ResultTable> ExecuteQueryAsync(QueryRequest request, CancellationToken token)
        {
            request.Columns.Insert(0, new _EntityColumn(EntityColumnFactory().BuildColumnDescription(), QueryName));

            DQueryable<T> query = Query
                .ToDQueryable(GetQueryDescription())
                .SelectMany(request.Multiplications)
                .Where(request.Filters)
                .OrderBy(request.Orders)
                .Select(request.Columns);

            var result = await query.TryPaginateAsync(request.Pagination, token);

            return result.ToResultTable(request);
        }

        public override object ExecuteQueryValue(QueryValueRequest request)
        {
            var query = Query.ToDQueryable(GetQueryDescription())
                .SelectMany(request.Multiplications)
                .Where(request.Filters);

            if (request.ValueToken == null)
                return query.Query.Count();

            if (request.ValueToken is AggregateToken)
                return query.SimpleAggregate((AggregateToken)request.ValueToken);

            return query.SelectOne(request.ValueToken).Unique(UniqueType.Single);
        }

        public override async Task<object> ExecuteQueryValueAsync(QueryValueRequest request, CancellationToken token)
        {
            var query = Query.ToDQueryable(GetQueryDescription())
                .SelectMany(request.Multiplications)
                .Where(request.Filters);
            
            if (request.ValueToken == null)
                return await query.Query.CountAsync(token);

            if (request.ValueToken is AggregateToken)
                return await query.SimpleAggregateAsync((AggregateToken)request.ValueToken, token);

            return await query.SelectOne(request.ValueToken).UniqueAsync(UniqueType.Single, token);
        }

        public override Lite<Entity> ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            throw new NotImplementedException();
        }

        public override async Task<Lite<Entity>> ExecuteUniqueEntityAsync(UniqueEntityRequest request, CancellationToken token)
        {
            var ex = new _EntityColumn(EntityColumnFactory().BuildColumnDescription(), QueryName);

            DQueryable<T> orderQuery = Query
                .ToDQueryable(GetQueryDescription())
                .SelectMany(request.Multiplications)
                .Where(request.Filters)
                .OrderBy(request.Orders);

            var result = await orderQuery
                .SelectOne(ex.Token)
                .UniqueAsync(request.UniqueType, token);

            return (Lite<Entity>)result;
        }


        

        public override ResultTable ExecuteQueryGroup(QueryGroupRequest request)
        {
            throw new NotImplementedException();
        }


        public override async Task<ResultTable> ExecuteQueryGroupAsync(QueryGroupRequest request, CancellationToken token)
        {
            var simpleFilters = request.Filters.Where(f => !(f.Token is AggregateToken)).ToList();
            var aggregateFilters = request.Filters.Where(f => f.Token is AggregateToken).ToList();

            var keys = request.Columns.Select(t => t.Token).Where(t => !(t is AggregateToken)).ToHashSet();

            var allAggregates = request.AllTokens().OfType<AggregateToken>().ToHashSet();

            DQueryable<T> query = Query
                .ToDQueryable(GetQueryDescription())
                .SelectMany(request.Multiplications)
                .Where(simpleFilters)
                .GroupBy(keys, allAggregates)
                .Where(aggregateFilters)
                .OrderBy(request.Orders);

            var cols = request.Columns
                .Select(column => (column, Expression.Lambda(column.Token.BuildExpression(query.Context), query.Context.Parameter))).ToList();

            var values = await query.Query.ToArrayAsync();

            return values.ToResultTable(cols, values.Length, new Pagination.All());
        }

        

        public override IQueryable<Lite<Entity>> GetEntities(QueryEntitiesRequest request)
        {
            var ex = new _EntityColumn(EntityColumnFactory().BuildColumnDescription(), QueryName);

            DQueryable<T> query = Query
             .ToDQueryable(GetQueryDescription())
             .OrderBy(request.Orders)
             .Where(request.Filters)
             .Select(new List<Column> { ex });

            var exp = Expression.Lambda<Func<object, Lite<Entity>>>(Expression.Convert(ex.Token.BuildExpression(query.Context), typeof(Lite<Entity>)), query.Context.Parameter);

            var result = query.Query.Select(exp);

            return result.TryTake(request.Count);
        }

        

        public override Expression Expression
        {
            get { return Query.Expression; }
        }
    }
}
