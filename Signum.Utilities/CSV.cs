using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;
using Signum.Utilities.Properties;
using System.Globalization;
using System.Reflection;

namespace Signum.Utilities
{
    public static class CSV
    {
        public static string ToCSV<T>(this IEnumerable<T> collection, string fileName)
        {
            using (FileStream fs = File.Create(fileName))
                ToCSV<T>(collection, fs, Encoding.UTF8, CultureInfo.CurrentCulture);

            return fileName;
        }

        public static string ToCSV<T>(this IEnumerable<T> collection, string fileName, Encoding encoding)
        {
            using (FileStream fs = File.Create(fileName))
                ToCSV<T>(collection, fs, encoding, CultureInfo.CurrentCulture);

            return fileName;
        }

        public static string ToCSV<T>(this IEnumerable<T> collection, string fileName, Encoding encoding, CultureInfo culture)
        {
            using (FileStream fs = File.Create(fileName))
                ToCSV<T>(collection, fs, encoding, culture);

            return fileName;
        }

        public static byte[] ToCSV<T>(this IEnumerable<T> collection)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                collection.ToCSV(ms, Encoding.UTF8, CultureInfo.CurrentCulture);
                return ms.ToArray();
            }
        }

        public static byte[] ToCSV<T>(this IEnumerable<T> collection, Encoding encoding)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                collection.ToCSV(ms, encoding, CultureInfo.CurrentCulture);
                return ms.ToArray();
            }
        }

        public static byte[] ToCSV<T>(this IEnumerable<T> collection, Encoding encoding, CultureInfo culture)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                collection.ToCSV(ms, encoding, culture);
                return ms.ToArray();
            }
        }

        public static void ToCSV<T>(this IEnumerable<T> collection, Stream stream, Encoding encoding, CultureInfo culture)
        {
            string separator = culture.TextInfo.ListSeparator; 

            List<MemberEntry<T>> members = MemberEntryFactory.GenerateList<T>();

            using (StreamWriter sw = new StreamWriter(stream, encoding))
            {
                sw.WriteLine(members.ToString(m => HandleSpaces(m.Name), separator));

                foreach (var item in collection)
                {
                    sw.WriteLine(members.ToString(m => m.Getter(item).TryCC(a => EncodeCSV(ConvertToString(a, culture), culture)), separator));
                }
            }
        }

        static string EncodeCSV(string p, CultureInfo culture)
        {
            string separator = culture.TextInfo.ListSeparator;

            if (p.Contains(separator) || p.Contains("\""))
            {
                return "\"" + p.Replace("\"", "\"\"") + "\"";
            }
            return p;
        }

        private static string ConvertToString(object a, CultureInfo culture)
        {
            IFormattable f = a as IFormattable;
            if (f != null)
                return f.ToString(null, culture);
            else
                return a.ToString();
        }

        private static string HandleSpaces(string p)
        {
            return p.Replace("__", "^").Replace("_", " ").Replace("^", "_");
        }

        public static List<T> ReadCVS<T>(string fileName) where T : new()
        {
            using (FileStream fs = File.OpenRead(fileName))
                return ReadCVS<T>(fs, Encoding.UTF8, CultureInfo.CurrentCulture, true).ToList();
        }

        public static List<T> ReadCVS<T>(string fileName, Encoding encoding) where T : new()
        {
            using (FileStream fs = File.OpenRead(fileName))
                return ReadCVS<T>(fs, encoding, CultureInfo.CurrentCulture, true).ToList();
        }

        public static List<T> ReadCVS<T>(string fileName, Encoding encoding, CultureInfo culture) where T : new()
        {
            using (FileStream fs = File.OpenRead(fileName))
                return ReadCVS<T>(fs, encoding, culture, true).ToList();
        }

        public static List<T> ReadCVS<T>(string fileName, Encoding encoding, CultureInfo culture, bool skipFirtsLine) where T : new()
        {
            using (FileStream fs = File.OpenRead(fileName))
                return ReadCVS<T>(fs, encoding, culture, skipFirtsLine).ToList();
        }

        public static List<T> ReadCVS<T>(byte[] data) where T : new()
        {
            using (MemoryStream ms = new MemoryStream(data))
                return ReadCVS<T>(ms, Encoding.UTF8, CultureInfo.CurrentCulture, true).ToList();
        }

        public static List<T> ReadCVS<T>(byte[] data, Encoding encoding) where T : new()
        {
            using (MemoryStream ms = new MemoryStream(data))
                return ReadCVS<T>(ms, encoding, CultureInfo.CurrentCulture, true).ToList();
        }

        public static List<T> ReadCVS<T>(byte[] data, Encoding encoding, CultureInfo culture) where T : new()
        {
            using (MemoryStream ms = new MemoryStream(data))
                return ReadCVS<T>(ms, encoding, culture, true).ToList();
        }

        public static List<T> ReadCVS<T>(byte[] data, Encoding encoding, CultureInfo culture, bool skipFirtsLine) where T : new()
        {
            using (MemoryStream ms = new MemoryStream(data))
                return ReadCVS<T>(ms, encoding, culture, skipFirtsLine).ToList();
        }

        
        public static IEnumerable<T> ReadCVS<T>(this Stream stream, Encoding encoding, CultureInfo culture, bool skipFirst)
            where T : new()
        {
            List<MemberEntry<T>> members = MemberEntryFactory.GenerateList<T>(MemberOptions.Fields | MemberOptions.Properties | MemberOptions.Untyped | MemberOptions.Setters);

            using (StreamReader sr = new StreamReader(stream, encoding))
            {
                if (skipFirst) sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    string currentLine = sr.ReadLine();

                    MatchCollection matches = Regex.Matches(currentLine, culture.TextInfo.ListSeparator + @"(?=([^""]*""[^""]*"")*(?![^""]*""))");

                    int[] nums = matches.Cast<Match>().Select(a => a.Index).PreAnd(-1).And(currentLine.Length).ToArray();

                    string[] parts = nums.BiSelect((a, b) => currentLine.Substring(a + 1, b - a - 1)).ToArray();

                    if (parts.Length != members.Count)
                        throw new FormatException(currentLine);

                    T t = new T();
                    for (int i = 0; i < parts.Length; i++)
                    {
                        object value = ConvertTo(DecodeCSV(parts[i]), members[i].MemberInfo.ReturningType(), culture);

                        members[i].UntypedSetter(t, value); 
                    }

                    yield return t;
                }
            }
        }

        static string DecodeCSV(string s)
        {
            if (s.StartsWith("\""))
            {
                if (!s.EndsWith("\""))
                    throw new FormatException("Cell starts by quotes but not ends with quotes".Formato(s));

                return s.Substring(1, s.Length - 2).Replace("\"\"", "\"");
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
                if (!s.Trim().HasText()) return null;
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
