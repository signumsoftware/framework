using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static int? PascalMatches(string identifier, string pattern)
        {
            int distance = identifier.Length;
            int j = 0;
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];

                for (; j < identifier.Length; j++)
                {
                    char c2 = identifier[i];
                    if (char.IsUpper(c2))
                    {
                        if (identifier[j] == c)
                        {
                            distance--;
                            break;
                        }
                    }
                }

                if (j == identifier.Length)
                    break;
            }

            return distance == identifier.Length ? (int?)null : distance;
        }

        public static int? GetPriority(string name, string niceName, string pattern, bool isPascalCase)
        {
            int? result;

            if (isPascalCase)
            {
                result = OmniboxUtils.PascalMatches(name, pattern);

                if (result.HasValue)
                    return result;
            }

            result = Min(name.StartsWith(pattern) ? name.Length - pattern.Length : (int?)null,
                niceName.StartsWith(pattern) ? niceName.Length - pattern.Length : (int?)null) / 2;

            if (result.HasValue)
                return result.Value;

            result = Min(name.StartsWith(pattern) ? name.Length - pattern.Length : (int?)null,
                niceName.StartsWith(pattern) ? niceName.Length - pattern.Length : (int?)null);

            return result;
        }

        static int? Min(int? a, int? b)
        {
            if (a == null)
                return b;

            if (b == null)
                return a;

            return Math.Min(a.Value, b.Value);
        }

    }
}
