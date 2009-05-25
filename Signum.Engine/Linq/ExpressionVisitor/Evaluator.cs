using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Linq
{

    public static class Evaluator
    {
        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression expression)
        {
            return SubtreeEvaluator.Eval(expression, Nominator.Nominate(expression));
        }

        /// <summary>
        /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
        /// </summary>
        class SubtreeEvaluator : DbExpressionVisitor
        {
            HashSet<Expression> candidates;

            private SubtreeEvaluator() { }

            static internal Expression Eval(Expression exp, HashSet<Expression> candidates)
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
                    return this.Evaluate(exp);
                }
                return base.Visit(exp);
            }

            private Expression Evaluate(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant)
                {
                    return e;
                }
                LambdaExpression lambda = Expression.Lambda(e);
                Delegate fn = lambda.Compile();

                object value;
                try
                {
                    value = fn.DynamicInvoke(null);
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;                   
                } 
                
                return Expression.Constant(value, e.Type);
            }
        }

        //internal static MethodInfo miOuter = ReflectionUtils.GetMethodInfo(() => Database.Outer((IQueryable<string>)null)).GetGenericMethodDefinition();

        /// <summary>
        /// Performs bottom-up analysis to determine which nodes can possibly
        /// be part of an evaluated sub-tree.
        /// </summary>
        class Nominator : DbExpressionVisitor
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
                    expression.NodeType.IsDbExpression() ||
                    expression.NodeType == ExpressionType.Lambda; // why? 
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