using Signum.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine
{
    public static class LinqHintsExpand
    {
        public static IQueryable<T> ExpandLite<T, L>(this IQueryable<T> source, Expression<Func<T, Lite<L>>> liteSelector, ExpandLite expandLite)
            where L : class, IEntity
        {
            if (source == null)
                throw new ArgumentNullException("query");

            return source.Provider.CreateQuery<T>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(T), typeof(L) }), new Expression[] { source.Expression, Expression.Quote(liteSelector), Expression.Constant(expandLite) }));
        }

        public static IQueryable<T> ExpandEntity<T, L>(this IQueryable<T> source, Expression<Func<T, L>> entitySelector, ExpandEntity expandEntity)
            where L : class, IEntity
        {
            if (source == null)
                throw new ArgumentNullException("query");

            return source.Provider.CreateQuery<T>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(T), typeof(L) }), new Expression[] { source.Expression, Expression.Quote(entitySelector), Expression.Constant(expandEntity) }));
        }
    }

    public enum ExpandLite
    {
        EntityEager,
        //Default,
        ToStringEager,
        ToStringLazy,
        ToStringNull,
    }

    public enum ExpandEntity
    {
        EagerEntity,
        LazyEntity,
    }
}
