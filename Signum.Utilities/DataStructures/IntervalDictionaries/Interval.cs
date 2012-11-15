using System;
using System.Collections.Generic;
using System.Text;
using Signum.Utilities.Properties;
using System.Globalization;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Utilities.DataStructures
{
    [Serializable]
    public struct Interval<T> : IEquatable<Interval<T>>, IComparable<Interval<T>>, IFormattable, IComparable
        where T:struct, IComparable<T>, IEquatable<T>
    {
        readonly T min;
        readonly T max;

        public T Min { get { return min; } }
        public T Max { get { return max; } }

        public bool IsEmpty { get { return min.Equals(max); } }

        public Interval(T min, T max)
        {
            if (min.CompareTo(max) > 0)
                throw new ArgumentException("Argument min is greater than max");

            this.min = min;
            this.max = max;
        }


        [MethodExpander(typeof(IntervalMethodExpander))]
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

        public int CompareTo(object obj)
        {
            return CompareTo((Interval<T>)obj);
        }

        public override string ToString()
        {
            return "[" + min + " - " + max + ")"; 
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

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null)
                return string.Format(formatProvider, "{0} - {1}", min, max);
            else if (format.Contains("{"))
                return string.Format(formatProvider, format, min, max);
            else
                return string.Format(formatProvider, "{0:" + format + "} - {1:" + format + "}", min, max);
        }

        public static bool operator==(Interval<T> first, Interval<T> second)
        {
            return first.Equals(second);
        }

        public static bool operator!=(Interval<T> first, Interval<T> second)
        {
            return !first.Equals(second);
        }
    }

    class IntervalMethodExpander: IMethodExpander
    {
        public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
        {
            if (instance.Type.IsInstantiationOf(typeof(Interval<>)))
                return Expression.And( //min <= value && value < max;                    
                    Expression.LessThanOrEqual(Expression.Property(instance, "Min"), arguments[0]),
                    Expression.LessThan(arguments[0], Expression.Property(instance, "Max")));
            else
                return Expression.And(
                    Expression.Or(Expression.Not(Expression.Property(Expression.Property(instance, "Min"), "HasValue")), Expression.LessThanOrEqual(Expression.Property(Expression.Property(instance, "Min"), "Value"), arguments[0])),
                    Expression.Or(Expression.Not(Expression.Property(Expression.Property(instance, "Max"), "HasValue")), Expression.LessThan(arguments[0], Expression.Property(Expression.Property(instance, "Max"), "Value"))));  

        }
    }


    [Serializable]
    public struct NullableInterval<T> : IEquatable<NullableInterval<T>>, IComparable<NullableInterval<T>>, IFormattable, IComparable
        where T : struct, IComparable<T>, IEquatable<T>
    {
        readonly T? min;
        readonly T? max;

        public T? Min { get { return min; } }
        public T? Max { get { return max; } }

        public bool IsEmpty { get { return min.HasValue && max.HasValue && min.Value.Equals(max.Value); } }

        public NullableInterval(T? min, T? max)
        {
            if (min.HasValue && max.HasValue && min.Value.CompareTo(max.Value) > 0)
                throw new ArgumentException("Argument min is greater than max");

            this.min = min;
            this.max = max;
        }

        [MethodExpander(typeof(IntervalMethodExpander))]
        public bool Contains(T value)
        {
            return (!min.HasValue || min.Value.CompareTo(value) <= 0) &&
                   (!max.HasValue || value.CompareTo(max.Value) < 0);
        }

        public bool Overlap(NullableInterval<T> other)
        {
            if (max.HasValue && other.min.HasValue && max.Value.CompareTo(other.min.Value) <= 0)
                return false;
            if (other.max.HasValue && min.HasValue && other.max.Value.CompareTo(min.Value) <= 0)
                return false;

            return true;
        }

        public NullableInterval<T>? Intersection(NullableInterval<T> other)
        {
            T? minVal = min.HasValue && other.min.HasValue ? (min.Value.CompareTo(other.min.Value) > 0 ? min.Value : other.min.Value) : min ?? other.min;
            T? maxVal = max.HasValue && other.max.HasValue ? (max.Value.CompareTo(other.max.Value) < 0 ? max.Value : other.max.Value) : max ?? other.max;

            if (minVal.HasValue && maxVal.HasValue && minVal.Value.CompareTo(maxVal.Value) >= 0)
                return null;

            return new NullableInterval<T>(minVal, maxVal);
        }

        public NullableInterval<T>? Union(NullableInterval<T> other)
        {
            if (!this.Overlap(other))
                return null;

            T? minVal = !min.HasValue || !other.min.HasValue ? (T?)null : min.Value.CompareTo(other.min.Value) > 0 ? other.min.Value : min.Value;
            T? maxVal = !max.HasValue || !other.max.HasValue ? (T?)null : max.Value.CompareTo(other.max.Value) < 0 ? other.max.Value : max.Value;

            return new NullableInterval<T>(minVal, maxVal);
        }

        public bool Subset(NullableInterval<T> other)
        {
            return
                (!this.min.HasValue || (other.min.HasValue && this.min.Value.CompareTo(other.min.Value) <= 0))
                &&
                (!this.max.HasValue || (other.max.HasValue && other.max.Value.CompareTo(this.max.Value) <= 0));
        }


        public IEnumerable<T?> Elements()
        {
            yield return min;
            yield return max;
        }

        public int CompareTo(NullableInterval<T> other)
        {
            if (min == null && other.min == null)
                return 0;

            var temp = min.HasValue.CompareTo(other.min.HasValue);

            if (temp != 0)
                return temp;

            return min.Value.CompareTo(other.min.Value);
        }

        public int CompareTo(object obj)
        {
            return CompareTo((NullableInterval<T>)obj);
        }

        public override string ToString()
        {
            return ToString(null, CultureInfo.CurrentCulture);
        }

        public bool Equals(NullableInterval<T> other)
        {
            return other.min.Equals(min) && other.max.Equals(max);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is NullableInterval<T>))
                return false;

            return Equals((NullableInterval<T>)obj);
        }

        public override int GetHashCode()
        {
            return min.GetHashCode();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                return BuildString(formatProvider, "{0} - {1}", "< {0}", "{0} ≤", "All");

            if (format.HasText() && format.Contains("{"))
            {
                if (format.Contains("|"))
                {
                    string[] parts = format.Split('|');
                    if (parts.Length != 4)
                        throw new FormatException("Formatting an interval needs 4 parts: Middle | Start | End | All");

                    return BuildString(formatProvider, parts[0], parts[1], parts[2], parts[3]);
                }

                return BuildString(formatProvider, format, format, format, format);
            }

            return BuildString(formatProvider,
                "{0:" + format + "} - {1:" + format + "}",
                "< {0:" + format + "}",
                "{0:" + format + "} ≤",
                "All");
        }

        string BuildString(IFormatProvider formatProvider, string middle, string start, string end, string all)
        {
            if (min.HasValue && max.HasValue)
                return string.Format(formatProvider, middle, min, max);

            if (max.HasValue)
                return string.Format(formatProvider, start, max);

            if (min.HasValue)
                return string.Format(formatProvider, end, min);

            return all;
        }
    }



    public static class IntervalExtensions
    {
        public static int Length(this Interval<int> interval)
        {
            return interval.Max - interval.Min;
        }

        public static long Length(this Interval<long> interval)
        {
            return interval.Max - interval.Min;
        }

        public static double Length(this Interval<double> interval)
        {
            return interval.Max - interval.Min;
        }

        public static float Length(this Interval<float> interval)
        {
            return interval.Max - interval.Min;
        }

        public static decimal Length(this Interval<decimal> interval)
        {
            return interval.Max - interval.Min;
        }

        public static TimeSpan Length(this Interval<DateTime> interval)
        {
            return interval.Max - interval.Min;
        }

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
