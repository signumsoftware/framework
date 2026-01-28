using System.IO;
using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Concurrent;
using System.Collections;
using System.IO.Pipes;

namespace Signum.Utilities;

public static class Csv
{
    // Default changed since Excel exports not to UTF8 and https://stackoverflow.com/questions/49215791/vs-code-c-sharp-system-notsupportedexception-no-data-is-available-for-encodin
    public static Encoding DefaultEncoding => Encoding.UTF8;

    public static CultureInfo? DefaultCulture = null;

    public static string ToCsvFile<T>(this IEnumerable<T> collection, string fileName, Encoding? encoding = null, CultureInfo? culture = null, bool writeHeaders = true, bool autoFlush = false, bool append = false,
        Func<CsvMemberInfo<T>, CultureInfo, Func<object?, string?>>? toStringFactory = null)
    {
        using (FileStream fs = append ? new FileStream(fileName, FileMode.Append, FileAccess.Write) : File.Create(fileName))
            ToCsv<T>(collection, fs, encoding, culture, writeHeaders, autoFlush, toStringFactory);

        return fileName;
    }

    public static byte[] ToCsvBytes<T>(this IEnumerable<T> collection, Encoding? encoding = null, CultureInfo? culture = null, bool writeHeaders = true, bool autoFlush = false,
        Func<CsvMemberInfo<T>, CultureInfo, Func<object?, string?>>? toStringFactory = null)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            collection.ToCsv(ms, encoding, culture, writeHeaders, autoFlush, toStringFactory);
            return ms.ToArray();
        }
    }

    public static void ToCsv<T>(this IEnumerable<T> collection, Stream stream, Encoding? encoding = null, CultureInfo? culture = null, bool writeHeaders = true, bool autoFlush = false,
        Func<CsvMemberInfo<T>, CultureInfo, Func<object?, string?>>? toStringFactory = null)
    {
        var defEncoding = encoding ?? DefaultEncoding;
        var defCulture = GetDefaultCulture(culture);

        string separator = GetListSeparator(defCulture).ToString();

        if (typeof(IList).IsAssignableFrom(typeof(T)))
        {
            using (StreamWriter sw = new StreamWriter(stream, defEncoding) { AutoFlush = autoFlush })
            {
                foreach (IList? row in collection)
                {
                    for (int i = 0; i < row!.Count; i++)
                    {
                        var obj = row![i];

                        var str = EncodeCsv(ConvertToString(obj, null, defCulture), defCulture);

                        sw.Write(str);
                        if (i < row!.Count - 1)
                            sw.Write(separator);
                        else
                            sw.WriteLine();
                    }

                }
            }
        }
        else
        {
            var members = CsvMemberCache<T>.Members;
            var toString = members.Select(c => GetToString(defCulture, c, toStringFactory)).ToList();

            using (StreamWriter sw = new StreamWriter(stream, defEncoding) { AutoFlush = autoFlush })
            {
                if (writeHeaders)
                    sw.WriteLine(members.ToString(m => HandleSpaces(m.MemberInfo.Name), separator));

                foreach (var item in collection)
                {
                    for (int i = 0; i < members.Count; i++)
                    {
                        var member = members[i];
                        var toStr = toString[i];
                        if (!member.IsCollection)
                        {
                            if (i != 0)
                                sw.Write(separator);

                            var obj = member.MemberEntry.Getter!(item);

                            var str = EncodeCsv(toStr(obj), defCulture);

                            sw.Write(str);
                        }
                        else
                        {
                            var list = (IList?)member.MemberEntry.Getter!(item);

                            for (int j = 0; j < list!.Count; j++)
                            {
                                if (!(i == 0 && j == 0))
                                    sw.Write(separator);

                                var str = EncodeCsv(toStr(list[j]), defCulture);

                                sw.Write(str);
                            }
                        }
                    }

                    sw.WriteLine();
                }
            }
        }
    }


    static string? EncodeCsv(string? p, CultureInfo culture)
    {
        if (p == null)
            return null;

        char separator = GetListSeparator(culture);

        if (p.Contains(separator) || p.Contains("\"") || p.Contains("\r") || p.Contains("\n"))
        {
            return "\"" + p.Replace("\"", "\"\"") + "\"";
        }
        return p;
    }

    private static Func<object?, string?> GetToString<T>(CultureInfo culture, CsvMemberInfo<T> column, Func<CsvMemberInfo<T>, CultureInfo, Func<object?, string?>>? toStringFactory)
    {
        if (toStringFactory != null)
        {
            var result = toStringFactory(column, culture);

            if (result != null)
                return result;
        }

        return obj => ConvertToString(obj, column.Format, culture);
    }

    static string ConvertToString(object? obj, string? format, CultureInfo culture)
    {
        if (obj == null)
            return "";

        if (obj is IFormattable f)
            return f.ToString(format, culture);
        else
            return obj!.ToString()!;
    }

    static string HandleSpaces(string p)
    {
        return p.Replace("__", "^").Replace("_", " ").Replace("^", "_");
    }

    public static List<T> ReadFile<T>(string fileName, Encoding? encoding = null, CultureInfo? culture = null, int skipLines = 1, CsvReadOptions<T>? options = null) where T : class
    {
        encoding ??= DefaultEncoding;
        culture ??= DefaultCulture ?? CultureInfo.CurrentCulture;

        using (FileStream fs = File.OpenRead(fileName))
            return ReadStream<T>(fs, encoding, culture, skipLines, options).ToList();
    }

    public static List<T> ReadBytes<T>(byte[] data, Encoding? encoding = null, CultureInfo? culture = null, int skipLines = 1, CsvReadOptions<T>? options = null) where T : class
    {
        using (MemoryStream ms = new MemoryStream(data))
            return ReadStream<T>(ms, encoding, culture, skipLines, options).ToList();
    }

    public static IEnumerable<T> ReadStream<T>(Stream stream, Encoding? encoding = null, CultureInfo? culture = null, int skipLines = 1, CsvReadOptions<T>? options = null) where T : class
    {
        encoding ??= DefaultEncoding;
        var defCulture = GetDefaultCulture(culture);
        var defOptions = options ?? new CsvReadOptions<T>();

        var members = CsvMemberCache<T>.Members;
        var parsers = members.Select(m => GetParser(defCulture, m, defOptions.ParserFactory)).ToList();

        Regex regex = GetRegex(defCulture, defOptions.RegexTimeout, defOptions.ListSeparator);

        if (defOptions.AsumeSingleLine)
        {
            using (StreamReader sr = new StreamReader(stream, encoding))
            {
                for (int i = 0; i < skipLines; i++)
                    sr.ReadLine();

                var line = skipLines;
                while (true)
                {
                    string? csvLine = sr.ReadLine();

                    if (csvLine == null)
                        yield break;

                    Match? m = null;
                    T? t = null;
                    try
                    {
                        m = regex.Match(csvLine);
                        if (m.Length > 0)
                        {
                            // Use the custom builder if it's available.
                            if (options?.Constructor != null)
                                t = options.Constructor(m);
                            else
                                t = ReadObject<T>(m, members, parsers);
                        }
                    }
                    catch (Exception e)
                    {
                        e.Data["row"] = line;

                        if (defOptions.SkipError == null || !defOptions.SkipError(e, m))
                            throw new ParseCsvException(e);
                    }

                    if (t != null)
                        yield return t;

                    line++;
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
                        T? t = null;
                        try
                        {
                            if (options?.Constructor != null)
                                t = options.Constructor(m);
                            else
                                t = ReadObject<T>(m, members, parsers);
                        }
                        catch (Exception e)
                        {
                            e.Data["row"] = line;

                            if (defOptions.SkipError == null || !defOptions.SkipError(e, m))
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

    public static T ReadLine<T>(string csvLine, CultureInfo? culture = null, CsvReadOptions<T>? options = null)
        where T : class
    {
        var defOptions = options ?? new CsvReadOptions<T>();

        var defCulture = GetDefaultCulture(culture);

        Regex regex = GetRegex(defCulture, defOptions.RegexTimeout);

        Match m = regex.Match(csvLine);

        var members = CsvMemberCache<T>.Members;

        return ReadObject<T>(m,
            members,
            members.Select(c => GetParser(defCulture, c, defOptions.ParserFactory)).ToList());
    }

    private static Func<string, object?> GetParser<T>(CultureInfo culture, CsvMemberInfo<T> column, Func<CsvMemberInfo<T>, CultureInfo, Func<string, object?>?>? parserFactory)
    {
        if (parserFactory != null)
        {
            var result = parserFactory(column, culture);

            if (result != null)
                return result;
        }

        var type = column.IsCollection ? column.MemberInfo.ReturningType().ElementType()! : column.MemberInfo.ReturningType();

        return str => ConvertTo(str, type, culture, column.Format);
    }

    static T ReadObject<T>(Match m, List<CsvMemberInfo<T>> members, List<Func<string, object?>> parsers)
    {
        var vals = m.Groups["val"].Captures;

        if (vals.Count < members.Count)
            throw new FormatException("Only {0} columns found (instead of {1}) in line: {2}".FormatWith(vals.Count, members.Count, m.Value));

        T t = Activator.CreateInstance<T>();

        for (int i = 0; i < members.Count; i++)
        {
            var member = members[i];
            var parser = parsers[i];
            string? str = null;
            try
            {
                if (!member.IsCollection)
                {
                    str = DecodeCsv(vals[i].Value);

                    object? val = parser(str);

                    member.MemberEntry.Setter!(t, val);
                }
                else
                {
                    var list = (IList)Activator.CreateInstance(member.MemberInfo.ReturningType())!;

                    for (int j = i; j < vals.Count; j++)
                    {
                        str = DecodeCsv(vals[j].Value);

                        object? val = parser(str);

                        list.Add(val);
                    }

                    member.MemberEntry.Setter!(t, list);
                }
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


    public static List<string[]> ReadUntypedFile(string fileName, Encoding? encoding = null, CultureInfo? culture = null, CsvReadOptions? options = null)
    {
        encoding ??= DefaultEncoding;
        culture ??= DefaultCulture ?? CultureInfo.CurrentCulture;

        using (FileStream fs = File.OpenRead(fileName))
            return ReadUntypedStream(fs, encoding, culture, options).ToList();
    }

    public static List<string[]> ReadUntypedBytes(byte[] data, Encoding? encoding = null, CultureInfo? culture = null, CsvReadOptions? options = null)
    {
        using (MemoryStream ms = new MemoryStream(data))
            return ReadUntypedStream(ms, encoding, culture, options).ToList();
    }

    public static IEnumerable<string[]> ReadUntypedStream(Stream stream, Encoding? encoding = null, CultureInfo? culture = null, CsvReadOptions? options = null)
    {
        encoding ??= DefaultEncoding;
        var defCulture = GetDefaultCulture(culture);
        var defOptions = options ?? new CsvReadOptions();

        Regex regex = GetRegex(defCulture, defOptions.RegexTimeout, defOptions.ListSeparator);
        if (defOptions.AsumeSingleLine)
        {
            using (StreamReader sr = new StreamReader(stream, encoding))
            {
                var line = 0;
                while (true)
                {
                    string? csvLine = sr.ReadLine();

                    if (csvLine == null)
                        yield break;

                    Match? m = null;
                    string[]? t = null;
                    try
                    {
                        m = regex.Match(csvLine);
                        if (m.Length > 0)
                        {
                            t = m.Groups["val"].Captures.Select(c => c.Value).ToArray();
                        }
                    }
                    catch (Exception e)
                    {
                        e.Data["row"] = line;

                        if (defOptions.SkipError == null || !defOptions.SkipError(e, m))
                            throw new ParseCsvException(e);
                    }

                    if (t != null)
                        yield return t;

                    line++;
                }
            }
        }
        else
        {
            using (StreamReader sr = new StreamReader(stream, encoding))
            {
                string str = sr.ReadToEnd();

                var matches = regex.Matches(str).Cast<Match>();

                int line = 0;
                foreach (var m in matches)
                {
                    if (m.Length > 0)
                    {
                        string[]? t = null;
                        try
                        {
                            t = m.Groups["val"].Captures.Select(c => c.Value).ToArray();
                        }
                        catch (Exception e)
                        {
                            e.Data["row"] = line;

                            if (defOptions.SkipError == null || !defOptions.SkipError(e, m))
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

    private static CultureInfo GetDefaultCulture(CultureInfo? culture)
    {
        return culture ?? DefaultCulture ?? CultureInfo.CurrentCulture;
    }

    public static string InferClassFromFile(string fileName, Encoding? encoding = null, CultureInfo? culture = null, CsvReadOptions? options = null)
    {
        var lines = ReadUntypedFile(fileName, encoding, culture, options);
        var classCode = InferClass(lines, culture);
        return classCode;
    }

    public static string InferClassFromBytes(byte[] data, Encoding? encoding = null, CultureInfo? culture = null, CsvReadOptions? options = null)
    {
        var lines = ReadUntypedBytes(data, encoding, culture, options);
        var classCode = InferClass(lines, culture);
        return classCode;
    }

    public static string InferClassFromStream(Stream stream, Encoding? encoding = null, CultureInfo? culture = null, CsvReadOptions? options = null)
    {
        var lines = ReadUntypedStream(stream, encoding, culture, options);
        var classCode = InferClass(lines.ToList(), culture);
        return classCode;
    }

    private static string InferClass(List<string[]> lines, CultureInfo? culture)
    {
        var defCulture = GetDefaultCulture(culture);
        var header = lines.FirstEx();
        var values = lines.Skip(1).ToList();

        string ToName(string name)
        {
            return name.Replace("-", "_").ToPascal();
        }

        string? InferType(int index)
        {
            var vals = values.Select(a => a[index]).ToList();
            var isNullable = values.Any(a => string.IsNullOrEmpty(a[index]));


            if (isNullable)
            {
                vals.RemoveAll(a => !a.HasText());
                var result = InferTypeFromVals(vals);
                return result == null ? null : result + "?";
            }

            return InferTypeFromVals(vals);
        }

        string? InferTypeFromVals(List<string> vals)
        {
            if (vals.Count == 0)
                return null;

            var longs = vals.Select(a => a.ToLong(NumberStyles.Integer, defCulture));
            if (longs.All(a => a != null))
                return longs.All(a => int.MinValue <= a && a <= int.MaxValue) ? "int" : "long";

            if (vals.All(a => DateOnly.TryParse(a, defCulture, out _)))
                return "DateOnly";

            if (vals.All(a => TimeOnly.TryParse(a, defCulture, out _)))
                return "TimeOnly";

            if (vals.All(a => DateTime.TryParse(a, defCulture, out _)))
                return "DateTime";

            if (vals.All(a => decimal.TryParse(a, defCulture, out _)))
                return "decimal";

            return "string";
        }

        return $$"""
            public class MyFileCSV
            {
            {{header.Select((name, i) =>
        {
            var type = InferType(i);
            return $"    public required {type ?? "string?"} {(type == null ? "_" : "") + ToName(name)};" + (type == null ? " //Empty" : null);
        }).ToString("\n")}}
            }
            """;
    }


    static ConcurrentDictionary<char, Regex> regexCache = new ConcurrentDictionary<char, Regex>();
    const string BaseRegex = @"^((?<val>'(?:[^']+|'')*'|[^;\n]*))?((?!($|\n));(?<val>'(?:[^']+|'')*'|[^;\n]*))*($|\n)";
    static Regex GetRegex(CultureInfo culture, TimeSpan timeout, char? listSeparator = null)
    {
        char separator = listSeparator ?? GetListSeparator(culture);

        return regexCache.GetOrAdd(separator, s =>
            new Regex(BaseRegex.Replace('\'', '"').Replace(';', s), RegexOptions.Multiline | RegexOptions.ExplicitCapture, timeout));
    }

    private static char GetListSeparator(CultureInfo culture)
    {
        return culture.TextInfo.ListSeparator.SingleEx();
    }

    static class CsvMemberCache<T>
    {
        static CsvMemberCache()
        {
            var memberEntries = MemberEntryFactory.GenerateList<T>(MemberOptions.Fields | MemberOptions.Properties | MemberOptions.Setters | MemberOptions.Getter);
            Members = memberEntries.Select((me, i) =>
            {
                var type = me.MemberInfo.ReturningType();
                var isCollection = typeof(IList).IsAssignableFrom(type);
                if (isCollection)
                {
                    if (type.IsArray)
                        throw new InvalidOperationException($"{me.MemberInfo.Name} is an array, use a List<T> instead");

                    if (i != memberEntries.Count - 1)
                        throw new InvalidOperationException($"{me.MemberInfo.Name} is of {type} but is not the last member");
                }
                return new CsvMemberInfo<T>(i, me, me.MemberInfo.GetCustomAttribute<FormatAttribute>()?.Format, isCollection);
            }).ToList();
        }

        public static List<CsvMemberInfo<T>> Members;
    }

    static string DecodeCsv(string s)
    {
        if (s.StartsWith("\"") && s.EndsWith("\""))
        {
            string str = s[1..^1].Replace("\"\"", "\"");

            return Regex.Replace(str, "(?<!\r)\n", "\n");
        }

        return s;
    }

    static object? ConvertTo(string s, Type type, CultureInfo culture, string? format)
    {
        Type? baseType = Nullable.GetUnderlyingType(type);
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

        if (type == typeof(DateOnly))
            if (format == null)
                return DateOnly.Parse(s, culture);
            else
                return DateOnly.ParseExact(s, format, culture);

        if (type == typeof(Guid))
            return Guid.Parse(s);

        return Convert.ChangeType(s, type, culture);
    }
}

public class CsvReadOptions<T> : CsvReadOptions
    where T : class
{
    public Func<CsvMemberInfo<T>, CultureInfo, Func<string, object?>?>? ParserFactory;
    public Func<Match, T>? Constructor;
}

public class CsvReadOptions
{
    public bool AsumeSingleLine = true;
    public Func<Exception, Match?, bool>? SkipError;
    public TimeSpan RegexTimeout = Regex.InfiniteMatchTimeout;
    public char? ListSeparator;
}


public class CsvMemberInfo<T>
{
    public readonly int Index;
    public readonly MemberEntry<T> MemberEntry;
    public readonly string? Format;
    public readonly bool IsCollection;

    public MemberInfo MemberInfo
    {
        get { return this.MemberEntry.MemberInfo; }
    }

    internal CsvMemberInfo(int index, MemberEntry<T> memberEntry, string? format, bool isCollection)
    {
        this.Index = index;
        this.MemberEntry = memberEntry;
        this.Format = format;
        this.IsCollection = isCollection;
    }
}


public class ParseCsvException : Exception
{
    public int? Row { get; set; }
    public string Member { get; set; }
    public string Value { get; set; }

    public ParseCsvException(Exception inner) : base(inner.Message, inner)
    {
        this.Row = (int?)inner.Data["row"];
        this.Value = (string)inner.Data["value"]!;
        this.Member = (string)inner.Data["member"]!;
    }

    public override string Message
    {
        get
        {
            return $"(Row: {this.Row}, Member: {this.Member}, Value: '{this.Value}') {base.Message})";
        }
    }
}
