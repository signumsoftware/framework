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
    public class EnumEntity<T> : IdentifiableEntity, IEquatable<EnumEntity<T>>
        where T: struct
    {
        public EnumEntity()
        {
            CleanSelfModified();
        }

        public static EnumEntity<T> FromEnum(T t)
        {
            return new EnumEntity<T>()
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
            return ToEnum().ToString(); 
        }

        public bool Equals(EnumEntity<T> other)
        {
            return EqualityComparer<T>.Default.Equals(ToEnum(), other.ToEnum()); 
        }

        public static implicit operator EnumEntity<T>(T enumerable)
        {
            return FromEnum(enumerable);
        }

        public static explicit operator T(EnumEntity<T> enumEntity)
        {
            return enumEntity.ToEnum();
        }
    }

    public static class EnumEntity
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

        public static bool IsEnumEntity(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EnumEntity<>);
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
            return typeof(EnumEntity<>).MakeGenericType(enumType);
        }

        public static Type Extract(Type enumEntityType)
        {
            if (enumEntityType.IsGenericType && enumEntityType.GetGenericTypeDefinition() == typeof(EnumEntity<>))
                return enumEntityType.GetGenericArguments()[0];
            return null;
        }

    }
}
