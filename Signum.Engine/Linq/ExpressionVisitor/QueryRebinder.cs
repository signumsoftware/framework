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
        ImmutableStack<Dictionary<ColumnExpression, Expression>> scopes = ImmutableStack<Dictionary<ColumnExpression, Expression>>.Empty;

        public Dictionary<ColumnExpression, Expression> CurrentScope { get { return scopes.Peek(); } }

        private QueryRebinder() { }

        internal static Expression Rebind(Expression binded)
        {
            QueryRebinder qr = new QueryRebinder();
            using (qr.NewScope())
            {
                return qr.Visit(binded); 
            }
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            this.Visit(proj.Projector);

            SelectExpression source = (SelectExpression)this.Visit(proj.Select);
            Expression projector = this.Visit(proj.Projector);

            if (source != proj.Select || projector != proj.Projector)
            {
                return new ProjectionExpression(source, projector, proj.UniqueFunction, proj.Type);
            }
            return proj;
        }

        protected override Expression VisitTable(TableExpression table)
        {
            var columns = CurrentScope.Keys.Where(ce => ce.Alias == table.Alias).ToList();

            CurrentScope.SetRange(columns, columns.Cast<Expression>());

            return table;
        }

        protected override Expression VisitSqlTableValuedFunction(SqlTableValuedFunctionExpression sqlFunction)
        {
            var columns = CurrentScope.Keys.Where(ce => ce.Alias == sqlFunction.Alias).ToList();

            CurrentScope.SetRange(columns, columns.Cast<Expression>());

            ReadOnlyCollection<Expression> args = sqlFunction.Arguments.NewIfChange(a => Visit(a));
            if (args != sqlFunction.Arguments)
                return new SqlTableValuedFunctionExpression(sqlFunction.SqlFunction, sqlFunction.Table, sqlFunction.Alias, args);
            return sqlFunction;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            if (join.Condition != null)
                this.Visit(join.Condition);
            else
                this.VisitSource(join.Right);

            SourceExpression left = this.VisitSource(join.Left);
            SourceExpression right = this.VisitSource(join.Right);
            Expression condition = this.Visit(join.Condition);
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.JoinType, left, right, condition);
            }
            return join;
        }

        protected override Expression VisitSetOperator(SetOperatorExpression set)
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
                CurrentScope.AddRange(askedColumns.ToDictionary(c => new ColumnExpression(c.Type, part.Alias, c.Name), c => (Expression)null));
                return (SourceWithAliasExpression)Visit(part);
            }
        }

        protected override Expression VisitDelete(DeleteExpression delete)
        {
            Visit(delete.Where);

            var source = Visit(delete.Source);
            var where = Visit(delete.Where);

            if (source != delete.Source || where != delete.Where)
                return new DeleteExpression(delete.Table, (SourceWithAliasExpression)source, where);

            return delete;
        }

        protected override Expression VisitUpdate(UpdateExpression update)
        {
            Visit(update.Where);
            update.Assigments.NewIfChange(VisitColumnAssigment);

            var source = Visit(update.Source);
            var where = Visit(update.Where);
            var assigments = update.Assigments.NewIfChange(VisitColumnAssigment);
            if (source != update.Source || where != update.Where || assigments != update.Assigments)
                return new UpdateExpression(update.Table, (SourceWithAliasExpression)source, where, assigments);

            return update;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            Dictionary<ColumnExpression, Expression> askedColumns = CurrentScope.Keys.Where(k => select.KnownAliases.Contains(k.Alias)).ToDictionary(k => k, k => (Expression)null);
            Dictionary<ColumnExpression, Expression> externalAnswers = CurrentScope.Where(kvp => !select.KnownAliases.Contains(kvp.Key.Alias) && kvp.Value != null).ToDictionary();

            var scope = NewScope();//SCOPE START

            CurrentScope.AddRange(askedColumns.Where(kvp => kvp.Key.Alias != select.Alias).ToDictionary());
            CurrentScope.AddRange(externalAnswers);

            this.Visit(select.Top);
            this.Visit(select.Where);
            select.Columns.NewIfChange(VisitColumnDeclaration);
            select.OrderBy.NewIfChange(VisitOrderBy);
            select.GroupBy.NewIfChange(Visit);

            SourceExpression from = this.VisitSource(select.From);

            Expression top = this.Visit(select.Top);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<OrderExpression> orderBy = select.OrderBy.NewIfChange(VisitOrderBy);
            ReadOnlyCollection<Expression> groupBy = select.GroupBy.NewIfChange(Visit);
            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns.NewIfChange(VisitColumnDeclaration); ;

            columns = AnswerAndExpand(columns, select.Alias, askedColumns);

            var externals = CurrentScope.Where(kvp => !select.KnownAliases.Contains(kvp.Key.Alias) && kvp.Value == null).ToDictionary();

            scope.Dispose(); ////SCOPE END 

            CurrentScope.SetRange(externals);
            CurrentScope.SetRange(askedColumns);

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.IsDistinct, top, columns, from, where, orderBy, groupBy, select.SelectOptions);

            return select;
        }

        private ReadOnlyCollection<ColumnDeclaration> AnswerAndExpand(ReadOnlyCollection<ColumnDeclaration> columns, Alias currentAlias, Dictionary<ColumnExpression, Expression> askedColumns)
        {
            ColumnGenerator cg = new ColumnGenerator(columns);
         
            foreach (var col in askedColumns.Keys.ToArray())
            {
                if (col.Alias == currentAlias)
                {
                    Expression expr = columns.SingleEx(cd => (cd.Name ?? "-") == col.Name).Expression;

                    askedColumns[col] = expr.NodeType == (ExpressionType)DbExpressionType.SqlConstant? expr: col;
                }
                else
                {
                    Expression expr = CurrentScope[col];
                    ColumnExpression colExp = expr as ColumnExpression;
                    if (colExp != null)
                    {
                        ColumnDeclaration cd = cg.Columns.FirstOrDefault(c => c.Expression.Equals(colExp));
                        if (cd == null)
                        {
                            cd = cg.MapColumn(colExp);
                        }

                        askedColumns[col] = new ColumnExpression(col.Type, currentAlias, cd.Name);
                    }
                    else
                    {
                        askedColumns[col] = expr;
                    }
                }
            }


            if (columns.Count != cg.Columns.Count())
                return cg.Columns.ToReadOnly();

            return columns;
        }

        private string GetUniqueColumnName(IEnumerable<ColumnDeclaration> columns, string name)
        {
            string baseName = name;
            int suffix = 1;
            while (columns.Select(c => c.Name).Contains(name))
                name = baseName + (suffix++);
            return name;
        }

        public IDisposable NewScope()
        {
            scopes = scopes.Push(new Dictionary<ColumnExpression, Expression>());
            return new Disposable(() => scopes = scopes.Pop());
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            Expression result;
            if (CurrentScope.TryGetValue(column, out result))
                return result ?? column;
            else
            {
                CurrentScope[column] = null;
                return column;
            }
        }

        protected override Expression VisitScalar(ScalarExpression scalar)
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