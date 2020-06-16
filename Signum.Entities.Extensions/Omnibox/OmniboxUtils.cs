using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities;
using System.ComponentModel;
using Newtonsoft.Json;

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

        public static OmniboxMatch? SubsequencePascal(object value, string identifier, string pattern)
        {
            char[] mask = new string('_', identifier.Length).ToCharArray();
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
                            mask[j] = '#';

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
                boldMask: new string(mask));
        }

        public static IEnumerable<OmniboxMatch> Matches<T>(Dictionary<string, T> values, Func<T, bool> filter, string pattern, bool isPascalCase)
        {
            pattern = pattern.RemoveDiacritics();

            if (values.TryGetValue(pattern, out T val) && filter(val))
            {
                yield return new OmniboxMatch(val!, 0, pattern, new string('#', pattern.Length));
            }
            else
            {
                foreach (var kvp in values.Where(kvp => filter(kvp.Value)))
                {
                    OmniboxMatch? result;
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

        public static OmniboxMatch? Contains(object value, string identifier, string pattern)
        {
            var parts = pattern.SplitNoEmpty(' ');

            char[] mask = new string('_', identifier.Length).ToCharArray();

            foreach (var p in parts)
            {
                int index = identifier.IndexOf(p, StringComparison.InvariantCultureIgnoreCase);
                if (index == -1)
                    return null;

                for (int i = 0; i < p.Length; i++)
                    mask[index + i] = '#';
            }

            return new OmniboxMatch(value,
                remaining: identifier.Length - pattern.Length,
                choosenString: identifier,
                boldMask: new string(mask));
        }

        public static string CleanCommas(string str)
        {
            return str.Trim('\'', '"');
        }
    }

    public class OmniboxMatch
    {
        public OmniboxMatch(object value, int remaining, string choosenString, string boldMask)
        {
            if (choosenString.Length != boldMask.Length)
                throw new ArgumentException("choosenString '{0}' is {1} long but boldIndices is {2}".FormatWith(choosenString, choosenString.Length, boldMask.Length));

            this.Value = value;

            this.Text = choosenString;
            this.BoldMask = boldMask;

            this.Distance = remaining;

            if (boldMask.Length > 0 && boldMask[0] == '#')
                this.Distance /= 2f;
        }

        [JsonIgnore]
        public object Value;

        public float Distance;
        public string Text;
        public string BoldMask;

        public IEnumerable<(string span, bool isBold)> BoldSpans()
        {
            return this.Text.ZipStrict(BoldMask)
                .GroupWhenChange(a => a.second == '#')
                .Select(gr => (span: new string(gr.Select(a => a.first).ToArray()), isBold: gr.Key));
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
        [Description("Omnibox Syntax Guide:")]
        Omnibox_OmniboxSyntaxGuide,
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
