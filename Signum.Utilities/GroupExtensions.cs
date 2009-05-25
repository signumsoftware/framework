using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Properties;

namespace Signum.Utilities
{
    public static class GroupExtensions
    {
        public static Dictionary<K, T> GroupDistinctToDictionary<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector)
        {
            return collection
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.Distinct().AssertSingle(g.Key));
        }

        public static Dictionary<K, V> GroupDistinctToDictionary<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector)
        {
            return collection
                .GroupBy(keySelector, valueSelector)
                .ToDictionary(g => g.Key, g => g.Distinct().AssertSingle(g.Key));
        }

        public static T AssertSingle<T>(this IEnumerable<T> collection, object key)
        {
            int c = collection.Count();
            if (c == 0) throw new InvalidOperationException(Resources.NoElementExistWithKey0.Formato(key));
            if (c > 1) throw new InvalidOperationException(Resources.ThereIsMoreThanOneElementWithKey0.Formato(key));
            return collection.Single();
        }

        public static Dictionary<K, List<T>> GroupToDictionary<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector)
        {
            return collection
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public static Dictionary<K, List<V>> GroupToDictionary<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector)
        {
            return collection
                .GroupBy(keySelector, valueSelector)
                .ToDictionary(g => g.Key, g=>g.ToList());
        }

        public static Dictionary<K, List<V>> GroupToDictionary<T, K, V>(this IQueryable<T> collection, Expression<Func<T, K>> keySelector, Expression<Func<T, V>> valueSelector)
        {
            return collection
                .GroupBy(keySelector, valueSelector)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public static Dictionary<K, List<T>> GroupToDictionaryDescending<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector)
        {
            return collection
                .GroupBy(keySelector)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public static Dictionary<K, List<V>> GroupToDictionaryDescending<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector)
        {
            return collection
                .GroupBy(keySelector, valueSelector)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public static Dictionary<K, List<V>> GroupToDictionaryDescending<T, K, V>(this IQueryable<T> collection, Expression<Func<T, K>> keySelector, Expression<Func<T, V>> valueSelector)
        {
            return collection
                .GroupBy(keySelector, valueSelector)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public static Dictionary<T, int> GroupCount<T>(this IEnumerable<T> collection)
        {
            return collection
                .GroupBy(a=>a)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public static Dictionary<K, int> GroupCount<T, K>(this IEnumerable<T> collection, Func<T, K> keySelector)
        {
            return collection
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public static Dictionary<K, int> GroupCount<T, K>(this IQueryable<T> collection, Expression<Func<T, K>> keySelector)
        {
            return collection
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionary(g => g.Key, g => g.Count);
        }

        public static Dictionary<K, V> AgGroupToDictionary<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<IGrouping<K, T>, V> agregateSelector)
        {
            return collection
                .GroupBy(t => keySelector(t))
                .ToDictionary(g => g.Key, agregateSelector);
        }

        public static Dictionary<K, V> AgGroupToDictionary<T, K, V>(this IQueryable<T> collection, Expression<Func<T, K>> keySelector, Expression<Func<IGrouping<K, T>, V>> agregateSelector)
        {
            var query =  collection.ToExpandable()
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Aggregate = agregateSelector.Expand(g) });

            return query.ToDictionary(g => g.Key, g=>g.Aggregate);
        }

        public static Dictionary<K, V> AgGroupToDictionaryDescending<T, K, V>(this IEnumerable<T> collection, Func<T, K> keySelector, Func<IGrouping<K, T>, V> agregateSelector)
        {
            return collection
                .GroupBy(t => keySelector(t))
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, agregateSelector);
        }

        public static Dictionary<K, V> AgGroupToDictionaryDescending<T, K, V>(this IQueryable<T> collection, Expression<Func<T, K>> keySelector, Func<IGrouping<K, T>, V> agregateSelector)
        {
            return collection
                .GroupBy(keySelector)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, agregateSelector);
        }
    }
}
