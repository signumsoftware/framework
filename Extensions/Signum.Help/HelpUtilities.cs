using System.Text.RegularExpressions;

namespace Signum.Help;

public static class HelpUtilities
{
    public static string Extract(this string s, Match m)
    {
        return Extract(s, m.Index, m.Index + m.Length);
    }

    public static string Extract(this string s, int start, int end)
    {
        if (s.Length <= etcLength) return s;

        int m = (start + end) / 2;
        int limMin = m - lp2;
        int limMax = m + lp2;
        if (limMin < 0)
        {
            limMin = 0;
            limMax = etcLength;
        }
        if (limMax > s.Length)
        {
            limMax = s.Length;
            limMin = limMax - etcLength;
        }

        return (limMin != 0 ? "..." : "")
        + s.Substring(limMin, limMax - limMin)
        + (limMax != end ? "..." : "");
    }

    const int etcLength = 300;
    const int lp2 = etcLength / 2;
}
