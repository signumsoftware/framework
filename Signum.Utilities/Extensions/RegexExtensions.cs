using System.Text.RegularExpressions;

namespace Signum.Utilities;

public static class RegexExtensions
{
    public static int EndIndex(this Match m)
    {
        return m.Index + m.Length;
    }

    public static bool Contains(this Capture larger, Capture smaller)
    {
        return
            larger.Index <= smaller.Index &&
           (smaller.Index + smaller.Length) <= (larger.Index + larger.Length);
    }

    public static IEnumerable<Capture> Captures(this Match match)
    {
        return match.Captures.Cast<Capture>();
    }

    public static IEnumerable<Capture> Captures(this Group match)
    {
        return match.Captures.Cast<Capture>();
    }

    public static IEnumerable<Group> Groups(this Match match)
    {
        return match.Groups.Cast<Group>();
    }

    public static T? MostSimilar<T>(this IEnumerable<T> collection, Func<T, string> stringSelector, string pattern)
    {
        StringDistance sd = new StringDistance();

        var bestMatch = pattern.Length;
        var bestMatchItem = default(T);

        foreach (var item in collection.OrderBy(item => Math.Abs(stringSelector(item).Length - pattern.Length))) 
        {
            var str = stringSelector(item);
            if (pattern == str)
                return item;

            if (Math.Abs(str.Length - pattern.Length) > bestMatch)
                return bestMatchItem;

            var match = sd.LevenshteinDistance(pattern, str);
            if(match < bestMatch)
            {
                bestMatch = match;
                bestMatchItem = item;
            }
        }
        return default;
    }

    public static IEnumerable<R> JoinSimilar<T, S, R>(this List<T> outer, List<S> inner,
        Func<T, string> outerKeySelector, 
        Func<S, string> innerKeySelector, 
        Func<T, S, int, R> resultSelector)
        where T : notnull
        where S : notnull
    {
        StringDistance sd = new StringDistance();
        Dictionary<Tuple<T, S>, int> distances = (from o in outer
                                                  from i in inner
                                                  select KeyValuePair.Create(Tuple.Create(o, i),
                                                    sd.LevenshteinDistance(outerKeySelector(o), innerKeySelector(i)))).ToDictionary();
        while (distances.Count > 0)
        {
            var kvp = distances.MinBy(a => a.Value);
            var tuple = kvp.Key;

            distances.RemoveRange(distances.Keys.Where(a => a.Item1.Equals(tuple.Item1) || a.Item2.Equals(tuple.Item2)).ToList());
            outer.Remove(tuple.Item1);
            inner.Remove(tuple.Item2);

            yield return resultSelector(tuple.Item1, tuple.Item2, kvp.Value);
        }
    }

    public static IEnumerable<(Match? match, string after)> SplitAfter(this Regex regex, string input)
    {
        var matches = regex.Matches(input).Cast<Match>().ToList();

        if (matches.IsEmpty())
        {
            yield return (null, input);
        }
        else
        {
            yield return (null, input.Substring(0, matches.FirstEx().Index));

            for (int i = 0; i < matches.Count; i++)
            {
                var from = matches[i].EndIndex();
                var to = i < matches.Count - 1 ? matches[i + 1].Index : input.Length;

                var str = input.Substring(from, to - from);
                yield return (matches[i], input.Substring(from, to - from));
            }
        }
    }
}
