using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Signum.Engine.Linq
{
    class ScalarSubqueryRewriter : DbExpressionVisitor
    {
        Connector connector = Connector.Current;
        public static Expression Rewrite(Expression expression)
        {
            return new ScalarSubqueryRewriter().Visit(expression);
        }

        bool inAggregate = false;
        protected internal override Expression VisitAggregate(AggregateExpression aggregate)
        {
            var saveInAggregate = this.inAggregate;

            this.inAggregate = true;

            var result = base.VisitAggregate(aggregate);

            this.inAggregate = saveInAggregate;

            return result;
        }

        SourceExpression? currentFrom;
        protected internal override Expression VisitSelect(SelectExpression select)
        {
            var saveFrom = this.currentFrom;
            var saveInAggregate = this.inAggregate;

            this.inAggregate = false;

            SourceExpression from = this.VisitSource(select.From!);
            this.currentFrom = from;

            Expression? top = this.Visit(select.Top);
            Expression? where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns = Visit(select.Columns, VisitColumnDeclaration);
            ReadOnlyCollection<OrderExpression> orderBy = Visit(select.OrderBy, VisitOrderBy);
            ReadOnlyCollection<Expression> groupBy = Visit(select.GroupBy, Visit);

            from = this.currentFrom;

            this.inAggregate = saveInAggregate;
            this.currentFrom = saveFrom;

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.IsDistinct, top, columns, from, where, orderBy, groupBy, select.SelectOptions);

            return select;

        }

        protected internal override Expression VisitScalar(ScalarExpression scalar)
        {
            if (connector.SupportsScalarSubquery &&
               (!inAggregate || connector.SupportsScalarSubqueryInAggregates))
            {
                return base.VisitScalar(scalar);
            }
            else
            {
                var select = scalar.Select!;
                if (string.IsNullOrEmpty(select.Columns[0].Name))
                {
                    select = new SelectExpression(select.Alias, select.IsDistinct, select.Top,
                        new[] { new ColumnDeclaration("scalar", select.Columns[0].Expression) },
                        select.From, select.Where, select.OrderBy, select.GroupBy, select.SelectOptions);
                }
                this.currentFrom = new JoinExpression(JoinType.OuterApply, this.currentFrom!, select, null);
                return new ColumnExpression(scalar.Type, scalar.Select!.Alias, select.Columns[0].Name);
            }
        }
    }
}
