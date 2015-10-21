using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using System.Reflection;

namespace Signum.Entities
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), InTypeScript(false)]
    [TicksColumn(false)]
    public class EnumEntity<T> : Entity, IEquatable<EnumEntity<T>>
        where T : struct
    {
        public EnumEntity()
        {
        }

        [MethodExpander(typeof(FromEnumMethodExpander))]
        public static EnumEntity<T> FromEnum(T t)
        {
            return new EnumEntity<T>()
            {
                id = new PrimaryKey(EnumExtensions.GetUnderlyingValue((Enum)(object)t)),
            };
        }

        [MethodExpander(typeof(FromEnumMethodExpander))]
        public static EnumEntity<T> FromEnumNotNew(T t)
        {
            return new EnumEntity<T>()
            {
                id = new PrimaryKey(EnumExtensions.GetUnderlyingValue((Enum)(object)t)),
                IsNew = false,
                Modified = ModifiedState.Clean
            };
        }

        public T ToEnum()
        {
            return (T)Enum.ToObject(typeof(T), Id.Object);
        }

        public override string ToString()
        {
            var en = ToEnum();

            return Enum.IsDefined(typeof(T), en) ? en.ToString() : (this.toStr ?? en.ToString());  //for aux sync
        }

        public bool Equals(EnumEntity<T> other)
        {
            return EqualityComparer<T>.Default.Equals(ToEnum(), other.ToEnum());
        }

        public static implicit operator EnumEntity<T>(T enumerable)
        {
            return FromEnumNotNew(enumerable);
        }

        public static explicit operator T(EnumEntity<T> enumEntity)
        {
            return enumEntity.ToEnum();
        }
    }

    public static class EnumEntity
    {
        public static Entity FromEnumUntyped(Enum value)
        {
            if (value == null) return null;

            Entity ident = (Entity)Activator.CreateInstance(Generate(value.GetType()));
            ident.Id = new PrimaryKey(EnumExtensions.GetUnderlyingValue(value));

            return ident;
        }

        public static Enum ToEnum(Entity ident)
        {
            if (ident == null) return null;

            Type enumType = Extract(ident.GetType());

            return (Enum)Enum.ToObject(enumType, ident.id);
        }

        public static bool IsEnumEntity(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EnumEntity<>);
        }

        public static bool IsEnumEntityOrSymbol(this Type type)
        {
            return type.IsEnumEntity() || typeof(Symbol).IsAssignableFrom(type);
        }

        public static IEnumerable<Enum> GetValues(Type enumType)
        {
            return EnumFieldCache.Get(enumType).Where(a => !a.Value.HasAttribute<IgnoreAttribute>()).Select(a => a.Key);
        }

        public static IEnumerable<Entity> GetEntities(Type enumType)
        {
            return GetValues(enumType).Select(a => FromEnumUntyped(a));
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

    class FromEnumMethodExpander : IMethodExpander
    {
        internal static MethodInfo miQuery;
        static readonly MethodInfo miSingleOrDefault = ReflectionTools.GetMethodInfo(() => Enumerable.SingleOrDefault<int>(null, i => true)).GetGenericMethodDefinition();

        public Expression Expand(Expression instance, Expression[] arguments, System.Reflection.MethodInfo mi)
        {
            var type = mi.DeclaringType;
            var query = Expression.Call(null, miQuery.MakeGenericMethod(mi.DeclaringType));

            var underlyingType = Enum.GetUnderlyingType(mi.DeclaringType.GetGenericArguments().Single());

            var param = Expression.Parameter(mi.DeclaringType);
            var filter = Expression.Lambda(Expression.Equal(
                Expression.Convert(Expression.Property(param, "Id"), underlyingType),
                Expression.Convert(arguments.Single(), underlyingType)),
                param);

            var result = Expression.Call(miSingleOrDefault.MakeGenericMethod(type), query, filter);

            return result;
        }
    }
}
