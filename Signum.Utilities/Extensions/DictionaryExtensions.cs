using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Signum.Utilities
{
    public static class DictionaryExtensions
    {
        public static V TryGet<K, V>(this IReadOnlyDictionary<K, V> dictionary, K key, V defaultValue)
        {
            if (dictionary.TryGetValue(key, out V result))
                return result;
            return defaultValue;
        }

        public static V TryGetC<K, V>(this IReadOnlyDictionary<K, V> dictionary, K key) where V : class
        {
            if (dictionary.TryGetValue(key, out V result))
                return result;
            return null;
        }

        public static V? TryGetS<K, V>(this IReadOnlyDictionary<K, V> dictionary, K key) where V : struct
        {
            if (dictionary.TryGetValue(key, out V result))
                return result;
            return null;
        }

        public static V? TryGetS<K, V>(this IReadOnlyDictionary<K, V?> dictionary, K key) where V : struct
        {
            if (dictionary.TryGetValue(key, out V? result))
                return result;
            return null;
        }

        public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key) where V : new()
        {
            if (!dictionary.TryGetValue(key, out V result))
            {
                result = new V();
                dictionary.Add(key, result);
            }
            return result;
        }

        public static V GetOrAdd<K, V>(this ConcurrentDictionary<K, V> dictionary, K key) where V : new()
        {
            return dictionary.GetOrAdd(key, k => new V());
        }

        public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key, V value)
        {
            if (!dictionary.TryGetValue(key, out V result))
            {
                result = value;
                dictionary.Add(key, result);
            }
            return result;
        }

        public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> generator)
        {
            if (!dictionary.TryGetValue(key, out V result))
            {
                result = generator();
                dictionary.Add(key, result);
            }
            return result;
        }

        public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> generator)
        {
            if (!dictionary.TryGetValue(key, out V result))
            {
                result = generator(key);
                dictionary.Add(key, result);
            }
            return result;
        }

        public static V GetOrThrow<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, Exception> exception)
        {
            if (!dictionary.TryGetValue(key, out V result))
                throw exception(key);
            return result;
        }

        public static V GetOrThrow<K, V>(this IDictionary<K, V> dictionary, K key, string messageWithFormat)
        {
            if (!dictionary.TryGetValue(key, out V result))
                throw new KeyNotFoundException(messageWithFormat.FormatWith(key));
            return result;
        }

        public static V GetOrThrow<K, V>(this IDictionary<K, V> dictionary, K key)
        {
            if (!dictionary.TryGetValue(key, out V result))
                throw new KeyNotFoundException("Key '{0}' ({1}) not found on {2}".FormatWith(key, key.GetType().TypeName(), dictionary.GetType().TypeName()));
            return result;
        }

        public static void AddOrThrow<K, V>(this IDictionary<K, V> dictionary, K key, V value, string messageWithFormat)
        {
            if (dictionary.ContainsKey(key))
                throw new ArgumentException(messageWithFormat.FormatWith(key));

            dictionary.Add(key, value);
        }

        public static Dictionary<K, V2> SelectDictionary<K, V1, V2>(this IDictionary<K, V1> dictionary, Func<V1, V2> mapValue)
        {
            return dictionary.ToDictionaryEx(k => k.Key, p => mapValue(p.Value));
        }

        public static Dictionary<K2, V2> SelectDictionary<K1, V1, K2, V2>(this IDictionary<K1, V1> dictionary, Func<K1, K2> mapKey, Func<V1, V2> mapValue)
        {
            return dictionary.ToDictionaryEx(p => mapKey(p.Key), p => mapValue(p.Value));
        }

        public static Dictionary<K2, V2> SelectDictionary<K1, V1, K2, V2>(this IDictionary<K1, V1> dictionary, Func<K1, K2> mapKey, Func<K1, V1, V2> mapValue)
        {
            return dictionary.ToDictionaryEx(p => mapKey(p.Key), p => mapValue(p.Key, p.Value));
        }

        public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> collection)
        {
            var result = new Dictionary<K, V>();
            result.AddRange<K, V>(collection);
            return result;
        }

        public static Dictionary<K, V> ToDictionaryEx<K, V>(this IEnumerable<KeyValuePair<K, V>> collection, string errorContext = null)
        {
            var result = new Dictionary<K, V>();
            result.AddRange<K, V>(collection, errorContext ?? typeof(K).TypeName());
            return result;
        }

        public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> collection, IEqualityComparer<K> comparer)
        {
            var result = new Dictionary<K, V>(comparer);
            result.AddRange<K, V>(collection);
            return result;
        }

        public static Dictionary<K, V> ToDictionaryEx<K, V>(this IEnumerable<KeyValuePair<K, V>> collection, IEqualityComparer<K> comparer, string errorContext = null)
        {
            var result = new Dictionary<K, V>(comparer);
            result.AddRange<K, V>(collection, errorContext ?? typeof(K).TypeName());
            return result;
        }


        public static Dictionary<K, T> ToDictionaryEx<T, K>(this IEnumerable<T> source, Func<T, K> keySelector, string errorContext = null)
        {
            Dictionary<K, T> result = new Dictionary<K, T>();
            result.AddRange(source, keySelector, v => v, errorContext ?? typeof(K).TypeName());
            return result;
        }

        public static Dictionary<K, V> ToDictionaryEx<T, K, V>(this IEnumerable<T> source, Func<T, K> keySelector, Func<T, V> elementSelector, string errorContext = null)
        {
            Dictionary<K, V> result = new Dictionary<K, V>();
            result.AddRange(source, keySelector, elementSelector, errorContext ?? typeof(K).TypeName());
            return result;
        }

        public static Dictionary<K, T> ToDictionaryEx<T, K>(this IEnumerable<T> source, Func<T, K> keySelector, IEqualityComparer<K> comparer, string errorContext = null)
        {
            Dictionary<K, T> result = new Dictionary<K, T>(comparer);
            result.AddRange(source, keySelector, v => v, errorContext ?? typeof(K).TypeName());
            return result;
        }

        public static Dictionary<K, V> ToDictionaryEx<T, K, V>(this IEnumerable<T> source, Func<T, K> keySelector, Func<T, V> elementSelector, IEqualityComparer<K> comparer, string errorContext = null)
        {
            Dictionary<K, V> result = new Dictionary<K, V>(comparer);
            result.AddRange(source, keySelector, elementSelector, errorContext ?? typeof(K).TypeName());
            return result;
        }

        public static Dictionary<K, V> JumpDictionary<K, Z, V>(this IDictionary<K, Z> dictionary, IDictionary<Z, V> other)
        {
            return dictionary.ToDictionaryEx(p => p.Key, p => other[p.Value]);
        }

        public static Dictionary<K, V3> JoinDictionary<K, V1, V2, V3>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Func<K, V1, V2, V3> mixer)
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.IntersectWith(dic2.Keys);

            return set.ToDictionaryEx(k => k, k => mixer(k, dic1[k], dic2[k]));
        }

        public static Dictionary<K, R> JoinDictionaryStrict<K, C, S, R>(
           Dictionary<K, C> currentDictionary,
           Dictionary<K, S> shouldDictionary,
           Func<C, S, R> resultSelector, string errorContext)
        {

            var currentOnly = currentDictionary.Keys.Where(k => !shouldDictionary.ContainsKey(k)).ToList();
            var shouldOnly = shouldDictionary.Keys.Where(k => !currentDictionary.ContainsKey(k)).ToList();

            if (currentOnly.Count != 0)
                if (shouldOnly.Count != 0)
                    throw new InvalidOperationException("Error {0}\r\n Extra: {1}\r\n Lacking: {2}".FormatWith(errorContext, currentOnly.ToString(", "), shouldOnly.ToString(", ")));
                else
                    throw new InvalidOperationException("Error {0}\r\n Extra: {1}".FormatWith(errorContext, currentOnly.ToString(", ")));
            else
                if (shouldOnly.Count != 0)
                throw new InvalidOperationException("Error {0}\r\n Missing: {1}".FormatWith(errorContext, shouldOnly.ToString(", ")));

            return currentDictionary.ToDictionaryEx(kvp => kvp.Key, kvp => resultSelector(kvp.Value, shouldDictionary[kvp.Key]));
        }

        public static void JoinDictionaryForeach<K, V1, V2>(this IDictionary<K, V1> dic1, IDictionary<K, V2> dic2, Action<K, V1, V2> action)
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.IntersectWith(dic2.Keys);

            foreach (var k in set)
                action(k, dic1[k], dic2[k]);
        }

        public static void JoinDictionaryForeachStrict<K, C, S>(
            Dictionary<K, C> currentDictionary,
            Dictionary<K, S> shouldDictionary,
            Action<K, C, S> mixAction, string errorContext)
        {

            var currentOnly = currentDictionary.Keys.Where(k => !shouldDictionary.ContainsKey(k)).ToList();
            var shouldOnly = shouldDictionary.Keys.Where(k => !currentDictionary.ContainsKey(k)).ToList();

            if (currentOnly.Count != 0)
                if (shouldOnly.Count != 0)
                    throw new InvalidOperationException("Error {0}\r\n Extra: {1}\r\n Lacking: {2}".FormatWith(errorContext, currentOnly.ToString(", "), shouldOnly.ToString(", ")));
                else
                    throw new InvalidOperationException("Error {0}\r\n Extra: {1}".FormatWith(errorContext, currentOnly.ToString(", ")));
            else
                if (shouldOnly.Count != 0)
                throw new InvalidOperationException("Error {0}\r\n Lacking: {1}".FormatWith(errorContext, shouldOnly.ToString(", ")));

            foreach (var kvp in currentDictionary)
            {
                mixAction(kvp.Key, kvp.Value, shouldDictionary[kvp.Key]);
            }
        }

        public static Dictionary<K, R> OuterJoinDictionaryCC<K, V1, V2, R>(this IReadOnlyDictionary<K, V1> dic1, IReadOnlyDictionary<K, V2> dic2, Func<K, V1, V2, R> mixer)
            where V1 : class
            where V2 : class
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionaryEx(k => k, k => mixer(k, dic1.TryGetC(k), dic2.TryGetC(k)));
        }

        public static Dictionary<K, R> OuterJoinDictionarySC<K, V1, V2, R>(this IReadOnlyDictionary<K, V1> dic1, IReadOnlyDictionary<K, V2> dic2, Func<K, V1?, V2, R> mixer)
            where V1 : struct
            where V2 : class
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionaryEx(k => k, k => mixer(k, dic1.TryGetS(k), dic2.TryGetC(k)));
        }

        public static Dictionary<K, V3> OuterJoinDictionarySC<K, V1, V2, V3>(this IReadOnlyDictionary<K, V1?> dic1, IReadOnlyDictionary<K, V2> dic2, Func<K, V1?, V2, V3> mixer)
            where V1 : struct
            where V2 : class
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionaryEx(k => k, k => mixer(k, dic1.TryGetS(k), dic2.TryGetC(k)));
        }

        public static Dictionary<K, R> OuterJoinDictionaryCS<K, V1, V2, R>(this IReadOnlyDictionary<K, V1> dic1, IReadOnlyDictionary<K, V2> dic2, Func<K, V1, V2?, R> mixer)
            where V1 : class
            where V2 : struct
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionaryEx(k => k, k => mixer(k, dic1.TryGetC(k), dic2.TryGetS(k)));
        }

        public static Dictionary<K, R> OuterJoinDictionaryCS<K, V1, V2, R>(this IReadOnlyDictionary<K, V1> dic1, IReadOnlyDictionary<K, V2?> dic2, Func<K, V1, V2?, R> mixer)
            where V1 : class
            where V2 : struct
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionaryEx(k => k, k => mixer(k, dic1.TryGetC(k), dic2.TryGetS(k)));
        }

        public static Dictionary<K, R> OuterJoinDictionarySS<K, V1, V2, R>(this IReadOnlyDictionary<K, V1> dic1, IReadOnlyDictionary<K, V2> dic2, Func<K, V1?, V2?, R> mixer)
            where V1 : struct
            where V2 : struct
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionaryEx(k => k, k => mixer(k, dic1.TryGetS(k), dic2.TryGetS(k)));
        }

        public static Dictionary<K, R> OuterJoinDictionarySS<K, V1, V2, R>(this IReadOnlyDictionary<K, V1?> dic1, IReadOnlyDictionary<K, V2?> dic2, Func<K, V1?, V2?, R> mixer)
            where V1 : struct
            where V2 : struct
        {
            HashSet<K> set = new HashSet<K>();
            set.UnionWith(dic1.Keys);
            set.UnionWith(dic2.Keys);

            return set.ToDictionaryEx(k => k, k => mixer(k, dic1.TryGetS(k), dic2.TryGetS(k)));
        }

        public static void AddRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys, IEnumerable<V> values)
        {
            foreach (var item in keys.ZipStrict(values))
                dictionary.Add(item.first, item.second);
        }

        public static void AddRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys, IEnumerable<V> values, string errorContext)
        {
            dictionary.AddRange(keys.ZipStrict(values), t => t.first, t => t.second, errorContext);
        }

        public static void AddRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<KeyValuePair<K, V>> collection)
        {
            foreach (var kvp in collection)
                dictionary.Add(kvp.Key, kvp.Value);
        }

        public static void AddRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<KeyValuePair<K, V>> collection, string errorContext)
        {
            dictionary.AddRange(collection, kvp => kvp.Key, kvp => kvp.Value, errorContext);
        }

        public static void AddRange<K, V, T>(this IDictionary<K, V> dictionary, IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector)
        {
            foreach (var item in collection)
                dictionary.Add(keySelector(item), valueSelector(item));
        }


        public static int ErrorExampleLimit = 10;
        public static void AddRange<K, V, T>(this IDictionary<K, V> dictionary, IEnumerable<T> collection, Func<T, K> keySelector, Func<T, V> valueSelector, string errorContext)
        {
            Dictionary<K, List<V>> repetitions = new Dictionary<K, List<V>>();
            foreach (var item in collection)
            {
                var key = keySelector(item);
                if (dictionary.ContainsKey(key))
                {
                    repetitions.GetOrCreate(key, () => new List<V> { dictionary[key] }).Add(valueSelector(item));
                }
                else
                    dictionary.Add(key, valueSelector(item));
            }

            if (repetitions.Count > 0)
                throw new ArgumentException($@"There are some repeated {errorContext}...
{repetitions.ToString(kvp => $@"Key ""{kvp.Key}"" has {kvp.Value.Count} repetitions:
{kvp.Value.Take(ErrorExampleLimit).ToString("\r\n").Indent(4)}", "\r\n")}");
        }

        public static void SetRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<KeyValuePair<K, V>> collection)
        {
            foreach (var item in collection)
                dictionary[item.Key] = item.Value;
        }

        public static void SetRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys, IEnumerable<V> values)
        {
            foreach (var item in keys.ZipStrict(values))
                dictionary[item.first] = item.second;
        }

        public static void SetRange<K, V, A>(this IDictionary<K, V> dictionary, IEnumerable<A> collection, Func<A, K> getKey, Func<A, V> getValue)
        {
            foreach (var item in collection)
                dictionary[getKey(item)] = getValue(item);
        }

        public static void DefaultRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<KeyValuePair<K, V>> collection)
        {
            foreach (var item in collection)
                if (!dictionary.ContainsKey(item.Key))
                    dictionary[item.Key] = item.Value;
        }

        public static void DefaultRange<K, V>(this IDictionary<K, V> dictionary, IEnumerable<K> keys, IEnumerable<V> values)
        {
            foreach (var item in keys.ZipStrict(values))
                if (!dictionary.ContainsKey(item.first))
                    dictionary[item.first] = item.second;
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

        public static void RemoveAll<K, V>(this IDictionary<K, V> dictionary, Func<KeyValuePair<K, V>, bool> condition)
        {
            dictionary.RemoveRange(dictionary.Where(condition).Select(a => a.Key).ToList());
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
            var aux = new Dictionary<K, V>(dictionary);
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
            V value = dictionary.GetOrThrow(key);
            dictionary.Remove(key);
            return value;
        }

        public static V Extract<K, V>(this IDictionary<K, V> dictionary, K key, string messageWithFormat)
        {
            V value = dictionary.GetOrThrow(key, messageWithFormat);
            dictionary.Remove(key);
            return value;
        }

        public static V Extract<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, Exception> exception)
        {
            V value = dictionary.GetOrThrow(key, exception);
            dictionary.Remove(key);
            return value;
        }

        public static Dictionary<V, K> Inverse<K, V>(this IDictionary<K, V> dic)
        {
            return dic.ToDictionaryEx(k => k.Value, k => k.Key);
        }

        public static Dictionary<V, K> Inverse<K, V>(this IDictionary<K, V> dic, IEqualityComparer<V> comparer)
        {
            return dic.ToDictionary(k => k.Value, k => k.Key, comparer);
        }

        public static Dictionary<V, K> Inverse<K, V>(this IDictionary<K, V> dic, string errorContext)
        {
            return dic.ToDictionaryEx(k => k.Value, k => k.Key, errorContext);
        }

        public static Dictionary<V, K> Inverse<K, V>(this IDictionary<K, V> dic, IEqualityComparer<V> comparer, string errorContext)
        {
            return dic.ToDictionaryEx(k => k.Value, k => k.Key, comparer, errorContext);
        }

        public static bool Decrement<K>(this IDictionary<K, int> dic, K key)
        {
            if (!dic.TryGetValue(key, out int count))
                return false;

            if (count == 1)
                dic.Remove(key);
            else
                dic[key] = count - 1;

            return true;
        }

        public static void Increment<K>(this IDictionary<K, int> dic, K key)
        {
            if (!dic.TryGetValue(key, out int count))
                dic[key] = 1;
            else
                dic[key] = count + 1;
        }

        public static NameValueCollection ToNameValueCollection<K, V>(this IDictionary<K, V> dic)
        {
            var collection = new NameValueCollection();

            foreach (var kvp in dic)
            {
                string value = null;
                if (kvp.Value != null)
                    value = kvp.Value.ToString();

                collection.Add(kvp.Key.ToString(), value);
            }

            return collection;
        }
    }

    public static class KVP
    {
        public static KeyValuePair<K, V> Create<K, V>(K key, V value)
        {
            return new KeyValuePair<K, V>(key, value);
        }
    }
}
