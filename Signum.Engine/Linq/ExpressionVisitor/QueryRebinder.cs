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

        internal static Expression Rebind(Expression binded)
        {
            QueryRebinder qr = new QueryRebinder();
            using (qr.NewScope(null))
            {
                return qr.Visit(binded); 
            }
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            this.Visit(proj.Projector);

            SelectExpression source = (SelectExpression)this.Visit(proj.Source);
            Expression projector = this.Visit(proj.Projector);

            if (source != proj.Source || projector != proj.Projector)
            {
                return new ProjectionExpression(source, projector, proj.UniqueFunction);
            }
            return proj;       
        }

        protected override Expression VisitTable(TableExpression table)
        {
            var columns = CurrentScope.Keys.Where(ce => ce.Alias == table.Alias).ToList();

            CurrentScope.SetRange(columns, columns);

            return table;
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

        protected override Expression VisitSelect(SelectExpression select)
        {
            Dictionary<ColumnExpression, ColumnExpression> askedColumns = CurrentScope.Keys.Where(k => select.KnownAliases.Contains(k.Alias)).ToDictionary(a => a, a => (ColumnExpression)null);

            var scope = NewScope(askedColumns.Keys.Where(a => a.Alias != select.Alias));

            this.Visit(select.Top);
            this.Visit(select.Where);
            this.VisitColumnDeclarations(select.Columns);
            this.VisitOrderBy(select.OrderBy);
            this.VisitGroupBy(select.GroupBy);

            SourceExpression from = this.VisitSource(select.From);

            Expression top = this.Visit(select.Top);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<OrderExpression> orderBy = this.VisitOrderBy(select.OrderBy);
            ReadOnlyCollection<Expression> groupBy = this.VisitGroupBy(select.GroupBy);
            ReadOnlyCollection<ColumnDeclaration> columns = VisitAndExpandColumns(select, askedColumns);

            var externals = CurrentScope.Extract(c => !select.KnownAliases.Contains(c.Alias));

            Debug.Assert(externals.All(kvp => kvp.Value == null));

            scope.Dispose();

            CurrentScope.SetRange(externals);
            CurrentScope.SetRange(askedColumns);

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                return new SelectExpression(select.Alias, select.Distinct, top, columns, from, where, orderBy, groupBy);

            return select;
        }

        private ReadOnlyCollection<ColumnDeclaration> VisitAndExpandColumns(SelectExpression select, Dictionary<ColumnExpression, ColumnExpression> askedColumns)
        {
            var columns = this.VisitColumnDeclarations(select.Columns);

            List<ColumnDeclaration> columnDeclarations = columns.ToList();
         
            foreach (var col in askedColumns.Keys.ToArray())
            {
                if (col.Alias == select.Alias)
                    askedColumns[col] = col;
                else
                {
                    ColumnExpression colExp = CurrentScope[col];
                    ColumnDeclaration cd = columnDeclarations.FirstOrDefault(c => c.Expression.Equals(col));
                    if (cd == null)
                    {
                        cd = new ColumnDeclaration(GetUniqueColumnName(columnDeclarations, colExp.Name), colExp);
                        columnDeclarations.Add(cd); 
                    }

                    askedColumns[col] = new ColumnExpression(col.Type, select.Alias, cd.Name);
                }
            }

            if (columns.Count != columnDeclarations.Count)
                columns = columnDeclarations.ToReadOnly();

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

        public IDisposable NewScope(IEnumerable<ColumnExpression> requestedColumns)
        {
            var newDic = requestedColumns == null ?
                new Dictionary<ColumnExpression, ColumnExpression>() :
                requestedColumns.ToDictionary(r => r, r => (ColumnExpression)null);
            scopes = scopes.Push(newDic);
            return new Disposable(() => scopes = scopes.Pop());
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            ColumnExpression result;
            if (CurrentScope.TryGetValue(column, out result))
                return result ?? column;
            else
            {
                CurrentScope[column] = null;
                return column;
            }
        }
    }
}