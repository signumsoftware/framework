﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Concurrent;

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
            int rubish;
            if (!Enum.IsDefined(enumType, value) && !int.TryParse(value, out rubish))
            {
                result = null;
                return false;
            }
            result = (Enum)Enum.Parse(enumType, value);
            return true;
        }

        public static IEnumerable<Enum> UntypedGetValues(Type type)
        {
            if (!type.IsEnum)
                return Enumerable.Empty<Enum>();

            return Enum.GetValues(type).Cast<Enum>();
        }

        public static T? GetByCode<T>(string code)
            where T: struct
        {
            return (T?)(object)EnumFieldCache.Get(typeof(T))
                .Where(kvp => kvp.Value.SingleAttribute<CodeAttribute>().Code == code)
                .Select(kvp => kvp.Key)
                .SingleOrDefaultEx();
        }
    }


    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class CodeAttribute : Attribute
    {
        public readonly string Code;

        public CodeAttribute(string code)
        {
            this.Code = code;
        }
    }

    public static class EnumFieldCache
    {
        static ConcurrentDictionary<Type, Dictionary<Enum, FieldInfo>> enumCache = new ConcurrentDictionary<Type, Dictionary<Enum, FieldInfo>>();

        public static FieldInfo Get(Enum value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return Get(value.GetType())[value];
        }

        static BindingFlags flags = BindingFlags.Static | BindingFlags.Public;

        public static Dictionary<Enum, FieldInfo> Get(Type type)
        {
            if (!type.IsEnum)
                throw new ArgumentException("{0} is not an Enum".Formato(type));

            return enumCache.GetOrAdd(type, t => t.GetFields(flags).ToDictionary(fi => (Enum)fi.GetValue(null), fi => fi));
        }
    }

}
