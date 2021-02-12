using System.Collections.Generic;
using System.Linq.Expressions;

namespace Signum.Engine.Linq
{
    /// <summary>
    ///  returns the set of all aliases produced by a query source
    /// </summary>
    internal class DeclaredAliasGatherer : DbExpressionVisitor
    {
        HashSet<Alias> aliases = new HashSet<Alias>();

        private DeclaredAliasGatherer() { }

        public static HashSet<Alias> GatherDeclared(Expression source)
        {
            DeclaredAliasGatherer ap = new DeclaredAliasGatherer();
            ap.Visit(source);
            return ap.aliases;
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            this.aliases.Add(select.Alias);
            return base.VisitSelect(select);
        }

        protected internal override Expression VisitTable(TableExpression table)
        {
            this.aliases.Add(table.Alias);
            return base.VisitTable(table);
        }

        protected internal override Expression VisitSetOperator(SetOperatorExpression set)
        {
            this.aliases.Add(set.Alias);
            return base.VisitSetOperator(set);
        }

        protected internal override Expression VisitSqlTableValuedFunction(SqlTableValuedFunctionExpression sqlFunction)
        {
            this.aliases.Add(sqlFunction.Alias);
            return base.VisitSqlTableValuedFunction(sqlFunction);
        }
    }

    internal class UsedAliasGatherer : DbExpressionVisitor
    {
        HashSet<Alias> externals = new HashSet<Alias>();

        private UsedAliasGatherer() { }

        public static HashSet<Alias> Externals(Expression source)
        {
            UsedAliasGatherer ap = new UsedAliasGatherer();

            ap.Visit(source);

            return ap.externals;
        }

        protected internal override Expression VisitColumn(ColumnExpression column)
        {
             externals.Add(column.Alias);

            return base.VisitColumn(column);
        }
    }
}
