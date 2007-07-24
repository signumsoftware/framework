using System;
using System.Collections.Generic;
using System.Text;

namespace SerializadorTexto.SerializadorAPila.LenguajeIncontextual
{
    internal class MultiDictionary<K, V> : IEnumerable<V>
    {
        Dictionary<K, List<V>> dictionary = new Dictionary<K, List<V>>();

        public void Add(K key, V value)
        {
            List<V> listaVs = GetOrCreateList(key);

            listaVs.Add(value);
        }

        public bool AddNoRepeat(K key, V value)
        {
            List<V> listaVs = GetOrCreateList(key);

            if (!listaVs.Contains(value))
            {
                listaVs.Add(value);
                return true;
            }
            return false; 
        }

        public bool AddRangeNoRepeat(K key, List<V> values)
        {
            List<V> listaVs = GetOrCreateList(key);
            bool result = false;
            foreach (V value in values)
            {
                if (!listaVs.Contains(value))
                {
                    listaVs.Add(value);
                    result = true; 
                }
            }
            return result; 
        }

        public void AddRange(K key, List<V> values)
        {
            List<V> listaVs = GetOrCreateList(key);

            listaVs.AddRange(values);
        }


        private List<V> GetOrCreateList(K key)
        {
            List<V> listaVs;
            if (!dictionary.TryGetValue(key, out listaVs))
            {
                listaVs = new List<V>();
                dictionary.Add(key, listaVs);
            }
            return listaVs;
        }

        public List<V> GetList(K key)
        {
            return dictionary[key];
        }


        internal bool TryGetValue(K key, out List<V> values)
        {
            return dictionary.TryGetValue(key, out values); 
        }

        #region IEnumerable<V> Members

        public IEnumerator<V> GetEnumerator()
        {
            foreach (KeyValuePair<K,List<V>> kvp in dictionary)
            {
                foreach (V val in kvp.Value)
                {
                    yield return val; 
                }
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator(); 
        }

        #endregion

       
    }
}
