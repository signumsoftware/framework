using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
    /// </summary>
    public class MetaEvaluator : ExpressionVisitor
    {
        [return: NotNullIfNotNull("expression")]
        public static Expression? Clean(Expression? expression)
        {
            Expression? expand = ExpressionCleaner.Clean(expression, MetaEvaluator.PartialEval, false);
            Expression? simplified = OverloadingSimplifier.Simplify(expand);
            return simplified;
        }

        HashSet<Expression> candidates;
        
        public MetaEvaluator(HashSet<Expression> candidates)
        {
            this.candidates = candidates;
        }

        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression exp)
        {
            return new MetaEvaluator(candidates: ExpressionNominator.Nominate(exp)).Visit(exp);
        }

        [return: NotNullIfNotNull("exp")]
        public override Expression? Visit(Expression? exp)
        {
            if (exp == null)
                return null!;

            if (this.candidates.Contains(exp) && exp.NodeType != ExpressionType.Constant)
            {
                if (exp.Type.IsInstantiationOf(typeof(IQueryable<>)))
                    return ExpressionEvaluator.PartialEval(exp);

                return miConstant.GetInvoker(exp.Type)();
            }
            return base.Visit(exp);
        }

        static GenericInvoker<Func<ConstantExpression>> miConstant = new GenericInvoker<Func<ConstantExpression>>(() => Constant<int>());
        static ConstantExpression Constant<T>()
        {
            return Expression.Constant(default(T)!, typeof(T));
        }
    }
}
