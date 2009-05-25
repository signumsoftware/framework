using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Collections.ObjectModel;

namespace Signum.Engine.Linq
{
   

    internal class AliasReplacer : DbExpressionVisitor
    {
        Dictionary<string, string> aliasMap;

        private AliasReplacer() { }

        public static Expression Replace(Expression source, Func<string> newAlias)
        {
            AliasReplacer ap = new AliasReplacer()
            {
                aliasMap = AliasGatherer.Gather(source).Reverse().ToDictionary(a => a, a => newAlias())
            };

            return ap.Visit(source);
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            if(aliasMap.ContainsKey(column.Alias))
                return new ColumnExpression(column.Type, aliasMap[column.Alias], column.Name);
            return column;
        }

        protected override Expression VisitTable(TableExpression table)
        {
            if (aliasMap.ContainsKey(table.Alias))
                return new TableExpression(table.Type, aliasMap[table.Alias], table.Name);
            return table;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            Expression from = this.VisitSource(select.From);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns = this.VisitColumnDeclarations(select.Columns);
            ReadOnlyCollection<OrderExpression> orderBy = this.VisitOrderBy(select.OrderBy);
            ReadOnlyCollection<Expression> groupBy = this.VisitGroupBy(select.GroupBy);
            string newAlias = aliasMap.TryGetC(select.Alias) ?? select.Alias;

            if (from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy || newAlias != select.Alias)
                return new SelectExpression(select.Type, newAlias, false, null, columns, from, where, orderBy, groupBy, null);

            return select;
        }
    }
}
