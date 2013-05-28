using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities.DataStructures
{
    public class ReadOnlyDictionary<K, V> : IDictionary<K, V>
    {
        private IDictionary<K, V> dictionary;

        public ReadOnlyDictionary()
        {
            dictionary = new Dictionary<K, V>();
        }

        public ReadOnlyDictionary(IDictionary<K, V> dictionary)
        {
            if (dictionary is ReadOnlyDictionary<K, V>)
                this.dictionary = ((ReadOnlyDictionary<K, V>)dictionary).dictionary;
            else
                this.dictionary = dictionary;
        }

        #region IDictionary<TKey,TValue> Members

        void IDictionary<K, V>.Add(K key, V value)
        {
            throw ReadOnlyException();
        }

        public bool ContainsKey(K key)
        {
            return dictionary.ContainsKey(key);
        }

        public ICollection<K> Keys
        {
            get { return dictionary.Keys; }
        }

        bool IDictionary<K, V>.Remove(K key)
        {
            throw ReadOnlyException();
        }

        public bool TryGetValue(K key, out V value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public ICollection<V> Values
        {
            get { return dictionary.Values; }
        }

        public V this[K key]
        {
            get
            {
                return dictionary[key];
            }
        }

        V IDictionary<K, V>.this[K key]
        {
            get
            {
                return this[key];
            }
            set
            {
                throw ReadOnlyException();
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item)
        {
            throw ReadOnlyException();
        }

        void ICollection<KeyValuePair<K, V>>.Clear()
        {
            throw ReadOnlyException();
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            return dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            dictionary.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item)
        {
            throw ReadOnlyException();
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private static Exception ReadOnlyException()
        {
            return new NotSupportedException("This dictionary is read-only");
        }
    }
}
