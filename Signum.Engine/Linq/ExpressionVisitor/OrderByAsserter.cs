using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Utilities; 

namespace Signum.Engine.Linq
{
    internal class OrderByAsserter : DbExpressionVisitor
    {
        HashSet<SelectExpression> AllowedSelects = new HashSet<SelectExpression>(); 

        static internal Expression Assert(Expression expression)
        {
            return new OrderByAsserter().Visit(expression);
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            AllowedSelects.Add(proj.Source);

            return base.VisitProjection(proj);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            if (select.OrderBy != null && !AllowedSelects.Contains(select))
                throw new InvalidOperationException("OrderBy should allways be the last operation");

            return base.VisitSelect(select);
        }
    }
}
