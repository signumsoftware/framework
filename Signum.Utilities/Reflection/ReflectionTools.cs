using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Collections;
using System.ComponentModel;
using System.Resources;
using System.Globalization;
using Signum.Utilities.Properties;

namespace Signum.Utilities.Reflection
{
    public static class ReflectionTools
    { 
        public static bool FieldEquals(FieldInfo f1, FieldInfo f2)
        {
            return MemeberEquals(f1, f2);
        }

        public static bool PropertyEquals(PropertyInfo p1, PropertyInfo p2)
        {
            return MemeberEquals(p1, p2);
        }

        public static bool MethodEqual(MethodInfo m1, MethodInfo m2)
        {
            return MemeberEquals(m1, m2);
        }

        public static bool MemeberEquals(MemberInfo m1, MemberInfo m2)
        {
            if (m1 == m2)
                return true;

            if (m1.DeclaringType != m2.DeclaringType)
                return false;

            // Methods on arrays do not have metadata tokens but their ReflectedType
            // always equals their DeclaringType
            if (m1.DeclaringType != null && m1.DeclaringType.IsArray)
                return false;

            if (m1.MetadataToken != m2.MetadataToken || m1.Module != m2.Module)
                return false;

            if (m1 is MethodInfo)
            {
                MethodInfo lhsMethod = m1 as MethodInfo;

                if (lhsMethod.IsGenericMethod)
                {
                    MethodInfo rhsMethod = m2 as MethodInfo;

                    Type[] lhsGenArgs = lhsMethod.GetGenericArguments();
                    Type[] rhsGenArgs = rhsMethod.GetGenericArguments();
                    for (int i = 0; i < rhsGenArgs.Length; i++)
                    {
                        if (lhsGenArgs[i] != rhsGenArgs[i])
                            return false;
                    }
                }
            }

            return true;
        }


        public static PropertyInfo GetPropertyInfo<R>(Expression<Func<R>> property)
        {
            return BasePropertyInfo(property);
        }

        public static PropertyInfo GetPropertyInfo<T, R>(Expression<Func<T, R>> property)
        {
            return BasePropertyInfo(property);
        }

        public static PropertyInfo BasePropertyInfo(LambdaExpression property)
        {
            if (property == null)
                throw new ArgumentNullException("property");

            Expression body = property.Body;
            if (body.NodeType == ExpressionType.Convert)
                body = ((UnaryExpression)body).Operand;

            MemberExpression ex = body as MemberExpression;
            if (ex == null)
                throw new ArgumentException("The lambda 'property' should be an expression accessing a property");

            PropertyInfo pi = ex.Member as PropertyInfo;
            if (pi == null)
                throw new ArgumentException("The lambda 'property' should be an expression accessing a property");

            return pi;
        }


        public static FieldInfo GetFieldInfo<R>(Expression<Func<R>> field)
        {
            return BaseFieldInfo(field);
        }

        public static FieldInfo GetFieldInfo<T,R>(Expression<Func<T, R>> field)
        {
            return BaseFieldInfo(field);
        }

        public static FieldInfo BaseFieldInfo(LambdaExpression field)
        {
            if (field == null)
                throw new ArgumentNullException("field");

            Expression body = field.Body;
            if (body.NodeType == ExpressionType.Convert)
                body = ((UnaryExpression)body).Operand;

            MemberExpression ex = body as MemberExpression;
            if (ex == null)
                throw new ArgumentException("The lambda 'field' should be an expression accessing a field");

            FieldInfo pi = ex.Member as FieldInfo;
            if (pi == null)
                throw new ArgumentException("The lambda 'field' should be an expression accessing a field");

            return pi;
        }


        public static MemberInfo GetMemberInfo<R>(Expression<Func<R>> member)
        {
            return BaseMemberInfo(member);
        }

        public static MemberInfo GetMemberInfo<T, R>(Expression<Func<T, R>> member)
        {
            return BaseMemberInfo(member);
        }

        public static MemberInfo BaseMemberInfo(LambdaExpression member)
        {
            if (member == null)
                throw new ArgumentNullException("member");

            Expression body = member.Body;
            if (body.NodeType == ExpressionType.Convert)
                body = ((UnaryExpression)body).Operand;

            MemberExpression ex = body as MemberExpression;
            if (ex == null)
                throw new ArgumentException("The lambda 'member' should be an expression accessing a member");

            return  ex.Member;
        }

    
        public static MethodInfo GetMethodInfo(Expression<Action> method)
        {
            return BaseMethodInfo(method);
        }

