using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities
{
    public static class JsonExtensions
    {
        public static string Quote(this string obj)
        {
            return "\"" + obj + "\"";
        }

        public static string ToJSonArray<T>(this IEnumerable<T> collection, Func<T, string> toString)
        {
            return "[" + collection.ToString(toString, ", ") + "]";
        }

        public static string ToJSonObject<K, V>(this IDictionary<K, V> dictionary, Func<K, string> keyQuoted, Func<V, string> value)
        {
            return "{" + dictionary.ToString(kvp =>
                "{0} : {1}".Formato(
                   keyQuoted(kvp.Key),
                   value(kvp.Value)), ", ") + "}";
        }

        public static string ToJSonArrayBig<T>(this IEnumerable<T> collection, Func<T, string> toString)
        {
            return "[\r\n" + collection.ToString(toString, ",\r\n").Indent(2) + "\r\n]";
        }

        public static string ToJSonObjectBig<K, V>(this IDictionary<K, V> dictionary, Func<K, string> keyQuoted, Func<V, string> value)
        {
            return "{\r\n" + dictionary.ToString(kvp =>
                "{0} : {1}".Formato(
                   keyQuoted(kvp.Key),
                   value(kvp.Value)), ",\r\n").Indent(3) + "\r\n}";
        }    
    }
}
