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

        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, string keyName)
        {
            Dictionary<TKey, TSource> result = new Dictionary<TKey, TSource>();
            HashSet<TKey> repetitions = new HashSet<TKey>(); 
            foreach (var item in source)
            {
                var key = keySelector(item); 
                if(result.ContainsKey(key))
                    repetitions.Add(key);
                else
                    result.Add(key, item);
            }

            if (repetitions.Count > 0)
                throw new ArgumentException(Resources.ThereAreSomeRepeated01.Formato(keyName, repetitions.ToString(", ")));

            return result;
        }

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, string keyName)
        {
            Dictionary<TKey, TElement> result = new Dictionary<TKey, TElement>();
            HashSet<TKey> repetitions = new HashSet<TKey>();
            foreach (var item in source)
            {
                var key = keySelector(item);
                if (result.ContainsKey(key))
                    repetitions.Add(key);
                else
                    result.Add(key, elementSelector(item));
            }

            if (repetitions.Count > 0)
                throw new ArgumentException(Resources.ThereAreSomeRepeated01.Formato(keyName, repetitions.ToString(", ")));

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

        public static void AddRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys, IEnumerable<V> values)
        {
            foreach (var item in keys.ZipStrict(values))
                dictionary.Add(item.First, item.Second);
        }

        public static void AddRange<K, V>(this IDictionary<K, V> dictionary, Dictionary<K, V> other)
        {
            foreach (var item in other)
            {
                dictionary.Add(item.Key, item.Value);
            }
        }

        public static void AddRange<K, V, A>(this IDictionary<K, V> dictionary, IEnumerable<A> collection, Func<A, K> getKey, Func<A, V> getValue)
        {
            foreach (var item in collection)
                dictionary.Add(getKey(item), getValue(item));
        }

        public static void SetRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys, IEnumerable<V> values)
        {
            foreach (var item in keys.ZipStrict(values))
                dictionary[item.First] = item.Second;
        }

        public static void SetRange<K, V>(this IDictionary<K, V> dictionary, Dictionary<K, V> other)
        {
            foreach (var item in other)
            {
                dictionary[item.Key] = item.Value;
            }
        }

        public static void SetRange<K, V, A>(this IDictionary<K, V> dictionary, IEnumerable<A> collection, Func<A, K> getKey, Func<A, V> getValue)
        {
            foreach (var item in collection)
                dictionary[getKey(item)] = getValue(item);
        }

        public static void RemoveRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys)
        {
            foreach (var k in keys)
                dictionary.Remove(k); 
        }

        public static Dictionary<K, V> Union<K, V>(this IDictionary<K, V> dictionary, IDictionary<K, V> other)
        {
            Dictionary<K, V> result = new Dictionary<K, V>(dictionary);
            foreach (var kvp in other)
            {
                V value = result.GetOrCreate(kvp.Key, kvp.Value);
                Debug.Assert(EqualityComparer<V>.Default.Equals(value, kvp.Value));
            }
            return result; 
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
    }
}
