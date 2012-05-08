using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Signum.Utilities
{
    public static class QueryableExtensions
    {
        public static IOrderedQueryable<T> Order<T>(this IQueryable<T> collection) where T : IComparable<T>
        {
            return collection.OrderBy(a => a);
        }

        public static IOrderedQueryable<T> OrderDescending<T>(this IQueryable<T> collection) where T : IComparable<T>
        {
            return collection.OrderByDescending(a => a);
        }
    }
}
