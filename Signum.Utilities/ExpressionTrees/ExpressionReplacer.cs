using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Signum.Utilities.ExpressionTrees
{
    public class ExpressionReplacer: ExpressionVisitor
    {
        Dictionary<ParameterExpression, Expression> replacements = new Dictionary<ParameterExpression, Expression>();

        public static Expression Replace(InvocationExpression invocation)
        {
            LambdaExpression lambda = invocation.Expression as LambdaExpression;
            var replacer = new ExpressionReplacer()
            {
                replacements = 0.To(lambda.Parameters.Count).ToDictionaryEx(i => lambda.Parameters[i], i => invocation.Arguments[i])
            };

            return replacer.Visit(lambda.Body); 
        }

        public static Expression Replace(Expression expression, Dictionary<ParameterExpression, Expression> replacements)
        {
            var replacer = new ExpressionReplacer()
            {
                replacements = replacements
            };

            return replacer.Visit(expression);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return replacements.TryGetC(p) ?? p;
        }
    }
}
