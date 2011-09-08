using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Signum.Engine.Linq
{
    internal class ProjectionExpressionFinder:DbExpressionVisitor
    {
        bool someFound = false;

        public static bool HasProjections(Expression exp)
        {
            ProjectionExpressionFinder pef = new ProjectionExpressionFinder();
            pef.Visit(exp);
            return pef.someFound; 
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            someFound = true; 
            return base.VisitProjection(proj);
        }
    }
}
