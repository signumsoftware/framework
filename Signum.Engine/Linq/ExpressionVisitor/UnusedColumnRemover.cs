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
        Dictionary<Alias, HashSet<string>> allColumnsUsed = new Dictionary<Alias, HashSet<string>>();

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

        bool IsConstant(Expression exp)
        {
            return ((DbExpressionType)exp.NodeType) == DbExpressionType.SqlConstant;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            // visit column projection first
            HashSet<string> columnsUsed = allColumnsUsed.GetOrCreate(select.Alias); // a veces no se usa

            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns.NewIfChange(
                c =>
                {
                    if (select.IsDistinct ? IsConstant(c.Expression) : !columnsUsed.Contains(c.Name))
                        return null;

                    var ex = Visit(c.Expression);

                    return ex == c.Expression ? c : new ColumnDeclaration(c.Name, ex);
                });

            ReadOnlyCollection<OrderExpression> orderbys = select.OrderBy.NewIfChange(VisitOrderBy);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<Expression> groupbys = select.GroupBy.NewIfChange(e => IsConstant(e) ? null : Visit(e));

            SourceExpression from = this.VisitSource(select.From);

            if (columns != select.Columns || orderbys != select.OrderBy || where != select.Where || from != select.From || groupbys != select.GroupBy)
            {
                return new SelectExpression(select.Alias, select.IsDistinct, select.IsReverse, select.Top, columns, from, where, orderbys, groupbys);
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
                    throw new InvalidOperationException("Subquery with {0} columns".Formato(subquery.Select.Columns.Count));
                allColumnsUsed.GetOrCreate(subquery.Select.Alias).Add(subquery.Select.Columns[0].Name);
            }
            return base.VisitSubquery(subquery);
        }


        protected override Expression VisitLite(LiteExpression lite)
        {   
            return base.VisitLite(lite);
        }

        protected override Expression VisitProjection(ProjectionExpression projection)
        {
            // visit mapping in reverse order
            Expression projector = this.Visit(projection.Projector);
            SelectExpression source = (SelectExpression)this.Visit(projection.Select);
            if (projector != projection.Projector || source != projection.Select)
            {
                return new ProjectionExpression(source, projector, projection.UniqueFunction, projection.Type);
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

        protected override Expression VisitDelete(DeleteExpression delete)
        {
            var where = Visit(delete.Where);
            var source = Visit(delete.Source);
            if (source != delete.Source || where != delete.Where)
                return new DeleteExpression(delete.Table, (SourceExpression)source, where);
            return delete;
        }

        protected override Expression VisitUpdate(UpdateExpression update)
        {
            var where = Visit(update.Where);
            var assigments = update.Assigments.NewIfChange(VisitColumnAssigment);
            var source = Visit(update.Source);
            if (source != update.Source || where != update.Where || assigments != update.Assigments)
                return new UpdateExpression(update.Table, (SourceExpression)source, where, assigments);
            return update;
        }

        protected override Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            var orderBys = rowNumber.OrderBy.NewIfChange(o => IsConstant(o.Expression) ? null : Visit(o.Expression).Let(e => e == o.Expression ? o : new OrderExpression(o.OrderType, e))); ;
            if (orderBys != rowNumber.OrderBy)
                return new RowNumberExpression(orderBys);
            return rowNumber;
        }

        protected override Expression VisitChildProjection(ChildProjectionExpression child)
        {
            Expression key = this.Visit(child.OuterKey);
            ProjectionExpression proj = (ProjectionExpression)UnusedColumnRemover.Remove(child.Projection);
         
            if (proj != child.Projection || key != child.OuterKey)
            {
                return new ChildProjectionExpression(proj, key, child.IsLazyMList, child.Type, child.Token);
            }
            return child;
        }
    }
}