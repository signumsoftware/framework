
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
using System.Collections;

namespace Signum.Utilities
{
    public static class Csv
    {
        public static Encoding DefaultEncoding = Encoding.GetEncoding(1252);
        public static CultureInfo DefaultCulture = null;

        public static string ToCsvFile<T>(this IEnumerable<T> collection, string fileName, Encoding encoding = null, CultureInfo culture = null, bool writeHeaders = true, bool autoFlush = false, bool append = false,
            Func<CsvColumnInfo<T>, CultureInfo, Func<object, string>> toStringFactory = null)
        {
            using (FileStream fs = append ? new FileStream(fileName, FileMode.Append, FileAccess.Write) : File.Create(fileName))
                ToCsv<T>(collection, fs, encoding, culture, writeHeaders, autoFlush, toStringFactory);

            return fileName;
        }

        public static byte[] ToCsvBytes<T>(this IEnumerable<T> collection, Encoding encoding = null, CultureInfo culture = null, bool writeHeaders = true, bool autoFlush = false,
            Func<CsvColumnInfo<T>, CultureInfo, Func<object, string>> toStringFactory = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                collection.ToCsv(ms, encoding, culture, writeHeaders, autoFlush, toStringFactory);
                return ms.ToArray();
            }
        }

        public static void ToCsv<T>(this IEnumerable<T> collection, Stream stream, Encoding encoding = null, CultureInfo culture = null, bool writeHeaders = true, bool autoFlush = false,
            Func<CsvColumnInfo<T>, CultureInfo, Func<object, string>> toStringFactory = null)
        {
            encoding = encoding ?? DefaultEncoding;
            culture = culture ?? DefaultCulture ?? CultureInfo.CurrentCulture;

            string separator = culture.TextInfo.ListSeparator;

            if (typeof(IList).IsAssignableFrom(typeof(T)))
            {
                using (StreamWriter sw = new StreamWriter(stream, encoding) { AutoFlush = autoFlush })
                {
                    foreach (IList row in collection)
                    {
                        for (int i = 0; i < row.Count; i++)
                        {
                            var obj = row[i];

                            var str = EncodeCsv(ConvertToString(obj, null, culture), culture);

                            sw.Write(str);
                            if (i < row.Count - 1)
                                sw.Write(separator);
                            else
                                sw.WriteLine();
                        }
                    }
                }
            }
            else
            {
                var columns = ColumnInfoCache<T>.Columns;
                var members = columns.Select(c => c.MemberEntry).ToList();
                var toString = columns.Select(c => GetToString(culture, c, toStringFactory)).ToList();

                using (StreamWriter sw = new StreamWriter(stream, encoding) { AutoFlush = autoFlush })
                {
                    if (writeHeaders)
                        sw.WriteLine(members.ToString(m => HandleSpaces(m.Name), separator));

                    foreach (var item in collection)
                    {
                        for (int i = 0; i < members.Count; i++)
                        {
                            var obj = members[i].Getter(item);

                            var str = EncodeCsv(toString[i](obj), culture);

                            sw.Write(str);
                            if (i < members.Count - 1)
                                sw.Write(separator);
                            else
                                sw.WriteLine();
                        }
                    }
                }
            }
        }


        static string EncodeCsv(string p, CultureInfo culture)
        {
            if (p == null)
                return null;

            string separator = culture.TextInfo.ListSeparator;

            if (p.Contains(separator) || p.Contains("\"") || p.Contains("\r") || p.Contains("\n"))
            {
                return "\"" + p.Replace("\"", "\"\"") + "\"";
            }
            return p;
        }

        private static Func<object, string> GetToString<T>(CultureInfo culture, CsvColumnInfo<T> column, Func<CsvColumnInfo<T>, CultureInfo, Func<object, string>> toStringFactory)
        {
            if (toStringFactory != null)
            {
                var result = toStringFactory(column, culture);

                if (result != null)
                    return result;
            }

            return obj => ConvertToString(obj, column.Format, culture);
        }

        static string ConvertToString(object obj, string format, CultureInfo culture)
        {
            if (obj == null)
                return "";

            if (obj is IFormattable f)
                return f.ToString(null, culture);
            else
                return obj.ToString();
        }

        static string HandleSpaces(string p)
        {
            return p.Replace("__", "^").Replace("_", " ").Replace("^", "_");
        }

