using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Collections.ObjectModel;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Linq
{
    internal class AliasReplacer : DbExpressionVisitor
    {
        Dictionary<Alias, Alias> aliasMap;

        private AliasReplacer() { }

        public static Expression Replace(Expression source, AliasGenerator aliasGenerator)
        {
            AliasReplacer ap = new AliasReplacer()
            {
                aliasMap = DeclaredAliasGatherer.GatherDeclared(source).Reverse().ToDictionary(a => a, aliasGenerator.CloneAlias)
            };

            return ap.Visit(source);
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
            Expression top = this.Visit(select.Top);
            SourceExpression from = this.VisitSource(select.From);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns =  Visit(select.Columns, VisitColumnDeclaration);
            ReadOnlyCollection<OrderExpression> orderBy = Visit(select.OrderBy, VisitOrderBy);
            ReadOnlyCollection<Expression> groupBy = Visit(select.GroupBy, Visit);
            Alias newAlias = aliasMap.TryGetC(select.Alias) ?? select.Alias;

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy || newAlias != select.Alias)
                return new SelectExpression(newAlias, select.IsDistinct, top, columns, from, where, orderBy, groupBy, select.SelectOptions);

            return select;
        }
    }
}
