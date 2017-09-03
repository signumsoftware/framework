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
    public static class Tsv
    {
        public static Encoding DefaultEncoding = Encoding.GetEncoding(1252);
        public static CultureInfo Culture = CultureInfo.InvariantCulture;

        public static string ToTsvFile<T>(T[,] collection, string fileName, Encoding encoding = null, bool autoFlush = false, bool append = false,
            Func<TsvColumnInfo<T>, Func<object, string>> toStringFactory = null)
        {
            encoding = encoding ?? DefaultEncoding;

            using (FileStream fs = append ? new FileStream(fileName, FileMode.Append, FileAccess.Write) : File.Create(fileName))
            { 
                using (StreamWriter sw = new StreamWriter(fs, encoding) { AutoFlush = autoFlush })
                {
                    for (int i = 0; i < collection.GetLength(0); i++)
                    {
                        for (int j = 0; j < collection.GetLength(1); j++)
                        {
                            sw.Write(collection[i, j]);
                            sw.Write(tab);
                        }
                        if (i < collection.GetLength(0))
                            sw.WriteLine();
                    }
                }
            }
        
            return fileName;
        }

        public static string ToTsvFile<T>(this IEnumerable<T> collection, string fileName, Encoding encoding = null, bool writeHeaders = true, bool autoFlush = false, bool append = false,
            Func<TsvColumnInfo<T>, Func<object, string>> toStringFactory = null)
        {
            using (FileStream fs = append ? new FileStream(fileName, FileMode.Append, FileAccess.Write) : File.Create(fileName))
                ToTsv<T>(collection, fs, encoding, writeHeaders, autoFlush, toStringFactory);

            return fileName;
        }

        public static byte[] ToTsvBytes<T>(this IEnumerable<T> collection, Encoding encoding = null, bool writeHeaders = true, bool autoFlush = false,
            Func<TsvColumnInfo<T>, Func<object, string>> toStringFactory = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                collection.ToTsv(ms, encoding, writeHeaders, autoFlush, toStringFactory);
                return ms.ToArray();
            }
        }

        private const string tab = "\t";

        public static void ToTsv<T>(this IEnumerable<T> collection, Stream stream, Encoding encoding = null, bool writeHeaders = true, bool autoFlush = false,
            Func<TsvColumnInfo<T>, Func<object, string>> toStringFactory = null)
        {
            encoding = encoding ?? DefaultEncoding;

            if (typeof(IList).IsAssignableFrom(typeof(T)))
            {
                using (StreamWriter sw = new StreamWriter(stream, encoding) { AutoFlush = autoFlush })
                {
                    foreach (IList row in collection)
                    {
                        for (int i = 0; i < row.Count; i++)
                        {
                            var obj = row[i];

                            var str = ConvertToString(obj);

                            sw.Write(str);
                            if (i < row.Count - 1)
                                sw.Write(tab);
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
                var toString = columns.Select(c => GetToString(c, toStringFactory)).ToList();

                using (StreamWriter sw = new StreamWriter(stream, encoding) { AutoFlush = autoFlush })
                {
                    if (writeHeaders)
                        sw.WriteLine(members.ToString(m => HandleSpaces(m.Name), tab));

                    foreach (var item in collection)
                    {
                        for (int i = 0; i < members.Count; i++)
                        {
                            var obj = members[i].Getter(item);

                            var str = toString[i](obj);

                            sw.Write(str);
                            if (i < members.Count - 1)
                                sw.Write(tab);
                            else
                                sw.WriteLine();
                        }
                    }
                }
            }
        }

        private static Func<object, string> GetToString<T>(TsvColumnInfo<T> column, Func<TsvColumnInfo<T>, Func<object, string>> toStringFactory)
        {
            if (toStringFactory != null)
            {
                var result = toStringFactory(column);

                if (result != null)
                    return result;
            }

            return ConvertToString;
        }

        static string ConvertToString(object obj)
        {
            if (obj == null)
                return "";

            if (obj is IFormattable f)
                return f.ToString(null, Culture);
            else
            {
                var p = obj.ToString();
                if (p != null && p.Contains(tab))
                    throw new InvalidDataException("TSV fields can't contain the tab character, found one in value: " + p);
                return p;
            }
        }

        static string HandleSpaces(string p)
        {
            return p.Replace("__", "^").Replace("_", " ").Replace("^", "_");
        }

        public static List<T> ReadFile<T>(string fileName, Encoding encoding = null, int skipLines = 1, TsvReadOptions<T> options = null) where T : class, new()
        {
            encoding = encoding ?? DefaultEncoding;

            using (FileStream fs = File.OpenRead(fileName))
                return ReadStream<T>(fs, encoding, skipLines, options).ToList();
        }

        public static List<T> ReadBytes<T>(byte[] data, Encoding encoding = null, int skipLines = 1, TsvReadOptions<T> options = null) where T : class, new()
        {
            using (MemoryStream ms = new MemoryStream(data))
                return ReadStream<T>(ms, encoding, skipLines, options).ToList();
        }

        public static IEnumerable<T> ReadStream<T>(Stream stream, Encoding encoding = null, int skipLines = 1, TsvReadOptions<T> options = null) where T : class, new()
        {
            encoding = encoding ?? DefaultEncoding;
            if (options == null)
                options = new TsvReadOptions<T>();

            var columns = ColumnInfoCache<T>.Columns;
            var members = columns.Select(c => c.MemberEntry).ToList();
            var parsers = columns.Select(c => GetParser(c, options.ParserFactory)).ToList();


            using (StreamReader sr = new StreamReader(stream, encoding))
            {
                for (int i = 0; i < skipLines; i++)
                    sr.ReadLine();

                var line = skipLines;
                while (true)
                {
                    string tsvLine = sr.ReadLine();

                    if (tsvLine == null)
                        yield break;

                    T t = null;
                    try
                    {
                        t = ReadObject<T>(tsvLine, members, parsers);
                    }
                    catch (Exception e)
                    {
                        e.Data["row"] = line;

                        if (options.SkipError == null || !options.SkipError(e, tsvLine))
                            throw new ParseCsvException(e);
                    }

                    if (t != null)
                        yield return t;
                }
            }
        }

        public static T ReadLine<T>(string tsvLine, TsvReadOptions<T> options = null)
            where T : class, new()
        {
            if (options == null)
                options = new TsvReadOptions<T>();

            var columns = ColumnInfoCache<T>.Columns;

            return ReadObject<T>(tsvLine,
                columns.Select(c => c.MemberEntry).ToList(),
                columns.Select(c => GetParser(c, options.ParserFactory)).ToList());
        }

        private static Func<string, object> GetParser<T>(TsvColumnInfo<T> column, Func<TsvColumnInfo<T>, Func<string, object>> parserFactory)
        {
            if (parserFactory != null)
            {
                var result = parserFactory(column);

                if (result != null)
                    return result;
            }

            return str => ConvertTo(str, column.MemberInfo.ReturningType(), column.Format);
        }

        static T ReadObject<T>(string line, List<MemberEntry<T>> members, List<Func<string, object>> parsers) where T : new()
        {
            var vals = line.Split(new[] { tab }, StringSplitOptions.None).ToList();

            if (vals.Count < members.Count)
                throw new FormatException("Only {0} columns found (instead of {1}) in line: {2}".FormatWith(vals.Count, members.Count, line));

            T t = new T();
            for (int i = 0; i < members.Count; i++)
            {
                string str = null;
                try
                {
                    str = vals[i];
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



        
        static class ColumnInfoCache<T>
        {
            public static List<TsvColumnInfo<T>> Columns = MemberEntryFactory.GenerateList<T>(MemberOptions.Fields | MemberOptions.Properties | MemberOptions.Typed | MemberOptions.Setters | MemberOptions.Getter)
                .Select((me, i) => new TsvColumnInfo<T>(i, me, me.MemberInfo.GetCustomAttribute<FormatAttribute>()?.Format)).ToList();
        }

        static object ConvertTo(string s, Type type, string format)
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
                    return DateTime.Parse(s, Culture);
                else
                    return DateTime.ParseExact(s, format, Culture);

            return Convert.ChangeType(s, type, Culture);
        }
    }

    public class TsvReadOptions<T> where T : class
    {
        public Func<TsvColumnInfo<T>, Func<string, object>> ParserFactory;
        public Func<Exception, string, bool> SkipError;
    }


    public class TsvColumnInfo<T>
    {
        public readonly int Index;
        public readonly MemberEntry<T> MemberEntry;
        public readonly string Format;

        public MemberInfo MemberInfo
        {
            get { return this.MemberEntry.MemberInfo; }
        }

        internal TsvColumnInfo(int index, MemberEntry<T> memberEntry, string format)
        {
            this.Index = index;
            this.MemberEntry = memberEntry;
            this.Format = format;
        }
    }


    [Serializable]
    public class ParseTsvException : Exception
    {
        public int? Row { get; set; }
        public string Member { get; set; }
        public string Value { get; set; }

        public ParseTsvException() { }
        public ParseTsvException(Exception inner) : base(inner.Message, inner)
        {
            this.Row = (int?)inner.Data["row"];
            this.Value = (string)inner.Data["value"];
            this.Member = (string)inner.Data["member"];

        }
        protected ParseTsvException(
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