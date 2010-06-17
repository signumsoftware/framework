using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Specialized;
using Signum.Utilities.Properties;

namespace Signum.Utilities
{
    public static class DictionaryExtensions
    {
        public static V TryGetC<K, V>(this IDictionary<K, V> dictionary, K key) where V : class
        {
            if (dictionary == null)
                return null;

            V result;
            if (dictionary.TryGetValue(key, out result))
                return result;
            return null;
        }

        public static V? TryGetS<K, V>(this IDictionary<K, V> dictionary, K key) where V : struct
        {
            if (dictionary == null)
                return null;

            V result;
            if (dictionary.TryGetValue(key, out result))
                return result;
            return null;
        }

        public static V? TryGetS<K, V>(this IDictionary<K, V?> dictionary, K key) where V : struct
        {
            if (dictionary == null)
                return null;

            V? result;
            if (dictionary.TryGetValue(key, out result))
                return result;
            return null;
        }

        public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key) where V : new()
        {
            V result;
            if (!dictionary.TryGetValue(key, out result))
            {
                result = new V();
                dictionary.Add(key, result);
            }
            return result;
        }

        public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key, V value)
        {
            V result;
            if (!dictionary.TryGetValue(key, out result))
            {
                result = value;
                dictionary.Add(key, result);
            }
            return result;
        }

        public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> generator)
        {
            V result;
            if (!dictionary.TryGetValue(key, out result))
            {
                result = generator();
                dictionary.Add(key, result);
            }
            return result;
        }


        public static V GetOrThrow<K, V>(this IDictionary<K, V> dictionary, K key, string message)
        {
            V result;
            if (!dictionary.TryGetValue(key, out result))
                throw new KeyNotFoundException(message.Formato(key));
            return result;
        }

        public static Dictionary<K2, V2> SelectDictionary<K1, V1, K2, V2>(this IDictionary<K1, V1> dictionary, Func<K1, K2> mapKey, Func<V1, V2> mapValue)
        {
            return dictionary.ToDictionary(p => mapKey(p.Key), p => mapValue(p.Value));
        }

        public static Dictionary<K2, V2> SelectDictionary<K1, V1, K2, V2>(this IDictionary<K1, V1> dictionary, Func<K1, K2> mapKey, Func<K1, V1, V2> mapValue)
        {
            return dictionary.ToDictionary(p => mapKey(p.Key), p => mapValue(p.Key, p.Value));
        }

        public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> collection)
        {
             var result = new Dictionary<K,V>();
             foreach (var kvp in collection)
                 result.Add(kvp.Key, kvp.Value);
            return result;
        }

        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, string errorContext)
        {
            Dictionary<TKey, TSource> result = new Dictionary<TKey, TSource>();
            result.AddRange(source, keySelector, v => v, errorContext);
            return result;
        }

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, string errorContext)
        {
            Dictionary<TKey, TElement> result = new Dictionary<TKey, TElement>();
            result.AddRange(source, keySelector, elementSelector, errorContext);
            return result;
        }

        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer, string errorContext)
        {
            Dictionary<TKey, TSource> result = new Dictionary<TKey, TSource>(comparer);
            result.AddRange(source, keySelector, v => v, errorContext);
            return result;
        }

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer, string errorContext)
        {
            Dictionary<TKey, TElement> result = new Dictionary<TKey, TElement>(comparer);
            result.AddRange(source, keySelector, elementSelector, errorContext);
            return result;
        }

        public static Dictionary<K, V> JumpDictionary<K, Z, V>(this IDictionary<K, Z> dictionary, IDictionary<Z, V> other)
        {
            return dictionary.ToDictionary(p => p.Key, p => other[p.Value]);
        }

        public static Dictionary<K, V3> JoinDictionary<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Func<K, V1, V2, V3> mixer)
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.IntersectWith(dic2.Keys);

            return set.ToDictionary(k => k, k => mixer(k, dic1[k], dic2[k]));
        }

        public static void JoinDictionaryForeach<K, V1, V2>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Action<K, V1, V2> action)
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.IntersectWith(dic2.Keys);

            foreach (var k in set)
                action(k, dic1[k], dic2[k]);
        }

        public static Dictionary<K, V3> OuterJoinDictionaryCC<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Func<K, V1, V2, V3> mixer)
            where V1 : class
            where V2 : class
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionary(k => k, k => mixer(k, dic1.TryGetC(k), dic2.TryGetC(k)));
        }

        public static Dictionary<K, V3> OuterJoinDictionarySC<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Func<K, V1?, V2, V3> mixer)
            where V1 : struct
            where V2 : class
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionary(k => k, k => mixer(k, dic1.TryGetS(k), dic2.TryGetC(k)));
        }

        public static Dictionary<K, V3> OuterJoinDictionarySC<K, V1, V2, V3>(this IDictionary<K, V1?> dic1, IDictionary<K, V2> dic2, Func<K, V1?, V2, V3> mixer)
            where V1 : struct
            where V2 : class
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionary(k => k, k => mixer(k, dic1.TryGetS(k), dic2.TryGetC(k)));
        }

        public static Dictionary<K, V3> OuterJoinDictionaryCS<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Func<K, V1, V2?, V3> mixer)
            where V1 : class
            where V2 : struct
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionary(k => k, k => mixer(k, dic1.TryGetC(k), dic2.TryGetS(k)));
        }

        public static Dictionary<K, V3> OuterJoinDictionaryCS<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2?> dic2, Func<K, V1, V2?, V3> mixer)
            where V1 : class
            where V2 : struct
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionary(k => k, k => mixer(k, dic1.TryGetC(k), dic2.TryGetS(k)));
        }

        public static Dictionary<K, V3> OuterJoinDictionarySS<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Func<K, V1?, V2?, V3> mixer)
            where V1 : struct
            where V2 : struct
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionary(k => k, k => mixer(k, dic1.TryGetS(k), dic2.TryGetS(k)));
        }

        public static Dictionary<K, V3> OuterJoinDictionarySS<K, V1, V2, V3>(this IDictionary<K, V1?> dic1, IDictionary<K, V2?> dic2, Func<K, V1?, V2?, V3> mixer)
            where V1 : struct
            where V2 : struct
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionary(k => k, k => mixer(k, dic1.TryGetS(k), dic2.TryGetS(k)));
        }

        public static void AddRange<K, V>(this IDictionary<K, V> dictionary, IDictionary<K, V> other)
        {
            foreach (var item in other)
            {
                dictionary.Add(item.Key, item.Value);
            }
        }

        public static void AddRange<K, V>(this IDictionary<K, V> dictionary, IDictionary<K, V> other, string errorContext)
        {
            dictionary.AddRange(other, kvp => kvp.Key, kvp => kvp.Value, errorContext); 
        }

        public static void AddRange<K, V, A>(this IDictionary<K, V> dictionary, IEnumerable<A> collection, Func<A, K> keySelector, Func<A, V> valueSelector)
        {
            foreach (var item in collection)
                dictionary.Add(keySelector(item), valueSelector(item));
        }

        public static void AddRange<K, V, A>(this IDictionary<K, V> dictionary, IEnumerable<A> collection, Func<A, K> keySelector, Func<A, V> valueSelector, string errorContext)
        {
            HashSet<K> repetitions = new HashSet<K>();
            foreach (var item in collection)
            {
                var key = keySelector(item);
                if (dictionary.ContainsKey(key))
                    repetitions.Add(key);
                else
                    dictionary.Add(key, valueSelector(item));
            }

            if (repetitions.Count > 0)
                throw new ArgumentException(Resources.ThereAreSomeRepeated01.Formato(errorContext, repetitions.ToString(", ")));
        }

        public static void SetRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys, IEnumerable<V> values)
        {
            foreach (var item in keys.ZipStrict(values))
                dictionary[item.First] = item.Second;
        }

        public static void SetRange<K, V>(this IDictionary<K, V> dictionary, Dictionary<K, V> other)
        {
            foreach (var item in other)
                dictionary[item.Key] = item.Value;
        }

        public static void SetRange<K, V, A>(this IDictionary<K, V> dictionary, IEnumerable<A> collection, Func<A, K> getKey, Func<A, V> getValue)
        {
            foreach (var item in collection)
                dictionary[getKey(item)] = getValue(item);
        }

        public static void DefaultRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys, IEnumerable<V> values)
        {
            foreach (var item in keys.ZipStrict(values))
                if (!dictionary.ContainsKey(item.First))
                    dictionary[item.First] = item.Second;
        }

        public static void DefaultRange<K, V>(this IDictionary<K, V> dictionary, Dictionary<K, V> other)
        {
            foreach (var item in other)
                if (!dictionary.ContainsKey(item.Key))
                    dictionary[item.Key] = item.Value;
        }

        public static void DefaultRange<K, V, A>(this IDictionary<K, V> dictionary, IEnumerable<A> collection, Func<A, K> getKey, Func<A, V> getValue)
        {
            foreach (var item in collection)
            {
                var key = getKey(item);
                if (!dictionary.ContainsKey(key))
                    dictionary[key] = getValue(item);
            }
        }

        public static void RemoveRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys)
        {
            foreach (var k in keys)
                dictionary.Remove(k); 
        }

        public static Dictionary<K, V> Extract<K, V>(this IDictionary<K, V> dictionary, Func<K, bool> condition)
        {
            Dictionary<K, V> result = new Dictionary<K, V>();
            foreach (var key in dictionary.Keys.ToList())
            {
                if (condition(key))
                {
                    result.Add(key, dictionary[key]);
                    dictionary.Remove(key);
                }
            }
            return result; 
        }

        public static Dictionary<K, V> Extract<K, V>(this IDictionary<K, V> dictionary, Func<K, V, bool> condition)
        {
            Dictionary<K, V> result = new Dictionary<K, V>();
            var aux = new Dictionary<K,V>( dictionary);
            foreach (var kvp in aux)
            {
                if (condition(kvp.Key, kvp.Value))
                {
                    result.Add(kvp.Key, kvp.Value);
                    dictionary.Remove(kvp.Key);
                }
            }
            return result;
        }


        public static V Extract<K, V>(this IDictionary<K, V> dictionary, K key)
        {
            V value = dictionary[key];
            dictionary.Remove(key);
            return value;
        }

        public static Dictionary<V, K> Inverse<K, V>(this IDictionary<K, V> dic)
        {
            return dic.ToDictionary(k => k.Value, k => k.Key);
        }

        public static Dictionary<V, K> Inverse<K, V>(this IDictionary<K, V> dic, IEqualityComparer<V> comparer)
        {
            return dic.ToDictionary(k => k.Value, k => k.Key, comparer);
        }

        public static Dictionary<V, K> Inverse<K, V>(this IDictionary<K, V> dic, string errorContext)
        {
            return dic.ToDictionary(k => k.Value, k => k.Key, errorContext);
        }

        public static Dictionary<V, K> Inverse<K, V>(this IDictionary<K, V> dic, IEqualityComparer<V> comparer, string errorContext)
        {
            return dic.ToDictionary(k => k.Value, k => k.Key, comparer, errorContext);
        }
    }
}
