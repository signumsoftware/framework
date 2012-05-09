using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Entities.Reflection; 

namespace Signum.Entities
{
    [Serializable]
    public class EnumProxy<T> : IdentifiableEntity, IEquatable<EnumProxy<T>>
        where T: struct
    {
        public EnumProxy()
        {
            CleanSelfModified();
        }

        public static EnumProxy<T> FromEnum(T t)
        {
            return new EnumProxy<T>()
            {
                id = Convert.ToInt32(t),
            };
        }

        public T ToEnum()
        {
            return (T)Enum.ToObject(typeof(T), Id);
        }

        public override string ToString()
        {
            return ((Enum)(object)ToEnum()).NiceToString(); 
        }

        public bool Equals(EnumProxy<T> other)
        {
            return EqualityComparer<T>.Default.Equals(ToEnum(), other.ToEnum()); 
        }

        public static implicit operator EnumProxy<T>(T enumerable)
        {
            return FromEnum(enumerable);
        }

        public static explicit operator T(EnumProxy<T> enumProxy)
        {
            return enumProxy.ToEnum();
        }
    }

    public static class EnumProxy
    {
        public static IdentifiableEntity FromEnum(Enum value)
        {
            if(value == null) return null; 

            IdentifiableEntity ident = (IdentifiableEntity)Activator.CreateInstance(Generate(value.GetType()));
            ident.Id = Convert.ToInt32(value);

            return ident;
        }

        public static Enum ToEnum(IdentifiableEntity ident)
        {
            if (ident == null) return null;

            Type enumType = Extract(ident.GetType());

            return (Enum)Enum.ToObject(enumType, ident.id);
        }

        public static bool IsEnumProxy(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EnumProxy<>);
        }

        public static IEnumerable<Enum> GetValues(Type enumType)
        {
            return EnumFieldCache.Get(enumType).Where(a => !a.Value.HasAttribute<IgnoreAttribute>()).Select(a => a.Key); 
        }

        public static IEnumerable<IdentifiableEntity> GetEntities(Type enumType)
        {
            return GetValues(enumType).Select(a => FromEnum(a));
        }

        public static Type Generate(Type enumType)
        {
            return typeof(EnumProxy<>).MakeGenericType(enumType);
        }

        public static Type Extract(Type enumProxyType)
        {
            if (enumProxyType.IsGenericType && enumProxyType.GetGenericTypeDefinition() == typeof(EnumProxy<>))
                return enumProxyType.GetGenericArguments()[0];
            return null;
        }

    }
}