        public static MethodInfo GetMethodInfo<R>(Expression<Func<R>> method)
        {
            return BaseMethodInfo(method);
        }

        public static MethodInfo GetMethodInfo<T, R>(Expression<Func<T, R>> method)
        {
            return BaseMethodInfo(method);
        }

        public static MethodInfo BaseMethodInfo(LambdaExpression method)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            Expression body = method.Body;
            if (body.NodeType == ExpressionType.Convert)
                body = ((UnaryExpression)body).Operand;

            MethodCallExpression ex = body as MethodCallExpression;
            if (ex == null)
                throw new ArgumentException("The lambda 'method' should be an expression calling a method");

            return ex.Method;
        }

        public static object GenericInvoke(this MethodInfo mi, Type[] typeArguments, object obj, object[] parameters)
        {
            if (!mi.IsGenericMethodDefinition)
                throw new ArgumentException("Argument mi should be a generic method definition");

            try
            {
               MethodInfo methodInfo = mi.MakeGenericMethod(typeArguments);
                return methodInfo.Invoke(obj, parameters); 
            }
            catch (TargetInvocationException ex)
            {
                Action savestack = Delegate.CreateDelegate(typeof(Action), ex.InnerException, "InternalPreserveStackTrace", false, false) as Action;

                if (savestack != null)
                    savestack();

                throw ex.InnerException;
            }
        }


        public static Type GetReceiverType<T, R>(Expression<Func<T, R>> lambda)
        {
            Expression body = lambda.Body;
            if (body.NodeType == ExpressionType.Convert)
                body = ((UnaryExpression)body).Operand;

            return ((MemberExpression)body).Expression.Type;
        }

        public static Func<T, P> CreateGetter<T, P>(MemberInfo m)
        {
            if ((m as PropertyInfo).TryCS(a => !a.CanRead) ?? false)
                return null;

            ParameterExpression p = Expression.Parameter(typeof(T), "p");
            var exp = Expression.Lambda(typeof(Func<T,P>), Expression.MakeMemberAccess(p, m), p);
            return (Func<T, P>)exp.Compile();
        }

        public static Func<T, object> CreateGetter<T>(MemberInfo m)
        {
            if ((m as PropertyInfo).TryCS(a => !a.CanRead) ?? false)
                return null;

            ParameterExpression p = Expression.Parameter(typeof(T), "p");
            Type lambdaType = typeof(Func<,>).MakeGenericType(typeof(T), typeof(object));
            var exp = Expression.Lambda(lambdaType, Expression.Convert(Expression.MakeMemberAccess(p, m), typeof(object)), p);
            return (Func<T, object>)exp.Compile();
        }

