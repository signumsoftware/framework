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

            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns.NewIfChange(VisitColumnDeclaration);

            innerSource = last;

            ReadOnlyCollection<OrderExpression> orderBy = select.OrderBy.NewIfChange(VisitOrderBy);
            ReadOnlyCollection<Expression> groupBy = select.GroupBy.NewIfChange(Visit);

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.IsDistinct, select.IsReverse, top, columns, from, where, orderBy, groupBy, select.ForXmlPathEmpty);

            return select;
        }

        protected override Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            if (rowNumber.OrderBy == null || rowNumber.OrderBy.IsEmpty())
            {
                SelectExpression inner = (SelectExpression)innerSource;

                var cols = inner.Columns.Select(cd => new OrderExpression(OrderType.Ascending, cd.GetReference(inner.Alias)));

                return new RowNumberExpression(cols.ToReadOnly());
            }

            return base.VisitRowNumber(rowNumber);
        }
    }
}