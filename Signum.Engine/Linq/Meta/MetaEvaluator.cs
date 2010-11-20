using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
    /// </summary>
    public class MetaEvaluator : SimpleExpressionVisitor
    {
        HashSet<Expression> candidates;

        private MetaEvaluator() { }

        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression exp)
        {
            return new MetaEvaluator { candidates = ExpressionNominator.Nominate(exp) }.Visit(exp);
        }

        protected override Expression Visit(Expression exp)
        {
            if (exp == null)
            {
                return null;
            }
            if (this.candidates.Contains(exp) && exp.NodeType != ExpressionType.Constant)
            {
                return (ConstantExpression)miConstant.GenericInvoke(new[] { exp.Type }, null, null); 
            }
            return base.Visit(exp);
        }

        static MethodInfo miConstant = ReflectionTools.GetMethodInfo(() => Constant<int>()).GetGenericMethodDefinition();
        static ConstantExpression Constant<T>()
        {
            return Expression.Constant(default(T), typeof(T)); 
        }
    }
}
