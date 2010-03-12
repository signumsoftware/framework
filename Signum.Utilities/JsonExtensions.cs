using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using System.Collections;

namespace Signum.Utilities
{
    public static class JsonExtensions
    {
        public static string Quote(this string obj)
        {
            return "\"" + obj + "\"";
        }

        public static string Unquote(this string obj)
        {
            if (obj.Length > 1 && obj[0] == '\"' && obj[obj.Length - 1] == '\"')
                return obj.Substring(1, obj.Length - 2);
            else
                return obj;
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

        static Dictionary<Type, IList> cache = new Dictionary<Type, IList>();

        public static string ToJSonObject<T>(this T element, Func<object, string> value)
        {
            List<MemberEntry<T>> entries = (List<MemberEntry<T>>)cache.GetOrCreate(typeof(T), () => MemberEntryFactory.GenerateList<T>());

            return "{\r\n" + entries.ToString(m =>
                "{0}:{1}".Formato(
                   Quote(m.Name),
                   value(m.Getter(element))), ",\r\n").Indent(3) + "\r\n}";
            
        }
    }
}
