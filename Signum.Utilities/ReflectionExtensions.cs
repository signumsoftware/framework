using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using System.Reflection.Emit;
using System.Collections;
using Signum.Utilities.Properties;
using Signum.Utilities.ExpressionTrees;

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
            return type.IsClass || type.IsNullable() ? type : typeof(Nullable<>).MakeGenericType(type);
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
                (m is ConstructorInfo) ? ((ConstructorInfo)m).DeclaringType :
                (m is EventInfo) ? ((EventInfo)m).EventHandlerType : null;
        }

        public static bool HasAttribute<T>(this ICustomAttributeProvider mi) where T : Attribute
        {
            return mi.IsDefined(typeof(T), false);
        }

        public static T SingleAttribute<T>(this ICustomAttributeProvider mi) where T : Attribute
        {
            return mi.GetCustomAttributes(typeof(T), false).Cast<T>().SingleOrDefault();
        }

        public static bool IsInstantiationOf(this Type type, Type genericTypeDefinition)
        {
            if (!genericTypeDefinition.IsGenericTypeDefinition)
                throw new ArgumentException(Resources.GenericTypeDefinitionShouldBeAGenericTypeDefinition);

            return type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition;
        }

        public static bool IsInstantiationOf(this MethodInfo method, MethodInfo genericMethodDefinitoin)
        {
            if (!genericMethodDefinitoin.IsGenericMethodDefinition)
                throw new ArgumentException(Resources.GenericMethodDefinitionShouldBeAGenericMethodDefinition);

            return method.IsGenericMethod && ReflectionTools.MethodEqual(method.GetGenericMethodDefinition(), genericMethodDefinitoin);
        }

        public static IEnumerable<Type> GetGenericInterfaces(this Type type, Type genericInterfaceDefinition)
        {
            return type.GetInterfaces().PreAnd(type).Where(i => i.IsInstantiationOf(genericInterfaceDefinition));
        }

        public static bool FieldEquals<S,T>(this FieldInfo fi, Expression<Func<S, T>> field)
        {
            return ReflectionTools.FieldEquals(ReflectionTools.BaseFieldInfo(field), fi);
        }

        public static bool PropertyEquals<S, T>(this PropertyInfo pi, Expression<Func<S, T>> property)
        {
            return ReflectionTools.PropertyEquals(ReflectionTools.BasePropertyInfo(property), pi);
        }

        public static bool IsReadOnly(this PropertyInfo pi)
        {
            MethodInfo mi = pi.GetSetMethod();

            return mi == null || !mi.IsPublic;
        }

        public static Type ElementType(this Type ft)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(ft))
                return null;

            return ft.GetInterfaces().PreAnd(ft)
                .SingleOrDefault(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .TryCC(ti => ti.GetGenericArguments()[0]);
        }
    }
}
