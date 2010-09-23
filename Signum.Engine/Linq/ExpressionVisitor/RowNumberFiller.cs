using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.DynamicQuery;

namespace Signum.Engine.Linq
{
    internal class RowNumberFiller : DbExpressionVisitor
    {
        static internal Expression Fill(Expression expression)
        {
            return new RowNumberFiller().Visit(expression);
        }

        SourceExpression innerSource;

        protected override Expression VisitSelect(SelectExpression select)
        {
            Expression top = this.Visit(select.Top);
            SourceExpression from = this.VisitSource(select.From);
            Expression where = this.Visit(select.Where);

            SourceExpression last = innerSource;
            innerSource = from; 

            ReadOnlyCollection<ColumnDeclaration> columns = this.VisitColumnDeclarations(select.Columns);

            innerSource = last;

            ReadOnlyCollection<OrderExpression> orderBy = this.VisitOrderBy(select.OrderBy);
            ReadOnlyCollection<Expression> groupBy = this.VisitGroupBy(select.GroupBy);

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.Distinct, top, columns, from, where, orderBy, groupBy);

            return select;
        }

        protected override Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            if (rowNumber.OrderBy == null || rowNumber.OrderBy.Empty())
            {
                SelectExpression inner = (SelectExpression)innerSource;

                var cols = from cr in inner.Columns
                           select new OrderExpression(OrderType.Ascending,
                                new ColumnExpression(cr.Expression.Type, inner.Alias, cr.Name));

                return new RowNumberExpression(cols.ToReadOnly());
            }

            return base.VisitRowNumber(rowNumber);
        }
    }
}