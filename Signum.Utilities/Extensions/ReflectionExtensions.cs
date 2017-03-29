using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using System.Reflection.Emit;
using System.Collections;
using Signum.Utilities.ExpressionTrees;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

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
            return type.IsClass || type.IsInterface || type.IsNullable() ? type : typeof(Nullable<>).MakeGenericType(type);
        }

        public static bool IsNullable(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        public static bool IsAnonymous(this Type type)
        {
            // HACK: The only way to detect anonymous types right now.
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
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

        public static bool HasAttributeInherit<T>(this ICustomAttributeProvider mi) where T : Attribute
        {
            return mi.IsDefined(typeof(T), true);
        }

        public static bool IsInstantiationOf(this Type type, Type genericTypeDefinition)
        {
            if (!genericTypeDefinition.IsGenericTypeDefinition)
                throw new ArgumentException("Parameter 'genericTypeDefinition' should be a generic type definition");

            return type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition;
        }

        public static bool IsInstantiationOf(this MethodInfo method, MethodInfo genericMethodDefinition)
        {
            if (!genericMethodDefinition.IsGenericMethodDefinition)
                throw new ArgumentException("Parameter 'genericMethodDefinition' should be a generic method definition");

            return method.IsGenericMethod && ReflectionTools.MethodEqual(method.GetGenericMethodDefinition(), genericMethodDefinition);
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
                .FirstOrDefault(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.Let(ti => ti.GetGenericArguments()[0]);
        }

        public static bool IsExtensionMethod(this MethodInfo m)
        {
            return m.IsStatic && m.HasAttribute<ExtensionAttribute>();
        }

        public static PropertyInfo GetBaseDefinition(this PropertyInfo propertyInfo)
        {
            var method = propertyInfo.GetAccessors(true)[0];
            if (method == null)
                return null;

            var baseMethod = method.GetBaseDefinition();

            if (baseMethod == method)
                return propertyInfo;

            var arguments = propertyInfo.GetIndexParameters().Select(p => p.ParameterType).ToArray();

            return baseMethod.DeclaringType.GetProperty(propertyInfo.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, propertyInfo.PropertyType, arguments, null);
        }

        public static bool IsStaticClass(this Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }



        static ConcurrentDictionary<Assembly, DateTime> CompilationDatesCache = new ConcurrentDictionary<Assembly, DateTime>();

        public static DateTime CompilationDate(this Assembly assembly)
        {
            return CompilationDatesCache.GetOrAdd(assembly, a =>
            {
                string filePath = a.Location;
                const int c_PeHeaderOffset = 60;
                const int c_LinkerTimestampOffset = 8;
                byte[] b = new byte[2048];
                System.IO.Stream s = null;

                try
                {
                    s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    s.Read(b, 0, 2048);
                }
                finally
                {
                    if (s != null)
                    {
                        s.Close();
                    }
                }

                int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
                int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                dt = dt.AddSeconds(secondsSince1970);
                dt = dt.ToLocalTime();
                return dt;
            });
        }

        public static void PreserveStackTrace(this Exception ex)
        {

            if (Delegate.CreateDelegate(typeof(Action), ex, "InternalPreserveStackTrace", false, false) is Action savestack)
                savestack();
        }
    }
}
