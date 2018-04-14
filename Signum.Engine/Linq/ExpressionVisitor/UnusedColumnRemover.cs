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

        protected internal override Expression VisitColumn(ColumnExpression column)
        {
            allColumnsUsed.GetOrCreate(column.Alias).Add(column.Name);
            return column;
        }

        bool IsConstant(Expression exp)
        {
            return ((DbExpressionType)exp.NodeType) == DbExpressionType.SqlConstant;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            // visit column projection first
            HashSet<string> columnsUsed = allColumnsUsed.GetOrCreate(select.Alias); // a veces no se usa

            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns.Select(c =>
            {
                if (select.IsDistinct || select.IsAllAggregates ? IsConstant(c.Expression) : !columnsUsed.Contains(c.Name))
                    return null;

                var ex = Visit(c.Expression);

                return ex == c.Expression ? c : new ColumnDeclaration(c.Name, ex);
            }).NotNull().ToReadOnly();

            ReadOnlyCollection<OrderExpression> orderbys = Visit(select.OrderBy, VisitOrderBy);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<Expression> groupBy = select.GroupBy.Select(e => IsConstant(e) ? null : Visit(e)).NotNull().ToReadOnly();

            SourceExpression from = this.VisitSource(select.From);

            if (columns != select.Columns || orderbys != select.OrderBy || where != select.Where || from != select.From || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.IsDistinct, select.Top, columns, from, where, orderbys, groupBy, select.SelectOptions);

            return select;
        }

        protected internal override Expression VisitIn(InExpression @in)
        {
            if (@in.Select != null)
            {
                AddSingleColumn(@in);
            }
            return base.VisitIn(@in);
        }

        protected internal override Expression VisitScalar(ScalarExpression scalar)
        {
            AddSingleColumn(scalar);

            return base.VisitScalar(scalar);
        }

        private void AddSingleColumn(SubqueryExpression subQuery)
        {
            if (subQuery.Select.Columns.Count != 1)
                throw new InvalidOperationException("Subquery has {0} columns: {1}".FormatWith(subQuery.Select.Columns.Count, subQuery.ToString()));
            allColumnsUsed.GetOrCreate(subQuery.Select.Alias).Add(subQuery.Select.Columns[0].Name);
        }

        protected internal override Expression VisitSetOperator(SetOperatorExpression set)
        {
            HashSet<string> columnsUsed = allColumnsUsed.GetOrCreate(set.Alias); // a veces no se usa

            allColumnsUsed.GetOrCreate(set.Left.Alias).AddRange(columnsUsed);
            allColumnsUsed.GetOrCreate(set.Right.Alias).AddRange(columnsUsed);

            return base.VisitSetOperator(set);
        }

        protected internal override Expression VisitProjection(ProjectionExpression projection)
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

        protected internal override Expression VisitJoin(JoinExpression join)
        {
            if (join.JoinType == JoinType.SingleRowLeftOuterJoin)
            {
                var source = join.Right as SourceWithAliasExpression;

                var hs = allColumnsUsed.TryGetC(source.Alias);

                if (hs == null || hs.Count == 0)
                    return Visit(join.Left);
            }

            if (join.JoinType == JoinType.OuterApply ||join.JoinType == JoinType.LeftOuterJoin)
            {

                if (join.Right is SelectExpression sql && sql.IsOneRow())
                {
                    var hs = allColumnsUsed.TryGetC(sql.Alias);
                    if (hs == null || hs.Count == 0)
                        return Visit(join.Left);
                }
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

        protected internal override Expression VisitDelete(DeleteExpression delete)
        {
            var where = Visit(delete.Where);
            var source = Visit(delete.Source);
            if (source != delete.Source || where != delete.Where)
                return new DeleteExpression(delete.Table, delete.UseHistoryTable, (SourceWithAliasExpression)source, where);
            return delete;
        }

        protected internal override Expression VisitUpdate(UpdateExpression update)
        {
            var where = Visit(update.Where);
            var assigments = Visit(update.Assigments, VisitColumnAssigment);
            var source = Visit(update.Source);
            if (source != update.Source || where != update.Where || assigments != update.Assigments)
                return new UpdateExpression(update.Table, update.UseHistoryTable, (SourceWithAliasExpression)source, where, assigments);
            return update;
        }

        protected internal override Expression VisitInsertSelect(InsertSelectExpression insertSelect)
        {
            var assigments = Visit(insertSelect.Assigments, VisitColumnAssigment);
            var source = Visit(insertSelect.Source);
            if (source != insertSelect.Source || assigments != insertSelect.Assigments)
                return new InsertSelectExpression(insertSelect.Table, insertSelect.UseHistoryTable, (SourceWithAliasExpression)source, assigments);
            return insertSelect;
        }

        protected internal override Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            var orderBys = Visit(rowNumber.OrderBy, o => IsConstant(o.Expression) ? null : Visit(o.Expression).Let(e => e == o.Expression ? o : new OrderExpression(o.OrderType, e))); ;
            if (orderBys != rowNumber.OrderBy)
                return new RowNumberExpression(orderBys);
            return rowNumber;
        }

        protected internal override Expression VisitChildProjection(ChildProjectionExpression child)
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