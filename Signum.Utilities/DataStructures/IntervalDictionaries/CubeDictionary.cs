using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Linq;

namespace Signum.Utilities.DataStructures
{
    public class CubeDictionary<K1, K2, K3, V>
        where K1 : struct, IComparable<K1>, IEquatable<K1>
        where K2 : struct, IComparable<K2>, IEquatable<K2>
        where K3 : struct, IComparable<K3>, IEquatable<K3>
    {
        IntervalDictionary<K1, int> xDimension;
        IntervalDictionary<K2, int> yDimension;
        IntervalDictionary<K3, int> zDimension;
        V[, ,] values;
        bool[, ,] used; 

        public CubeDictionary(IEnumerable<(Cube<K1, K2, K3> cube, V value)> dic)
        {
            IEnumerable<Cube<K1, K2, K3>> cubes = dic.Select(p => p.cube);

            xDimension = cubes.ToIndexIntervalDictinary(c =>c.XInterval.Elements());
            yDimension = cubes.ToIndexIntervalDictinary(c =>c.YInterval.Elements());
            zDimension = cubes.ToIndexIntervalDictinary(c =>c.ZInterval.Elements());

            values = new V[xDimension.Count, yDimension.Count, zDimension.Count];
            used = new bool[xDimension.Count, yDimension.Count, zDimension.Count];


            foreach (var item in dic)
                Add(item.cube, item.value);
        }

        void Add(Cube<K1, K2, K3> cube, V value)
        {
            Interval<int> xs = xDimension.FindIntervalIndex(cube.XInterval);
            Interval<int> ys = yDimension.FindIntervalIndex(cube.YInterval);
            Interval<int> zs = zDimension.FindIntervalIndex(cube.ZInterval);

            for (int x = xs.Min; x < xs.Max; x++)
                for (int y = ys.Min; y < ys.Max; y++)
                    for (int z = zs.Min; z < zs.Max; z++)
                    {
                        if (used[x, y, z])
                            throw new InvalidOperationException(string.Format("Inconsistence found on cube [{0}, {1}, {2}], could have values '{3}' or '{4}'", xDimension.Intervals[x], yDimension.Intervals[y], zDimension.Intervals[z], values[x, y, z], value));

                        values[x, y, z] = value;

                        used[x, y, z] = true;
                    }
        }

        public V this[K1 x, K2 y, K3 z]
        {
            get
            {
                if (!xDimension.TryGetValue(x, out int ix) ||
                    !yDimension.TryGetValue(y, out int iy) ||
                    !zDimension.TryGetValue(z, out int iz) || !used[ix, iy, iz])
                    throw new KeyNotFoundException("Cube not found");

                return values[ix, iy, iz];
            }
        }


        public bool TryGetValue(K1 x, K2 y, K3 z, out V value)
        {
            if (!xDimension.TryGetValue(x, out int ix) ||
                !yDimension.TryGetValue(y, out int iy) ||
                !zDimension.TryGetValue(z, out int iz) || !used[ix, iy, iz])
            {
                value = default(V);
                return false;
            }

            value = values[ix, iy,iz];
            return true;
        }

        public IntervalValue<V> TryGetValue(K1 x, K2 y, K3 z)
        {
            if (!xDimension.TryGetValue(x, out int ix) ||
                !yDimension.TryGetValue(y, out int iy) ||
                !zDimension.TryGetValue(z, out int iz) || !used[ix, iy, iz]) 
            {
                return new IntervalValue<V>();
            }

            return new IntervalValue<V>(values[ix, iy, iz]);
        }

        public string Counts()
        {
            return this.xDimension.Count + " x " + this.yDimension.Count + " x " + this.zDimension.Count;
        }
    }
}
