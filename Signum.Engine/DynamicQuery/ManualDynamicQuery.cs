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
        }

        protected override ColumnDescriptionFactory[] InitializeColumns()
        {
            var result = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
              .Select((e, i) =>
              {
                  if (e.MemberInfo.ReturningType().IsIIdentifiable())
                      throw new InvalidOperationException("The Type of column {0} is a subtype of IIdentifiable, for Manual queries use a Lite instead".Formato(e.MemberInfo.Name));

                  return new ColumnDescriptionFactory(i, e.MemberInfo, null);
              })
            .ToArray();
            return result;
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

        public override Lite<IdentifiableEntity> ExecuteUniqueEntity(UniqueEntityRequest request)
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
            Func<object, Lite<IIdentifiable>> entitySelector = Expression.Lambda<Func<object, Lite<IIdentifiable>>>(TupleReflection.TupleChainProperty(pe, 0), pe).Compile();

            return (Lite<IdentifiableEntity>)mr.Collection.Select(entitySelector).Unique(request.UniqueType);
        }
    }
}
