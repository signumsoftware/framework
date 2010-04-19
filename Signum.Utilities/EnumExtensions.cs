using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using Signum.Utilities.Properties;
using System.ComponentModel;
using System.Reflection;

namespace Signum.Utilities
{
    public static class EnumExtensions
    {
        public static bool HasFlag(this Enum value, Enum flag)
        {
            int val = Convert.ToInt32(flag);
            return (Convert.ToInt32(value) & val) == val;
        }

        public static T ToEnum<T>(this string str) where T : struct
        {
            return (T)Enum.Parse(typeof(T), str);
        }

        public static T ToEnum<T>(this string str, bool ignoreCase) where T : struct
        {
            return (T)Enum.Parse(typeof(T), str, ignoreCase);
        }

        public static T[] GetValues<T>()
        {
            return (T[])Enum.GetValues(typeof(T));
        }

        public static List<string> GetStringValues<T>()
        {
            return GetValues<T>().Select(x => x.ToString()).ToList();
        }

        public static bool IsDefined<T>(T value) where T : struct
        {
            return Enum.IsDefined(typeof(T), value);
        }

        public static int MinFlag(int value)
        {
            int result = 1;
            while ((result & value) == 0 && result != 0)
                result <<= 1;
            return result;
        }

        public static int MaxFlag(int value)
        {
            int result = (int.MaxValue >> 1) + 1; // because C2
            while ((result & value) == 0 && result != 0) 
                result >>= 1;
            return result;
        }

        public static bool TryParse(string value, Type enumType, bool ignoreCase, out Enum result)
        {
            if (!Enum.IsDefined(enumType, value))
            {
                result = null;
                return false;
            }
            result = (Enum)Enum.Parse(enumType, value);
            return true;
        }

        public static bool TryParse<T>(string value, bool ignoreCase, out T result)
            where T:struct
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                result = default(T);
                return false;
            }
            result = (T)(object)Enum.Parse(typeof(T), value);
            return true;
        }
    }
}
