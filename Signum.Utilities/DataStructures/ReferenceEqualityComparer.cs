using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace Signum.Utilities.DataStructures
{
    [Serializable]
    public class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        static ReferenceEqualityComparer<T> _default;
        
        ReferenceEqualityComparer() { }
        
        public static ReferenceEqualityComparer<T> Default
        {
            get { return _default ?? (_default = new ReferenceEqualityComparer<T>()); }
        }

        public int GetHashCode(T item)
        {
            return RuntimeHelpers.GetHashCode(item);
        }

        public bool Equals(T i1, T i2)
        {
            return object.ReferenceEquals(i1, i2);
        }
    }

}
