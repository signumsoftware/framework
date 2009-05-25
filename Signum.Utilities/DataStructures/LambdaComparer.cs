using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Signum.Utilities.DataStructures
{
    public class LambdaComparer<T, S> : IComparer<T>, IEqualityComparer<T>, IComparer, IEqualityComparer
    {
        Func<T, S> func;
        IComparer<S> comparer = Comparer<S>.Default;
        IEqualityComparer<S> equalityComparer = EqualityComparer<S>.Default;

        public LambdaComparer(Func<T, S> func)
        {
            if (func == null)
                throw new ArgumentNullException("func");

            this.func = func;
        }

        public int Compare(T x, T y)
        {
            return comparer.Compare(func(x), func(y));
        }

        public bool Equals(T x, T y)
        {
            return equalityComparer.Equals(func(x), func(y));
        }

        public int GetHashCode(T obj)
        {
            return equalityComparer.GetHashCode(func(obj));
        }

        public int GetHashCode(object obj)
        {
            return equalityComparer.GetHashCode(func((T)obj));
        }

        public int Compare(object x, object y)
        {
            return comparer.Compare(func((T)x), func((T)y));
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return equalityComparer.Equals(func((T)x), func((T)y));
        }
    }
}