        public static List<T> ReadFile<T>(string fileName, Encoding encoding = null, CultureInfo culture = null, int skipLines = 1, CsvReadOptions<T> options = null) where T : class, new()
        {
            encoding = encoding ?? DefaultEncoding;
            culture = culture ?? DefaultCulture ?? CultureInfo.CurrentCulture;

            using (FileStream fs = File.OpenRead(fileName))
                return ReadStream<T>(fs, encoding, culture, skipLines, options).ToList();
        }

        public static List<T> ReadBytes<T>(byte[] data, Encoding encoding = null, CultureInfo culture = null, int skipLines = 1, CsvReadOptions<T> options = null) where T : class, new()
        {
            using (MemoryStream ms = new MemoryStream(data))
                return ReadStream<T>(ms, encoding, culture, skipLines, options).ToList();
        }

        public static IEnumerable<T> ReadStream<T>(Stream stream, Encoding encoding = null, CultureInfo culture = null, int skipLines = 1, CsvReadOptions<T> options = null) where T : class, new()
        {
            encoding = encoding ?? DefaultEncoding;
            culture = culture ?? DefaultCulture ?? CultureInfo.CurrentCulture;
            if (options == null)
                options = new CsvReadOptions<T>();

            var columns = ColumnInfoCache<T>.Columns;
            var members = columns.Select(c => c.MemberEntry).ToList();
            var parsers = columns.Select(c => GetParser(culture, c, options.ParserFactory)).ToList();

            Regex regex = GetRegex(culture, options.RegexTimeout);

            if (options.AsumeSingleLine)
            {
                using (StreamReader sr = new StreamReader(stream, encoding))
                {
                    for (int i = 0; i < skipLines; i++)
                        sr.ReadLine();

                    var line = skipLines;
                    while(true)
                    {
                        string csvLine = sr.ReadLine();

                        if (csvLine == null)
                            yield break;

                        Match m = null;
                        T t = null;
                        try
                        {
                            m = regex.Match(csvLine);
                            if (m.Length > 0)
                            {
                                t = ReadObject<T>(m, members, parsers);
                            }
                        }
                        catch(Exception e)
                        {
                            e.Data["row"] = line;

                            if (options.SkipError == null || !options.SkipError(e, m))
                                throw new ParseCsvException(e);
                        }

                        if (t != null)
                            yield return t;
                    }
                }
            }
            else
            {
                using (StreamReader sr = new StreamReader(stream, encoding))
                {
                    string str = sr.ReadToEnd();

                    var matches = regex.Matches(str).Cast<Match>();

                    if (skipLines > 0)
                        matches = matches.Skip(skipLines);

                    int line = skipLines;
                    foreach (var m in matches)
                    {
                        if (m.Length > 0)
                        {
                            T t = null;
                            try
                            {
                                t = ReadObject<T>(m, members, parsers);
                            }
                            catch (Exception e)
                            {
                                e.Data["row"] = line;

                                if (options.SkipError == null || !options.SkipError(e, m))
                                    throw new ParseCsvException(e);
                            }
                            if (t != null)
                                yield return t;
                        }
                        line++;
                    }
                }
            }
        }

        public static T ReadLine<T>(string csvLine, CultureInfo culture = null, CsvReadOptions<T> options = null)
            where T : class, new()
        {
            if (options == null)
                options = new CsvReadOptions<T>();

            culture = culture ?? DefaultCulture ?? CultureInfo.CurrentCulture;

            Regex regex = GetRegex(culture, options.RegexTimeout);

            Match m = regex.Match(csvLine);

            var columns = ColumnInfoCache<T>.Columns;

            return ReadObject<T>(m,
                columns.Select(c => c.MemberEntry).ToList(),
                columns.Select(c => GetParser(culture, c, options.ParserFactory)).ToList());
        }

        private static Func<string, object> GetParser<T>(CultureInfo culture, CsvColumnInfo<T> column, Func<CsvColumnInfo<T>, CultureInfo, Func<string, object>> parserFactory)
        {
            if (parserFactory != null)
            {
                var result = parserFactory(column, culture);

                if (result != null)
                    return result;
            }

            return str => ConvertTo(str, column.MemberInfo.ReturningType(), culture, column.Format);
        }

