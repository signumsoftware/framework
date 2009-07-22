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

        public static string NiceToString(this Enum a)
        {
            return ReflectionTools.GetDescription(EnumDescriptionCache.Get(a)) ?? a.ToString().NiceName();
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
    }

    internal static class EnumDescriptionCache
    {
        static Dictionary<Type, Dictionary<Enum, FieldInfo>> dictionary = new Dictionary<Type, Dictionary<Enum, FieldInfo>>();

        public static FieldInfo Get(Enum value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return Create(value.GetType())[value];
        }

        static Dictionary<Enum, FieldInfo> Create(Type type)
        {
            if (!type.IsEnum)
                throw new ApplicationException(Resources.IsNotAnEnum.Formato(type));

            lock (dictionary)
                return dictionary.GetOrCreate(type, () => type.GetFields().Skip(1).ToDictionary(
                    fi => (Enum)fi.GetValue(null),
                    fi => fi));
        }
    }
}
