using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Collections.ObjectModel;

namespace Signum.Engine.Linq
{
    internal class AliasReplacer : DbExpressionVisitor
    {
        readonly Dictionary<Alias, Alias> aliasMap;

        private AliasReplacer(Dictionary<Alias, Alias> aliasMap)
        {
            this.aliasMap = aliasMap;
        }

        public static Expression Replace(Expression source, AliasGenerator aliasGenerator)
        {
            var aliasMap = DeclaredAliasGatherer.GatherDeclared(source).Reverse().ToDictionary(a => a, aliasGenerator.CloneAlias);

            AliasReplacer ap = new AliasReplacer(aliasMap);

            return ap.Visit(source);
        }

        protected internal override Expression VisitAggregateRequest(AggregateRequestsExpression request)
        {
            var ag = (AggregateExpression)this.Visit(request.Aggregate);
            var newAlias = aliasMap.TryGetC(request.GroupByAlias) ?? request.GroupByAlias;
            if (ag != request.Aggregate || request.GroupByAlias != newAlias)
                return new AggregateRequestsExpression(newAlias, ag);

            return request;
        }

        protected internal override Expression VisitColumn(ColumnExpression column)
        {
            if(aliasMap.ContainsKey(column.Alias))
                return new ColumnExpression(column.Type, aliasMap[column.Alias], column.Name);
            return column;
        }

        protected internal override Expression VisitTable(TableExpression table)
        {
            if (aliasMap.ContainsKey(table.Alias))
                return new TableExpression(aliasMap[table.Alias], table.Table, table.SystemTime, table.WithHint);
            return table;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            Expression? top = this.Visit(select.Top);
            SourceExpression from = this.VisitSource(select.From!);
            Expression? where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns =  Visit(select.Columns, VisitColumnDeclaration);
            ReadOnlyCollection<OrderExpression> orderBy = Visit(select.OrderBy, VisitOrderBy);
            ReadOnlyCollection<Expression> groupBy = Visit(select.GroupBy, Visit);
            Alias newAlias = aliasMap.TryGetC(select.Alias) ?? select.Alias;

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy || newAlias != select.Alias)
                return new SelectExpression(newAlias, select.IsDistinct, top, columns, from, where, orderBy, groupBy, select.SelectOptions);

            return select;
        }

        protected internal override Expression VisitSqlTableValuedFunction(SqlTableValuedFunctionExpression sqlFunction)
        {
            ReadOnlyCollection<Expression> args = Visit(sqlFunction.Arguments);
            Alias newAlias = aliasMap.TryGetC(sqlFunction.Alias) ?? sqlFunction.Alias;
            if (args != sqlFunction.Arguments || sqlFunction.Alias != newAlias)
                return new SqlTableValuedFunctionExpression(sqlFunction.SqlFunction, sqlFunction.ViewTable, sqlFunction.SingleColumnType, newAlias, args);
            return sqlFunction;
        }

        protected internal override Expression VisitSetOperator(SetOperatorExpression set)
        {
            SourceWithAliasExpression left = (SourceWithAliasExpression)this.VisitSource(set.Left)!;
            SourceWithAliasExpression right = (SourceWithAliasExpression)this.VisitSource(set.Right)!;
            Alias newAlias = aliasMap.TryGetC(set.Alias) ?? set.Alias;
            if (left != set.Left || right != set.Right || newAlias != set.Alias)
            {
                return new SetOperatorExpression(set.Operator, left, right, newAlias);
            }
            return set;
        }

        protected internal override Expression VisitEntity(EntityExpression ee)
        {
            var bindings = Visit(ee.Bindings!, VisitFieldBinding);
            var mixins = Visit(ee.Mixins!, VisitMixinEntity);

            var externalId = (PrimaryKeyExpression)Visit(ee.ExternalId);
            var externalPeriod = (IntervalExpression?)Visit(ee.ExternalPeriod);

            var period = (IntervalExpression?)Visit(ee.TablePeriod);

            Alias? newAlias = ee.TableAlias == null ? null : aliasMap.TryGetC(ee.TableAlias) ?? ee.TableAlias;

            if (ee.Bindings != bindings || ee.ExternalId != externalId || ee.ExternalPeriod != externalPeriod || ee.Mixins != mixins || ee.TablePeriod != period)
                return new EntityExpression(ee.Type, externalId, externalPeriod, newAlias, bindings, mixins, period, ee.AvoidExpandOnRetrieving);

            return ee;
        }
    }
}