        public static Func<object, object> CreateGetterUntyped(Type type, MemberInfo m)
        {
            if ((m as PropertyInfo).TryCS(a => !a.CanRead) ?? false)
                return null;

            ParameterExpression p = Expression.Parameter(typeof(object), "p");
            Type lambdaType = typeof(Func<,>).MakeGenericType(typeof(object), typeof(object));
            var exp = Expression.Lambda(lambdaType, Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(p, type), m), typeof(object)), p);
            return (Func<object, object>)exp.Compile();
        }

        public static Action<T, P> CreateSetter<T, P>(MemberInfo m)
        {
            if ((m as PropertyInfo).TryCS(a => !a.CanWrite) ?? false)
                return null;

            ParameterExpression t = Expression.Parameter(typeof(T), "t");
            ParameterExpression  p = Expression.Parameter(typeof(P), "p");
            var exp = Expression.Lambda(typeof(Action<T, P>), 
                Expression.Assign(Expression.MakeMemberAccess(t, m), p), t,p);
            return (Action<T, P>)exp.Compile();
        }

        //Replace when C# 4.0 is available
        public static Action<T, object> CreateSetter<T>(MemberInfo m)
        {
            if ((m as PropertyInfo).TryCS(a => !a.CanWrite) ?? false)
                return null;

            ParameterExpression t = Expression.Parameter(typeof(T), "t");
            ParameterExpression p = Expression.Parameter(typeof(object), "p");
            var exp = Expression.Lambda(typeof(Action<T, object>),
                Expression.Assign(Expression.MakeMemberAccess(t, m), Expression.Convert(p, m.ReturningType())), t, p);
            return (Action<T, object>)exp.Compile();
        }

        static Module module = ((Expression<Func<int>>)(() => 2)).Compile().Method.Module;

        //Replace when C# 4.0 is available
        public static Action<object, object> CreateSetterUntyped(Type type, MemberInfo m)
        {
            if ((m as PropertyInfo).TryCS(a => !a.CanWrite) ?? false)
                return null;

            ParameterExpression t = Expression.Parameter(typeof(object), "t");
            ParameterExpression p = Expression.Parameter(typeof(object), "p");
            var exp = Expression.Lambda(typeof(Action<object, object>),
                Expression.Assign(Expression.MakeMemberAccess(Expression.Convert(t, type), m), Expression.Convert(p, m.ReturningType())), t, p);
            return (Action<object, object>)exp.Compile();
        }

        public static Func<T> CreateConstructor<T>(Type type)
        {
            return Expression.Lambda<Func<T>>(Expression.Convert(Expression.New(type), typeof(T))).Compile();
        }

        public static Func<object> CreateConstructorUntyped(Type type)
        {
            return Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(type), typeof(object))).Compile();
        }

        public static T Parse<T>(string value)
        {
            if (typeof(T) == typeof(string))
                return (T)(object)value;

            if (value == null || value == "")
                return (T)(object)null;

            Type utype = typeof(T).UnNullify();
            if (utype.IsEnum)
                return (T)Enum.Parse(utype, (string)value);
            else
                return (T)Convert.ChangeType(value, utype);
        }

        public static object Parse(string value, Type type)
        {
            if (type == typeof(string))
                return (object)value;

            if (value == null || value == "")
                return (object)null;

            Type utype = type.UnNullify(); 
            if (utype.IsEnum)
                return Enum.Parse(utype, (string)value);
            else
                return Convert.ChangeType(value, utype);
        }

        public static bool TryParse<T>(string value, out T result)
        {
            object objResult;
            if (TryParse(value, typeof(T), out objResult))
            {
                result = (T)objResult;
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        public static bool TryParse(string value, Type type, out object result)
        {
            if (type == typeof(string))
            {
                result = value;
                return true;
            }

            result = null;

            if (string.IsNullOrEmpty(value))
            {
                if (Nullable.GetUnderlyingType(type) == null && type.IsValueType)
                {
                    return false;
                }
                return true;
            }

            Type utype = type.UnNullify();
            if (utype.IsEnum)
            {
                Enum _result;
                if (EnumExtensions.TryParse(value, utype, true, out _result))
                {
                    result = _result;
                    return true; 
                }
                else return false; 
            }
            else if (utype == typeof(bool))
            {
                bool _result;
                if (bool.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(char))
            {
                char _result;
                if (char.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(SByte))
            {
                SByte _result;
                if (SByte.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(byte))
            {
                byte _result;
                if (byte.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(Int16))
            {
                Int16 _result;
                if (Int16.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(UInt16))
            {
                UInt16 _result;
                if (UInt16.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(Int32))
            {
                Int32 _result;
                if (Int32.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(UInt32))
            {
                UInt32 _result;
                if (UInt32.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(Int64))
            {
                Int64 _result;
                if (Int64.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(UInt64))
            {
                UInt64 _result;
                if (UInt64.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(float))
            {
                float _result;
                if (float.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(double))
            {
                double _result;
                if (double.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(decimal))
            {
                decimal _result;
                if (decimal.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(DateTime))
            {
                DateTime _result;
                if (DateTime.TryParse(value, out _result))
                {
                    result = _result;
                    return true;
                }
                else return false; 
            }
            else if (utype == typeof(object))
            {
                result = value;
                return true;
            }
            else
            {
                TypeConverter converter = TypeDescriptor.GetConverter(utype);
                if(converter.CanConvertFrom(typeof(string)))
                {
                    try
                    {
                        result = converter.ConvertFromString(value);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                else return false;
            }
        }

        public static T ChangeType<T>(object value)
        {
            if (value == null)
                return (T)(object)null;

            if (value.GetType() == typeof(T))
                return (T)value;
            else
            {
                Type utype = typeof(T).UnNullify();

                if (utype.IsEnum)
                {
                    if (value is string)
                        return (T)Enum.Parse(utype, (string)value);
                    else
                        return (T)Enum.ToObject(utype, value);
                }

                else
                    return (T)Convert.ChangeType(value, utype);
            }
        }

        public static object ChangeType(object value, Type type)
        {
            if (value == null)
                return null;

            if (value.GetType() == type)
                return value;
            else
            {
                Type utype = type.UnNullify();

                if (utype.IsEnum)
                {
                    if (value is string)
                        return Enum.Parse(utype, (string)value);
                    else
                        return Enum.ToObject(utype, value);
                }
                else
                    return Convert.ChangeType(value, utype);
            }
        }
    }
}
