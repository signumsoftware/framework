using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Entities;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Engine.DynamicQuery
{
    public class ManualDynamicQuery<T> : DynamicQuery<T>
    {
        public Func<QueryRequest, List<ColumnDescription>, DEnumerableCount<T>> Execute { get; private set; }

        public ManualDynamicQuery(Func<QueryRequest, List<ColumnDescription>, DEnumerableCount<T>> execute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            this.Execute = execute;

            InitializeColumns(mi => null);
        }

        public override ResultTable ExecuteQuery(QueryRequest request)
        {
            request.Columns.Insert(0, new _EntityColumn(EntityColumn().BuildColumnDescription()));

            DEnumerableCount<T> manualResult = Execute(request, GetColumnDescriptions());

            return manualResult.ToResultTable(request); 
        }

        public override int ExecuteQueryCount(QueryCountRequest request)
        {
            var req = new QueryRequest
            {
                QueryName = request.QueryName,
                Filters = request.Filters,
                Columns = new List<Column>() { new Column(this.EntityColumn().BuildColumnDescription()) },
                Orders = new List<Order>(),
                ElementsPerPage = QueryRequest.AllElements,
            };

            return Execute(req, GetColumnDescriptions()).Collection.Count();
        }

        public override Lite ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            var req = new QueryRequest
            {
                QueryName = request.QueryName,
                Filters = request.Filters,
                ElementsPerPage = 2,
                Orders = request.Orders,
                Columns = new List<Column> { new Column(this.EntityColumn().BuildColumnDescription()) }
            };

            DEnumerable<T> mr = Execute(req, GetColumnDescriptions());

            ParameterExpression pe = Expression.Parameter(typeof(object), "p");
            Func<object, Lite> entitySelector = Expression.Lambda<Func<object, Lite>>(TupleReflection.TupleChainProperty(pe, 0), pe).Compile();

            return mr.Collection.Select(entitySelector).Unique(request.UniqueType);
        }
    }
}
