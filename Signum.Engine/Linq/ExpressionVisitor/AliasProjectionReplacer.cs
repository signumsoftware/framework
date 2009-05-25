using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Signum.Engine.Linq
{
    internal class AliasProjectionReplacer : DbExpressionVisitor
    {
        ProjectionExpression root;

        public static ProjectionExpression Replace(ProjectionExpression proj)
        {
            AliasProjectionReplacer apr = new AliasProjectionReplacer() { root = proj };
            return (ProjectionExpression)apr.Visit(proj);
        }
        int aliasCount = 0;
        private string GetNextAlias()
        {
            return "r" + (aliasCount++);
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            if (proj != root)
                return (ProjectionExpression)AliasReplacer.Replace(base.VisitProjection(proj), GetNextAlias);
            else
                return (ProjectionExpression)base.VisitProjection(proj);
        }
    }

}
