using Signum.DynamicQuery.Tokens;
using Signum.Engine.Maps;
using System.Collections.Frozen;

namespace Signum.DynamicQuery;

public static class DynamicQueryFluentInclude
{
    //public static FluentInclude<T> WithQuery<T, A>(this FluentInclude<T> fi, Func<Expression<Func<T, A>>> lazyQuerySelector)  <-- C# Generic argument inference not so smart as to do this
    public static FluentInclude<T> WithQuery<T>(this FluentInclude<T> fi, Func<Expression<Func<T, object?>>> lazyQuerySelector)
        where T : Entity
    {
        QueryLogic.Queries.Register(typeof(T), new DynamicQueryBucket(typeof(T), () => DynamicQueryCore.FromSelectorUntyped(lazyQuerySelector()), Implementations.By(typeof(T))));
        return fi;
    }

    public static FluentInclude<T> WithQuery<T, Q>(this FluentInclude<T> fi, Func<DynamicQueryCore<Q>> lazyGetQuery)
         where T : Entity
    {
        QueryLogic.Queries.Register<Q>(typeof(T), () => lazyGetQuery());
        return fi;
    }

    /// <summary>
    /// Uses typeof(T) NicePluralName as niceName
    /// </summary>
    public static FluentInclude<T> WithExpressionFrom<T, F>(this FluentInclude<T> fi, Expression<Func<F, IQueryable<T>>> lambdaToMethodOrProperty)
        where T : Entity
    {
        QueryLogic.Expressions.Register(lambdaToMethodOrProperty, () => typeof(T).NicePluralName());
        return fi;
    }

    public static FluentInclude<T> WithExpressionFrom<T, F>(this FluentInclude<T> fi, Expression<Func<F, IQueryable<T>>> lambdaToMethodOrProperty, Func<string> niceName)
        where T : Entity
    {
        QueryLogic.Expressions.Register(lambdaToMethodOrProperty, niceName);
        return fi;
    }

    /// <summary>
    /// Uses typeof(T) NiceName as niceName
    /// </summary>
    public static FluentInclude<T> WithExpressionFrom<T, F>(this FluentInclude<T> fi, Expression<Func<F, T?>> lambdaToMethodOrProperty)
        where T : Entity
    {
        QueryLogic.Expressions.Register(lambdaToMethodOrProperty, () => typeof(T).NiceName());
        return fi;
    }

    public static FluentInclude<T> WithExpressionFrom<T, F>(this FluentInclude<T> fi, Expression<Func<F, T?>> lambdaToMethodOrProperty, Func<string> niceName)
        where T : Entity
    {
        QueryLogic.Expressions.Register(lambdaToMethodOrProperty, niceName);
        return fi;
    }

    /// <summary>
    /// Prefer WithExpressionFrom to keep dependencies between modules clean!. Uses typeof(T) NicePluralName as niceName.
    /// </summary>
    public static FluentInclude<F> WithExpressionTo<F, T>(this FluentInclude<F> fi, Expression<Func<F, IQueryable<T>>> lambdaToMethodOrProperty)
        where F : Entity
        where T : Entity
    {
        QueryLogic.Expressions.Register(lambdaToMethodOrProperty, () => typeof(T).NicePluralName());
        return fi;
    }

    /// <summary>
    /// Prefer WithExpressionFrom to keep dependencies between modules clean!. Uses typeof(T) NicePluralName as niceName.
    /// </summary>
    public static FluentInclude<F> WithExpressionTo<F, T>(this FluentInclude<F> fi, Expression<Func<F, IQueryable<Lite<T>>>> lambdaToMethodOrProperty)
        where F : Entity
        where T : Entity
    {
        QueryLogic.Expressions.Register(lambdaToMethodOrProperty, () => typeof(T).NicePluralName());
        return fi;
    }
    /// <summary>
    /// Prefer WithExpressionFrom to keep dependencies between modules clean!.
    /// </summary>
    public static FluentInclude<F> WithExpressionTo<F, T>(this FluentInclude<F> fi, Expression<Func<F, IQueryable<T>>> lambdaToMethodOrProperty, Func<string> niceName)
        where F : Entity
    {
        QueryLogic.Expressions.Register(lambdaToMethodOrProperty, niceName);
        return fi;
    }

