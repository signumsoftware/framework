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

        public static long MinFlag(long value)
        {
            int result = 1;
            while ((result & value) == 0 && result != 0)
                result <<= 1;
            return result;
        }

        public static long MaxFlag(long value)
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

        public static IEnumerable<Enum> UntypedGetValues(Type type)
        {
            if (!type.IsEnum)
                return Enumerable.Empty<Enum>();

            return Enum.GetValues(type).Cast<Enum>();
        }
    }



    public static class EnumFieldCache
    {
        static Dictionary<Type, Dictionary<Enum, FieldInfo>> enumCache = new Dictionary<Type, Dictionary<Enum, FieldInfo>>();

        public static FieldInfo Get(Enum value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return Get(value.GetType())[value];
        }

        public static Dictionary<Enum, FieldInfo> Get(Type type)
        {
            if (!type.IsEnum)
                throw new ArgumentException("{0} is not an Enum".Formato(type));

            lock (enumCache)
                return enumCache.GetOrCreate(type, () => type.GetFields().Skip(1).ToDictionary(
                    fi => (Enum)fi.GetValue(null),
                    fi => fi));
        }
    }
}
