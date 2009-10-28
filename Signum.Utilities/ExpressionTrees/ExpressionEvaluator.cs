using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Utilities.ExpressionTrees
{
    /// <summary>
    /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
    /// </summary>
    public class ExpressionEvaluator : ExpressionVisitor
    {
        HashSet<Expression> candidates;

        private ExpressionEvaluator() { }

        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression exp)
        {
            return new ExpressionEvaluator { candidates = ExpressionNominator.Nominate(exp) }.Visit(exp);
        }

        public static object Eval(Expression expression)
        {
            if (expression is ConstantExpression)
                return ((ConstantExpression)expression).Value;

            Delegate fn = Expression.Lambda(expression).Compile();

            try
            {
                return fn.DynamicInvoke(null);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        protected override Expression Visit(Expression exp)
        {
            if (exp == null)
            {
                return null;
            }
            if (this.candidates.Contains(exp))
            {
                if (exp.NodeType == ExpressionType.Constant)
                {
                    return exp;
                }

                return Expression.Constant(Eval(exp), exp.Type);
            }
            return base.Visit(exp);
        }
    }

  
}