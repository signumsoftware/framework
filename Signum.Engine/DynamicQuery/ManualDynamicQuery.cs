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
    public class ManualDynamicQueryCore<T> : DynamicQueryCore<T>
    {
        public Func<QueryRequest, List<ColumnDescription>, DEnumerableCount<T>> Execute { get; private set; }

        public ManualDynamicQueryCore(Func<QueryRequest, List<ColumnDescription>, DEnumerableCount<T>> execute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            this.Execute = execute;

            this.StaticColumns = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
              .Select((e, i) =>
              {
                  if (e.MemberInfo.ReturningType().IsIIdentifiable())
                      throw new InvalidOperationException("The Type of column {0} is a subtype of IIdentifiable, for Manual queries use a Lite instead".Formato(e.MemberInfo.Name));

                  return new ColumnDescriptionFactory(i, e.MemberInfo, null);
              })
            .ToArray();
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

            return (Lite<IdentifiableEntity>)mr.Collection.Select(entitySelector.Value).Unique(request.UniqueType);
        }

        static readonly Lazy<Func<object, Lite<IIdentifiable>>> entitySelector = new Lazy<Func<object, Lite<IIdentifiable>>>(() =>
        {
            ParameterExpression pe = Expression.Parameter(typeof(object), "p");
            return  Expression.Lambda<Func<object, Lite<IIdentifiable>>>(TupleReflection.TupleChainProperty(pe, 0), pe).Compile();
        }, true);

        public override ResultTable ExecuteQueryGroup(QueryGroupRequest request)
        {
            var simpleFilters = request.Filters.Where(f => !(f.Token is AggregateToken)).ToList();
            var aggregateFilters = request.Filters.Where(f => f.Token is AggregateToken).ToList();

            var keys = request.Columns.Select(t => t.Token).Where(t => !(t is AggregateToken)).ToHashSet();

            var allAggregates = request.AllTokens().OfType<AggregateToken>().ToHashSet();

            DEnumerableCount<T> plainCollection = Execute(new QueryRequest
            {   
                Columns = keys.Concat(allAggregates.Select(at => at.Parent).NotNull()).Distinct().Select(t => new Column(t, t.NiceName())).ToList(),
                Orders = new List<Order>(),
                Filters = simpleFilters,
                QueryName = request.QueryName,
                ElementsPerPage = QueryRequest.AllElements,
            }, GetColumnDescriptions());

            var groupCollection = plainCollection
                     .GroupBy(keys, allAggregates)
                     .Where(aggregateFilters)
                     .OrderBy(request.Orders);

            var cols = request.Columns
                .Select(c => Tuple.Create(c, Expression.Lambda(c.Token.BuildExpression(groupCollection.Context), groupCollection.Context.Parameter))).ToList();

            var values = groupCollection.Collection.ToArray();

            return values.ToResultTable(cols, values.Length, 0, QueryRequest.AllElements);
        }
    }
}
