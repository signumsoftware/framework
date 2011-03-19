using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Engine.DynamicQuery
{
    public class AutoDynamicQuery<T> : DynamicQuery<T>
    {
        public IQueryable<T> Query { get; private set; }
        Dictionary<string, Meta> metas;

        public AutoDynamicQuery(IQueryable<T> query)
        {
            if (query == null)
                throw new ArgumentNullException("query");

            this.Query = query;

            metas = DynamicQuery.QueryMetadata(query);

            InitializeColumns(mi => metas[mi.Name]);
        }

        public override ResultTable ExecuteQuery(QueryRequest request)
        {
            request.Columns.Insert(0, new _EntityColumn(EntityColumn().BuildColumnDescription()));

            DQueryable<T> result = Query
                .Where(request.Filters)
                .SelectExpandable(request.Columns, request.Orders)
                .OrderBy(request.Orders).TryTake(request.Limit);

            DEnumerable<T> array = result.ToArray();

            return array.ToResultTable(request.Columns);
        }

        public override int ExecuteQueryCount(QueryCountRequest request)
        {
            return Query.Where(request.Filters).Count();
        }

        public override Lite ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            var columns = new List<Column> { new _EntityColumn(EntityColumn().BuildColumnDescription()) };
            DQueryable<T> orderQuery = Query.Where(request.Filters).SelectExpandable(columns, request.Orders).OrderBy(request.Orders);

            var entitySelect = GetEntitySelector(orderQuery.TupleType);

            return (Lite)orderQuery.Query.Select(entitySelect).Unique(request.UniqueType);
        }

        private static Expression<Func<object, Lite>> GetEntitySelector(Type tupleType)
        {
            ParameterExpression p = Expression.Parameter(typeof(object), "p");
            return Expression.Lambda<Func<object, Lite>>(
                Expression.Convert(
                    TupleReflection.TupleChainProperty(Expression.Convert(p, tupleType), 0),
                typeof(Lite)), p);
        }

        public override Expression Expression
        {
            get { return Query.Expression; }
        }
    }
}
