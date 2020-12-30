using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// Rewrite aggregate expressions, moving them into same select expression that has the group-by clause
    /// </summary>
    internal class DuplicateHistory : DbExpressionVisitor
    {
        private AliasGenerator aliasGenerator;

        public DuplicateHistory(AliasGenerator generator)
        {
            this.aliasGenerator = generator;
        }

        public static Expression Rewrite(Expression expr, AliasGenerator generator)
        {
            if (!Schema.Current.Settings.IsPostgres)
                return expr;

            return new DuplicateHistory(generator).Visit(expr);
        }

        public Dictionary<Alias, Dictionary<ColumnExpression, ColumnExpression?>> columnReplacements = new Dictionary<Alias, Dictionary<ColumnExpression, ColumnExpression?>>();

        protected internal override Expression VisitTable(TableExpression table)
        {
            if (table.SystemTime != null)
            {
                if (table.SystemTime is SystemTime.HistoryTable)
                    return table;

                var requests = columnReplacements.TryGetC(table.Alias);

                SelectExpression CreateSelect(string tableNameForAlias, SystemTime? systemTime)
                {
                    var tableExp = new TableExpression(aliasGenerator.NextTableAlias(tableNameForAlias), table.Table, systemTime, null);

                    ColumnExpression GetTablePeriod() => new ColumnExpression(typeof(NpgsqlTypes.NpgsqlRange<DateTime>), tableExp.Alias, table.Table.SystemVersioned!.PostgreeSysPeriodColumnName);
                    SqlFunctionExpression tstzrange(DateTime start, DateTime end) => new SqlFunctionExpression(typeof(NpgsqlTypes.NpgsqlRange<DateTime>), null, PostgresFunction.tstzrange.ToString(),
                        new[] { Expression.Constant(new DateTimeOffset(start)), Expression.Constant(new DateTimeOffset(end)) });

                    var where = table.SystemTime is SystemTime.All ? null :
                        table.SystemTime is SystemTime.AsOf asOf ? new SqlFunctionExpression(typeof(bool), null, PostgressOperator.Contains, new Expression[] { GetTablePeriod(), Expression.Constant(new DateTimeOffset(asOf.DateTime)) }) :
                        table.SystemTime is SystemTime.Between b ? new SqlFunctionExpression(typeof(bool), null, PostgressOperator.Overlap, new Expression[] { tstzrange(b.StartDateTime, b.EndtDateTime), GetTablePeriod() }) :
                        table.SystemTime is SystemTime.ContainedIn ci ? new SqlFunctionExpression(typeof(bool), null, PostgressOperator.Contains, new Expression[] { tstzrange(ci.StartDateTime, ci.EndtDateTime), GetTablePeriod() }) :
                        throw new UnexpectedValueException(table.SystemTime);

                    var newSelect = new SelectExpression(aliasGenerator.NextTableAlias(tableNameForAlias), false, null,
                        columns: requests?.Select(kvp => new ColumnDeclaration(kvp.Key.Name!, new ColumnExpression(kvp.Key.Type, tableExp.Alias, kvp.Key.Name))),
                        tableExp, where, null, null, 0);

                    return newSelect;
                }

                var current = CreateSelect(table.Table.Name.Name, null);
                var history = CreateSelect(table.Table.SystemVersioned!.TableName.Name, new SystemTime.HistoryTable());

                var unionAlias = aliasGenerator.NextTableAlias(table.Table.Name.Name);
                if (requests != null)
                {
                    foreach (var col in requests.Keys.ToList())
                    {
                        requests[col] = new ColumnExpression(col.Type, unionAlias, col.Name);
                    }
                }

                return new SetOperatorExpression(SetOperator.UnionAll, current, history, unionAlias);
            }

            return base.VisitTable(table);
        }

      

        protected internal override Expression VisitJoin(JoinExpression join)
        {
            this.Visit(join.Condition);
            if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply)
                this.VisitSource(join.Right);

            SourceExpression left = this.VisitSource(join.Left);
            SourceExpression right = this.VisitSource(join.Right);
            Expression? condition = this.Visit(join.Condition);
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.JoinType, left, right, condition);
            }
            return join;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            //if (select.SelectRoles == SelectRoles.Where && select.From is TableExpression table && table.SystemTime != null && !(table.SystemTime is SystemTime.HistoryTable))
            //{
            //    var current = (SelectExpression)AliasReplacer.Replace(select, this.aliasGenerator);
            //    var history = (SelectExpression)AliasReplacer.Replace(select, this.aliasGenerator);

            //    var newAlias = aliasGenerator.NextSelectAlias();

            //    if (columnReplacements.ContainsKey(select.Alias))
            //        throw new InvalidOperationException("Requests to trivial select (only where) not expected");

            //    var requests = columnReplacements.TryGetC(table.Alias).EmptyIfNull().Select(ce => new ColumnDeclaration(ce.Key,  ));

            //    return new SetOperatorExpression(SetOperator.UnionAll, current, history, table.Alias);
            //}
            //else
            //{
                this.Visit(select.Top);
                this.Visit(select.Where);
                Visit(select.Columns, VisitColumnDeclaration);
                Visit(select.OrderBy, VisitOrderBy);
                Visit(select.GroupBy, Visit);
                SourceExpression from = this.VisitSource(select.From!);
                Expression? top = this.Visit(select.Top);
                Expression? where = this.Visit(select.Where);
                ReadOnlyCollection<ColumnDeclaration> columns = Visit(select.Columns, VisitColumnDeclaration);
                ReadOnlyCollection<OrderExpression> orderBy = Visit(select.OrderBy, VisitOrderBy);
                ReadOnlyCollection<Expression> groupBy = Visit(select.GroupBy, Visit);

                if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy)
                    return new SelectExpression(select.Alias, select.IsDistinct, top, columns, from, where, orderBy, groupBy, select.SelectOptions);

                return select;
            //}
        }

        protected internal override Expression VisitProjection(ProjectionExpression proj)
        {
            this.Visit(proj.Projector);
            SelectExpression source = (SelectExpression)this.Visit(proj.Select);
            Expression projector = this.Visit(proj.Projector);

            if (source != proj.Select || projector != proj.Projector)
                return new ProjectionExpression(source, projector, proj.UniqueFunction, proj.Type);

            return proj;
        }

        protected internal override Expression VisitColumn(ColumnExpression column)
        {
            if (column.Name == null)
                return column;

            if (this.columnReplacements.TryGetValue(column.Alias, out var dic) && dic.TryGetValue(column, out var repColumn))
                return repColumn ?? column;

            this.columnReplacements.GetOrCreate(column.Alias).Add(column, null);

            return column;
        }

     

    }
}
