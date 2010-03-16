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
        ///  FUNCTION Enquote Public Domain 2002 JSON.org 
        ///  @author JSON.org 
        ///  @version 0.1 
        ///  Ported to C# by Are Bjolseth, teleplan.no 
        public static string Quote(this string s)
        {
            if (s == null || s.Length == 0)
                return "\"\"";

            int len = s.Length;
            
            StringBuilder sb = new StringBuilder(len + 4);
            sb.Append('"');
            for (int i = 0; i < len; i += 1)
            {
                char c = s[i];
                switch (c)
                {
                    case '\\':
                    case '"':
                    case '>':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '\b': sb.Append("\\b"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\r': sb.Append("\\r"); break;
                    default:
                        if (c < ' ')
                        {
                            //t = "000" + Integer.toHexString(c); 
                            string tmp = new string(c, 1);
                            string t = "000" + int.Parse(tmp, System.Globalization.NumberStyles.HexNumber);
                            sb.Append("\\u" + t.Substring(t.Length - 4));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
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
