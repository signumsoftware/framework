using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Utilities; 

namespace Signum.Engine.Linq
{
    /// <summary>
    /// Move order-bys to the outermost select
    /// </summary>
    internal class OrderByRewriter : DbExpressionVisitor
    {
        IEnumerable<OrderExpression> gatheredOrderings;
        bool isOuterMostSelect = true;

        private OrderByRewriter() { }

        static internal ProjectionExpression Rewrite(ProjectionExpression expression)
        {
            return (ProjectionExpression)new OrderByRewriter().Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            bool saveIsOuterMostSelect = this.isOuterMostSelect;
            try
            {
                this.isOuterMostSelect = false;
                select = (SelectExpression)base.VisitSelect(select);
                bool hasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;
                if (hasOrderBy)
                {
                    this.PrependOrderings(select.OrderBy);
                }
                bool canHaveOrderBy = saveIsOuterMostSelect;
                bool canPassOnOrderings = !saveIsOuterMostSelect;
                IEnumerable<OrderExpression> orderings = (canHaveOrderBy) ? this.gatheredOrderings : null;
                ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;
                if (this.gatheredOrderings != null)
                {
                    if (canPassOnOrderings)
                    {
                        HashSet<string> producedAliases = AliasGatherer.Gather(select.From);
                        // reproject order expressions using this select's alias so the outer select will have properly formed expressions
                        BindResult project = this.RebindOrderings(this.gatheredOrderings, select.Alias, producedAliases, select.Columns);
                        this.gatheredOrderings = project.Orderings;
                        columns = project.Columns;
                    }
                    else
                    {
                        this.gatheredOrderings = null;
                    }
                }
                if (orderings != select.OrderBy || columns != select.Columns)
                {
                    select = new SelectExpression(select.Alias, false, select.Top, columns, select.From, select.Where, orderings, select.GroupBy);
                }
                return select;
            }
            finally
            {
                this.isOuterMostSelect = saveIsOuterMostSelect;
            }
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            // make sure order by expressions lifted up from the left side are not lost
            // when visiting the right side
            SourceExpression left = this.VisitSource(join.Left);
            IEnumerable<OrderExpression> leftOrders = this.gatheredOrderings;
            this.gatheredOrderings = null; // start on the right with a clean slate
            SourceExpression right = this.VisitSource(join.Right);
            this.PrependOrderings(leftOrders);
            Expression condition = this.Visit(join.Condition);
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.Type, join.JoinType, left, right, condition);
            }
            return join;
        }

        /// <summary>
        /// Add a sequence of order expressions to an accumulated list, prepending so as
        /// to give precedence to the new expressions over any previous expressions
        /// </summary>
        /// <param name="newOrderings"></param>
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

        protected class BindResult
        {
            ReadOnlyCollection<ColumnDeclaration> columns;
            ReadOnlyCollection<OrderExpression> orderings;
            public BindResult(IEnumerable<ColumnDeclaration> columns, IEnumerable<OrderExpression> orderings)
            {
                this.columns = columns.ToReadOnly();
                this.orderings = orderings.ToReadOnly(); 
            }
            public ReadOnlyCollection<ColumnDeclaration> Columns
            {
                get { return this.columns; }
            }
            public ReadOnlyCollection<OrderExpression> Orderings
            {
                get { return this.orderings; }
            }
        }

        /// <summary>
        /// Rebind order expressions to reference a new alias and add to column declarations if necessary
        /// </summary>
        protected virtual BindResult RebindOrderings(IEnumerable<OrderExpression> orderings, string alias, HashSet<string> existingAliases, IEnumerable<ColumnDeclaration> existingColumns)
        {
            List<ColumnDeclaration> newColumns = null;
            List<OrderExpression> newOrderings = new List<OrderExpression>();
            foreach (OrderExpression ordering in orderings)
            {
                Expression expr = ordering.Expression;
                ColumnExpression column = expr as ColumnExpression;
                if (column == null || (existingAliases != null && existingAliases.Contains(column.Alias)))
                {
                    // check to see if a declared column already contains a similar expression
                    int iOrdinal = 0;
                    foreach (ColumnDeclaration decl in existingColumns)
                    {
                        ColumnExpression declColumn = decl.Expression as ColumnExpression;
                        if (decl.Expression == ordering.Expression ||
                            (column != null && declColumn != null && column.Alias == declColumn.Alias && column.Name == declColumn.Name))
                        {
                            // found it, so make a reference to this column
                            expr = new ColumnExpression(column.Type, alias, decl.Name);
                            break;
                        }
                        iOrdinal++;
                    }
                    // if not already projected, add a new column declaration for it
                    if (expr == ordering.Expression)
                    {
                        if (newColumns == null)
                        {
                            newColumns = new List<ColumnDeclaration>(existingColumns);
                            existingColumns = newColumns;
                        }
                        string colName = column != null ? column.Name : "c" + iOrdinal;
                        newColumns.Add(new ColumnDeclaration(colName, ordering.Expression));
                        expr = new ColumnExpression(expr.Type, alias, colName);
                    }
                    newOrderings.Add(new OrderExpression(ordering.OrderType, expr));
                }
            }
            return new BindResult(existingColumns, newOrderings);
        }
    }
}
