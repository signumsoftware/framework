using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Signum.Utilities.DataStructures
{
    public class LambdaComparer<T, S> : IComparer<T>, IEqualityComparer<T>, IComparer, IEqualityComparer
    {
        readonly Func<T, S> func;
        readonly IComparer<S> comparer = null;
        readonly IEqualityComparer<S> equalityComparer = null;

        int descending = 1;
        public bool Descending
        {
            get { return descending == -1; }
            set { descending = value ? -1 : 1; }
        }
        
        public LambdaComparer(Func<T, S> func, IEqualityComparer<S> equalityComparer = null, IComparer<S> comparer = null)
        {
            if (func == null)
                throw new ArgumentNullException("func");

            this.func = func;
            this.equalityComparer = equalityComparer ?? EqualityComparer<S>.Default;
            this.comparer = comparer ?? Comparer<S>.Default;
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

    public static class LambdaComparer
    {
        public static LambdaComparer<T, S> By<T, S>(Func<T, S> func, IEqualityComparer<S> equalityComparer = null, IComparer<S> comparer = null)
        {
            return new LambdaComparer<T, S>(func, equalityComparer, comparer);
        }

        public static LambdaComparer<T, S> ByDescending<T, S>(Func<T, S> func, IEqualityComparer<S> equalityComparer = null, IComparer<S> comparer = null)
        {
            return new LambdaComparer<T, S>(func, equalityComparer, comparer) { Descending = true };
        }

        public static IComparer<T> Then<T>(this IComparer<T> comparer1, IComparer<T> comparer2)
        {
            return new CombineComparer<T>(comparer1, comparer2);
        }

        public class CombineComparer<T> : IComparer<T>, IComparer
        {
            private IComparer<T> comparer1;
            private IComparer<T> comparer2;

            public CombineComparer(IComparer<T> comparer1, IComparer<T> comparer2)
            {
                this.comparer1 = comparer1;
                this.comparer2 = comparer2;
            }

            public int Compare(T x, T y)
            {
                return comparer1.Compare(x, y).DefaultToNull() ?? comparer2.Compare(x, y);
            }

            public int Compare(object x, object y)
            {
                return this.Compare((T)x, (T)y);
            }
        }

        public static IEqualityComparer<T> AndAlso<T>(this IEqualityComparer<T> comparer1, IEqualityComparer<T> comparer2)
        {
            return new CombineEqualityComparer<T>(comparer1, comparer2);
        }

        public class CombineEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer
        {
            private IEqualityComparer<T> comparer1;
            private IEqualityComparer<T> comparer2;

            public CombineEqualityComparer(IEqualityComparer<T> comparer1, IEqualityComparer<T> comparer2)
            {
                this.comparer1 = comparer1;
                this.comparer2 = comparer2;
            }
           
            public bool Equals(T x, T y)
            {
                return comparer1.Equals(x, y) && comparer2.Equals(x,y);
            }

            public new bool Equals(object x, object y) => this.Equals((T)x, (T)y);

            public int GetHashCode(T obj)
            {
                return comparer1.GetHashCode(obj) ^ comparer2.GetHashCode(obj);
            }

            public int GetHashCode(object obj) => this.GetHashCode((T)obj);
            
        }
    }
}
