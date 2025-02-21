using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Microsoft.SqlServer.Types;
using Signum.Engine.Maps; // Requires Microsoft.SqlServer.Types NuGet package

public static class HierarchyIdString
{
    public static string? ToSortableString(this SqlHierarchyId hierarchyId)
    {
        if (hierarchyId.IsNull)
            return null;


        if (hierarchyId.GetLevel() == 0)
            return "";

        var parts = hierarchyId.ToString().AsSpan()[1..^1];

        StringBuilder sb = new StringBuilder();
        bool isFirst = true;
        foreach (var range in parts.Split('/'))
        {
            if (isFirst)
                isFirst = false;
            else
                sb.Append('.');

            EncodeSegment(parts[range], sb);
        }
        return sb.ToString();
    }

    public static SqlHierarchyId FromSortableString(string? sortableString)
    {
        if (sortableString == null) 
            return SqlHierarchyId.Null;

        if (sortableString.Length == 0)
            return SqlHierarchyId.GetRoot();


        StringBuilder sb = new StringBuilder();
        sb.Append('/');
        var span = sortableString.AsSpan();
        foreach (var range in span.Split('.'))
        {
            DecodeSegment(span[range], sb);
            sb.Append('/');
        }

        return SqlHierarchyId.Parse(sb.ToString());
    }

    static void EncodeSegment(ReadOnlySpan<char> sqlSegment, StringBuilder sb)
    {
        foreach (var range in sqlSegment.Split('.'))
        {
            sb.Append(EncodeNumber(int.Parse(sqlSegment[range])));
        }
    }

    static void DecodeSegment(ReadOnlySpan<char> encodedSegment, StringBuilder sb)
    {
        int start = 0;
        bool isFirst = true;
        while( start < encodedSegment.Length)
        {
            var key = encodedSegment[start];
            if (isFirst)
                isFirst = false;
            else
                sb.Append('.');

            var end = start + patterns.GetOrThrow(key).NumChars + 1;

            long value = DecodeString(encodedSegment.Slice(start, end - start));
            sb.Append(value);            

            start = end;
        }
    }


    public class Pattern
    {
        public char Key;
        public int NumChars;
        public long Min;
        public long Max;

        public override string ToString() => $"{Key}{{{NumChars}}} ({Min} - {Max})";
    }

    static Dictionary<char, Pattern> patterns; 
    static List<Pattern> positivePatterns;
    static List<Pattern> negativePatterns;

    static HierarchyIdString()
    {
        checked
        {
            patterns = [];
            positivePatterns = [];
            negativePatterns = [];
            long offset = 0;
            for (var i = 0; i < 6; i++)
            {
                var numChars = (i + 1) * 2;
                var size = (long)Math.Pow(alphabet.Length, numChars);

                var p = new Pattern
                {
                    Key = (char)('M' + i),
                    NumChars = numChars,
                    Min = offset,
                    Max = offset + size - 1,
                };

                patterns.Add(p.Key, p);
                positivePatterns.Add(p);
                offset += size;
            }

            offset = -1;
            for (var i = 0; i < 6; i++)
            {
                var numChars = (i + 1) * 2;
                var size = (long)Math.Pow(alphabet.Length, numChars);

                var p = new Pattern
                {
                    Key = (char)('L' - i),
                    NumChars = numChars,
                    Min = offset - size + 1,
                    Max = offset,
                };

                patterns.Add(p.Key, p);
                negativePatterns.Add(p);
                offset -= size;
            }
        }
    }

    internal static readonly string alphabet = "abcdefghijklmnopqrstuvwxyz";

    //https://www.ascii-code.com/
    internal static string EncodeNumber(int number)
    {
        Pattern? pattern = null;
        if (number >= 0)
        {
            for (int i = 0; i < positivePatterns.Count && pattern == null; i++)
            {
                if (positivePatterns[i].Min  <= number && number <= positivePatterns[i].Max)
                    pattern = positivePatterns[i];
            }
        }
        else if (number < 0)
        {
            for (int i = 0; i < negativePatterns.Count && pattern == null; i++)
            {
                if (negativePatterns[i].Min <= number && number <= negativePatterns[i].Max)
                    pattern = negativePatterns[i];
            }
        }

        if (pattern == null)
            throw new ApplicationException($"There is not any matching pattern for {number}");
       
        var n = number - pattern.Min;

        StringBuilder sb = new StringBuilder(pattern.NumChars + 1);
        sb.Append(pattern.Key);
        for (int i = 0; i < pattern.NumChars; i++)
            sb.Append(' ');

        for (int i = 0; i < pattern.NumChars; i++)
        {
            var (quote, rem) = Math.DivRem(n, alphabet.Length);
            sb[pattern.NumChars - i - 1 + 1] = alphabet[(int)rem];
            n = quote;
        }

        return sb.ToString();
    }


    internal static long DecodeString(ReadOnlySpan<char> s)
    {       
        long result = 0;
        var key = s[0];

        Pattern pattern = patterns.GetOrThrow(key);

        long mFactor = 1;
        for (int i = s.Length - 1; i >= 1; i--)
        {
            var c = s[i];
            var digit = c - alphabet[0];
            result += mFactor * digit;
            mFactor *= alphabet.Length;
        }
        return result + pattern.Min;
    }
}
