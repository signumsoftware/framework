using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.ComponentModel;

namespace Signum.Entities.Omnibox
{
    public static class OmniboxUtils
    {
        public static bool IsPascalCasePattern(string ident)
        {
            if (string.IsNullOrEmpty(ident))
                return false;

            for (int i = 0; i < ident.Length; i++)
            {
                if (!char.IsUpper(ident[i]))
                    return false;
            }

            return true;
        }

        public static OmniboxMatch SubsequencePascal(object value, string identifier, string pattern)
        {
            bool[] indices = new bool[identifier.Length];
            int j = 0;
            for (int i = 0; i < pattern.Length; i++)
            {
                var pc = pattern[i];
                for (; j < identifier.Length; j++)
                {
                    var ic = identifier[j];

                    if (char.IsUpper(ic))
                    {
                        if (ic == pc)
                        {
                            indices[j] = true;

                            break;
                        }
                    }
                }

                if (j == identifier.Length)
                    return null;

                j++;
            }

            return new OmniboxMatch(value,
                remaining: identifier.Count(char.IsUpper) - pattern.Length,
                choosenString: identifier,
                boldIndices: indices);
        }

        public static IEnumerable<OmniboxMatch> Matches<T>(Dictionary<string, T> values, Func<T, bool> filter, string pattern, bool isPascalCase)
        {
            T val;
            if (values.TryGetValue(pattern, out val) && filter(val))
            {
                yield return new OmniboxMatch(val, 0, pattern, Enumerable.Repeat(true, pattern.Length).ToArray());
            }
            else
            {
                foreach (var kvp in values.Where(kvp => filter(kvp.Value)))
                {
                    OmniboxMatch result;
                    if (isPascalCase)
                    {
                        result = SubsequencePascal(kvp.Value, kvp.Key, pattern);

                        if (result != null)
                        {
                            yield return result;
                            continue;
                        }
                    }

                    result = Contains(kvp.Value, kvp.Key, pattern);
                    if (result != null)
                    {
                        yield return result;
                        continue;
                    }
                }
            }
        }

        public static OmniboxMatch Contains(object value, string identifier, string pattern)
        {
            var parts = pattern.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            bool[] indices = null;

            foreach (var p in parts)
	        {
                int index = identifier.IndexOf(p, StringComparison.InvariantCultureIgnoreCase);
                if (index == -1)
                    return null;

                if(indices == null)
                    indices = new bool[identifier.Length];

                for (int i = 0; i < p.Length; i++)
                    indices[index + i] = true;
	        }

            return new OmniboxMatch(value,
                remaining: identifier.Length - pattern.Length,
                choosenString: identifier,
                boldIndices: indices ?? new bool[identifier.Length]);
        }

        public static string CleanCommas(string str)
        {
            return str.Trim('\'', '"');;
        }
    }

    public class OmniboxMatch
    {
        public OmniboxMatch(object value, int remaining, string choosenString, bool[] boldIndices)
        {
            this.Value = value;

            this.Text = choosenString;
            this.BoldIndices = boldIndices;

            this.Distance = remaining;

            if (boldIndices.Length > 0 && boldIndices[0])
                this.Distance /= 2f;
        }

        public object Value; 

        public float Distance;
        public string Text;
        public bool[] BoldIndices;

        public IEnumerable<Tuple<string, bool>> BoldSpans()
        {
            bool lastIsBool = BoldIndices[0];
            int lastIndex = 0;
            for (int i = 1; i < Text.Length; i++)
            {
                if (BoldIndices[i] != lastIsBool)
                {
                    yield return Tuple.Create(Text.Substring(lastIndex, i - lastIndex), lastIsBool);

                    lastIsBool = BoldIndices[i];
                    lastIndex = i;
                }
            }

            yield return Tuple.Create(Text.Substring(lastIndex, Text.Length - lastIndex), lastIsBool);
        }
    }


    public enum OmniboxMessage
    {
        [Description("no")]
        No,
        [Description("[Not found]")]
        NotFound,
        [Description("Searching between 'apostrophe' will make queries to the database")]
        Omnibox_DatabaseAccess,
        [Description("With [Tab] you disambiguate you query")]
        Omnibox_Disambiguate,
        [Description("Field")]
        Omnibox_Field,
        [Description("Help")]
        Omnibox_Help,
        [Description("You can match results by (st)art, mid(dle) or (U)pper(C)ase")]
        Omnibox_MatchingOptions,
        [Description("Query")]
        Omnibox_Query,
        [Description("Type")]
        Omnibox_Type,
        [Description("UserChart")]
        Omnibox_UserChart,
        [Description("UserQuery")]
        Omnibox_UserQuery,
        [Description("Dashboard")]
        Omnibox_Dashboard,
        [Description("Value")]
        Omnibox_Value,
        Unknown,
        [Description("yes")]
        Yes,
        [Description(@"\b(the|of) ")]
        ComplementWordsRegex,
        [Description("Search...")]
        Search,
    }
}
