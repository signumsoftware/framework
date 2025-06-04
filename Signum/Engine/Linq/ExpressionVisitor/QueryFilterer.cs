using Signum.Engine.Maps;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Linq;

public class QueryFilterer : ExpressionVisitor
{
    static GenericInvoker<Func<Schema, FilterQueryArgs, LambdaExpression?>> giFilter = new((s, args) => s.GetInDatabaseFilter<TypeEntity>(args));
    static MethodInfo miWhere = ReflectionTools.GetMethodInfo((IQueryable<object> q) => q.Where(a => true)).GetGenericMethodDefinition();



    internal static Expression? Filter(Expression? expression, bool filter)
    {
        return new QueryFilterer { filter = filter, rootExpression = expression! }.Visit(expression);
    }

    Expression rootExpression; 
    bool filter;


    protected override Expression VisitConstant(ConstantExpression c)
    {

        using (HeavyProfiler.LogNoStackTrace("VisitConstant"))
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
                            using (HeavyProfiler.LogNoStackTrace("queryType"))
                            {
                                LambdaExpression? rawFilter = null;
                                using (HeavyProfiler.LogNoStackTrace("rawFilter"))
                                    rawFilter = giFilter.GetInvoker(queryType)(Schema.Current, new FilterQueryArgs(this.rootExpression, (ConstantExpression)query.Expression));

                                if (rawFilter != null)
                                {
                                    var cleanFilter = (LambdaExpression)DbQueryProvider.Clean(rawFilter, filter: false, null)!;
                                    using (HeavyProfiler.LogNoStackTrace("Call"))
                                        return Expression.Call(miWhere.MakeGenericMethod(queryType), query.Expression, cleanFilter);
                                }
                            }
                        else if (queryType.IsInstantiationOf(typeof(MListElement<,>)))
                            using (HeavyProfiler.LogNoStackTrace("MListElement"))
                            {
                                Type entityType = queryType.GetGenericArguments()[0];

                                LambdaExpression? rawFilter = giFilter.GetInvoker(entityType)(Schema.Current, new FilterQueryArgs(this.rootExpression, (ConstantExpression)query.Expression));
                                if (rawFilter != null)
                                {
                                    var param = Expression.Parameter(queryType, "mle");
                                    var lambda = Expression.Lambda(Expression.Invoke(rawFilter, Expression.Property(param, "Parent")), param);

                                    var cleanFilter = (LambdaExpression)DbQueryProvider.Clean(lambda, filter: false, null)!;

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



}
