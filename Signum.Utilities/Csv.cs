
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.Collections.Concurrent;

namespace Signum.Utilities
{
    public static class Csv
    {
        public static Encoding DefaultEncoding = Encoding.GetEncoding(1252);

        public static string ToCsvFile<T>(this IEnumerable<T> collection, string fileName, Encoding encoding = null, CultureInfo culture = null, bool writeHeaders = true, bool autoFlush = false, bool append = false)
        {
            using (FileStream fs = append ? new FileStream(fileName, FileMode.Append, FileAccess.Write) : File.Create(fileName))
                ToCsv<T>(collection, fs, encoding, culture, writeHeaders, autoFlush);

            return fileName;
        }

        public static byte[] ToCsvBytes<T>(this IEnumerable<T> collection, Encoding encoding = null, CultureInfo culture = null, bool writeHeaders = true, bool autoFlush = false)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                collection.ToCsv(ms, encoding, culture, writeHeaders, autoFlush);
                return ms.ToArray();
            }
        }

        public static void ToCsv<T>(this IEnumerable<T> collection, Stream stream, Encoding encoding = null, CultureInfo culture = null, bool writeHeaders = true, bool autoFlush = false)
        {
            encoding = encoding ?? DefaultEncoding;
            culture = culture ?? CultureInfo.CurrentCulture;

            string separator = culture.TextInfo.ListSeparator;

            List<MemberEntry<T>> members = MemberEntryFactory.GenerateList<T>();

            using (StreamWriter sw = new StreamWriter(stream, encoding) { AutoFlush = autoFlush })
            {
                if (writeHeaders)
                    sw.WriteLine(members.ToString(m => HandleSpaces(m.Name), separator));

                foreach (var item in collection)
                {
                    sw.WriteLine(members.ToString(m => m.Getter(item).TryCC(a => EncodeCsv(ConvertToString(a, culture), culture)), separator));
                }
            }
        }

        static string EncodeCsv(string p, CultureInfo culture)
        {
            if (p == null)
                return p;

            string separator = culture.TextInfo.ListSeparator;

            if (p.Contains(separator) || p.Contains("\"") || p.Contains("\r") || p.Contains("\n"))
            {
                return "\"" + p.Replace("\"", "\"\"") + "\"";
            }
            return p;
        }

        static string ConvertToString(object a, CultureInfo culture)
        {
            IFormattable f = a as IFormattable;
            if (f != null)
                return f.ToString(null, culture);
            else
                return a.ToString();
        }

        static string HandleSpaces(string p)
        {
            return p.Replace("__", "^").Replace("_", " ").Replace("^", "_");
        }

        public static List<T> ReadFile<T>(string fileName, Encoding encoding = null, CultureInfo culture = null, int skipLines = 1, bool trim = false) where T : new()
        {
            encoding = encoding ?? DefaultEncoding;
            culture = culture ?? CultureInfo.CurrentCulture;

            using (FileStream fs = File.OpenRead(fileName))
                return ReadStream<T>(fs, encoding, culture, skipLines, trim).ToList();
        }

        public static List<T> ReadBytes<T>(byte[] data, Encoding encoding = null, CultureInfo culture = null, int skipLines = 1, bool trim = true) where T : new()
        {
            using (MemoryStream ms = new MemoryStream(data))
                return ReadStream<T>(ms, encoding, culture, skipLines, trim).ToList();
        }

        public static IEnumerable<T> ReadStream<T>(this Stream stream, Encoding encoding = null, CultureInfo culture = null, int skipLines = 1, bool trim = true)
            where T : new()
        {
            encoding = encoding ?? DefaultEncoding;
            culture = culture ?? CultureInfo.CurrentCulture;

            Regex regex = GetRegex(culture);

            using (StreamReader sr = new StreamReader(stream, encoding))
            {
                string str = sr.ReadToEnd();

                var matches = regex.Matches(str).Cast<Match>();

                if (skipLines > 0)
                    matches = matches.Skip(skipLines);

                foreach (var m in matches)
                {
                    if (m.Length > 0)
                    {
                        T t = ReadObject<T>(culture, m, trim);

                        yield return t;
                    }
                }
            }
        }

        public static T ReadLine<T>(string csvLine, CultureInfo culture = null, bool trim = true)
            where T : new()
        {
            culture = culture ?? CultureInfo.CurrentCulture;

            Regex regex = GetRegex(culture);

            Match m = regex.Match(csvLine);

            return ReadObject<T>(culture, m);
        }

        static T ReadObject<T>(CultureInfo culture, Match m, bool trim = true) where T : new()
        {
            var members = MemberEntryCache<T>.Members;

            var vals = m.Groups["val"].Captures;

            if (vals.Count < members.Count)
                throw new FormatException("Not enought fields on line: " + m.Value);

            T t = new T();
            for (int i = 0; i < members.Count; i++)
            {
                string str = DecodeCsv(vals[i].Value);

                if(trim)
                    str = str.Trim();

                object value = ConvertTo(str, members[i].MemberInfo.ReturningType(), culture);

                members[i].UntypedSetter(t, value);
            }
            return t;
        }

        static ConcurrentDictionary<char, Regex> regexCache = new ConcurrentDictionary<char, Regex>();
        const string BaseRegex = @"^((?<val>'(?:[^']+|'')*'|[^;\r\n]*))?((?!($|\r\n));(?<val>'(?:[^']+|'')*'|[^;\r\n]*))*($|\r\n)";
        static Regex GetRegex(CultureInfo culture)
        {
            char separator = culture.TextInfo.ListSeparator.SingleEx();

            return regexCache.GetOrAdd(separator, s =>
                new Regex(BaseRegex.Replace('\'', '"').Replace(';', s), RegexOptions.Multiline | RegexOptions.ExplicitCapture));
        }

        static class MemberEntryCache<T> where T : new()
        {
            public static List<MemberEntry<T>> Members = MemberEntryFactory.GenerateList<T>(MemberOptions.Fields | MemberOptions.Properties | MemberOptions.Untyped | MemberOptions.Setters);
        }

        static string DecodeCsv(string s)
        {
            if (s.StartsWith("\""))
            {
                if (!s.EndsWith("\""))
                    throw new FormatException("Cell starts by quotes but not ends with quotes".Formato(s));

                string str = s.Substring(1, s.Length - 2).Replace("\"\"", "\"");

                return Regex.Replace(str, "(?<!\r)\n", "\r\n");
            }

            if (s.Contains("\""))
                throw new FormatException("Cell has quotes ina unexpected position".Formato(s));

            return s;
        }

        static object ConvertTo(string s, Type type, CultureInfo culture)
        {
            Type baseType = Nullable.GetUnderlyingType(type);
            if (baseType != null)
            {
                if (!s.HasText()) return null;
                else return ConvertTo(s, baseType, culture);
            }

            if (type.IsEnum)
                return Enum.Parse(type, s);

            if (type == typeof(DateTime))
                return DateTime.Parse(s, culture);

            return Convert.ChangeType(s, type, culture);
        }
    }
}