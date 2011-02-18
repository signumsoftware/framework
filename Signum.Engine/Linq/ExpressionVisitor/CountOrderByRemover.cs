using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    //Removes Orderby when there's a 
    internal class CountOrderByRemover : DbExpressionVisitor
    {
        public bool ShouldRemoveOrders;

        public IDisposable SetShouldRemove(bool value)
        {
            var saved = this.ShouldRemoveOrders; 
            this.ShouldRemoveOrders = value;
            return new Disposable(() => this.ShouldRemoveOrders = saved); 
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            bool shouldRemove = ShouldRemoveOrders || IsCountSumOrAvg(select); 

            using (SetShouldRemove(false))
            {                
                Expression top = this.Visit(select.Top);
                SourceExpression from;
                using (SetShouldRemove(shouldRemove))
                    from  = this.VisitSource(select.From);

                Expression where = this.Visit(select.Where);
                ReadOnlyCollection<ColumnDeclaration> columns = this.VisitColumnDeclarations(select.Columns);

                ReadOnlyCollection<OrderExpression> orderBy;
                if (shouldRemove && select.OrderBy != null)
                    orderBy = null;
                else 
                    orderBy = this.VisitOrderBy(select.OrderBy);

                ReadOnlyCollection<Expression> groupBy = this.VisitGroupBy(select.GroupBy);

                if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                    return new SelectExpression(select.Alias, select.Distinct, top, columns, from, where, orderBy, groupBy);

                return select;
            }
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

        internal static Expression Remove(Expression expression)
        {
            return new CountOrderByRemover().Visit(expression); 
        }
    }
}
