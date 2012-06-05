using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.Engine.Linq
{
    /// <summary>
    ///  returns the set of all aliases produced by a query source
    /// </summary>
    internal class AliasGatherer : DbExpressionVisitor
    {
        HashSet<Alias> aliases = new HashSet<Alias>();

        private AliasGatherer() { }

        public static HashSet<Alias> Gather(Expression source)
        {
            AliasGatherer ap = new AliasGatherer();
            ap.Visit(source);
            return ap.aliases;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            this.aliases.Add(select.Alias);
            return base.VisitSelect(select);
        }

        protected override Expression VisitTable(TableExpression table)
        {
            this.aliases.Add(table.Alias);
            return base.VisitTable(table);
        }
    }

    internal class ExternalAliasGatherer : DbExpressionVisitor
    {
        HashSet<Alias> internals;

        HashSet<Alias> externals = new HashSet<Alias>();

        private ExternalAliasGatherer() { }

        public static HashSet<Alias> Externals(Expression source, HashSet<Alias> internals)
        {
            ExternalAliasGatherer ap = new ExternalAliasGatherer()
            {
                internals = internals
            };

            ap.Visit(source);

            return ap.externals;
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            if (!internals.Contains(column.Alias))
                externals.Add(column.Alias);

            return base.VisitColumn(column);
        }
    }
}
