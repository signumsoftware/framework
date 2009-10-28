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

namespace Signum.Engine.Linq
{
    internal class UnusedColumnRemover : DbExpressionVisitor
    {
        Dictionary<string, HashSet<string>> allColumnsUsed = new Dictionary<string, HashSet<string>>();

        private UnusedColumnRemover() { }

        static internal Expression Remove(Expression expression)
        {
            return new UnusedColumnRemover().Visit(expression);
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            allColumnsUsed.GetOrCreate(column.Alias).Add(column.Name);
            return column;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            // visit column projection first
            HashSet<string> columnsUsed = allColumnsUsed.GetOrCreate(select.Alias); // a veces no se usa

            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns.NewIfChange(
                c => !columnsUsed.Contains(c.Name) && !select.Distinct ? null : Visit(c.Expression).Map(ex => ex == c.Expression ? c : new ColumnDeclaration(c.Name, ex)));

            ReadOnlyCollection<OrderExpression> orderbys = this.VisitOrderBy(select.OrderBy);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<Expression> groupbys = this.VisitGroupBy(select.GroupBy);
            SourceExpression from = this.VisitSource(select.From);

            if (columns != select.Columns || orderbys != select.OrderBy || where != select.Where || from != select.From || groupbys != select.GroupBy)
            {
                return new SelectExpression(select.Alias, select.Distinct, select.Top, columns, from, where, orderbys, groupbys);
            }

            return select;
        }

        protected override Expression VisitSubquery(SubqueryExpression subquery)
        {
            if ((subquery.NodeType == (ExpressionType)DbExpressionType.Scalar ||
                subquery.NodeType == (ExpressionType)DbExpressionType.In) &&
                subquery.Select != null)
            {
                if (subquery.Select.Columns.Count != 1)
                    System.Diagnostics.Debug.Fail("subquery with {0} columns!".Formato(subquery.Select.Columns.Count));
                allColumnsUsed.GetOrCreate(subquery.Select.Alias).Add(subquery.Select.Columns[0].Name);
            }
            return base.VisitSubquery(subquery);
        }


        protected override Expression VisitLiteReference(LiteReferenceExpression lazy)
        {   
            return base.VisitLiteReference(lazy);
        }

        protected override Expression VisitProjection(ProjectionExpression projection)
        {
            // visit mapping in reverse order
            Expression projector = this.Visit(projection.Projector);
            SelectExpression source = (SelectExpression)this.Visit(projection.Source);
            if (projector != projection.Projector || source != projection.Source)
            {
                return new ProjectionExpression(source, projector, projection.UniqueFunction);
            }
            return projection;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            if (join.JoinType == JoinType.SingleRowLeftOuterJoin)
            {
                var table = (TableExpression)join.Right;

                var hs = allColumnsUsed.TryGetC(table.Alias);

                if (hs == null || hs.Count == 0)
                    return Visit(join.Left);
            }

            // visit join in reverse order
            Expression condition = this.Visit(join.Condition);
            SourceExpression right = this.VisitSource(join.Right);
            SourceExpression left = this.VisitSource(join.Left);
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.JoinType, left, right, condition);
            }
            return join;

        }
    }
}