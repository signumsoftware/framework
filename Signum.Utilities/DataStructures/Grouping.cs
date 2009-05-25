using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities.DataStructures
{
    public class Grouping<K, T> : List<T>, IGrouping<K, T>
    {
        public K Key{get; private set;}

        public Grouping(K key)
        {
            this.Key = key;
        }

        public Grouping(K key, IEnumerable<T> collection)
        {
            this.Key = key;
            AddRange(collection);
        }
    }
}