        static T ReadObject<T>(Match m, List<MemberEntry<T>> members, List<Func<string, object>> parsers) where T : new()
        {
            var vals = m.Groups["val"].Captures;

            if (vals.Count < members.Count)
                throw new FormatException("Only {0} columns found (instead of {1}) in line: {2}".FormatWith(vals.Count, members.Count, m.Value));

            T t = new T();
            for (int i = 0; i < members.Count; i++)
            {
                string str = null; 
                try
                {
                    str = DecodeCsv(vals[i].Value);

                    object val = parsers[i](str);

                    members[i].Setter(t, val);
                }
                catch (Exception e)
                {
                    e.Data["value"] = str;
                    e.Data["member"] = members[i].MemberInfo.Name;
                    throw;
                }
            }
            return t;
        }

     

        static ConcurrentDictionary<char, Regex> regexCache = new ConcurrentDictionary<char, Regex>();
        const string BaseRegex = @"^((?<val>'(?:[^']+|'')*'|[^;\r\n]*))?((?!($|\r\n));(?<val>'(?:[^']+|'')*'|[^;\r\n]*))*($|\r\n)";
        static Regex GetRegex(CultureInfo culture, TimeSpan timeout)
        {
            char separator = culture.TextInfo.ListSeparator.SingleEx();

            return regexCache.GetOrAdd(separator, s =>
                new Regex(BaseRegex.Replace('\'', '"').Replace(';', s), RegexOptions.Multiline | RegexOptions.ExplicitCapture, timeout));
        }

        static class ColumnInfoCache<T>
        {
            public static List<CsvColumnInfo<T>> Columns = MemberEntryFactory.GenerateList<T>(MemberOptions.Fields | MemberOptions.Properties | MemberOptions.Typed | MemberOptions.Setters | MemberOptions.Getter)
                .Select((me, i) => new CsvColumnInfo<T>(i, me, me.MemberInfo.GetCustomAttribute<FormatAttribute>()?.Format)).ToList();
        }

        static string DecodeCsv(string s)
        {
            if (s.StartsWith("\"") && s.EndsWith("\""))
            {
                string str = s.Substring(1, s.Length - 2).Replace("\"\"", "\"");

                return Regex.Replace(str, "(?<!\r)\n", "\r\n");
            }

            return s;
        }

        static object ConvertTo(string s, Type type, CultureInfo culture, string format)
        {
            Type baseType = Nullable.GetUnderlyingType(type);
            if (baseType != null)
            {
                if (!s.HasText()) 
                    return null;

                type = baseType;
            }

            if (type.IsEnum)
                return Enum.Parse(type, s);

            if (type == typeof(DateTime))
                if (format == null)
                    return DateTime.Parse(s, culture);
                else
                    return DateTime.ParseExact(s, format, culture);

            return Convert.ChangeType(s, type, culture);
        }
    }

    public class CsvReadOptions<T> where T: class
    {
        public Func<CsvColumnInfo<T>, CultureInfo, Func<string, object>> ParserFactory;
        public bool AsumeSingleLine = false;
        public Func<Exception, Match, bool> SkipError;
        public TimeSpan RegexTimeout = Regex.InfiniteMatchTimeout;
    }


    public class CsvColumnInfo<T>
    {
        public readonly int Index;
        public readonly MemberEntry<T> MemberEntry;
        public readonly string Format;

        public MemberInfo MemberInfo
        {
            get { return this.MemberEntry.MemberInfo; }
        }

        internal CsvColumnInfo(int index, MemberEntry<T> memberEntry, string format)
        {
            this.Index = index;
            this.MemberEntry = memberEntry;
            this.Format = format;
        }
    }


    [Serializable]
    public class ParseCsvException : Exception
    {
        public int? Row { get; set; }
        public string Member { get; set; }
        public string Value { get; set; }

        public ParseCsvException() { }
        public ParseCsvException(Exception inner) : base(inner.Message, inner)
        {
            this.Row = (int?)inner.Data["row"];
            this.Value = (string)inner.Data["value"];
            this.Member = (string)inner.Data["member"];

        }
        protected ParseCsvException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }

        public override string Message
        {
            get
            {
                return $"(Row: {this.Row}, Member: {this.Member}, Value: '{this.Value}') {base.Message})";
            }
        }
    }
}