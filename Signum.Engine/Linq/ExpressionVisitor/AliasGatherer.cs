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
        HashSet<string> aliases = new HashSet<string>();

        private AliasGatherer() { }

        public static HashSet<string> Gather(Expression source)
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
}
