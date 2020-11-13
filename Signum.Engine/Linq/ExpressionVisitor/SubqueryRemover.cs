using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    internal class SubqueryRemover : DbExpressionVisitor
    {
        HashSet<SelectExpression> selectsToRemove;
        Dictionary<Alias, Dictionary<string, Expression>> map;
        
        public SubqueryRemover(IEnumerable<SelectExpression> selectsToRemove)
        {
            this.map = selectsToRemove.ToDictionary(d => d.Alias, d => d.Columns.ToDictionary(d2 => d2.Name, d2 => d2.Expression));
            this.selectsToRemove = new HashSet<SelectExpression>(selectsToRemove);
        }

        public static Expression Remove(Expression expression, IEnumerable<SelectExpression> selectsToRemove)
        {
            return new SubqueryRemover(selectsToRemove).Visit(expression);
        }

        protected internal override Expression VisitSelect(SelectExpression select)
        {
            if (this.selectsToRemove.Contains(select))
                return this.Visit(select.From)!;
            else
                return base.VisitSelect(select);
        }

        protected internal override Expression VisitColumn(ColumnExpression column)
        {
            return map.TryGetC(column.Alias)
                    ?.Let(d => d.GetOrThrow(column.Name!, "Reference to undefined column {0}")) ?? column;
        }
    }
}
