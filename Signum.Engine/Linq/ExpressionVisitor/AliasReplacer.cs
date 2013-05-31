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

        protected override Expression VisitColumn(ColumnExpression column)
        {
            if(aliasMap.ContainsKey(column.Alias))
                return new ColumnExpression(column.Type, aliasMap[column.Alias], column.Name);
            return column;
        }

        protected override Expression VisitTable(TableExpression table)
        {
            if (aliasMap.ContainsKey(table.Alias))
                return new TableExpression(aliasMap[table.Alias], table.Table);
            return table;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            Expression top = this.Visit(select.Top);
            SourceExpression from = this.VisitSource(select.From);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns = select.Columns.NewIfChange(VisitColumnDeclaration);
            ReadOnlyCollection<OrderExpression> orderBy = select.OrderBy.NewIfChange(VisitOrderBy);
            ReadOnlyCollection<Expression> groupBy = select.GroupBy.NewIfChange(Visit);
            Alias newAlias = aliasMap.TryGetC(select.Alias) ?? select.Alias;

            if (top != select.Top || from != select.From || where != select.Where || columns != select.Columns || orderBy != select.OrderBy || groupBy != select.GroupBy || newAlias != select.Alias)
                return new SelectExpression(newAlias, select.IsDistinct, select.IsReverse, top, columns, from, where, orderBy, groupBy, select.ForXmlPathEmpty);

            return select;
        }
    }
}
