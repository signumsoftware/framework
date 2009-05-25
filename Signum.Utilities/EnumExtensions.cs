using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using Signum.Utilities.Properties;
using System.ComponentModel;

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

        public static string NiceToString<T>(T a) where T : struct
        {
            return EnumDescriptionCache.Get(a) ?? a.ToString().SpacePascal(true);
        }

        public static string NiceToString(object a)
        {
            return EnumDescriptionCache.Get(a) ?? a.ToString().SpacePascal(true);
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
        static Dictionary<Type, Dictionary<Enum, string>> dictionary = new Dictionary<Type, Dictionary<Enum, string>>();

        public static string Get<T>(T value) where T : struct
        {
            return Create(typeof(T))[(Enum)(object)value];
        }

        public static string Get(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return Create(value.GetType())[(Enum)value];
        }

        static Dictionary<Enum, string> Create(Type type)
        {
            if (!type.IsEnum)
                throw new ApplicationException(Resources.IsNotAnEnum.Formato(type));

            lock (dictionary)
                return dictionary.GetOrCreate(type, () => type.GetFields().Skip(1).ToDictionary(
                    fi => (Enum)fi.GetValue(null),
                    fi => fi.SingleAttribute<DescriptionAttribute>().TryCC(a => a.Description)));
        }
    }
}
