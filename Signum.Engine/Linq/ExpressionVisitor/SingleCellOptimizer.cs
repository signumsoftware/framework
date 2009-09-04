using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Signum.Engine.Linq
{
    internal class SingleCellOptimizer: DbExpressionVisitor
    {
        ProjectionExpression root;

        internal static ProjectionExpression Optimize(ProjectionExpression projection)
        {
            SingleCellOptimizer sco = new SingleCellOptimizer() { root = projection };
            return (ProjectionExpression)sco.Visit(projection);
        }

        protected  override Expression VisitProjection(ProjectionExpression proj)
        {
            ProjectionExpression newProj = (ProjectionExpression)base.VisitProjection(proj);

            if (proj != root && proj.IsOneCell)
                return newProj.Source;
            else
                return newProj; 
        }
    }
}
