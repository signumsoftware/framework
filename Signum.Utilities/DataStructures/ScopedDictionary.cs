using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities.DataStructures
{
    public class ScopedDictionary<TKey, TValue>
    {
        ScopedDictionary<TKey, TValue> previous;
        Dictionary<TKey, TValue> map;
        public IEqualityComparer<TKey> Comparer { get { return map.Comparer; } } 

        public ScopedDictionary(ScopedDictionary<TKey, TValue> previous)
            :this(previous, EqualityComparer<TKey>.Default)
        {
            this.previous = previous;
            this.map = new Dictionary<TKey, TValue>();
        }

        public ScopedDictionary(ScopedDictionary<TKey, TValue> previous, IEqualityComparer<TKey> comparer)
        {
            this.previous = previous;
            this.map = new Dictionary<TKey, TValue>(comparer);
        }

        public void Add(TKey key, TValue value)
        {
            this.map.Add(key, value);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            for (ScopedDictionary<TKey, TValue> scope = this; scope != null; scope = scope.previous)
            {
                if (scope.map.TryGetValue(key, out value))
                    return true;
            }
            value = default(TValue);
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            for (ScopedDictionary<TKey, TValue> scope = this; scope != null; scope = scope.previous)
            {
                if (scope.map.ContainsKey(key))
                    return true;
            }
            return false;
        }

        public string ToString()
        {
            var str = map.ToString(kvp => kvp.Key.ToString() + " -> " + kvp.Value.ToString(), "\r\n");

            if (this.previous == null)
                return str;

            return str + "\r\n-----------------------\r\n" + previous.ToString();
        }

        public string ToString(Func<TKey, string> keyRenderer, Func<TValue, string> valueRenderer)
        {
            var str = map.ToString(kvp => keyRenderer(kvp.Key) + " -> " + valueRenderer(kvp.Value), "\r\n");

            if (this.previous == null)
                return str;

            return str + "\r\n-----------------------\r\n" + previous.ToString(keyRenderer, valueRenderer);
        }

        public TValue GetOrCreate(TKey key, Func<TValue> valueFactory)
        {
            TValue result;
            if (!TryGetValue(key, out result))
            {
                result = valueFactory();
                Add(key, result);
            }
            return result;
        }
    }
}
