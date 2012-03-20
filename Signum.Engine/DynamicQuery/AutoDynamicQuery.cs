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
            return Query.ToDQueryable(GetColumnDescriptions()).Where(request.Filters).Query.Count();
        }

        public override Lite ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            var ex = new _EntityColumn(EntityColumn().BuildColumnDescription());

            DQueryable<T> orderQuery = Query
                .ToDQueryable(GetColumnDescriptions())
                .Where(request.Filters)
                .OrderBy(request.Orders)
                .Select(new List<Column> { ex});

            var exp = Expression.Lambda<Func<object, Lite>>(Expression.Convert(ex.Token.BuildExpression(orderQuery.Context), typeof(Lite)), orderQuery.Context.Parameter);

            return orderQuery.Query.Select(exp).Unique(request.UniqueType);
        }
        public override Expression Expression
        {
            get { return Query.Expression; }
        }
    }
}
