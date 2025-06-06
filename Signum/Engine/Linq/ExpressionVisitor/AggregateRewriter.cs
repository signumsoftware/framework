
namespace Signum.Engine.Linq;

/// <summary>
/// Rewrite aggregate expressions, moving them into same select expression that has the group-by clause
/// </summary>
internal class AggregateRewriter : DbExpressionVisitor
{
    ILookup<Alias, AggregateRequestsExpression> lookup;
    Dictionary<AggregateRequestsExpression, ColumnExpression> map = new Dictionary<AggregateRequestsExpression, ColumnExpression>();

    private AggregateRewriter(Expression expr)
    {
        this.lookup = AggregateGatherer.Gather(expr).ToLookup(a => a.GroupByAlias);
    }

    public static Expression Rewrite(Expression expr)
    {
        return new AggregateRewriter(expr).Visit(expr);
    }

    protected internal override Expression VisitSelect(SelectExpression select)
    {
        select = (SelectExpression)base.VisitSelect(select);
        if (lookup.Contains(select.Alias))
        {
            List<ColumnDeclaration> aggColumns = new List<ColumnDeclaration>(select.Columns);
            foreach (AggregateRequestsExpression ae in lookup[select.Alias])
            {
                ColumnDeclaration cd = new ColumnDeclaration("agg" + aggColumns.Count, ae.Aggregate);
                this.map.Add(ae, cd.GetReference(ae.GroupByAlias));
                aggColumns.Add(cd);
            }
            return new SelectExpression(select.Alias, select.IsDistinct, select.Top, aggColumns, select.From, select.Where, select.OrderBy, select.GroupBy, select.SelectOptions);
        }
        return select;
    }

    protected internal override Expression VisitAggregateRequest(AggregateRequestsExpression aggregate)
    {
        return this.map.GetOrThrow(aggregate);
    }

    class AggregateGatherer : DbExpressionVisitor
    {
        List<AggregateRequestsExpression> aggregates = new List<AggregateRequestsExpression>();
        private AggregateGatherer()
        {
        }

        internal static List<AggregateRequestsExpression> Gather(Expression expression)
        {
            AggregateGatherer gatherer = new AggregateGatherer();
            gatherer.Visit(expression);
            return gatherer.aggregates;
        }

        protected internal override Expression VisitAggregateRequest(AggregateRequestsExpression aggregate)
        {
            this.aggregates.Add(aggregate);
            return base.VisitAggregateRequest(aggregate);
        }
    }
}
