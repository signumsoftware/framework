using System;
using System.Collections.Generic;
using System.Text;
using Signum.Utilities.Properties;

namespace Signum.Utilities.DataStructures
{
    [Serializable]
    public struct Interval<T> : IEquatable<Interval<T>>, IComparable<Interval<T>> 
        where T:struct, IComparable<T>, IEquatable<T>
    {
        public readonly T min;
        public readonly T max;

        public T Min { get { return min; } }
        public T Max { get { return max; } }

        public Interval(T min, T max)
        {
            if (min.CompareTo(max) > 0)
                throw new ArgumentException(Resources.MinIsGreaterThanMax);

            this.min = min;
            this.max = max;
        }

        public bool Contains(T value)
        {
            return min.CompareTo(value) <= 0 && value.CompareTo(max) < 0;
        }

        public bool Overlap(Interval<T> other)
        {
            if (max.CompareTo(other.min)<=0)
                return false;
            if (other.max.CompareTo(min) <= 0)
                return false;

            return true;
        }


        public Interval<T>? Intersection(Interval<T> other)
        {
            T minVal = min.CompareTo(other.min) > 0 ? min : other.min;
            T maxVal = max.CompareTo(other.max) < 0 ? max : other.max;

            if (minVal.CompareTo(maxVal) >= 0)
                return null;

            return new Interval<T>(minVal, maxVal);
        }

        public Interval<T>? Union(Interval<T> other)
        {
            if (!this.Overlap(other))
                return null;

            T minVal = min.CompareTo(other.min) > 0 ? other.min : min;
            T maxVal = max.CompareTo(other.max) < 0 ? other.max : max;

            return new Interval<T>(minVal, maxVal);
        }

        public bool Subset(Interval<T> other)
        {
            return this.min.CompareTo(other.min) <= 0 && other.max.CompareTo(this.max) <= 0;
        }


        public IEnumerable<T> Elements()
        {
            yield return min;
            yield return max;
        }

        public int CompareTo(Interval<T> other)
        {
            return min.CompareTo(other.min);
        }


        public override string ToString()
        {
            return "[" + min + " - " + max + "]"; 
        }        

        public bool Equals(Interval<T> other)
        {
            return other.min.Equals(min) && other.max.Equals(max);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Interval<T>))
                return false;

            return Equals((Interval<T>)obj); 
        }

        public override int GetHashCode()
        {
            return min.GetHashCode();
        }
    }

    public static class IntervalExtensions
    {
        public static int Distance(this Interval<int> interval, int point)
        {
            return point < interval.Min ? interval.Min - point :
                    point > interval.Max ? point - interval.Max : 0;
        }

        public static long Distance(this Interval<long> interval, long point)
        {
            return point < interval.Min ? interval.Min - point :
                    point > interval.Max ? point - interval.Max : 0;
        }

        public static double Distance(this Interval<double> interval, double point)
        {
            return point < interval.Min ? interval.Min - point :
                    point > interval.Max ? point - interval.Max : 0;
        }

        public static float Distance(this Interval<float> interval, float point)
        {
            return point < interval.Min ? interval.Min - point :
                    point > interval.Max ? point - interval.Max : 0;
        }

        public static decimal Distance(this Interval<decimal> interval, decimal point)
        {
            return point < interval.Min ? interval.Min - point :
                    point > interval.Max ? point - interval.Max : 0;
        }

        public static TimeSpan Distance(this Interval<DateTime> interval, DateTime point)
        {
            return point < interval.Min ? interval.Min - point :
                    point > interval.Max ? point - interval.Max : new TimeSpan(0);
        }
    }
}
