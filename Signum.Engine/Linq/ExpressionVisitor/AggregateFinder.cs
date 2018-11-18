using System.Linq.Expressions;

namespace Signum.Engine.Linq
{
    /// <summary>
    ///  returns the set of all aliases produced by a query source
    /// </summary>
    internal class AggregateFinder : DbExpressionVisitor
    {
        bool hasAggregates = false;

        private AggregateFinder() { }

        public static bool HasAggregates(Expression source)
        {
            AggregateFinder ap = new AggregateFinder();
            ap.Visit(source);
            return ap.hasAggregates;
        }

        protected internal override Expression VisitAggregate(AggregateExpression aggregate)
        {
            hasAggregates = true;
            return base.VisitAggregate(aggregate);
        }
    }
}
