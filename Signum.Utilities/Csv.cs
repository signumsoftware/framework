
using System.IO;
using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Concurrent;
using System.Collections;
using System.IO.Pipes;
using System;
using System.ComponentModel.Design.Serialization;

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
        Regex valueRegex = GetRegex(isLine: false, defCulture, defOptions.RegexTimeout, defOptions.ListSeparator);

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

                    if (csvLine.Length > 0)
                    {
                        T? t = null;
                        try
                        {
                            var m = valueRegex.EnumerateMatches(csvLine);

                            t = ReadObject<T>(m, csvLine.AsSpan(), members, parsers);
                        }
                        catch (Exception e)
                        {
                            e.Data["row"] = line;

                            if (defOptions.SkipError == null || !defOptions.SkipError(e, csvLine))
                                throw new ParseCsvException(e);
                        }

                        if (t != null)
                            yield return t;

                    }
                    line++;
                }
            }
        }
        else
        {
            Regex lineRegex = GetRegex(isLine: true, defCulture, defOptions.RegexTimeout, defOptions.ListSeparator);

            using (StreamReader sr = new StreamReader(stream, encoding))
            {
                string str = sr.ReadToEnd();

                int i = 0;
                foreach (Match m in lineRegex.Matches(str))
                {
                    if (i < skipLines)
                        continue;

                    if (m.Length > 0)
                    {
                        T? t = null;
                        try
                        {
                            var line = m.Value;

                            if (options?.Constructor != null)
                                t = options.Constructor(line);
                            else
                                t = ReadObject<T>(valueRegex.EnumerateMatches(line), line, members, parsers);
                        }
                        catch (Exception e)
                        {
                            e.Data["row"] = i;

                            if (defOptions.SkipError == null || !defOptions.SkipError(e, str.Substring(m.Index, m.Length)))
                                throw new ParseCsvException(e);
                        }
                        if (t != null)
                            yield return t;
                    }
                    i++;
                }
            }
        }
    }

    public static T ReadLine<T>(string csvLine, CultureInfo? culture = null, CsvReadOptions<T>? options = null)
        where T : class
    {
        var defOptions = options ?? new CsvReadOptions<T>();

        var defCulture = GetDefaultCulture(culture);

        Regex regex = GetRegex(isLine: false, defCulture, defOptions.RegexTimeout);

        var vme = regex.EnumerateMatches(csvLine);

        var members = CsvMemberCache<T>.Members;

        return ReadObject<T>(vme,
            csvLine.AsSpan(),
            members,
            members.Select(c => GetParser(defCulture, c, defOptions.ParserFactory)).ToList());
    }


    private static ValueParser GetParser<T>(CultureInfo culture, CsvMemberInfo<T> column, Func<CsvMemberInfo<T>, CultureInfo, ValueParser?>? parserFactory)
    {
        if (parserFactory != null)
        {
            var result = parserFactory(column, culture);

            if (result != null)
                return result;
        }

        var type = column.IsCollection ? column.MemberInfo.ReturningType().ElementType()! : column.MemberInfo.ReturningType();

        return GetBasicParser(type.UnNullify(), culture, column.Format);
    }

    public delegate object? ValueParser(ReadOnlySpan<char> str);

    static T ReadObject<T>(Regex.ValueMatchEnumerator vme, ReadOnlySpan<char> line, List<CsvMemberInfo<T>> members, List<ValueParser> parsers)
    {
        T t = Activator.CreateInstance<T>();

        bool endsInCollection = false; 
        int i = 0;
        foreach (var v in vme)
        {
            if (members.Count <= i)
                continue;

            var value = line.Slice(v.Index, v.Length);
            var member = members[i];
            var parser = parsers[i];
            try
            {
                if (!member.IsCollection)
                {
                    value = DecodeCsv(value);

                    object? val = parser(value);

                    member.MemberEntry.Setter!(t, val);
                }
                else
                {
                    if (i != members.Count - 1)
                        throw new InvalidOperationException($"Collection {member.MemberInfo} should be the last member");
                    endsInCollection = true;
                    var list = (IList)Activator.CreateInstance(member.MemberInfo.ReturningType())!;

                    value = DecodeCsv(value);
                    object? val = parser(value);
                    list.Add(val);

                    foreach (var v2 in vme)
                    {
                        value = line.Slice(v2.Index, v2.Length);
                        value = DecodeCsv(value);
                        val = parser(value);
                        list.Add(val);
                    }

                    member.MemberEntry.Setter!(t, list);
                }
            }
            catch (Exception e)
            {
                e.Data["value"] = new String(value);
                e.Data["member"] = members[i].MemberInfo.Name;
                throw;
            }

            i++;
        }

        if (!endsInCollection && i != members.Count)
            throw new FormatException("Only {0} columns found (instead of {1}) in line: {2}".FormatWith(i, members.Count, new string(line)));

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

        Regex valueRegex = GetRegex(false, defCulture, defOptions.RegexTimeout, defOptions.ListSeparator);
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
                        m = valueRegex.Match(csvLine);
                        if (m.Length > 0)
                        {
                            t = m.Groups["val"].Captures.Select(c => c.Value).ToArray();
                        }
                    }
                    catch (Exception e)
                    {
                        e.Data["row"] = line;

                        if (defOptions.SkipError == null || !defOptions.SkipError(e, csvLine))
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

                var matches = valueRegex.Matches(str).Cast<Match>();

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

                            if (defOptions.SkipError == null || !defOptions.SkipError(e, m.Value))
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
            if(longs.All(a=> a != null))
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
        }).ToString("\r\n")}}
            }
            """;
    }

    static ConcurrentDictionary<(bool multiLine, char separator, TimeSpan timeout), Regex> regexCache = new();
    readonly static string ValueRegex = "'(?:[^']+|'')*'|[^;\r\n]*".Replace('\'', '"');
    readonly static string LineRegex = $@"^({ValueRegex})?((?!($|\r\n));({ValueRegex}))*($|\r\n)";
    static Regex GetRegex(bool isLine, CultureInfo culture, TimeSpan timeout, char? listSeparator = null)
    {
        char separator = listSeparator ?? GetListSeparator(culture);

        return regexCache.GetOrAdd((isLine, separator, timeout), a =>
            new Regex((isLine ? LineRegex : ValueRegex).Replace(';', a.separator), RegexOptions.Multiline | RegexOptions.ExplicitCapture, a.timeout));
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



    static ReadOnlySpan<char> DecodeCsv(ReadOnlySpan<char> s)
    {
        if (s.StartsWith("\"") && s.EndsWith("\""))
        {
            string str = new string(s[1..^1]).Replace("\"\"", "\"");

            return Regex.Replace(str, "(?<!\r)\n", "\r\n");
        }

        return s;
    }

    static ValueParser GetBasicParser(Type type, CultureInfo culture, string? format)
    {
        return type switch
        {
            _ when type == typeof(string) => str => str.Length == 0 ? null : str.ToString(),
            _ when type == typeof(byte) => str => str.Length == 0 ? null : byte.Parse(str, NumberStyles.Integer, culture),
            _ when type == typeof(sbyte) => str => str.Length == 0 ? null : sbyte.Parse(str, NumberStyles.Integer, culture),
            _ when type == typeof(short) => str => str.Length == 0 ? null : short.Parse(str, NumberStyles.Integer, culture),
            _ when type == typeof(ushort) => str => str.Length == 0 ? null : ushort.Parse(str, NumberStyles.Integer, culture),
            _ when type == typeof(int) => str => str.Length == 0 ? null : int.Parse(str, NumberStyles.Integer, culture),
            _ when type == typeof(uint) => str => str.Length == 0 ? null : uint.Parse(str, NumberStyles.Integer, culture),
            _ when type == typeof(long) => str => str.Length == 0 ? null : long.Parse(str, NumberStyles.Integer, culture),
            _ when type == typeof(ulong) => str => str.Length == 0 ? null : ulong.Parse(str, NumberStyles.Integer, culture),
            _ when type == typeof(float) => str => str.Length == 0 ? null : float.Parse(str, NumberStyles.Float, culture),
            _ when type == typeof(double) => str => str.Length == 0 ? null : double.Parse(str, NumberStyles.Float, culture),
            _ when type == typeof(decimal) => str => str.Length == 0 ? null : decimal.Parse(str, NumberStyles.Number, culture),
            _ when type == typeof(DateTime) => str => str.Length == 0 ? null : DateTime.ParseExact(str, format, culture),
            _ when type == typeof(DateTimeOffset) => str => str.Length == 0 ? null : DateTimeOffset.ParseExact(str, format, culture),
            _ when type == typeof(DateOnly) => str => str.Length == 0 ? null : DateOnly.ParseExact(str, format, culture),
            _ when type == typeof(TimeOnly) => str => str.Length == 0 ? null : TimeOnly.ParseExact(str, format, culture),
            _ when type == typeof(Guid) => str => str.Length == 0 ? null : Guid.Parse(str.ToString()),
            _ when type.IsEnum => str => str.Length == 0 ? null : Enum.Parse(type, str),
            _ => str => Convert.ChangeType(new string(str), type, culture)
        };
    }
}

public class CsvReadOptions<T> : CsvReadOptions
    where T : class 
{
    public Func<CsvMemberInfo<T>, CultureInfo, Csv.ValueParser?>? ParserFactory;
    public CsvConstructor<T>? Constructor;
}

public delegate T CsvConstructor<T>(ReadOnlySpan<char> line);

public class CsvReadOptions
{
    public bool AsumeSingleLine = true; //Breaking change!
    public Func<Exception, string, bool>? SkipError;
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
