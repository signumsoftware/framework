using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using Signum.Utilities.Properties;
using System.Globalization;
using System.Linq.Expressions;

namespace Signum.Utilities
{
    public static class StringExtensions
    {
        static Expression<Func<string, bool>> HasTextExpression = str => str != null && str != "";

        public static bool HasText(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        public static string DefaultText(this string str, string defaultText)
        {
            if (str.HasText()) return str;
            else return defaultText;
        }

        public static string AssertHasText(this string str, string errorMessage)
        {
            if (str.HasText())
                return str;
            else
                throw new ArgumentException(errorMessage);
        }

        public static string Add(this string str, string part, string separator)
        {
            if (str.HasText())
            {
                if (part.HasText())
                    return str + separator + part;
                else
                    return str;
            }
            else
                return part;
        }

        public static string AddLine(this string str, string part)
        {
            return Add(str, part, "\r\n");
        }

        public static string[] Lines(this string str)
        {
            if (str.HasText())
                return str.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            else
                return new string[0];
        }

        public static string Left(this string str, int numChars)
        {
            return Left(str, numChars, true);
        }

        public static string Left(this string str, int numChars, bool throws)
        {
            if (numChars > str.Length)
            {
                if (throws)
                    throw new InvalidOperationException(Resources.String0IsTooShort.Formato(str));
                return str;
            }

            return str.Substring(0, numChars);
        }

        public static string Right(this string str, int numChars)
        {
            return Right(str, numChars, true);
        }

        public static string Right(this string str, int numChars, bool throws)
        {
            if (numChars > str.Length)
            {
                if (throws)
                    throw new InvalidOperationException(Resources.String0IsTooShort.Formato(str));
                return str;
            }

            return str.Substring(str.Length - numChars, numChars);
        }

        public static string RemoveLeft(this string str, int numChars)
        {
            return str.RemoveLeft(numChars, true);
        }

        public static string RemoveLeft(this string str, int numChars, bool throws)
        {
            if (numChars > str.Length)
            {
                if (throws)
                    throw new InvalidOperationException(Resources.String0IsTooShort.Formato(str));
                return "";
            }

            return str.Substring(numChars);
        }

        public static string RemoveRight(this string str, int numChars)
        {
            return str.RemoveRight(numChars, true);
        }

        public static string RemoveRight(this string str, int numChars, bool throws)
        {
            if (numChars > str.Length)
            {
                if (throws)
                    throw new InvalidOperationException(Resources.String0IsTooShort.Formato(str));
                return "";
            }

            return str.Substring(0, str.Length - numChars);
        }


        public static string PadChopRight(this string str, int length)
        {
            str = str ?? "";
            return str.Length > length ? str.Substring(0, length) : str.PadRight(length);
        }

        public static string PadChopLeft(this string str, int length)
        {
            str = str ?? "";
            return str.Length > length ? str.Substring(str.Length - length, length) : str.PadLeft(length);
        }

        public static string VerticalEtc(this string str, int maxLines)
        {
            return str.VerticalEtc(maxLines, "(...)");
        }

        public static string VerticalEtc(this string str, int maxLines, string etcString)
        {
            if (str.HasText() && (str.Contains("/r/n")))
            {
                string[] arr = str.Split(new string[] { "/r/n" }, maxLines, StringSplitOptions.None);
                string res = arr.ToString("/r/n");
                if (res.Length < str.Length)
                    res += etcString;
                str = res;
            }
            return str;
        }

        public static string Etc(this string str, int max, string etcString)
        {
            if (str.HasText() && (str.Length > max))
                return str.Left(max - (etcString.HasText() ? etcString.Length : 0)) + etcString;
            return str;
        }

        public static string Etc(this string str, int max)
        {
            return str.Etc(max, "(...)");
        }

        public static string EtcLines(this string str, int max)
        {
            if(!str.HasText())
                return str;

            int pos = str.IndexOfAny(new []{'\n','\r'});
            if(pos != -1 && pos < max)
                max = pos;

            return str.Etc(max);
        }

        public static bool ContinuesWith(this string str, string subString, int pos)
        {
            return str.IndexOf(subString, pos) == pos;
        }

        public static string RemoveChars(this string str, params char[] chars)
        {
            StringBuilder sb = new StringBuilder(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                if (!chars.Contains(str[i]))
                    sb.Append(str[i]);
            }
            return sb.ToString();
        }

        public static string Formato(string format, object arg0)
        {
            return string.Format(format, arg0);
        }

        public static string Formato(string format, object arg0, object arg1)
        {
            return string.Format(format, arg0, arg1);
        }

        public static string Formato(string format, object arg0, object arg1, object arg2)
        {
            return string.Format(format, arg0, arg1, arg2);
        }

        public static string Formato(this string pattern, params object[] parameters)
        {
            return string.Format(pattern, parameters);
        }

        public static string Formato(this string format, IFormatProvider provider, params object[] args)
        {
            return string.Format(provider, format, args);
        }

        public static string Replace(this string str, Dictionary<string, string> replacements)
        {
            return replacements.Aggregate(str, (s, kvp) => s.Replace(kvp.Key, kvp.Value));
        }

        public static string Indent(this string str, int numChars)
        {
            return Indent(str, numChars, ' ');
        }

        public static string Indent(this string str, int numChars, char indentChar)
        {
            string space = new string(indentChar, numChars);
            StringBuilder sb = new StringBuilder();
            using (StringReader sr = new StringReader(str))
            {
                for (string line = sr.ReadLine(); line != null; line = sr.ReadLine())
                {
                    sb.Append(space);
                    sb.AppendLine(line);
                }
            }

            string result = sb.ToString(0, str.EndsWith("\r\n") ? sb.Length : Math.Max(sb.Length - 2, 0));

            return result;
        }

        public static string NiceName(this string memberName)
        {
            return memberName.Contains('_') ?
              memberName.Replace('_', ' ') :
              memberName.SpacePascal();
        }

        public static string SpacePascal(this string pascalStr)
        {
            return SpacePascal(pascalStr, CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "en");
        }

        public static string SpacePascal(this string pascalStr, bool preserveUppercase)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < pascalStr.Length; i++)
            {
                char a = pascalStr[i];
                if (char.IsUpper(a) && i + 1 < pascalStr.Length && !char.IsUpper(pascalStr[i + 1]))
                {
                    if (sb.Length > 0)
                        sb.Append(' ');
                    sb.Append(preserveUppercase || i == 0 ? a : char.ToLower(a));
                }
                else
                    sb.Append(a);

            }

