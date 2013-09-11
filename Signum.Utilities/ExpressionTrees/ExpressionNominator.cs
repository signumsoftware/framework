using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Utilities.ExpressionTrees
{
    /// <summary>
    /// Performs bottom-up analysis to determine which nodes can possibly
    /// be part of an evaluated sub-tree.
    /// </summary>
    public class ExpressionNominator : SimpleExpressionVisitor
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
                expression.NodeType == ExpressionType.Call && ((MethodCallExpression)expression).Method.DeclaringType == typeof(Queryable) ||
                expression.NodeType == ExpressionType.Parameter ||
                expression.NodeType == ExpressionType.Lambda || // why? 
                !EnumExtensions.IsDefined(expression.NodeType);
        }

        protected override Expression Visit(Expression expression)
        {
            if (expression != null)
            {
                if (expression.NodeType == ExpressionType.Call &&
                    ((MethodCallExpression)expression).Method.DeclaringType == typeof(LinqHints) &&
                    ((MethodCallExpression)expression).Method.Name == "KeepConstantSubexpressions")
                {
                    this.hasDependencies = true;
                    return expression;
                }

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

namespace Signum.Utilities
{
    public static class LinqHints
    {
        public static T InSql<T>(this T value)
        {
            return value;
        }

        public static T KeepConstantSubexpressions<T>(this T value)
        {
            return value;
        }

        public static T DisableQueryFilter<T>(this T value)
        {
            return value;
        }

        public static IQueryable<T> OrderAlsoByKeys<T>(this IQueryable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Provider.CreateQuery<T>(Expression.Call(null, 
                ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(T) }), 
                new Expression[] { source.Expression }));
        }
    }
}
