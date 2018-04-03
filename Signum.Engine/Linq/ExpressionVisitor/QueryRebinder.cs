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
using System.Diagnostics;
using Signum.Utilities.DataStructures;

namespace Signum.Engine.Linq
{
    internal class QueryRebinder : DbExpressionVisitor
    {
        ImmutableStack<Dictionary<ColumnExpression, ColumnExpression>> scopes = ImmutableStack<Dictionary<ColumnExpression, ColumnExpression>>.Empty;

        public Dictionary<ColumnExpression, ColumnExpression> CurrentScope { get { return scopes.Peek(); } }

        private QueryRebinder() { }


        internal class ColumnCollector : DbExpressionVisitor
        {
            internal Alias[] knownAliases;
            internal Dictionary<ColumnExpression, ColumnExpression> currentScope;
            
            protected internal override Expression VisitColumn(ColumnExpression column)
            {
                if (knownAliases.Contains(column.Alias))
                    currentScope[column] = null;

                return base.VisitColumn(column);
            }
        }

        ColumnCollector cachedCollector = new ColumnCollector();
        public ColumnCollector GetColumnCollector(Alias[] knownAliases)
        {
            cachedCollector.currentScope = CurrentScope;
            cachedCollector.knownAliases = knownAliases;
            return cachedCollector;
        }

        internal static Expression Rebind(Expression binded)
        {
            QueryRebinder qr = new QueryRebinder();
            using (qr.NewScope())
            {
                return qr.Visit(binded);
            }
        }

        protected internal override Expression VisitProjection(ProjectionExpression proj)
        {
            using (HeavyProfiler.LogNoStackTrace(nameof(VisitProjection), () => proj.Type.TypeName()))
            {
                GetColumnCollector(proj.Select.KnownAliases).Visit(proj.Projector);

                SelectExpression source = (SelectExpression)this.Visit(proj.Select);
                Expression projector = this.Visit(proj.Projector);
                CurrentScope.RemoveAll(kvp =>
                {
                    if (source.KnownAliases.Contains(kvp.Key.Alias))
                    {
                        if (kvp.Value == null)
                            throw new InvalidOperationException();

                        return true;
                    }

                    return false;
                });

                if (source != proj.Select || projector != proj.Projector)
                {
                    return new ProjectionExpression(source, projector, proj.UniqueFunction, proj.Type);
                }
                return proj;
            }
        }

        protected internal override Expression VisitTable(TableExpression table)
        {
            var columns = CurrentScope.Keys.Where(ce => ce.Alias == table.Alias).ToList();

            CurrentScope.SetRange(columns, columns);

            return table;
        }

        protected internal override Expression VisitSqlTableValuedFunction(SqlTableValuedFunctionExpression sqlFunction)
        {
            var columns = CurrentScope.Keys.Where(ce => ce.Alias == sqlFunction.Alias).ToList();

            CurrentScope.SetRange(columns, columns);

            ReadOnlyCollection<Expression> args = Visit(sqlFunction.Arguments);
            if (args != sqlFunction.Arguments)
                return new SqlTableValuedFunctionExpression(sqlFunction.SqlFunction, sqlFunction.Table, sqlFunction.Alias, args);
            return sqlFunction;
        }

        protected internal override Expression VisitJoin(JoinExpression join)
        {
            if (join.Condition != null)
                GetColumnCollector(join.KnownAliases).Visit(join.Condition);
            else if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply)
                GetColumnCollector(join.Left.KnownAliases).Visit(join.Right);
            
