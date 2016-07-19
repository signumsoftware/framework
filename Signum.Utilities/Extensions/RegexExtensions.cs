using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Utilities.DataStructures;

namespace Signum.Utilities
{
    public static class RegexExtensions
    {
        public static int EndIndex(this Match m)
        {
            return m.Index + m.Length;
        }

        public static IEnumerable<Match> Match(this IEnumerable<string> collection, string regex)
        {
            Regex reg = new Regex(regex);
            return collection.Select(s => reg.Match(s)).Where(m => m.Success);
        }

        public static IEnumerable<Tuple<string, Match>> MatchPair(this IEnumerable<string> collection, string regex)
        {
            Regex reg = new Regex(regex);
            return from s in collection
                   let m = reg.Match(s)
                   where m.Success
                   select new Tuple<string,Match>(s, m);
        }

        public static IEnumerable<Match> Match<T>(this IEnumerable<T> collection, Func<T, string> stringSelector, string regex)
        {
            Regex reg = new Regex(regex);
            return collection.Select(s => reg.Match(stringSelector(s))).Where(m => m.Success);
        }

        public static IEnumerable<Tuple<T, Match>> MatchPair<T>(this IEnumerable<T> collection, Func<T, string> stringSelector, string regex)
        {
            Regex reg = new Regex(regex);
            return from s in collection
                   let m = reg.Match(stringSelector(s))
                   where m.Success
                   select new Tuple<T, Match>(s, m);
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

        public static T MostSimilar<T>(this IEnumerable<T> collection, Func<T, string> stringSelector, string pattern)
        {
            StringDistance sd = new StringDistance();
            return collection.WithMin(item => sd.LevenshteinDistance(stringSelector(item), pattern));
        }

        public static IEnumerable<R> JoinSimilar<T, S, R>(this List<T> outer, List<S> inner,
            Func<T, string> outerKeySelector, Func<S, string> innerKeySelector, Func<T, S, int, R> resultSelector)
        {
            StringDistance sd = new StringDistance();
            Dictionary<Tuple<T, S>, int> distances = (from o in outer
                                                      from i in inner
                                                      select KVP.Create(Tuple.Create(o, i), 
                                                        sd.LevenshteinDistance(outerKeySelector(o), innerKeySelector(i)))).ToDictionary();
            while (distances.Count > 0)
            {
                var kvp = distances.WithMin(a => a.Value); 
                var tuple = kvp.Key;

                distances.RemoveRange(distances.Keys.Where(a => a.Item1.Equals(tuple.Item1) || a.Item2.Equals(tuple.Item2)).ToList());
                outer.Remove(tuple.Item1);
                inner.Remove(tuple.Item2);

                yield return resultSelector(tuple.Item1, tuple.Item2, kvp.Value);
            }
        }
    }
}
