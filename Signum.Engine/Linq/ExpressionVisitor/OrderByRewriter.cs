using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;

namespace Signum.Engine.Linq
{
    internal class OrderByRewriter : DbExpressionVisitor
    {
        IEnumerable<OrderExpression> gatheredOrderings;
        SelectExpression outerMostSelect;

        private OrderByRewriter() { }

        static internal Expression Rewrite(Expression expression)
        {
            return new OrderByRewriter().Visit(expression);
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            var oldGatheredOrderings = gatheredOrderings;
            var oldOuterMostSelect = outerMostSelect;

            gatheredOrderings = Enumerable.Empty<OrderExpression>();
            outerMostSelect = proj.Source;

            var result = base.VisitProjection(proj);

            gatheredOrderings = oldGatheredOrderings;
            outerMostSelect = oldOuterMostSelect;

            return result;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            bool isOuterMost = select == outerMostSelect;

            select = (SelectExpression)base.VisitSelect(select);
            if (select.GroupBy != null && select.GroupBy.Any())
                gatheredOrderings = Enumerable.Empty<OrderExpression>();

            if (select.Reverse)
                gatheredOrderings = gatheredOrderings.Select(o => 
                    new OrderExpression(o.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending, o.Expression));  

            if (select.OrderBy != null && select.OrderBy.Count > 0)
                this.PrependOrderings(select.OrderBy);

            var orderings = (isOuterMost && !IsCountSumOrAvg(select)) ? gatheredOrderings : null;

            if (select.Top != null)
            {
                orderings = gatheredOrderings;
                gatheredOrderings = Enumerable.Empty<OrderExpression>();
            }

            if (AreEqual(select.OrderBy, orderings) && !select.Reverse)
                return select;

            return new SelectExpression(select.Alias, select.Distinct, false, select.Top, select.Columns, select.From, select.Where, orderings, select.GroupBy);
        }

        static bool AreEqual(IEnumerable<OrderExpression> col1, IEnumerable<OrderExpression> col2)
        {
            bool col1Empty = col1 == null || col1.IsEmpty();
            bool col2Empty = col2 == null || col2.IsEmpty();

            if (col1Empty && col2Empty)
                return true;

            if (col1Empty || col2Empty)
                return false;

            return col1 == col2;
        }

        private bool IsCountSumOrAvg(SelectExpression select)
        {
            ColumnDeclaration col = select.Columns.Only();
            if (col == null)
                return false;

            AggregateExpression exp = col.Expression as AggregateExpression;
            if (exp == null)
                return false;

            return exp.AggregateFunction == AggregateFunction.Count || exp.AggregateFunction == AggregateFunction.Sum || exp.AggregateFunction == AggregateFunction.Average;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            SourceExpression left = this.VisitSource(join.Left);

            IEnumerable<OrderExpression> leftOrders = this.gatheredOrderings;
            this.gatheredOrderings = null;

            SourceExpression right = this.VisitSource(join.Right);

            this.PrependOrderings(leftOrders);

            Expression condition = this.Visit(join.Condition);

            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.JoinType, left, right, condition);
            }
            return join;
        }

        protected void PrependOrderings(IEnumerable<OrderExpression> newOrderings)
        {
            if (newOrderings != null)
            {
                if (this.gatheredOrderings == null)
                {
                    this.gatheredOrderings = newOrderings;
                }
                else
                {
                    List<OrderExpression> list = this.gatheredOrderings as List<OrderExpression>;
                    if (list == null)
                    {
                        this.gatheredOrderings = list = new List<OrderExpression>(this.gatheredOrderings);
                    }
                    list.InsertRange(0, newOrderings);
                }
            }
        }
    }
}
