
using Signum.Utilities.DataStructures;
using System.Reflection.Metadata.Ecma335;

namespace Signum.Utilities;

public static class RandomExtensions
{
    public static bool NextBool(this Random r)
    {
        return r.Next(2) == 1;
    }

    const string Lowercase = "abcdefgijkmnopqrstwxyz";
    const string Upercase = "ABCDEFGHJKLMNPQRSTWXYZ"; 


    public static char NextUppercase(this Random r)
    {
        return Upercase[r.Next(Upercase.Length)];
    }

    public static char NextLowercase(this Random r)
    {
        return Lowercase[r.Next(Lowercase.Length)];
    }

    public static char NextChar(this Random r)
    {
        string s = r.NextBool() ? Upercase : Lowercase;
        return s[r.Next(s.Length)];
    }

    public static string NextUppercaseString(this Random r, int length)
    {
        return new string(0.To(length).Select(i=>r.NextUppercase()).ToArray());
    }

    public static string NextLowercaseString(this Random r, int length)
    {
        return new string(0.To(length).Select(i => r.NextLowercase()).ToArray());
    }

    public static string NextString(this Random r, int length)
    {
        return new string(0.To(length).Select(i => r.NextChar()).ToArray());
    }

    public static string NextString(this Random r, int length, string chars)
    {
        return new string(0.To(length).Select(i => chars[r.Next(chars.Length)]).ToArray());
    }

    public static string NextSubstring(this Random r, string text, int minLength, int maxLength)
    {
        int length = r.Next(minLength, maxLength); 

        if(length > text.Length)
            return text;

        return text.Substring(r.Next(text.Length - length), length);
    }

    public static int NextAlphaColor(this Random r)
    {
        return Color(r.Next(256), r.Next(256), r.Next(256), r.Next(256));
    }

    public static int NextColor(this Random r)
    {
        return Color(255, r.Next(256), r.Next(256), r.Next(256));
    }

    public static int NextColor(this Random r, int minR, int maxR, int minG, int maxG, int minB, int maxB)
    {
        return Color(255, minR + r.Next(maxR - minR), minG + r.Next(maxG - minG), minB + r.Next(maxB - minB)); 
    }

    static int Color(int a, int r, int g, int b)
    {
        return a << 24 | r << 16 | g << 8 | b;
    }

    public static DateTime NextDateTime(this Random r, DateTime min, DateTime max)
    {
        if (min.Kind != max.Kind)
            throw new ArgumentException("min and max have differend Kind"); 

        return new DateTime(min.Ticks + r.NextLong(max.Ticks - min.Ticks), min.Kind);
    }

    public static long NextLong(this Random r, long max)
    {
        return (long)(r.NextDouble() * max);
    }

    public static long NextLong(this Random r, long min, long max)
    {
        return (long)(min + r.NextDouble() * (max - min));
    }

    public static T NextParams<T>(this Random r, params T[] elements)
    {
        return elements[r.Next(elements.Length)];
    }

    public static T NextElement<T>(this Random r, IList<T> elements)
    {
        return elements[r.Next(elements.Count)];
    }

    public static decimal NextDecimal(this Random r, decimal min, decimal max)
    {
        return r.NextLong((long)(min * 100L), (long)(max * 100L)) / 100m;
    }
}

public class ProbabilityDictionary<K> where K : notnull
{
    IntervalDictionary<double, K> accumulatedProbabilities;
    public double TotalMax;

    public ProbabilityDictionary(Dictionary<K, double> probabilities)
    {
        accumulatedProbabilities = probabilities
            .SelectAggregate(new { Value = default(K)!, Acum = 0.0 }, (acum, kvp) => new { Value = kvp.Key, Acum = kvp.Value + acum.Acum })
            .BiSelectC((p, next) => KeyValuePair.Create(new Interval<double>(p!.Acum, next!.Acum), next!.Value!))
            .ToIntervalDictionary();
        TotalMax = accumulatedProbabilities.TotalMax!.Value;
    }

    public K NextElement(Random r)
    {
        return this.accumulatedProbabilities[r.NextDouble() * TotalMax];
    }
}
