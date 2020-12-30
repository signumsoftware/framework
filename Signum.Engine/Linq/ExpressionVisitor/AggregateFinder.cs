using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Signum.Engine.Linq
{
    /// <summary>
    ///  returns the set of all aliases produced by a query source
    /// </summary>
    internal class AggregateFinder : DbExpressionVisitor
    {
        List<AggregateExpression>? aggregates;

        private AggregateFinder() { }

        protected internal override Expression VisitAggregate(AggregateExpression aggregate)
        {
            if (aggregates == null)
                aggregates = new List<AggregateExpression>();

            aggregates.Add(aggregate);
            return base.VisitAggregate(aggregate);
        }

        public static List<AggregateExpression>? GetAggregates(ReadOnlyCollection<ColumnDeclaration> columns)
        {
            AggregateFinder ap = new AggregateFinder();
            Visit(columns, ap.VisitColumnDeclaration);
            return ap.aggregates;
        }

        protected internal override Expression VisitScalar(ScalarExpression scalar)
        {
            return scalar;
        }
    }
}
