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


        public static T MostSimilar<T>(this IEnumerable<T> collection, Func<T, string> stringSelector, string pattern)
        {
            StringDistance sd = new StringDistance();
            return collection.WithMin(item => sd.LevenshteinDistance(stringSelector(item), pattern));
        }
    }
}
