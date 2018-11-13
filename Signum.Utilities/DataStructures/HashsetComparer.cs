using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Signum.Utilities.DataStructures
{
    [Serializable]
    public class HashSetComparer<T> : IEqualityComparer<HashSet<T>>, IEqualityComparer
    {
        public bool Equals(HashSet<T> x, HashSet<T> y)
        {
            return x.SetEquals(y);
        }

        public int GetHashCode(HashSet<T> obj)
        {
            var comparer = obj.Comparer;
            return obj.Aggregate(0, (acum, o) => acum ^ comparer.GetHashCode(o));
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return Equals((HashSet<T>)x, (HashSet<T>)y);
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            return GetHashCode((HashSet<T>)obj);
        }
    }
}
