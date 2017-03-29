using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Linq;

namespace Signum.Utilities.DataStructures
{
    public class SquareDictionary<K1, K2, V>
        where K1 : struct, IComparable<K1>, IEquatable<K1>
        where K2 : struct, IComparable<K2>, IEquatable<K2>
    {
        IntervalDictionary<K1, int> xDimension;
        IntervalDictionary<K2, int> yDimension;
        V[,] values;
        bool[,] used;  


        public SquareDictionary(IEnumerable<(Square<K1, K2> square, V value)> dictionary)
        {
            IEnumerable<Square<K1, K2>> squares = dictionary.Select(p => p.square);

            xDimension = squares.ToIndexIntervalDictinary(s => s.XInterval.Elements());
            yDimension = squares.ToIndexIntervalDictinary(s => s.YInterval.Elements());

            values = new V[xDimension.Count, yDimension.Count];
            used = new bool[xDimension.Count, yDimension.Count]; 

            foreach (var item in dictionary)
                Add(item.square, item.value);
        }

        void Add(Square<K1, K2> square, V value)
        {
            Interval<int> xs = xDimension.FindIntervalIndex(square.XInterval);
            Interval<int> ys = yDimension.FindIntervalIndex(square.YInterval);

            for (int x = xs.Min; x < xs.Max; x++)
                for (int y = ys.Min; y < ys.Max; y++)
                {
                    if (used[x, y])
                        throw new InvalidOperationException(string.Format("Inconsistende found on square [{0},{1}] could have values '{2}' or '{3}'", xDimension.Intervals[x], yDimension.Intervals[y], values[x, y], value));

                    values[x,y] = value;

                    used[x, y] = true;
                }
        }

        public V this[K1 x, K2 y]
        {
            get
            {
                if (!xDimension.TryGetValue(x, out int ix) ||
                    !yDimension.TryGetValue(y, out int iy) || !used[ix, iy])
                    throw new KeyNotFoundException("Square not found"); 

                return values[ix, iy];
            }
        }

        public bool TryGetValue(K1 x, K2 y, out V value)
        {
            if (!xDimension.TryGetValue(x, out int ix) ||
                !yDimension.TryGetValue(y, out int iy) || !used[ix,iy])
            {
                value = default(V); 
                return false;
            }

            value = values[ix, iy];
            return true; 
        }

        public IntervalValue<V> TryGetValue(K1 x, K2 y)
        {
            if (!xDimension.TryGetValue(x, out int ix) ||
                !yDimension.TryGetValue(y, out int iy) || !used[ix, iy])
            {
                return new IntervalValue<V>();
            }

            return new IntervalValue<V>(values[ix, iy]); 
        }

        public string Counts()
        {
            return this.xDimension.Count + " x " + this.yDimension.Count; 
        }
    }
}