            SourceExpression left = this.VisitSource(join.Left);
            SourceExpression right = this.VisitSource(join.Right);
            Expression condition = this.Visit(join.Condition);
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.JoinType, left, right, condition);
            }
            return join;
        }

        protected internal override Expression VisitSetOperator(SetOperatorExpression set)
        {
            List<ColumnExpression> askedColumns = CurrentScope.Keys.Where(k => k.Alias == set.Alias).ToList();

            SourceWithAliasExpression left = VisitSetOperatorPart(set.Left, askedColumns);
            SourceWithAliasExpression right = VisitSetOperatorPart(set.Right, askedColumns);

            CurrentScope.SetRange(askedColumns, askedColumns);

            if (left != set.Left || right != set.Right)
                return new SetOperatorExpression(set.Operator, left, right, set.Alias);

            return set;
        }

        private SourceWithAliasExpression VisitSetOperatorPart(SourceWithAliasExpression part, List<ColumnExpression> askedColumns)
        {
            using (NewScope())
            {
                CurrentScope.AddRange(askedColumns.ToDictionary(c => new ColumnExpression(c.Type, part.Alias, c.Name), c => (ColumnExpression)null));
                return (SourceWithAliasExpression)Visit(part);
            }
        }

        protected internal override Expression VisitDelete(DeleteExpression delete)
        {
            GetColumnCollector(delete.Source.KnownAliases).Visit(delete.Where);

            var source = Visit(delete.Source);
            var where = Visit(delete.Where);

            if (source != delete.Source || where != delete.Where)
                return new DeleteExpression(delete.Table, delete.UseHistoryTable, (SourceWithAliasExpression)source, where);

            return delete;
        }

        protected internal override Expression VisitUpdate(UpdateExpression update)
        {
            var coll = GetColumnCollector(update.Source.KnownAliases);
            coll.Visit(update.Where);
            foreach (var ca in update.Assigments)
                coll.Visit(ca.Expression);
            
            var source = Visit(update.Source);
            var where = Visit(update.Where);
            var assigments = Visit(update.Assigments, VisitColumnAssigment);
            if (source != update.Source || where != update.Where || assigments != update.Assigments)
                return new UpdateExpression(update.Table, update.UseHistoryTable, (SourceWithAliasExpression)source, where, assigments);

            return update;
        }

        protected internal override Expression VisitInsertSelect(InsertSelectExpression insertSelect)
        {
            var coll = GetColumnCollector(insertSelect.Source.KnownAliases);
            foreach (var ca in insertSelect.Assigments)
                coll.Visit(ca.Expression);

            var source = Visit(insertSelect.Source);
            var assigments = Visit(insertSelect.Assigments, VisitColumnAssigment);
            if (source != insertSelect.Source || assigments != insertSelect.Assigments)
                return new InsertSelectExpression(insertSelect.Table, insertSelect.UseHistoryTable, (SourceWithAliasExpression)source, assigments);

            return insertSelect;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            Dictionary<ColumnExpression, ColumnExpression> askedColumns = CurrentScope.Keys.Where(k => select.KnownAliases.Contains(k.Alias)).ToDictionary(k => k, k => (ColumnExpression)null);
            Dictionary<ColumnExpression, ColumnExpression> externalAnswers = CurrentScope.Where(kvp => !select.KnownAliases.Contains(kvp.Key.Alias) && kvp.Value != null).ToDictionary();

            var disposable = NewScope();//SCOPE START
            var scope = CurrentScope;
            scope.AddRange(askedColumns.Where(kvp => kvp.Key.Alias != select.Alias).ToDictionary());
            scope.AddRange(externalAnswers);

            var col = GetColumnCollector(select.KnownAliases);
            col.Visit(select.Top);
            col.Visit(select.Where);
            foreach (var cd in select.Columns)
                col.Visit(cd.Expression);
            foreach (var oe in select.OrderBy)
                col.Visit(oe.Expression);
            foreach (var e in select.GroupBy)
                col.Visit(e);

            SourceExpression from = this.VisitSource(select.From);
            Expression top = this.Visit(select.Top);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<OrderExpression> orderBy = Visit(select.OrderBy, VisitOrderBy);
            if (orderBy.HasItems())
                orderBy = RemoveDuplicates(orderBy);
            ReadOnlyCollection<Expression> groupBy = Visit(select.GroupBy, Visit);
            ReadOnlyCollection<ColumnDeclaration> columns = Visit(select.Columns, VisitColumnDeclaration); ;
            columns = AnswerAndExpand(columns, select.Alias, askedColumns);
            var externals = CurrentScope.Where(kvp => !select.KnownAliases.Contains(kvp.Key.Alias) && kvp.Value == null).ToDictionary();
            disposable.Dispose(); ////SCOPE END 

            CurrentScope.SetRange(externals);
            CurrentScope.SetRange(askedColumns);

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.IsDistinct, top, columns, from, where, orderBy, groupBy, select.SelectOptions);

            return select;
        }

        protected internal override Expression VisitRowNumber(RowNumberExpression rowNumber)
        {
            var orderBys = RemoveDuplicates(Visit(rowNumber.OrderBy, VisitOrderBy));
            if (orderBys != rowNumber.OrderBy)
                return new RowNumberExpression(orderBys);
            return rowNumber;
        }

        private static ReadOnlyCollection<OrderExpression> RemoveDuplicates(ReadOnlyCollection<OrderExpression> orderBy)
        {
            List<OrderExpression> result = new List<OrderExpression>();
            HashSet<Expression> used = new HashSet<Expression>();
            foreach (var ord in orderBy)
            {
                if (used.Add(ord.Expression))
                    result.Add(ord);
            }
            return result.AsReadOnly();
        }

        private ReadOnlyCollection<ColumnDeclaration> AnswerAndExpand(ReadOnlyCollection<ColumnDeclaration> columns, Alias currentAlias, Dictionary<ColumnExpression, ColumnExpression> askedColumns)
        {
            ColumnGenerator cg = new ColumnGenerator(columns);

            foreach (var col in askedColumns.Keys.ToArray())
            {
                if (col.Alias == currentAlias)
                {
                    //Expression expr = columns.SingleEx(cd => (cd.Name ?? "-") == col.Name).Expression;
                    //askedColumns[col] = expr is SqlConstantExpression ? expr : col;
                    askedColumns[col] = col;
                }
                else
                {
                    ColumnExpression colExp = CurrentScope[col];
                    //if (expr is ColumnExpression colExp)
                    //{
                    ColumnDeclaration cd = cg.Columns.FirstOrDefault(c => c.Expression.Equals(colExp));
                    if (cd == null)
                    {
                        cd = cg.MapColumn(colExp);
                    }

                    askedColumns[col] = new ColumnExpression(col.Type, currentAlias, cd.Name);
                    //}
                    //else
                    //{
                    //    askedColumns[col] = expr;
                    //}
                }
            }


            if (columns.Count != cg.Columns.Count())
                return cg.Columns.ToReadOnly();

            return columns;
        }

        public IDisposable NewScope()
        {
            scopes = scopes.Push(new Dictionary<ColumnExpression, ColumnExpression>());
            return new Disposable(() => scopes = scopes.Pop());
        }

        protected internal override Expression VisitColumn(ColumnExpression column)
        {
            if (CurrentScope.TryGetValue(column, out ColumnExpression result))
                return result ?? column;
            else
            {
                CurrentScope[column] = null;
                return column;
            }
        }

        protected internal override Expression VisitScalar(ScalarExpression scalar)
        {
            var column = scalar.Select.Columns.SingleEx();

            VisitColumn(new ColumnExpression(scalar.Type, scalar.Select.Alias, column.Name ?? "-"));

            var select = (SelectExpression)this.Visit(scalar.Select);
            if (select != scalar.Select)
                return new ScalarExpression(scalar.Type, select);
            return scalar;
        }
    }

   
}