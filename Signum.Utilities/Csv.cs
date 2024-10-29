
using System.IO;
using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Concurrent;
using System.Collections;
using System.IO.Pipes;
using System;
using System.ComponentModel.Design.Serialization;
using System.Data;

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
        var separator = defOptions.ListSeparator ?? GetListSeparator(defCulture);

        var members = CsvMemberCache<T>.Members;
        var parsers = members.Select(m => GetParser(defCulture, m, defOptions.ParserFactory)).ToList();
        using (StreamReader sr = new StreamReader(stream, encoding))
        {
            var i = 0;
            foreach (var csvLine in defOptions.AsumeSingleLine ? CsvLinesSplitter.ReadCsvLinesSimple(sr) : CsvLinesSplitter.ReadCsvLines(sr))
            {
                if (skipLines <= i)
                {
                    if(csvLine.Length > 0)
                    {
                        T? t = null;
                        try
                        {
                            t = ReadObject<T>(csvLine, members, separator, parsers);
                        }
                        catch (Exception e)
                        {
                            e.Data["row"] = csvLine;

                            if (defOptions.SkipError == null || !defOptions.SkipError(e, csvLine))
                                throw new ParseCsvException(e);
                        }

                        if (t != null)
                            yield return t;
                    }
                }

                i++;
            }
        }
    }

    public static T ReadLine<T>(string csvLine, CultureInfo? culture = null, CsvReadOptions<T>? options = null)
        where T : class
    {
        var defOptions = options ?? new CsvReadOptions<T>();
        var defCulture = GetDefaultCulture(culture);
        var members = CsvMemberCache<T>.Members;
        var separator = defOptions.ListSeparator ?? GetListSeparator(defCulture);
        var parsers = members.Select(c => GetParser(defCulture, c, defOptions.ParserFactory)).ToList();
        return ReadObject<T>(csvLine, members, separator, parsers);
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

    static T ReadObject<T>(string line, List<CsvMemberInfo<T>> members, char separator, List<ValueParser> parsers)
    {
        T t = Activator.CreateInstance<T>();

        bool endsInCollection = false;
        var enumerator = new CsvEnumerator(line, separator);
        int i = 0;
        while (enumerator.MoveNext())
        {
            var span = enumerator.Current;
            if (members.Count <= i)
                continue;

            var member = members[i];
            var parser = parsers[i];
            try
            {
                if (!member.IsCollection)
            {
              
                    object? val = parser(span);

                    member.MemberEntry.Setter!(t, val);
              
            }
            else
            {
                if (i != members.Count - 1)
                    throw new InvalidOperationException($"Collection {member.MemberInfo} should be the last member");

                endsInCollection = true;
                var list = (IList)Activator.CreateInstance(member.MemberInfo.ReturningType())!;

                object? val = parser(span);
                list.Add(val);

                while (enumerator.MoveNext())
                {
                    span = enumerator.Current;
                    val = parser(span);
                    list.Add(val);
                }

                member.MemberEntry.Setter!(t, list);
            }
            }
            catch (Exception e)
            {
                e.Data["value"] = new String(span);
                e.Data["member"] = member.MemberInfo.Name;
                throw;
            }

            i++;
        }

        if (!endsInCollection && i != members.Count)
            throw new FormatException("Only {0} columns found (instead of {1}) in line: {2}".FormatWith(i, members.Count, new string(line)));

        return t;
    }


    public static List<List<string>> ReadUntypedFile(string fileName, Encoding? encoding = null, CultureInfo? culture = null, CsvReadOptions? options = null)
    {
        encoding ??= DefaultEncoding;
        culture ??= DefaultCulture ?? CultureInfo.CurrentCulture;

        using (FileStream fs = File.OpenRead(fileName))
            return ReadUntypedStream(fs, encoding, culture, options).ToList();
    }

    public static List<List<string>> ReadUntypedBytes(byte[] data, Encoding? encoding = null, CultureInfo? culture = null, CsvReadOptions? options = null) 
    {
        using (MemoryStream ms = new MemoryStream(data))
            return ReadUntypedStream(ms, encoding, culture, options).ToList();
    }

    public static IEnumerable<List<string>> ReadUntypedStream(Stream stream, Encoding? encoding = null, CultureInfo? culture = null, CsvReadOptions? options = null)
    {
        encoding ??= DefaultEncoding;
        var defCulture = GetDefaultCulture(culture);
        var defOptions = options ?? new CsvReadOptions();
        var separator = defOptions.ListSeparator ?? GetListSeparator(defCulture);

        int? capacity = null;
        using (StreamReader sr = new StreamReader(stream, encoding))
        {
            foreach (var csvLine in defOptions.AsumeSingleLine ? CsvLinesSplitter.ReadCsvLinesSimple(sr) : CsvLinesSplitter.ReadCsvLines(sr))
            {
                if (csvLine.Length > 0)
                {
                    List<string>? cells = null;
                    try
                    {
                        cells = ToArray(separator, capacity, csvLine);

                        if (capacity == null)
                            capacity = cells.Count;
                    }
                    catch (Exception e)
                    {
                        e.Data["row"] = csvLine;

                        if (defOptions.SkipError == null || !defOptions.SkipError(e, csvLine))
                            throw new ParseCsvException(e);
                    }

                    if (cells != null)
                        yield return cells;
                }
            }
        }
    }

    static List<string> ToArray(char separator, int? lastCapacity, string csvLine)
    {
        List<string> cells = lastCapacity.HasValue ? new List<string>(lastCapacity.Value) : new List<string>();
        CsvEnumerator enumerator = new CsvEnumerator(csvLine, separator);
        foreach (var span in enumerator)
        {
            cells.Add(span.ToString());
        }

        return cells;
    }

    static CultureInfo GetDefaultCulture(CultureInfo? culture)
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

    private static string InferClass(List<List<string>> lines, CultureInfo? culture)
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

    static ValueParser GetBasicParser(Type type, CultureInfo culture, string? format)
    {
        if(format != null)
        {
            return type switch
            {
                _ when type == typeof(DateTime) => span => span.Length == 0 ? null : DateTime.ParseExact(span, format, culture),
                _ when type == typeof(DateTimeOffset) => span => span.Length == 0 ? null : DateTimeOffset.ParseExact(span, format, culture),
                _ when type == typeof(DateOnly) => span => span.Length == 0 ? null : DateOnly.ParseExact(span, format, culture),
                _ when type == typeof(TimeOnly) => span => span.Length == 0 ? null : TimeOnly.ParseExact(span, format, culture),
                _ => throw new InvalidOperationException("Format not expected for " + type.Name)
            };
        }


        return type switch
        {
            _ when type == typeof(string) => span => span.Length == 0 ? null : span.ToString(),
            _ when type == typeof(byte) => span => span.Length == 0 ? null : byte.Parse(span, NumberStyles.Integer, culture),
            _ when type == typeof(sbyte) => span => span.Length == 0 ? null : sbyte.Parse(span, NumberStyles.Integer, culture),
            _ when type == typeof(short) => span => span.Length == 0 ? null : short.Parse(span, NumberStyles.Integer, culture),
            _ when type == typeof(ushort) => span => span.Length == 0 ? null : ushort.Parse(span, NumberStyles.Integer, culture),
            _ when type == typeof(int) => span => span.Length == 0 ? null : int.Parse(span, NumberStyles.Integer, culture),
            _ when type == typeof(uint) => span => span.Length == 0 ? null : uint.Parse(span, NumberStyles.Integer, culture),
            _ when type == typeof(long) => span => span.Length == 0 ? null : long.Parse(span, NumberStyles.Integer, culture),
            _ when type == typeof(ulong) => span => span.Length == 0 ? null : ulong.Parse(span, NumberStyles.Integer, culture),
            _ when type == typeof(float) => span => span.Length == 0 ? null : float.Parse(span, NumberStyles.Float, culture),
            _ when type == typeof(double) => span => span.Length == 0 ? null : double.Parse(span, NumberStyles.Float, culture),
            _ when type == typeof(decimal) => span => span.Length == 0 ? null : decimal.Parse(span, NumberStyles.Number, culture),
            _ when type == typeof(DateTime) => span => span.Length == 0 ? null : DateTime.Parse(span, culture),
            _ when type == typeof(DateTimeOffset) => span => span.Length == 0 ? null : DateTimeOffset.Parse(span, culture),
            _ when type == typeof(DateOnly) => span => span.Length == 0 ? null : DateOnly.Parse(span, culture),
            _ when type == typeof(TimeOnly) => span => span.Length == 0 ? null : TimeOnly.Parse(span, culture),
            _ when type == typeof(Guid) => span => span.Length == 0 ? null : Guid.Parse(span.ToString()),
            _ when type.IsEnum => span => span.Length == 0 ? null : Enum.Parse(type, span),
            _ => span => Convert.ChangeType(new string(span), type, culture)
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

    public override string ToString() => MemberInfo.Name;

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

public static class CsvLinesSplitter
{
    public static IEnumerable<string> ReadCsvLinesSimple(StreamReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            yield return line;
        }
    }

    public static IEnumerable<string> ReadCsvLines(StreamReader reader)
    {
        var lineBuilder = new StringBuilder();
        bool insideQuotes = false;

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            for (int i = 0; i < line.Length; i++)
            {
                char currentChar = line[i];

                if (currentChar == '"')
                {
                    // Toggle the insideQuotes flag only if it's not an escaped quote (i.e., not a double quote inside quotes).
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Add one quote to the builder and skip the next one.
                        lineBuilder.Append('"');
                        i++;
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }
                }
                else
                {
                    lineBuilder.Append(currentChar);
                }
            }

            if (insideQuotes)
            {
                // If inside quotes, continue to the next line and append to the current line.
                lineBuilder.Append(Environment.NewLine);
            }
            else
            {
                // Yield the complete line when we're not inside quotes and reset the builder for the next line.
                yield return lineBuilder.ToString();
                lineBuilder.Clear();
            }
        }

        // In case the last line ends while still inside quotes (for edge cases).
        if (lineBuilder.Length > 0)
        {
            yield return lineBuilder.ToString();
        }
    }
}

