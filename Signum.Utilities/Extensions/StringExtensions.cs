using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace Signum.Utilities
{
    public static class StringExtensions
    {
        static readonly Expression<Func<string, bool>> HasTextExpression = str => (str ?? "").Length > 0;
        [ExpressionField("HasTextExpression")]
        public static bool HasText(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        static readonly Expression<Func<string, string, string>> DefaultTextExpression = (a, b) => ((a ?? "").Length > 0) ? a : b;
        [ExpressionField("DefaultTextExpression")]
        public static string DefaultText(this string str, string defaultText)
        {
            if (str.HasText())
                return str;
            else
                return defaultText;
        }

        public static string AssertHasText(this string str, string errorMessage)
        {
            if (str.HasText())
                return str;
            else
                throw new ArgumentException(errorMessage);
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static string Add(this string str, string separator, string part)
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
            return Add(str, "\r\n", part);
        }

        public static string[] Lines(this string str)
        {
            return str.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        static InvalidOperationException NotFound(string str, char separator)
        {
            return new InvalidOperationException("Separator '{0}' not found in '{1}'".FormatWith(separator, str));
        }

        static InvalidOperationException NotFound(string str, string separator)
        {
            return new InvalidOperationException("Separator '{0}' not found in '{1}'".FormatWith(separator, str));
        }


        /// <summary>
        /// get the substring before the first occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <exception cref="NotFound">If the separator is not found in the string</exception>
        /// <returns></returns>
        public static string Before(this string str, char separator)
        {
            int index = str.IndexOf(separator);
            if (index == -1)
                throw NotFound(str, separator);

            return str.Substring(0, index);
        }

        /// <summary>
        /// get the substring before the first occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <exception cref="NotFound">If the separator is not found in the string</exception>
        /// <returns></returns>
        public static string Before(this string str, string separator)
        {
            int index = str.IndexOf(separator);
            if (index == -1)
                throw NotFound(str, separator);

            return str.Substring(0, index);
        }


        /// <summary>
        /// get the substring after the first occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <exception cref="NotFound">If the separator is not found in the string</exception>
        /// <returns></returns>
        public static string After(this string str, char separator)
        {
            int index = str.IndexOf(separator);
            if (index == -1)
                throw NotFound(str, separator);

            return str.Substring(index + 1);
        }

        /// <summary>
        /// get the substring after the first occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <exception cref="NotFound">If the separator is not found in the string</exception>
        /// <returns></returns>
        public static string After(this string str, string separator)
        {
            int index = str.IndexOf(separator);
            if (index == -1)
                throw NotFound(str, separator);

            return str.Substring(index + separator.Length);
        }


        /// <summary>
        /// get the substring before the first occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns>the substring before the first occurence of the separator or null if not found</returns>
        public static string TryBefore(this string str, char separator)
        {
            if (str == null)
                return null;

            int index = str.IndexOf(separator);
            if (index == -1)
                return null;

            return str.Substring(0, index);
        }


        /// <summary>
        /// get the substring before the first occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns>the substring before the first occurence of the separator or null if not found</returns>
        public static string TryBefore(this string str, string separator)
        {
            if (str == null)
                return null;

            int index = str.IndexOf(separator);
            if (index == -1)
                return null;

            return str.Substring(0, index);
        }

        /// <summary>
        /// get the substring after the first occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns>the substring after the first occurence of the separator or null if not found</returns>
        public static string TryAfter(this string str, char separator)
        {
            if (str == null)
                return null;

            int index = str.IndexOf(separator);
            if (index == -1)
                return null;

            return str.Substring(index + 1);
        }

        /// <summary>
        /// get the substring after the first occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns>the substring after the first occurence of the separator or null if not found</returns>
        public static string TryAfter(this string str, string separator)
        {
            if (str == null)
                return null;

            int index = str.IndexOf(separator);
            if (index == -1)
                return null;

            return str.Substring(index + separator.Length);
        }

        /// <summary>
        /// get the substring before the last occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <exception cref="NotFound">If the separator is not found in the string</exception>
        /// <returns>the substring before the last occurence of the separator</returns>
        public static string BeforeLast(this string str, char separator)
        {
            int index = str.LastIndexOf(separator);
            if (index == -1)
                throw NotFound(str, separator);

            return str.Substring(0, index);
        }


        /// <summary>
        /// get the substring before the last occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <exception cref="NotFound">If the separator is not found in the string</exception>
        /// <returns>the substring before the last occurence of the separator</returns>
        public static string BeforeLast(this string str, string separator)
        {
            int index = str.LastIndexOf(separator);
            if (index == -1)
                throw NotFound(str, separator);

            return str.Substring(0, index);
        }



        /// <summary>
        /// get the substring after the last occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <exception cref="NotFound">If the separator is not found in the string</exception>
        /// <returns>the substring after the last occurence of the separator</returns>
        public static string AfterLast(this string str, char separator)
        {
            int index = str.LastIndexOf(separator);
            if (index == -1)
                throw NotFound(str, separator);

            return str.Substring(index + 1);
        }

        /// <summary>
        /// get the substring after the last occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <exception cref="NotFound">If the separator is not found in the string</exception>
        /// <returns>the substring after the last occurence of the separator</returns>
        public static string AfterLast(this string str, string separator)
        {
            int index = str.LastIndexOf(separator);
            if (index == -1)
                throw NotFound(str, separator);

            return str.Substring(index + separator.Length);
        }


        /// <summary>
        /// get the substring before the last occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns>the substring before the last occurence of the separator or null if not found</returns>
        public static string TryBeforeLast(this string str, char separator)
        {
            if (str == null)
                return null;

            int index = str.LastIndexOf(separator);
            if (index == -1)
                return null;

            return str.Substring(0, index);
        }


        /// <summary>
        /// get the substring before the last occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns>the substring before the last occurence of the separator or null if not found</returns>
        public static string TryBeforeLast(this string str, string separator)
        {
            if (str == null)
                return null;

            int index = str.LastIndexOf(separator);
            if (index == -1)
                return null;

            return str.Substring(0, index);
        }

        /// <summary>
        /// get the substring after the last occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns>the substring after the last occurence of the separator or null if not found</returns>
        public static string TryAfterLast(this string str, char separator)
        {
            if (str == null)
                return null;

            int index = str.LastIndexOf(separator);
            if (index == -1)
                return null;

            return str.Substring(index + 1);
        }

        /// <summary>
        /// get the substring after the last occurence of the separator
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns>the substring after the last occurence of the separator or null if not found</returns>
        public static string TryAfterLast(this string str, string separator)
        {
            if (str == null)
                return null;

            int index = str.LastIndexOf(separator);
            if (index == -1)
                return null;

            return str.Substring(index + separator.Length);
        }
        
        public static string Between(this string str, string firstSeparator, string secondSeparator = null)
        {
            if (secondSeparator == null)
                secondSeparator = firstSeparator;

            int start = str.IndexOf(firstSeparator);
            if (start == -1)
                throw NotFound(str, firstSeparator);

            start = start+firstSeparator.Length ;

            int end = str.IndexOf(secondSeparator, start);
            if (start == -1)
                throw NotFound(str, secondSeparator);

            return str.Substring(start, end - start);
        }

        public static string Between(this string str, char firstSeparator, char secondSeparator = (char)0)
        {
            if (secondSeparator == 0)
                secondSeparator = firstSeparator;

            int start = str.IndexOf(firstSeparator);
            if (start == -1)
                throw NotFound(str, firstSeparator);

            start = start + 1;

            int end = str.IndexOf(secondSeparator, start);
            if (start == -1)
                throw NotFound(str, secondSeparator);

            return str.Substring(start, end - start);
        }

        public static string TryBetween(this string str, string firstSeparator, string secondSeparator = null)
        {
            if (secondSeparator == null)
                secondSeparator = firstSeparator;

            int start = str.IndexOf(firstSeparator);
            if (start == -1)
                return null;

            start = start + 1;

            int end = str.IndexOf(secondSeparator, start);
            if (start == -1)
                return null;

            return str.Substring(start, end - start);
        }

        public static string TryBetween(this string str, char firstSeparator, char secondSeparator = (char)0)
        {
            if (secondSeparator == 0)
                secondSeparator = firstSeparator;

            int start = str.IndexOf(firstSeparator);
            if (start == -1)
                return null;

            start = start + 1;

            int end = str.IndexOf(secondSeparator, start);
            if (start == -1)
                return null;

            return str.Substring(start, end - start);
        }

        public static string Start(this string str, int numChars)
        {
            if (numChars > str.Length)
                throw new InvalidOperationException("String '{0}' is too short".FormatWith(str));

            return str.Substring(0, numChars);
        }

        public static string TryStart(this string str, int numChars)
        {
            if (str == null)
                return null;

            if (numChars > str.Length)
                return str;

            return str.Substring(0, numChars);
        }

        public static string End(this string str, int numChars)
        {
            if (numChars > str.Length)
                throw new InvalidOperationException("String '{0}' is too short".FormatWith(str));

            return str.Substring(str.Length - numChars, numChars);
        }

        public static string TryEnd(this string str, int numChars)
        {
            if (str == null)
                return null;

            if (numChars > str.Length)
                return str;

            return str.Substring(str.Length - numChars, numChars);
        }

        public static string RemoveStart(this string str, int numChars)
        {
            if (numChars > str.Length)
                throw new InvalidOperationException("String '{0}' is too short".FormatWith(str));

            return str.Substring(numChars);
        }

        public static string TryRemoveStart(this string str, int numChars)
        {
            if (numChars > str.Length)
                return "";

            return str.Substring(numChars);
        }

        public static string RemoveEnd(this string str, int numChars)
        {
            if (numChars > str.Length)
                throw new InvalidOperationException("String '{0}' is too short".FormatWith(str));

            return str.Substring(0, str.Length - numChars);
        }

        public static string TryRemoveEnd(this string str, int numChars)
        {
            if (numChars > str.Length)
                return "";

            return str.Substring(0, str.Length - numChars);
        }

        public static List<string> SplitInGroupsOf(this string str, int maxChars)
        {
            if (string.IsNullOrEmpty(str))
                return new List<string>();

            if (maxChars <= 0)
                throw new ArgumentOutOfRangeException("maxChars should be greater than 0");

            List<string> result = new List<string>();

            for (int i = 0; i < str.Length; i+= maxChars)
            {
                if (i + maxChars < str.Length)
                    result.Add(str.Substring(i, maxChars));
                else
                    result.Add(str.Substring(i));
            }

            return result;
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

        public static string FirstNonEmptyLine(this string str)
        {
            if (str == null)
                return null;

            int index = 0;

            while (index < str.Length)
        {
                var newIndex = str.IndexOfAny(new[] { '\r', '\n' }, index).NotFound(str.Length);

                if (newIndex > index + 1)
            {
                    var res = str.Substring(index, newIndex - index).Trim();
                    if (res.Length > 0)
                        return res;
                }

                index = newIndex + 1;
            }

            return ""; 
        }

        public static string Etc(this string str, int max, string etcString)
        {
            if (str.HasText() && (str.Length > max))
                return str.Start(max - (etcString.HasText() ? etcString.Length : 0)) + etcString;
            return str;
        }

        public static string Etc(this string str, int max) //Expressions and optionals don't work
        {
            return str.Etc(max, "(…)");
        }

        public static string VerticalEtc(this string str, int maxLines, string etcString = "(…)")
        {
            if (str.HasText() && (str.Contains("\r\n")))
        {
                string[] arr = str.Split(new string[] { "\r\n" }, maxLines - 1, StringSplitOptions.None);
                string res = arr.ToString("\r\n");
                if (res.Length < str.Length)
                    res += etcString;
                return res;
            }
            return str;
        }

        public static bool ContinuesWith(this string str, string subString, int pos)
        {
            return str.IndexOf(subString, pos) == pos;
        }

        public static string RemoveChars(this string str, params char[] chars)
        {
            if (!str.HasText())
                return str;

            StringBuilder sb = new StringBuilder(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                if (!chars.Contains(str[i]))
                    sb.Append(str[i]);
            }
            return sb.ToString();
        }

        public static string FormatWith(this string format, object arg0)
        {
            return string.Format(format, arg0);
        }

        public static string FormatWith(this string format, object arg0, object arg1)
        {
            return string.Format(format, arg0, arg1);
        }

        public static string FormatWith(this string format, object arg0, object arg1, object arg2)
        {
            return string.Format(format, arg0, arg1, arg2);
        }

        public static string FormatWith(this string pattern, params object[] parameters)
        {
            return string.Format(pattern, parameters);
        }

        public static string FormatWith(this string format, IFormatProvider provider, params object[] args)
        {
            return string.Format(provider, format, args);
        }

        public static string Replace(this string str, Dictionary<string, string> replacements)
        {
            return replacements.Aggregate(str, (s, kvp) => s.Replace(kvp.Key, kvp.Value));
        }

        public static string Replace(this string str, Dictionary<char, char> replacements)
        {
            StringBuilder sb = null;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (replacements.TryGetValue(c, out char rep))
                {
                    if (sb == null)
                        sb = new StringBuilder(str, 0, i, str.Length);

                    sb.Append(rep);
                }
                else if (sb != null)
                    sb.Append(c);
            }

            return sb == null ? str : sb.ToString();
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
        
        public static string FirstUpper(this string str)
        {
            if (str.HasText() && char.IsLower(str[0]))
                return char.ToUpperInvariant(str[0]) + str.Substring(1);
            return str;
        }

        public static string FirstLower(this string str)
        {
            if (str.HasText() && char.IsUpper(str[0]))
                return char.ToLowerInvariant(str[0]) + str.Substring(1);
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

        public static bool Wildcards(this string fileName, IEnumerable<string> wildcards)
        {
            return wildcards.Any(wc => fileName.Wildcards(wc));
        }

        static Dictionary<string, string> wildcardsPatterns = new Dictionary<string, string>();
        public static bool Wildcards(this string fileName, string wildcard)
        {
            var pattern = wildcardsPatterns.GetOrCreate(wildcard, wildcard.Replace(".", "[.]").Replace("*", ".*").Replace("?", "."));
            return Regex.IsMatch(fileName, pattern);
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

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
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

            return "{0:#,###.00} {1}".FormatWith(valor, (useAbbreviations ? abbreviations : magnitudes)[i]);
        }

        public static string Combine(this string separator, params object[] elements)
        {
            StringBuilder sb = null;
            foreach (var item in elements)
            {
                if (item != null)
                {
                    if (sb == null)
                        sb = new StringBuilder();
                    else
                        sb.Append(separator);

                    sb.Append(item.ToString());
                }
            }

            return sb == null ? "" : sb.ToString();  // Remove at the end is faster
        }

        public static string CombineIfNotEmpty(this string separator, params object[] elements)
        {
            StringBuilder sb = null;
            foreach (var item in elements)
            {
                string str;
                if (item != null && (str = item.ToString()).HasText())
                {
                    if (sb == null)
                        sb = new StringBuilder();
                    else
                        sb.Append(separator);

                    sb.Append(str);
                }
            }

            return sb == null ? "" : sb.ToString();  // Remove at the end is faster
        }

        public static StringBuilder AppendLines(this StringBuilder sb, IEnumerable<string> strings)
        {
            foreach (var item in strings)
            {
                sb.AppendLine(item);
            }

            return sb;
        }

        public static int CountRepetitions(this string text, string part)
        {
            int result = 0;
            int index = 0;
            while (true)
            {
                index = text.IndexOf(part, index);
                if (index == -1)
                    return result;

                index += part.Length;
                result++;
            }
        }

        public static string[] SplitNoEmpty(this string text, string separator)
        {
            return text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string[] SplitNoEmpty(this string text, params string[] separators)
        {
            return text.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string[] SplitNoEmpty(this string text, char separator)
        {
            return text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string[] SplitNoEmpty(this string text, params char[] separators)
        {
            return text.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
