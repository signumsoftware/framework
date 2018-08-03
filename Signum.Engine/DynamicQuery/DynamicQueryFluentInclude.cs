using System;
using System.Linq;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine.Maps;
using System.Linq.Expressions;
using Signum.Engine.Basics;

namespace Signum.Engine.DynamicQuery
{
    public static class DynamicQueryFluentInclude
    {
        public static FluentInclude<T> WithQuery<T>(this FluentInclude<T> fi, Func<Expression<Func<T, object>>> lazyQuerySelector)
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
        /// Uses NicePluralName as niceName
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
        /// Uses NiceName as niceName
        /// </summary>
        public static FluentInclude<T> WithExpressionFrom<T, F>(this FluentInclude<T> fi, Expression<Func<F, T>> lambdaToMethodOrProperty)
            where T : Entity
        {
            QueryLogic.Expressions.Register(lambdaToMethodOrProperty, () => typeof(T).NiceName());
            return fi;
        }

        public static FluentInclude<T> WithExpressionFrom<T, F>(this FluentInclude<T> fi, Expression<Func<F, T>> lambdaToMethodOrProperty, Func<string> niceName)
            where T : Entity
        {
            QueryLogic.Expressions.Register(lambdaToMethodOrProperty, niceName);
            return fi;
        }

        /// <summary>
        /// Prefer WithExpressionFrom to keep dependencies between modules clean!. Uses NicePluralName as niceName. 
        /// </summary>
        public static FluentInclude<F> WithExpressionTo<F, T>(this FluentInclude<F> fi, Expression<Func<F, IQueryable<T>>> lambdaToMethodOrProperty)
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
        /// Prefer WithExpressionFrom to keep dependencies between modules clean!. Uses NiceName as niceName. 
        /// </summary>
        public static FluentInclude<F> WithExpressionTo<F, T>(this FluentInclude<F> fi, Expression<Func<F, T>> lambdaToMethodOrProperty)
            where F : Entity
            where T : Entity
        {
            QueryLogic.Expressions.Register(lambdaToMethodOrProperty, () => typeof(T).NiceName());
            return fi;
        }

        /// <summary>
        /// Prefer WithExpressionFrom to keep dependencies between modules clean!. Uses NiceName as niceName. 
        /// </summary>
        public static FluentInclude<F> WithExpressionTo<F, T>(this FluentInclude<F> fi, Expression<Func<F, Lite<T>>> lambdaToMethodOrProperty)
            where F : Entity
            where T : Entity
        {
            QueryLogic.Expressions.Register(lambdaToMethodOrProperty, () => typeof(T).NiceName());
            return fi;
        }

        /// <summary>
        /// Prefer WithExpressionFrom to keep dependencies between modules clean!.
        /// </summary>
        public static FluentInclude<F> WithExpressionTo<F, T>(this FluentInclude<F> fi, Expression<Func<F, Lite<T>>> lambdaToMethodOrProperty, Func<string> niceName)
            where F : Entity
            where T : Entity
        {
            QueryLogic.Expressions.Register(lambdaToMethodOrProperty, niceName);
            return fi;
        }
    }
}
