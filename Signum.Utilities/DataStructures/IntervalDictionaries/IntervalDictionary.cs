using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Linq;
using Signum.Utilities;
using System.Diagnostics;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Utilities.DataStructures
{
    [Serializable]
    public class IntervalDictionary<K,V>: IEnumerable<KeyValuePair<Interval<K>, V>> 
        where K: struct, IComparable<K>, IEquatable<K>
    {
        SortedList<Interval<K>, V> dic = new SortedList<Interval<K>, V>();

        public IntervalDictionary()
        {
        }

        public IntervalDictionary(IEnumerable<KeyValuePair<Interval<K>,V>> pairs)
        {
            foreach (var kvp in pairs)
            {
                Add(kvp.Key, kvp.Value);
            }
        }

        public IList<Interval<K>> Intervals
        {
            get { return dic.Keys; }
        }

        public int Count
        {
            get { return dic.Count; }
        }

        public void Add(K min, K max, V value)
        {
            Add(new Interval<K>(min, max), value);
        }

        public void Add(Interval<K> interval, V value)
        {
            if (interval.IsEmpty)
                return;

            if (dic.Count != 0) // no vac�o
            {
                int index = PossibleIndex(interval.Min);

                if (index != -1)
                {
                    Interval<K> previousInt = dic.Keys[index];
                    if (previousInt.Overlaps(interval))
                        throw new ArgumentException("Interval {0} overlaps with the exisiting one {1} (value {2})".FormatWith(interval, previousInt, value));
                }

                int next = index + 1;
                if (next < dic.Count)
                {
                    Interval<K> nextInt = dic.Keys[next];
                    if (nextInt.Overlaps(interval))
                        throw new ArgumentException(String.Format("Interval {0} overlaps with the exisiting one {1}", interval, nextInt));
                }
            }

            dic.Add(interval, value);
        }

        public void Override(Interval<K> interval, V value)
        {
            if (interval.IsEmpty)
                return;

            RemoveCutting(interval);
            Add(interval, value); 
        }

        public void RemoveCutting(Interval<K> interval)
        {
            if (interval.IsEmpty)
                return; 

            var overlapping = Overlapping(interval).ToArray();

            if (overlapping.Length > 0)
            {
                Interval<K> first = overlapping[0];
                V firstValue = dic[first];

                Interval<K> last = overlapping[overlapping.Length-1];
                V lastValue = dic[last];
                
                foreach (var item in overlapping)
                {
                    Remove(item);
                }

                if (first.Min.CompareTo(interval.Min) < 0)
                {
                    Add(new Interval<K>(first.Min, interval.Min), firstValue);
                }

                if (last.Max.CompareTo(interval.Max) > 0)
                {
                    Add(new Interval<K>(interval.Max, last.Max), lastValue);
                }
            }
        }

        public IEnumerable<Interval<K>> Overlapping(Interval<K> interval)
        {
            int indexMin = PossibleIndex(interval.Min);

            if (indexMin == -1)
                indexMin = 0;
            else if (!dic.Keys[indexMin].Overlaps(interval))
                indexMin++;

            int indexMax = PossibleIndex(interval.Max);
            if (indexMax != -1 && !dic.Keys[indexMax].Overlaps(interval))
                indexMax--;

            for (int i = indexMin; i <= indexMax; i++)
            {
                yield return dic.Keys[i];
            }
        }


        public V this[K key]
        {
            get
            {
                int index = PossibleIndex(key);
                if (index == -1)
                    throw new KeyNotFoundException("No interval found in {0}".FormatWith(this.GetType().TypeName()));

                if (dic.Keys[index].Contains(key))
                    return dic.Values[index];

                throw new KeyNotFoundException("No interval found in {0}".FormatWith(this.GetType().TypeName()));
            }
            
        }

        public V TryGet(K key, V defaultValue)
        {
            this.TryGetValue(key, out defaultValue);
            return defaultValue;
        }

        public bool TryGetValue(K key, out V value)
        {
            value = default(V);

            int index = PossibleIndex(key);
            if (index == -1)
                return false;

            Debug.Assert(0 <= index && index < dic.Count);

            if (dic.Keys[index].Contains(key))
            {
                value = dic.Values[index];
                return true;
            }

            return false;
        }

        public IntervalValue<V> TryGetValue(K key)
        {
            if (TryGetValue(key, out V val))
                return new IntervalValue<V>(val);

            return new IntervalValue<V>();
        }

        public bool Remove(Interval<K> interval)
        {
            return dic.Remove(interval); 
        }


        int PossibleIndex(K key)
        {
            int index = BinarySearch(key);
            if (index == ~0)
                return -1;

            if (index < 0) // not found
                return (~index) - 1;

            return index;
        }

        int BinarySearch(K value)
        {
            int min = 0;
            int max = dic.Count - 1;
            while (min <= max)
            {
                int privot = min + ((max - min) >> 1);

                int comp = dic.Keys[privot].Min.CompareTo(value);
                if (comp == 0)
                {
                    return privot;
                }
                if (comp < 0)
                {
                    min = privot + 1;
                }
                else
                {
                    max = privot - 1;
                }
            }
            return ~min;
        }

        internal Interval<int> FindIntervalIndex(Interval<K> interval)
        {
            int min = BinarySearch(interval.Min);
            int max = BinarySearch(interval.Max);
            if (max == ~Count) max = ~max;

            if (min < 0 || max < 0)
                throw new InvalidOperationException("Interval limits do not exist on dictionary");

            return new Interval<int>(min, max);
        }

        public K? TotalMin
        {
            get
            {
                if (dic.Count == 0)
                    return null;

                return dic.FirstEx().Key.Min;
            }
        }

        public K? TotalMax
        {
            get
            {
                if (dic.Count == 0)
                    return null;
                return dic.Last().Key.Max;
            }
        }

        public IEnumerator<KeyValuePair<Interval<K>, V>> GetEnumerator()
        {
            return dic.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override string ToString()
        {
            return dic.ToString(a => "[{0},{1}] -> {2}".FormatWith(a.Key.Min, a.Key.Max, a.Value), "\r\n");
        }
    }    

    public struct IntervalValue<T>
    {
        public readonly bool HasInterval;
        public readonly T Value;

        public IntervalValue(T value):this()
        {
            this.Value = value;
            this.HasInterval = true;
        }
    }

    public static class IntervalDictionaryExtensions
    {
        public static IntervalDictionary<K, VR> Mix<K, V1, V2, VR>(this IntervalDictionary<K, V1> me, IntervalDictionary<K, V2> other, Func<Interval<K>, IntervalValue<V1>, IntervalValue<V2>, IntervalValue<VR>> mixer)
            where K : struct, IComparable<K>, IEquatable<K>
        {
            Interval<K>[] keys = me.Intervals.Concat(other.Intervals).SelectMany(a => a.Elements()).Distinct().OrderBy().BiSelect((min, max) => new Interval<K>(min, max)).ToArray();
            return new IntervalDictionary<K, VR>(keys
                .Select(k => new { Intervalo = k, Valor = mixer(k, me.TryGetValue(k.Min), other.TryGetValue(k.Min)) })
                .Where(a => a.Valor.HasInterval).Select(a => KVP.Create(a.Intervalo, a.Valor.Value)));
        }

        public static IntervalDictionary<K, VR> Collapse<K, V, VR>(this IEnumerable<IntervalDictionary<K, V>> collection, Func<Interval<K>, IEnumerable<V>, VR> mixer)
            where K : struct, IComparable<K>, IEquatable<K>
        {
            Interval<K>[] keys = collection.SelectMany(a => a).SelectMany(a => a.Key.Elements()).Distinct().OrderBy().BiSelect((min, max) => new Interval<K>(min, max)).ToArray();
            return new IntervalDictionary<K, VR>(keys.Select(k => KVP.Create(k, mixer(k, collection.Select(intDic => intDic.TryGetValue(k.Min)).Where(vi => vi.HasInterval).Select(vi => vi.Value)))));
        }

        public static IntervalDictionary<K, VR> AggregateIntervalDictionary<K, V, VR>(this IEnumerable<(Interval<K> interval, V value)> collection, Func<Interval<K>, IEnumerable<V>, VR> mixer)
           where K : struct, IComparable<K>, IEquatable<K>
        {
            Interval<K>[] keys = collection.SelectMany(a => a.interval.Elements()).Distinct().OrderBy().BiSelect((min, max) => new Interval<K>(min, max)).ToArray();
            return new IntervalDictionary<K, VR>(keys.Select(k => KVP.Create(k, mixer(k, collection.Where(a => a.interval.Subset(k)).Select(a => a.value)))));
        }

        public static IntervalDictionary<K, VR> Filter<K, V, VR>(this IntervalDictionary<K, V> me, Interval<K> filter, Func<Interval<K>, V, VR> mapper)
                 where K : struct, IComparable<K>, IEquatable<K>
        {
            IntervalDictionary<K, VR> result  = new IntervalDictionary<K,VR>(); 
            foreach (var item in me)
	        {
                var intersection = item.Key.TryIntersection(filter);
                if(intersection != null)
                    result.Add(intersection.Value, mapper(intersection.Value, item.Value)); 
	        }
            return result; 
        }


        public static IntervalDictionary<K, V> ToIntervalDictionary<K, V>(this IEnumerable<KeyValuePair<Interval<K>, V>> collection)
       where K : struct, IEquatable<K>, IComparable<K>
        {
            return new IntervalDictionary<K, V>(collection);
        }

        internal static IntervalDictionary<K, int> ToIndexIntervalDictinary<Q, K>(this IEnumerable<Q> squares, Func<Q, IEnumerable<K>> func)
          where K : struct, IComparable<K>, IEquatable<K>
        {
            List<K> list = squares.SelectMany(func).Distinct().ToList();
            list.Sort();

            IntervalDictionary<K, int> result = new IntervalDictionary<K, int>();
            for (int i = 0; i < list.Count - 1; i++)
                result.Add(new Interval<K>(list[i], list[i + 1]), i);

            return result;
        }
    }
}
