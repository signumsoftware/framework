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
    public static class ExpressionEvaluator
    {
        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression expression)
        {
            return SubtreeEvaluator.PartialEval(expression, Nominator.Nominate(expression));
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

        /// <summary>
        /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
        /// </summary>
        class SubtreeEvaluator : ExpressionVisitor
        {
            HashSet<Expression> candidates;

            private SubtreeEvaluator() { }

            static internal Expression PartialEval(Expression exp, HashSet<Expression> candidates)
            {
                return new SubtreeEvaluator { candidates = candidates }.Visit(exp);
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

        //internal static MethodInfo miOuter = ReflectionUtils.GetMethodInfo(() => Database.Outer((IQueryable<string>)null)).GetGenericMethodDefinition();

        /// <summary>
        /// Performs bottom-up analysis to determine which nodes can possibly
        /// be part of an evaluated sub-tree.
        /// </summary>
        class Nominator : ExpressionVisitor
        {
            HashSet<Expression> candidates = new HashSet<Expression>();
            bool hasDependencies;

            private Nominator() { }

            static internal HashSet<Expression> Nominate(Expression expression)
            {
                Nominator n = new Nominator();
                n.Visit(expression);
                return n.candidates;
            }

            private bool ExpressionHasDependencies(Expression expression)
            {
                return
                    expression.NodeType == ExpressionType.Parameter ||
                    (expression.NodeType == ExpressionType.Call && ((MethodCallExpression)expression).Method.DeclaringType == typeof(Queryable)) ||
                    expression.NodeType == ExpressionType.Lambda || // why? 
                    !EnumExtensions.IsDefined(expression.NodeType) ; 
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
}