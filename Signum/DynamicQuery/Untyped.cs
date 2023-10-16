using Signum.Utilities.Reflection;
using System.Collections;

namespace Signum.DynamicQuery;

public static class Untyped
{
    static MethodInfo miSelectQ =
        ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Select((Expression<Func<string, int>>)null!)).GetGenericMethodDefinition();
    public static IQueryable Select(IQueryable query, LambdaExpression selector)
    {
        var types = selector.Type.GenericTypeArguments;

        var mi = miSelectQ.MakeGenericMethod(types);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(selector) }));
    }

    static GenericInvoker<Func<IEnumerable, Delegate, IEnumerable>> giSelectE =
    new((q, selector) => ((IEnumerable<string>)q).Select((Func<string, int>)selector));
    public static IEnumerable Select(IEnumerable collection, Delegate selector)
    {
        var types = selector.GetType().GenericTypeArguments;

        return giSelectE.GetInvoker(types)(collection, selector);
    }

    static MethodInfo miSelectManyQ =
    ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).SelectMany((Expression<Func<string, IEnumerable<int>>>)null!, (Expression<Func<string, int, bool>>)null!)).GetGenericMethodDefinition();
    public static IQueryable SelectMany(IQueryable query, LambdaExpression collectionSelector, LambdaExpression resultSelector)
    {
        var types = resultSelector.Type.GenericTypeArguments;

        var mi = miSelectManyQ.MakeGenericMethod(types);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector) }));
    }

    static GenericInvoker<Func<IEnumerable, Delegate, Delegate, IEnumerable>> giSelectManyE =
    new((q, collectionSelector, resultSelector) => ((IEnumerable<string>)q).SelectMany((Func<string, IEnumerable<int>>)collectionSelector, (Func<string, int, bool>)resultSelector));
    public static IEnumerable SelectMany(IEnumerable collection, Delegate collectionSelector, Delegate resultSelector)
    {
        var types = resultSelector.GetType().GenericTypeArguments;

        var mi = miSelectManyQ.MakeGenericMethod(types);

        return giSelectManyE.GetInvoker(types)(collection, collectionSelector, resultSelector);
    }

    public static MethodInfo miJoin = ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Join(((IQueryable<string>)null!),
        (Expression<Func<string, string>>)null!, 
        (Expression<Func<string, string>>)null!,
        (Expression<Func<string, string, string>>)null!)).GetGenericMethodDefinition();
    public static IQueryable Join(IQueryable outer, IQueryable inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector)
    {
        var types = resultSelector.Type.GenericTypeArguments;

        var mi = miJoin.MakeGenericMethod(outer.ElementType, inner.ElementType, outerKeySelector.ReturnType, resultSelector.ReturnType);

        return outer.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { outer.Expression, inner.Expression, 
            Expression.Quote(outerKeySelector),
            Expression.Quote(innerKeySelector), 
            Expression.Quote(resultSelector) }));
    }


    static MethodInfo miWhereQ =
        ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Where((Expression<Func<string, bool>>)null!)).GetGenericMethodDefinition();
    public static IQueryable Where(IQueryable query, LambdaExpression predicate)
    {
        var types = query.GetType().GenericTypeArguments;

        var mi = miWhereQ.MakeGenericMethod(types);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(predicate) }));
    }

    static GenericInvoker<Func<IEnumerable, Delegate, IEnumerable>> giWhereE =
        new((q, predicate) => ((IEnumerable<string>)q).Where<string>((Func<string, bool>)predicate));
    public static IEnumerable Where(IEnumerable collection, Delegate predicate)
    {
        var types = predicate.GetType().GenericTypeArguments[0];

        return giWhereE.GetInvoker(types)(collection, predicate);
    }

    static MethodInfo miDistinctQ =
        ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Distinct()).GetGenericMethodDefinition();
    public static IQueryable Distinct(IQueryable query, Type elementType)
    {
        var mi = miDistinctQ.MakeGenericMethod(elementType);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression }));
    }

    static MethodInfo miOrderAlsoByKeysQ =
    ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).OrderAlsoByKeys()).GetGenericMethodDefinition();
    public static IQueryable OrderAlsoByKeys(IQueryable query, Type elementType)
    {
        var mi = miOrderAlsoByKeysQ.MakeGenericMethod(elementType);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression }));
    }

    static GenericInvoker<Func<IEnumerable, int, IEnumerable>> giTakeE =
    new((q, limit) => ((IEnumerable<string>)q).Take<string>(limit));
    public static IEnumerable Take(IEnumerable collection, int limit, Type elementType)
    {
        return giTakeE.GetInvoker(elementType)(collection, limit);
    }

    static MethodInfo miTakeQ =
      ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Take(3)).GetGenericMethodDefinition();
    public static IQueryable Take(IQueryable query, int limit, Type elementType)
    {
        var mi = miTakeQ.MakeGenericMethod(elementType);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Constant(limit) }));
    }

    static GenericInvoker<Func<IEnumerable, int, IEnumerable>> giSkipE =
        new((q, limit) => ((IEnumerable<string>)q).Skip<string>(limit));
    public static IEnumerable Skip(IEnumerable collection, int limit, Type elementType)
    {
        return giSkipE.GetInvoker(elementType)(collection, limit);
    }

    static MethodInfo miSkipQ =
      ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Skip(3)).GetGenericMethodDefinition();
    public static IQueryable Skip(IQueryable query, int limit, Type elementType)
    {
        var mi = miSkipQ.MakeGenericMethod(elementType);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Constant(limit) }));
    }

    static GenericInvoker<Func<IEnumerable, int>> giCountE =
    new((q) => ((IEnumerable<string>)q).Count());
    public static int Count(IEnumerable collection, Type elementType)
    {
        return giCountE.GetInvoker(elementType)(collection);
    }


    static MethodInfo miCountQ =
        ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Count()).GetGenericMethodDefinition();
    public static int Count(IQueryable query, Type elementType)
    {
        var mi = miCountQ.MakeGenericMethod(elementType);

        return (int)query.Provider.Execute(Expression.Call(null, mi, new Expression[] { query.Expression }))!;
    }

    public static async Task<int> CountAsync(IQueryable query, CancellationToken token, Type elementType)
    {
        var mi = miCountQ.MakeGenericMethod(elementType);

        var result = await ((IQueryProviderAsync)query.Provider).ExecuteAsync(Expression.Call(null, mi, new Expression[] { query.Expression }), token)!;

        return (int)result!;
    }

    static MethodInfo miConcatQ =
      ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Concat((IQueryable<string>)null!)).GetGenericMethodDefinition();
    public static IQueryable Concat(IQueryable query, IQueryable query2, Type elementType)
    {
        var mi = miConcatQ.MakeGenericMethod(elementType);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, query2.Expression }));
    }

    static GenericInvoker<Func<IEnumerable, IEnumerable, IEnumerable>> gConcatE =
        new((q, q2) => ((IEnumerable<string>)q).Concat((IEnumerable<string>)q2));

    public static IEnumerable Concat(IEnumerable collection, IEnumerable collection2, Type elementType)
    {
        return gConcatE.GetInvoker(elementType)(collection, collection2);
    }

    static GenericInvoker<Func<IEnumerable, Array>> gToArrayE =
        new((q) => ((IEnumerable<string>)q).ToArray());
    public static Array ToArray(IEnumerable collection, Type elementType)
    {
        return gToArrayE.GetInvoker(elementType)(collection);
    }

    static GenericInvoker<Func<IEnumerable, IList>> gToListE =
    new((q) => ((IEnumerable<string>)q).ToList());

    public static IList ToList(IEnumerable collection, Type elementType)
    {
        return gToListE.GetInvoker(elementType)(collection);
    }

    static GenericInvoker<Func<IQueryable, CancellationToken, Task<IList>>> gToListAsyncQ =
        new((q, token) => ToIListAsync((IQueryable<string>)q, token));

    public static Task<IList> ToListAsync(IQueryable query, CancellationToken token, Type elementType)
    {
        return gToListAsyncQ.GetInvoker(elementType)(query, token);
    }

    static async Task<IList> ToIListAsync<T>(IQueryable<T> query, CancellationToken token)
    {
        return await query.ToListAsync(token);
    }

    static readonly GenericInvoker<Func<IEnumerable, Delegate, IEnumerable>> giOrderByE = new((col, del) => ((IEnumerable<object>)col).OrderBy((Func<object, object?>)del));
    static readonly GenericInvoker<Func<IEnumerable, Delegate, IEnumerable>> giOrderByDescendingE = new((col, del) => ((IEnumerable<object>)col).OrderByDescending((Func<object, object?>)del));
    public static IEnumerable OrderBy(IEnumerable collection, LambdaExpression lambda, OrderType orderType)
    {
        var mi = orderType == OrderType.Ascending ? giOrderByE : giOrderByDescendingE;

        return mi.GetInvoker(lambda.Type.GetGenericArguments())(collection, lambda.Compile());
    }

    static readonly GenericInvoker<Func<IEnumerable, Delegate, IEnumerable>> giThenByE = new((col, del) => ((IOrderedEnumerable<object>)col).ThenBy((Func<object, object?>)del));
    static readonly GenericInvoker<Func<IEnumerable, Delegate, IEnumerable>> giThenByDescendingE = new((col, del) => ((IOrderedEnumerable<object>)col).ThenByDescending((Func<object, object?>)del));
    public static IEnumerable ThenBy(IEnumerable collection, LambdaExpression lambda, OrderType orderType)
    {
        var mi = orderType == OrderType.Ascending ? giThenByE : giThenByDescendingE;

        return mi.GetInvoker(lambda.Type.GetGenericArguments())(collection, lambda.Compile());
    }

    public static IEnumerable OrderBy(IEnumerable collection, List<(LambdaExpression lambda, OrderType orderType)> orders)
    {
        if (orders == null || orders.Count == 0)
            return collection;

        IEnumerable result = Untyped.OrderBy(collection, orders[0].lambda, orders[0].orderType);

        foreach (var (lambda, orderType) in orders.Skip(1))
        {
            result = Untyped.ThenBy(result, lambda, orderType);
        }

        return result;
    }

    static MethodInfo miOrderByQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().OrderBy(t => t.Id)).GetGenericMethodDefinition();
    static MethodInfo miOrderByDescendingQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().OrderByDescending(t => t.Id)).GetGenericMethodDefinition();
    public static IOrderedQueryable OrderBy(IQueryable query, LambdaExpression lambda, OrderType orderType)
    {
        MethodInfo mi = (orderType == OrderType.Ascending ? miOrderByQ : miOrderByDescendingQ).MakeGenericMethod(lambda.Type.GetGenericArguments());

        return (IOrderedQueryable)query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(lambda) }));
    }

    static MethodInfo miThenByQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().OrderBy(t => t.Id).ThenBy(t => t.Id)).GetGenericMethodDefinition();
    static MethodInfo miThenByDescendingQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().OrderBy(t => t.Id).ThenByDescending(t => t.Id)).GetGenericMethodDefinition();
    public static IOrderedQueryable ThenBy(IOrderedQueryable query, LambdaExpression lambda, OrderType orderType)
    {
        MethodInfo mi = (orderType == OrderType.Ascending ? miThenByQ : miThenByDescendingQ).MakeGenericMethod(lambda.Type.GetGenericArguments());

        return (IOrderedQueryable)query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(lambda) }));
    }

    public static IQueryable OrderBy(IQueryable query, List<(LambdaExpression lambda, OrderType orderType)> orders)
    {
        if (orders == null || orders.Count == 0)
            return query;

        IOrderedQueryable result = Untyped.OrderBy(query, orders[0].lambda, orders[0].orderType);

        foreach (var (lambda, orderType) in orders.Skip(1))
        {
            result = Untyped.ThenBy(result, lambda, orderType);
        }

        return result;
    }
}
