using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Utilities.ExpressionTrees
{
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
            if (expression.NodeType == ExpressionType.Call)
            {
                var m = ((MethodCallExpression)expression);

                return m.Method.DeclaringType == typeof(Queryable) || m.Method.HasAttribute<AvoidEagerEvaluationAttribute>();
            }

            //Query<UserEntity>().Select(u => new ComboBox()) expects N different ComboBoxes
            //also solves problems with MemberInitExpression and CollectionInitExpressions
            if (expression.NodeType == ExpressionType.New)
                return true;

            return expression.NodeType == ExpressionType.Parameter ||
                expression.NodeType == ExpressionType.Lambda || // why?
                !EnumExtensions.IsDefined(expression.NodeType);
        }

        public override Expression? Visit(Expression? expression)
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
            return expression!;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.HasAttribute<ForceEagerEvaluationAttribute>())
                return node;

            return base.VisitMethodCall(node);
        }
    }

    public static class LinqHints
    {
        public static T InSql<T>(this T value)
        {
            return value;
        }

        public static T KeepConstantSubexpressions<T>(T value)
        {
            return value;
        }

        [AvoidEagerEvaluation]
        public static IQueryable<T> DisableQueryFilter<T>(this IQueryable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Provider.CreateQuery<T>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()!).MakeGenericMethod(new Type[] { typeof(T) }),
                new Expression[] { source.Expression }));
        }

        public static IQueryable<T> OrderAlsoByKeys<T>(this IQueryable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (!(source.Provider is QueryProvider))
                return source; //AsQueryable or any other provider won't implement it

            return source.Provider.CreateQuery<T>(Expression.Call(null,
                ((MethodInfo)MethodBase.GetCurrentMethod()!).MakeGenericMethod(new Type[] { typeof(T) }),
                new Expression[] { source.Expression }));
        }


        [MethodExpander(typeof(DistinctNullExpander))]
        public static bool DistinctNull<T>(T? a, T? b) where T : class
        {
            return (a == null && b != null) ||
                  (a != null && b == null) ||
                  (a != null && b != null && !a.Equals(b));
        }

        [MethodExpander(typeof(DistinctNullExpander))]
        public static bool DistinctNull<T>(T? a, T? b) where T : struct
        {
            return (a == null && b != null) ||
                  (a != null && b == null) ||
                  (a != null && b != null && !a.Value.Equals(b.Value));
        }

        class DistinctNullExpander : IMethodExpander
        {
            public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
            {
                var a = arguments[0];
                var b = arguments[1];

                var n = Expression.Constant(null, mi.GetGenericArguments().Single().Nullify());

                var c1 = Expression.And(Expression.Equal(a, n), Expression.NotEqual(b, n));
                var c2 = Expression.And(Expression.NotEqual(a, n), Expression.Equal(b, n));
                var c3 = Expression.And(Expression.And(Expression.NotEqual(a, n), Expression.NotEqual(b, n)), Expression.NotEqual(a, b));

                return Expression.Or(Expression.Or(c1, c2), c3);
            }
        }

        [AvoidEagerEvaluation]
        public static IQueryable<T> WithHint<T>(this IQueryable<T> source, string hint)
        {
            if (source == null)
                throw new ArgumentNullException("hint");

            return source.Provider.CreateQuery<T>(Expression.Call(null, ((MethodInfo) MethodBase.GetCurrentMethod()!).MakeGenericMethod(new Type[] { typeof(T) }), new Expression[] { source.Expression, Expression.Constant(hint, typeof(string)) }));
        }

        /// <param name="collation">database_default</param>
        [return: NotNullIfNotNull("str")]
        public static string? Collate(this string? str, string collation = "database_default") => throw new InvalidOperationException("Collate only supported inside queries");
    }
}
