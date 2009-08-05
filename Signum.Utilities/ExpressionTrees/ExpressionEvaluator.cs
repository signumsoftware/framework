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

    /// <summary>
    /// Performs bottom-up analysis to determine which nodes can possibly
    /// be part of an evaluated sub-tree.
    /// </summary>
    public class ExpressionNominator : ExpressionVisitor
    {
        HashSet<Expression> candidates = new HashSet<Expression>();
        bool hasDependencies;

        private ExpressionNominator() { }

        public static HashSet<Expression> Nominate(Expression expression)
        {
            ExpressionNominator n = new ExpressionNominator();
            n.Visit(expression);
            return n.candidates;
        }

        private bool ExpressionHasDependencies(Expression expression)
        {
            return
                expression.NodeType == ExpressionType.Parameter ||
                (expression.NodeType == ExpressionType.Call && ((MethodCallExpression)expression).Method.DeclaringType == typeof(Queryable)) ||
                expression.NodeType == ExpressionType.Lambda || // why? 
                !EnumExtensions.IsDefined(expression.NodeType);
        }

        protected override Expression Visit(Expression expression)
        {
            if (expression != null)
            {
                bool saveHasDependencies = this.hasDependencies;
                this.hasDependencies = false;
                base.Visit(expression);
                if (!this.hasDependencies)
                {
                    if (ExpressionHasDependencies(expression))
                        this.hasDependencies = true;
                    else
                        this.candidates.Add(expression);
                }
                this.hasDependencies |= saveHasDependencies;
            }
            return expression;
        }
    }
}