    /// <summary>
    /// Prefer WithExpressionFrom to keep dependencies between modules clean!. Uses typeof(T) NiceName as niceName.
    /// </summary>
    public static FluentInclude<F> WithExpressionTo<F, T>(this FluentInclude<F> fi, Expression<Func<F, T?>> lambdaToMethodOrProperty)
        where F : Entity
        where T : Entity
    {
        QueryLogic.Expressions.Register(lambdaToMethodOrProperty, () => typeof(T).NiceName());
        return fi;
    }

    /// <summary>
    /// Prefer WithExpressionFrom to keep dependencies between modules clean!.
    /// </summary>
    public static FluentInclude<F> WithExpressionTo<F, T>(this FluentInclude<F> fi, Expression<Func<F, T?>> lambdaToMethodOrProperty, Func<string> niceName)
        where F : Entity
        where T : Entity
    {
        QueryLogic.Expressions.Register(lambdaToMethodOrProperty, niceName);
        return fi;
    }

    /// <summary>
    /// Prefer WithExpressionFrom to keep dependencies between modules clean!. Uses typeof(T) NiceName as niceName.
    /// </summary>
    public static FluentInclude<F> WithExpressionTo<F, T>(this FluentInclude<F> fi, Expression<Func<F, Lite<T>?>> lambdaToMethodOrProperty)
        where F : Entity
        where T : Entity
    {
        QueryLogic.Expressions.Register(lambdaToMethodOrProperty, () => typeof(T).NiceName());
        return fi;
    }

    /// <summary>
    /// Prefer WithExpressionFrom to keep dependencies between modules clean!.
    /// </summary>
    public static FluentInclude<F> WithExpressionTo<F, T>(this FluentInclude<F> fi, Expression<Func<F, Lite<T>?>> lambdaToMethodOrProperty, Func<string> niceName)
        where F : Entity
        where T : Entity
    {
        QueryLogic.Expressions.Register(lambdaToMethodOrProperty, niceName);
        return fi;
    }



    public static ResetLazy<FrozenSet<K>> GetAllKeysLazy<K>(this SchemaBuilder sb)
    {
        if (typeof(K).IsEnum)
            return new ResetLazy<FrozenSet<K>>(() => EnumExtensions.GetValues<K>().ToFrozenSet());

        if (typeof(K).IsLite())
            return sb.GlobalLazy(() => Database.RetrieveAllLite(typeof(K).CleanType()).Cast<K>().ToFrozenSet(), new InvalidateWith(typeof(K).CleanType()));

        throw new InvalidOperationException("Unable to get all the possible keys for " + typeof(K).TypeName());
    }

    /// <summary>
    /// Registers and dictionary-like expression, like: Product.ExtraFields.[Color]
    /// </summary>
    /// <typeparam name="E">The entity</typeparam>
    /// <typeparam name="K">The key of the pseudo-dictonary, typically a lite, and enum, or a string</typeparam>
    /// <typeparam name="V">The value returned from accesing the pseudo-dictionary</typeparam>
    /// <param name="fi"></param>
    /// <param name="enumMessage">This message will be used for the key and the niceName</param>
    /// <param name="getKeys">A lambda like: qt => ProducsKeysLazy.Value</param>
    /// <param name="extensionLambda">A lambda like: (ProductEntity p, ProductExtraFieldDefinitionEntity pef) => p.ExtraFields.SingleOrDefault(ef => ef.Key.Is(pef))!.Value</param>
    /// <returns></returns>
    public static FluentInclude<E> WithExpressionWithParameter<E, K, V>(this FluentInclude<E> fi, Enum enumMessage, Func<QueryToken, IEnumerable<K>> getKeys, Expression<Func<E, K, V>> extensionLambda, bool autoExpand = false)
        where E : Entity
        where K : notnull
    {
        QueryLogic.Expressions.RegisterWithParameter(enumMessage.ToString(), () => enumMessage.NiceToString(), getKeys, extensionLambda, autoExpand);

        return fi;
    }
}
