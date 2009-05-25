using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities
{
    public static class MyRandom
    {

        [ThreadStatic]
        static Random random;

        public static Random Current
        {
            get { return random ?? (random = new Random()); }
        }

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
            return new DateTime(min.Ticks + r.NextLong(max.Ticks - min.Ticks));
        }

        public static long NextLong(this Random r, long max)
        {
            return (long)(r.NextDouble() * max);
        }

        public static long NextLong(this Random r, long min, long max)
        {
            return (long)(min + r.NextDouble() * (max - min));
        }

        public static T NextElement<T>(this Random r, params T[] elements)
        {
            return elements[r.Next(elements.Length)];
        }
    }
}