            return sb.ToString();
        }

        public static string FirstUpper(this string str)
        {
            if (str.HasText())
                return char.ToUpper(str[0]) + str.Substring(1);
            return str;
        }

        public static string Replicate(this string str, int times)
        {
            if (times < 0)
                throw new ArgumentException("times");

            StringBuilder sb = new StringBuilder(str.Length * times);
            for (int i = 0; i < times; i++)
                sb.Append(str);
            return sb.ToString();
        }

        public static string Reverse(this string str)
        {
            char[] arr = new char[str.Length];
            int len = str.Length;
            for (int i = 0; i < len; i++)
                arr[i] = str[len - 1 - i];
            return new string(arr);
        }

        // like has an optional ESCAPE not available
        public static bool Like(this string str, string pattern)
        {
            pattern = Regex.Escape(pattern);
            pattern = pattern.Replace("%", ".*").Replace("_", ".");
            pattern = pattern.Replace(@"\[", "[").Replace(@"\]", "]").Replace(@"\^", "^");
            return Regex.IsMatch(str, pattern);
        }

        public static string RemoveDiacritics(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s; 

            string normalizedString = s.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }

        public static string ToComputerSize(this long value)
        {
            return ToComputerSize(value, false);
        }

        static string[] abbreviations = new[] { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string[] magnitudes = new[] { "Bytes", "KBytes", "MBytes", "GBytes", "TBytes", "PBytes", "EBytes", "ZBytes", "YBytes" };
        public static string ToComputerSize(this long value, bool useAbbreviations)
        {
            double valor = value;
            long i;
            for (i = 0; i < magnitudes.Length && valor >= 1024; i++)
                valor /= 1024.0;

            return "{0:#,###.00} {1}".Formato(valor, (useAbbreviations ? abbreviations : magnitudes)[i]);
        }


        public static string Combine(this string separator, params object[] elements)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in elements)
            {
                if (item != null)
                {
                    sb.Append(item.ToString());
                    sb.Append(separator);
                }
            }
            return sb.ToString(0, Math.Max(0, sb.Length - separator.Length));  // Remove at the end is faster
        }
    }
}
