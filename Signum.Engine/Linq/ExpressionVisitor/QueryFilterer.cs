using System;
using System.Linq;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using Signum.Entities;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Signum.Entities.Basics;

namespace Signum.Engine.Linq
{
    public class QueryFilterer : ExpressionVisitor
    {
        static GenericInvoker<Func<Schema, LambdaExpression?>> giFilter = new GenericInvoker<Func<Schema, LambdaExpression?>>(s => s.GetInDatabaseFilter<TypeEntity>());
        static MethodInfo miWhere = ReflectionTools.GetMethodInfo((IQueryable<object> q) => q.Where(a => true)).GetGenericMethodDefinition();

        bool filter;

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (disableQueryFilter)
                return base.VisitConstant(c);

            if (typeof(IQueryable).IsAssignableFrom(c.Type))
            {
                IQueryable query = (IQueryable)c.Value!;

                if (query.IsBase())
                {
                    Type queryType = c.Type.GetGenericArguments().SingleEx();

                    if (filter)
                    {
                        if (typeof(Entity).IsAssignableFrom(queryType))
                        {
                            LambdaExpression? rawFilter = giFilter.GetInvoker(queryType)(Schema.Current);
                            if (rawFilter != null)
                            {
                                Expression clean = ExpressionCleaner.Clean(rawFilter)!;
                                var cleanFilter = (LambdaExpression)OverloadingSimplifier.Simplify(clean)!;

                                return Expression.Call(miWhere.MakeGenericMethod(queryType), query.Expression, cleanFilter);
                            }
                        }
                        else if (queryType.IsInstantiationOf(typeof(MListElement<,>)))
                        {
                            Type entityType = queryType.GetGenericArguments()[0];

                            LambdaExpression? rawFilter = giFilter.GetInvoker(entityType)(Schema.Current);
                            if (rawFilter != null)
                            {
                                var param = Expression.Parameter(queryType, "mle");
                                var lambda = Expression.Lambda(Expression.Invoke(rawFilter, Expression.Property(param, "Parent")), param);

                                Expression clean = ExpressionCleaner.Clean(lambda)!;
                                var cleanFilter = (LambdaExpression)OverloadingSimplifier.Simplify(clean)!;

                                return Expression.Call(miWhere.MakeGenericMethod(queryType), query.Expression, cleanFilter);
                            }
                        }
                    }

                    return c;
                }
                else
                {
                    /// <summary>
                    /// Replaces every expression like ConstantExpression{ Type = IQueryable, Value = complexExpr } by complexExpr
                    /// </summary>
                    return DbQueryProvider.Clean(query.Expression, filter, null)!;
                }
            }

            return base.VisitConstant(c);
        }

        bool disableQueryFilter = false;
        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(LinqHints) && m.Method.Name == "DisableQueryFilter")
            {
                var old = disableQueryFilter;
                disableQueryFilter = true;
                var result = Visit(m.Arguments[0]);
                disableQueryFilter = old;

                return result;
            }
            else
                return base.VisitMethodCall(m);
        }


        internal static Expression? Filter(Expression? expression, bool filter)
        {
            return new QueryFilterer { filter = filter }.Visit(expression);
        }
    }
}