public ref struct CsvEnumerator
{
    private readonly ReadOnlySpan<char> _line;
    private readonly char _separator;
    int _currentIndex;
    ReadOnlySpan<char> _currentField;

    public CsvEnumerator(ReadOnlySpan<char> line, char separator)
    {
        _line = line;
        _separator = separator;
        _currentIndex = 0;
        _currentField = default;
    }

    public CsvEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        if(_currentIndex == _line.Length)
        {
            _currentField = _line.Slice(_currentIndex, 0);
            _currentIndex++;
            return true;
        }

        if (_currentIndex > _line.Length)
            return false;

        bool inQuotes = false;
        int start = _currentIndex;

        for (int i = _currentIndex; i < _line.Length; i++)
        {
            char c = _line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < _line.Length && _line[i + 1] == '"')
                {
                    // Escaped double quote within a quoted field
                    i++; // Skip the next quote
                }
                else
                {
                    // Toggle inQuotes state
                    inQuotes = !inQuotes;
                }
            }
            else if (c == _separator && !inQuotes)
            {
                // Separator outside quotes - end of field
                _currentField = _line.Slice(start, i - start);
                _currentIndex = i + 1; // Move past the separator
                return true;
            }

            // End of line reached
            if (i == _line.Length - 1)
            {
                _currentField = _line.Slice(start, i - start + 1);
                _currentIndex = _line.Length + 1;
                return true;
            }
        }

        return false;
    }

    public ReadOnlySpan<char> Current
    {
        get
        {
            if (_currentField.Length > 1 && _currentField[0] == '"' && _currentField[^1] == '"')
            {
                // Trim surrounding quotes and handle escaped quotes
                var unquotedField = _currentField.Slice(1, _currentField.Length - 2);
                return RemoveEscapedQuotes(unquotedField);
            }
            return _currentField;
        }
    }

    private static ReadOnlySpan<char> RemoveEscapedQuotes(ReadOnlySpan<char> span)
    {
        int quoteCount = 0;
        for (int i = 0; i < span.Length - 1; i++)
        {
            if (span[i] == '"' && span[i + 1] == '"')
            {
                quoteCount++;
                i++;
            }
        }

        if (quoteCount == 0)
            return span; // No escaped quotes

        char[] result = new char[span.Length - quoteCount];
        int index = 0;
        for (int i = 0; i < span.Length; i++)
        {
            if (i < span.Length - 1 && span[i] == '"' && span[i + 1] == '"')
            {
                result[index++] = '"';
                i++; // Skip the next quote
            }
            else
            {
                result[index++] = span[i];
            }
        }

        return result.AsSpan();
    }
}
