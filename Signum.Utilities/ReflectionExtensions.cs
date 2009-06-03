using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using System.Reflection.Emit;

namespace Signum.Utilities
{
    public static class ReflectionExtensions
    {
        public static Type UnNullify(this Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        public static Type Nullify(this Type type)
        {
            return type.IsNullable() ? type : typeof(Nullable<>).MakeGenericType(type);
        }

        public static bool IsNullable(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        public static Type ReturningType(this MemberInfo m)
        {
            return (m is PropertyInfo) ? ((PropertyInfo)m).PropertyType :
                (m is FieldInfo) ? ((FieldInfo)m).FieldType :
                (m is MethodInfo) ? ((MethodInfo)m).ReturnType :
                (m is ConstructorInfo) ? ((ConstructorInfo)m).DeclaringType:
                ((EventInfo)m).EventHandlerType;
        }

        public static bool HasAttribute<T>(this MemberInfo mi) where T : Attribute
        {
            return mi.IsDefined(typeof(T), false);
        }

        public static T SingleAttribute<T>(this MemberInfo mi) where T : Attribute
        {
            return mi.GetCustomAttributes(typeof(T), false).Cast<T>().SingleOrDefault();
        }

        public static bool IsInstantiationOf(this Type type, Type genericType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == genericType;
        }

        public static bool FieldEquals<T>(this FieldInfo fi, Expression<Func<T, object>> lambdaToFiel)
        {
            return ReflectionTools.FieldEquals(ReflectionTools.GetFieldInfo(lambdaToFiel), fi);
        }

        public static bool PropertyEquals<T>(this PropertyInfo fi, Expression<Func<T, object>> lambdaToProperty)
        {
            return ReflectionTools.PropertyEquals(ReflectionTools.GetPropertyInfo(lambdaToProperty), fi);
        }
    }
}
