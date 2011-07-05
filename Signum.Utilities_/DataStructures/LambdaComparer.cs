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
        IComparer<S> comparer = null;
        IEqualityComparer<S> equalityComparer = null;
        
        public LambdaComparer(Func<T, S> func) : this(func, EqualityComparer<S>.Default, Comparer<S>.Default) { }

        public LambdaComparer(Func<T, S> func, IEqualityComparer<S> equalityComparer, IComparer<S> comparer)
        {
            if (func == null)
                throw new ArgumentNullException("func");

            if (equalityComparer == null)
                throw new ArgumentNullException("equalityComparer"); 

            this.func = func;
            this.equalityComparer = equalityComparer;
            this.comparer = comparer;
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
