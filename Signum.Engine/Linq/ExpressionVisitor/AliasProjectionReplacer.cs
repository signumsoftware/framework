using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Signum.Engine.Linq
{
    internal class AliasProjectionReplacer : DbExpressionVisitor
    {
        AliasGenerator generator; 
        ProjectionExpression root;

        public static Expression Replace(Expression proj, AliasGenerator generator)
        {
            AliasProjectionReplacer apr = new AliasProjectionReplacer()
            {
                root = proj as ProjectionExpression,
                generator = generator
            };
            return apr.Visit(proj);
        }       

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            if (proj != root)
                return (ProjectionExpression)AliasReplacer.Replace(base.VisitProjection(proj), generator);
            else
                return (ProjectionExpression)base.VisitProjection(proj);
        }
    }

}
