using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Signum.Utilities
{
    [DebuggerStepThrough]
    public static class Extensions
    {
        #region Parse Number
        public static int? ToInt(this string str, NumberStyles ns = NumberStyles.Integer, CultureInfo ci = null)
        {
            if (int.TryParse(str, ns, ci ?? CultureInfo.CurrentCulture, out int result))
                return result;
            else
                return null;
        }

        public static long? ToLong(this string str, NumberStyles ns = NumberStyles.Integer, CultureInfo ci = null)
        {
            if (long.TryParse(str, ns, ci ?? CultureInfo.CurrentCulture, out long result))
                return result;
            else
                return null;
        }

        public static short? ToShort(this string str, NumberStyles ns = NumberStyles.Integer, CultureInfo ci = null)
        {
            if (short.TryParse(str, ns, ci ?? CultureInfo.CurrentCulture, out short result))
                return result;
            else
                return null;
        }

        public static float? ToFloat(this string str, NumberStyles ns = NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo ci = null)
        {
            if (float.TryParse(str, ns, ci ?? CultureInfo.CurrentCulture, out float result))
                return result;
            else
                return null;
        }

        public static double? ToDouble(this string str, NumberStyles ns = NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo ci = null)
        {
            if (double.TryParse(str, ns, ci ?? CultureInfo.CurrentCulture, out double result))
                return result;
            else
                return null;
        }

        public static decimal? ToDecimal(this string str, NumberStyles ns = NumberStyles.Number, CultureInfo ci = null)
        {
            if (decimal.TryParse(str, ns, ci ?? CultureInfo.CurrentCulture, out decimal result))
                return result;
            else
                return null;
        }

        public static bool? ToBool(this string str)
        {
            if (bool.TryParse(str, out bool result))
                return result;
            else
                return null;
        }

        public static int ToInt(this string str, string error)
        {
            if (int.TryParse(str, out int result))
                return result;

            throw new FormatException(error);
        }

        public static long ToLong(this string str, string error)
        {
            if (long.TryParse(str, out long result))
                return result;

            throw new FormatException(error);
        }

        public static short ToShort(this string str, string error)
        {
            if (short.TryParse(str, out short result))
                return result;

            throw new FormatException(error);
        }


        public static float? ToFloat(this string str, string error)
        {
            if (float.TryParse(str, out float result))
                return result;

            throw new FormatException(error);
        }

        public static double ToDouble(this string str, string error)
        {
            if (double.TryParse(str, out double result))
                return result;

            throw new FormatException(error);
        }

        public static decimal ToDecimal(this string str, string error)
        {
            if (decimal.TryParse(str, out decimal result))
                return result;

            throw new FormatException(error);
        }

        public static bool ToBool(this string str, string error)
        {
            if (bool.TryParse(str, out bool result))
                return result;

            throw new FormatException(error);
        }

        public static decimal ToDecimal(this double number)
        {
            return (decimal)number;
        }

        public static decimal RoundTo(this decimal number, int decimals)
        {
            return Math.Round(number, decimals);
        }

        public static double RoundTo(this double number, int decimals)
        {
            return Math.Round(number, decimals);
        }

        #endregion

        #region Math

        //http://en.wikipedia.org/wiki/Modulo_operation
        public static int Mod(this int a, int b)
        {
            int result = a % b;

            if (a < 0)
                result += b;

            if (b < 0)
                result -= b;

            return result;
        }


        public static long Mod(this long a, long b)
        {
            long mod = a % b;

            if (a < 0)
                mod += b;

            if (b < 0)
                mod -= b;

            return mod;
        }

        public static int DivMod(this int a, int b, out int mod)
        {
            int result = Math.DivRem(a, b, out mod);

            if (a < 0)
            {
                result--;
                mod += b;
            }

            if (b < 0)
            {
                result++;
                mod -= b;
            }

            return result;
        }

        public static long DivMod(this long a, long b, out long mod)
        {
            long result = Math.DivRem(a, b, out mod);

            if (a < 0)
            {
                result--;
                mod += b;
            }

            if (b < 0)
            {
                result++;
                mod -= b;
            }

            return result;
        }

        public static int DivCeiling(this int a, int b)
        {
            return (a + b - 1) / b;
        }

        public static long DivCeiling(this int a, long b)
        {
            return (a + b - 1) / b;
        }

        #endregion

        #region DateTime

        public static DateTime? ToDateTimeExact(this string date, string format, IFormatProvider formatProvider = null,
            DateTimeStyles? styles = null)
        {
            if (DateTime.TryParseExact(date, format,
    formatProvider ?? CultureInfo.CurrentCulture,
    styles ?? DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            return null;
        }

        #endregion

        public static T? DefaultToNull<T>(this T value)
            where T : struct
        {
            return EqualityComparer<T>.Default.Equals(default(T), value) ? (T?)null : value;
        }

        public static T? DefaultToNull<T>(this T value, T defaultValue)
            where T : struct
        {
            return EqualityComparer<T>.Default.Equals(defaultValue, value) ? (T?)null : value;
        }

        public static int? NotFoundToNull(this int value)
        {
            return value == -1 ? null : (int?)value;
        }

        public static int NotFound(this int value, int defaultValue)
        {
            return value == -1 ? defaultValue : value;
        }

        public static T ThrowIfNull<T>(this T? t, string message)
         where T : struct
        {
            if (t == null)
                throw new NullReferenceException(message);
            return t.Value;
        }

        public static T ThrowIfNull<T>(this T t, string message)
            where T : class
        {
            if (t == null)
                throw new NullReferenceException(message);
            return t;
        }


        public static T ThrowIfNull<T>(this T? t, Func<string> message)
         where T : struct
        {
            if (t == null)
                throw new NullReferenceException(message());
            return t.Value;
        }

        public static T ThrowIfNull<T>(this T t, Func<string> message)
            where T : class
        {
            if (t == null)
                throw new NullReferenceException(message());
            return t;
        }

        public static R Let<T, R>(this T t, Func<T, R> func)
        {
            return func(t);
        }

        public static T Do<T>(this T t, Action<T> action)
        {
            action(t);
            return t;
        }

        public static IEnumerable<int> To(this int start, int endNotIncluded)
        {
            for (int i = start; i < endNotIncluded; i++)
                yield return i;
        }

        public static IEnumerable<int> To(this int start, int endNotIncluded, int step)
        {
            for (int i = start; i < endNotIncluded; i += step)
                yield return i;
        }

        public static IEnumerable<DateTime> To(this DateTime start, DateTime endNotIncluded)
        {
            for (DateTime i = start; i < endNotIncluded; i = i.AddDays(1))
                yield return i;
        }

        public static IEnumerable<DateTime> To(this DateTime start, DateTime endNotIncluded, TimeSpan span)
        {
            for (DateTime i = start; i < endNotIncluded; i = i.Add(span))
                yield return i;
        }

        public static IEnumerable<int> DownTo(this int startNotIncluded, int end)
        {
            for (int i = startNotIncluded - 1; i >= end; i--)
                yield return i;
        }

        public static IEnumerable<int> DownTo(this int startNotIncluded, int end, int step)
        {
            for (int i = startNotIncluded - 1; i >= end; i -= step)
                yield return i;
        }

        public static IEnumerable<T> For<T>(this T start, Func<T, bool> condition, Func<T, T> increment)
        {
            for (T i = start; condition(i); i = increment(i))
                yield return i;
        }

        public delegate R FuncCC<in T, R>(T input)
            where T : class
            where R : class;
        public static IEnumerable<T> Follow<T>(this T start, FuncCC<T, T> next) where T : class
        {
            for (T i = start; i != null; i = next(i))
                yield return i;
        }

        public delegate R FuncSN<in T, R>(T input) where T : struct;
        public static IEnumerable<T> Follow<T>(this T start, FuncSN<T, T?> next) where T : struct
        {
            for (T? i = start; i.HasValue; i = next(i.Value))
                yield return i.Value;
        }

        public static IEnumerable<T> Follow<T>(this T? start, FuncSN<T, T?> next) where T : struct
        {
            for (T? i = start; i.HasValue; i = next(i.Value))
                yield return i.Value;
        }

        public static IEnumerable<D> GetInvocationListTyped<D>(this D multicastDelegate)
            where D : class, ICloneable, ISerializable
        {
            if (multicastDelegate == null)
                return Enumerable.Empty<D>();

            return ((MulticastDelegate)(object)multicastDelegate).GetInvocationList().Cast<D>();
        }

        public static string GetQueryString(object obj)
        {
            var result = new List<string>();
            var props = obj.GetType().GetProperties().Where(p => p.GetValue(obj, null) != null);
            foreach (var p in props)
            {
                var value = p.GetValue(obj, null);
                var enumerable = value as ICollection;
                if (enumerable != null)
                {
                    result.AddRange(from object v in enumerable select string.Format("{0}={1}", p.Name, HttpUtility.UrlEncode(v?.ToString() ?? "")));
                }
                else
                {
                    result.Add(string.Format("{0}={1}", p.Name, HttpUtility.UrlEncode(value?.ToString() ?? "")));
                }
            }

            return string.Join("&", result.ToArray());
        }

    }

}
