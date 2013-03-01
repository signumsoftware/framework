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

namespace Signum.Engine.DynamicQuery
{
    public class AutoDynamicQueryCore<T> : DynamicQueryCore<T>
    {
        public IQueryable<T> Query { get; private set; }
        
        Dictionary<string, Meta> metas;

        public AutoDynamicQueryCore(IQueryable<T> query)
        {
            this.Query = query;

            metas = DynamicQuery.QueryMetadata(Query);

            StaticColumns = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
              .Select((e, i) => new ColumnDescriptionFactory(i, e.MemberInfo, metas[e.MemberInfo.Name])).ToArray();
        }

        public override ResultTable ExecuteQuery(QueryRequest request)
        {
            request.Columns.Insert(0, new _EntityColumn(EntityColumn().BuildColumnDescription()));

            DQueryable<T> query = Query
                .ToDQueryable(GetColumnDescriptions())
                .SelectMany(request.Multiplications)
                .Where(request.Filters)
                .OrderBy(request.Orders)
                .Select(request.Columns);

            var result = query.TryPaginate(request.ElementsPerPage, request.CurrentPage);

            return result.ToResultTable(request);
        }

        public override int ExecuteQueryCount(QueryCountRequest request)
        {
            return Query.ToDQueryable(GetColumnDescriptions())
                .SelectMany(request.Multiplications)
                .Where(request.Filters)
                .Query
                .Count();
        }

        public override Lite<IdentifiableEntity> ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            var ex = new _EntityColumn(EntityColumn().BuildColumnDescription());

            DQueryable<T> orderQuery = Query
                .ToDQueryable(GetColumnDescriptions())
                .SelectMany(request.Multiplications)
                .Where(request.Filters)
                .OrderBy(request.Orders)
                .Select(new List<Column> { ex});

            var exp = Expression.Lambda<Func<object, Lite<IIdentifiable>>>(Expression.Convert(ex.Token.BuildExpression(orderQuery.Context), typeof(Lite<IIdentifiable>)), orderQuery.Context.Parameter);

            return (Lite<IdentifiableEntity>)orderQuery.Query.Select(exp).Unique(request.UniqueType);
        }

        public override ResultTable ExecuteQueryGroup(QueryGroupRequest request)
        {
            var simpleFilters = request.Filters.Where(f => !(f.Token is AggregateToken)).ToList();
            var aggregateFilters = request.Filters.Where(f => f.Token is AggregateToken).ToList();

            var keys = request.Columns.Select(t => t.Token).Where(t => !(t is AggregateToken)).ToHashSet();

            var allAggregates = request.AllTokens().OfType<AggregateToken>().ToHashSet();

            DQueryable<T> query = Query
                .ToDQueryable(GetColumnDescriptions())
                .SelectMany(request.Multiplications)
                .Where(request.Filters)
                .GroupBy(keys, allAggregates)
                .Where(aggregateFilters)
                .OrderBy(request.Orders);

            var cols = request.Columns
                .Select(c => Tuple.Create(c, Expression.Lambda(c.Token.BuildExpression(query.Context), query.Context.Parameter))).ToList();

            var values = query.Query.ToArray();

            return values.ToResultTable(cols, values.Length, 0, QueryRequest.AllElements);
        }


        public override Expression Expression
        {
            get { return Query.Expression; }
        }
    }
}
