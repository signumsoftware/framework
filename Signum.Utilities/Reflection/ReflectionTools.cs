using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Signum.Utilities.ExpressionTrees;
using System.IO;

namespace Signum.Utilities.Reflection;

public static class ReflectionTools
{
    public static bool? IsNullable(this FieldInfo fi)
    {
        var info = new NullabilityInfoContext().Create(fi);

        if (info.ReadState == NullabilityState.Unknown)
            return null;

        return info.ReadState == NullabilityState.Nullable;
    }

    public static bool? IsNullable(this PropertyInfo pi)
    {
        var info = new NullabilityInfoContext().Create(pi);

        if (info.ReadState == NullabilityState.Unknown)
            return null;
        
            return info.ReadState == NullabilityState.Nullable;
    }

    public static bool FieldEquals(FieldInfo? f1, FieldInfo? f2)
    {
        return MemeberEquals(f1, f2);
    }

    public static bool PropertyEquals(PropertyInfo? p1, PropertyInfo? p2)
    {
        return MemeberEquals(p1, p2);
    }

    public static bool MethodEqual(MethodInfo? m1, MethodInfo? m2)
    {
        return MemeberEquals(m1, m2);
    }

    public static bool MemeberEquals(MemberInfo? m1, MemberInfo? m2)
    {
        if (m1 == m2)
            return true;

        if (m1 == null || m2 == null)
            return false;

        if (m1.DeclaringType != m2.DeclaringType)
            return false;

        // Methods on arrays do not have metadata tokens but their ReflectedType
        // always equals their DeclaringType
        if (m1.DeclaringType != null && m1.DeclaringType.IsArray)
            return false;

        if (m1.MetadataToken != m2.MetadataToken || m1.Module != m2.Module)
            return false;

        if (m1 is MethodInfo lhsMethod)
        {
            if (lhsMethod.IsGenericMethod)
            {
                MethodInfo rhsMethod = (MethodInfo)m2;

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
        
        if (!(body is MemberExpression ex))
            throw new ArgumentException("The lambda 'property' should be an expression accessing a property");
        
        if (!(ex.Member is PropertyInfo pi))
            throw new ArgumentException("The lambda 'property' should be an expression accessing a property");

        return pi;
    }

    public static ConstructorInfo GetConstuctorInfo<R>(Expression<Func<R>> constuctor)
    {
        return BaseConstuctorInfo(constuctor);
    }

    public static ConstructorInfo BaseConstuctorInfo(LambdaExpression constuctor)
    {
        if (constuctor == null)
            throw new ArgumentNullException("constuctor");

        Expression body = constuctor.Body;
        if (body.NodeType == ExpressionType.Convert)
            body = ((UnaryExpression)body).Operand;

        if (!(body is NewExpression ex))
            throw new ArgumentException("The lambda 'constuctor' should be an expression constructing an object");

        return ex.Constructor!;
    }


    public static FieldInfo GetFieldInfo<R>(Expression<Func<R>> field)
    {
        return BaseFieldInfo(field);
    }

    public static FieldInfo GetFieldInfo<T, R>(Expression<Func<T, R>> field)
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
        
        if (!(body is MemberExpression ex))
            throw new ArgumentException("The lambda 'field' should be an expression accessing a field");

        if (!(ex.Member is FieldInfo fi))
            throw new ArgumentException("The lambda 'field' should be an expression accessing a field");

        return fi;
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

        if (!(body is MemberExpression me))
            throw new ArgumentException("The lambda 'member' should be an expression accessing a member");

        return me.Member;
    }


    public static MethodInfo GetMethodInfo(Expression<Action> method)
    {
        return BaseMethodInfo(method);
    }

    public static MethodInfo GetMethodInfo<T>(Expression<Action<T>> method)
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

        if (!(body is MethodCallExpression ex))
            throw new ArgumentException("The lambda 'method' should be an expression calling a method");

        return ex.Method;
    }

    public static ConstructorInfo GetGenericConstructorDefinition(this ConstructorInfo ci)
    {
        return ci.DeclaringType!.GetGenericTypeDefinition().GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SingleEx(a => a.MetadataToken == ci.MetadataToken);
    }

    public static ConstructorInfo MakeGenericConstructor(this ConstructorInfo ci, params Type[] types)
    {
        return ci.DeclaringType!.MakeGenericType(types).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SingleEx(a => a.MetadataToken == ci.MetadataToken);
    }


    public static Type GetReceiverType<T, R>(Expression<Func<T, R>> lambda)
    {
        Expression body = lambda.Body;
        if (body.NodeType == ExpressionType.Convert)
            body = ((UnaryExpression)body).Operand;

        return ((MemberExpression)body).Expression!.Type;
    }

    public static Func<T, R>? CreateGetter<T, R>(MemberInfo m)
    {
        using (HeavyProfiler.LogNoStackTrace("CreateGetter"))
        {
            if (m is PropertyInfo pi && !pi.CanRead)
                return null;

            ParameterExpression t = Expression.Parameter(typeof(T), "t");
            var member = Expression.MakeMemberAccess(t.ConvertIfNeeded(m.DeclaringType!), m);
            var exp = Expression.Lambda(typeof(Func<T, R>), member.ConvertIfNeeded(typeof(R)), t);
            return (Func<T, R>)exp.Compile();
        }
    }

    static Expression ConvertIfNeeded(this Expression expression, Type type)
    {
        if (type.IsAssignableFrom(expression.Type) && expression.Type.IsValueType == type.IsValueType)
            return expression;

        return Expression.Convert(expression, type);
    }

    public static Action<T, P>? CreateSetter<T, P>(MemberInfo m)
    {
        using (HeavyProfiler.LogNoStackTrace("CreateSetter"))
        {
            if (m is PropertyInfo pi && !pi.CanWrite)
                return null;

            ParameterExpression t = Expression.Parameter(typeof(T), "t");
            ParameterExpression p = Expression.Parameter(typeof(P), "p");
            
            var t2 = t.ConvertIfNeeded(m.DeclaringType!);
            var p2 = p.ConvertIfNeeded(m.ReturningType());

            var exp = Expression.Lambda(typeof(Action<T, P>), Expression.Assign(Expression.MakeMemberAccess(t2, m), p2), t, p);
            return (Action<T, P>)exp.Compile();
        }
    }

    public static bool IsNumber(Type type)
    {
        type = type.UnNullify();
        if (type.IsEnum)
            return false;

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:

            case TypeCode.Byte:

            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64: return true;
        }

        return false;
    }

    public static bool IsIntegerNumber(Type type)
    {
        type = type.UnNullify();
        if (type.IsEnum)
            return false;

        switch (Type.GetTypeCode(type))
        {

            case TypeCode.Byte:

            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64: return true;
        }

        return false;
    }

    public static bool IsDecimalNumber(Type type)
    {
        type = type.UnNullify();
        if (type.IsEnum)
            return false;

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
        }
        return false;
    }

    public static bool IsDate(Type type)
    {
        return type == typeof(DateOnly)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset);
    }


    public static bool IsPercentage(string formatString, CultureInfo culture)
    {
        return formatString.HasText() && formatString.StartsWith("p", StringComparison.InvariantCultureIgnoreCase);
    }

    public static object? ParsePercentage(string value, Type targetType, CultureInfo culture)
    {
        value = value.Trim(culture.NumberFormat.PercentSymbol.ToCharArray());

        if (string.IsNullOrEmpty(value))
            return null;

        switch (Type.GetTypeCode(targetType.UnNullify()))
        {
            case TypeCode.Single: return Single.Parse(value, culture) / 100.0f;
            case TypeCode.Double: return Double.Parse(value, culture) / 100.0;
            case TypeCode.Decimal: return Decimal.Parse(value, culture) / 100M;

            case TypeCode.Byte: return (Byte)(Byte.Parse(value, culture) / 100);

            case TypeCode.Int16: return (Int16)(Int16.Parse(value, culture) / 100);
            case TypeCode.Int32: return (Int32)(Int32.Parse(value, culture) / 100);
            case TypeCode.Int64: return (Int64)(Int64.Parse(value, culture) / 100);
            case TypeCode.UInt16: return (UInt16)(UInt16.Parse(value, culture) / 100);
            case TypeCode.UInt32: return (UInt32)(UInt32.Parse(value, culture) / 100);
            case TypeCode.UInt64: return (UInt64)(UInt64.Parse(value, culture) / 100);
            default:
                throw new InvalidOperationException("targetType is not a number");
        }
    }

    public static T Parse<T>(string? value)
    {
        if (typeof(T) == typeof(string))
            return (T)(object)value!;

        if (value == null || value == "")
            return (T)(object?)null!;

        Type utype = typeof(T).UnNullify();
        if (utype.IsEnum)
            return (T)Enum.Parse(utype, (string)value);

        if (utype == typeof(Guid))
            return (T)(object)Guid.Parse(value);

        if (utype == typeof(DateOnly))
            return (T)(object)DateOnly.Parse(value);

        return (T)Convert.ChangeType(value, utype)!;
    }

    public static object? Parse(string? value, Type type)
    {
        if (type == typeof(string))
            return (object?)value;

        if (value == null || value == "" || value == " ")
            return (object?)null;

        Type utype = type.UnNullify();
        if (utype.IsEnum)
            return Enum.Parse(utype, (string)value);

        if (utype == typeof(Guid))
            return Guid.Parse(value);

        if (utype == typeof(DateOnly))
            return DateOnly.Parse(value);

        if (CustomParsers.TryGetValue(utype, out var func))
            return func(value); //Delay reference

        return Convert.ChangeType(value, utype);
    }


    public static Dictionary<Type, Func<string, object>> CustomParsers = new Dictionary<Type, Func<string, object>>
    {
        //{ typeof(SqlHierarchyId), str => SqlHierarchyId.Parse(str) },
        //{ typeof(SqlGeography), str => SqlGeography.Parse(str) },
        //{ typeof(SqlGeometry), str => SqlGeometry.Parse(str) },
    };

    public static T Parse<T>(string value, CultureInfo culture)
    {
        if (typeof(T) == typeof(string))
            return (T)(object)value;

        if (value == null || value == "")
            return (T)(object?)null!;

        Type utype = typeof(T).UnNullify();
        if (utype.IsEnum)
            return (T)Enum.Parse(utype, (string)value);

        if (utype == typeof(Guid))
            return (T)(object)Guid.Parse(value);

        return (T)Convert.ChangeType(value, utype, culture)!;
    }

    public static object? Parse(string value, Type type, CultureInfo culture)
    {
        if (type == typeof(string))
            return value;

        if (value == null || value == "")
            return null;

        Type utype = type.UnNullify();
        if (utype.IsEnum)
            return Enum.Parse(utype, (string)value);

        if (utype == typeof(Guid))
            return Guid.Parse(value);

        return Convert.ChangeType(value, utype, culture);
    }

    public static bool TryParse<T>(string value, out T result)
    {
        if (TryParse(value, typeof(T), CultureInfo.CurrentCulture, out object? objResult))
        {
            result = (T)objResult!;
            return true;
        }
        else
        {
            result = default!;
            return false;
        }
    }

    public static bool TryParse<T>(string value,  CultureInfo ci, out T result)
    {
        if (TryParse(value, typeof(T), ci, out object? objResult))
        {
            result = (T)objResult!;
            return true;
        }
        else
        {
            result = default!;
            return false;
        }
    }

    public static bool TryParse(string? value, Type type, out object? result)
    {
        return TryParse(value, type, CultureInfo.CurrentCulture, out result);
    }

    public static bool TryParse(string? value, Type type, CultureInfo ci, out object? result)
    {
        if (type == typeof(string))
        {
            result = value;
            return true;
        }

        result = null;

        if (!value.HasText())
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
            if (EnumExtensions.TryParse(value, utype, true, out Enum _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(bool))
        {
            if (bool.TryParse(value, out bool _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(char))
        {
            if (char.TryParse(value, out char _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(SByte))
        {
            if (SByte.TryParse(value, NumberStyles.Integer, ci, out sbyte _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(byte))
        {
            if (byte.TryParse(value, NumberStyles.Integer, ci, out byte _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(Int16))
        {
            if (Int16.TryParse(value, NumberStyles.Integer, ci, out short _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(UInt16))
        {
            if (UInt16.TryParse(value, NumberStyles.Integer, ci, out ushort _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(Int32))
        {
            if (Int32.TryParse(value, NumberStyles.Integer, ci, out int _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(UInt32))
        {
            if (UInt32.TryParse(value, NumberStyles.Integer, ci, out uint _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(Int64))
        {
            if (Int64.TryParse(value, NumberStyles.Integer, ci, out long _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(UInt64))
        {
            if (UInt64.TryParse(value, NumberStyles.Integer, ci, out ulong _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(float))
        {
            if (float.TryParse(value, NumberStyles.Number, ci, out float _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(double))
        {
            if (double.TryParse(value, NumberStyles.Number, ci, out double _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(decimal))
        {
            if (decimal.TryParse(value, NumberStyles.Number, ci, out decimal _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(DateTime))
        {
            if (DateTime.TryParse(value, ci, DateTimeStyles.None, out DateTime _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(DateOnly))
        {
            if (DateOnly.TryParse(value, ci, DateTimeStyles.None, out DateOnly _result))
            {
                result = _result;
                return true;
            }
            else return false;
        }
        else if (utype == typeof(Guid))
        {
            if (Guid.TryParse(value, out Guid _result))
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
            if (converter.CanConvertFrom(typeof(string)))
            {
                try
                {
                    result = converter.ConvertFromString(null, ci, value);
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

    public static T ChangeType<T>(object? value)
    {
        if (value == null)
            return (T)(object?)null!;

        if (value.GetType() == typeof(T))
            return (T)value;
        else
        {
            Type utype = typeof(T).UnNullify();

            if (utype.IsEnum)
            {
                if (value is string)
                    return (T)Enum.Parse(utype, (string)value);


                return (T)Enum.ToObject(utype, value);
            }

            if (utype == typeof(Guid) && value is string s)
                return (T)(object)Guid.Parse(s);

            if (utype == typeof(DateOnly))
            {
                if (value is string s2)
                    return (T)(object)DateOnly.Parse(s2);

                if (value is DateTime dt)
                    return (T)(object)DateOnly.FromDateTime(dt);

                if (value is DateTimeOffset dto)
                    return (T)(object)DateOnly.FromDateTime(dto.DateTime);
            }

            if (utype == typeof(DateTime))
            {
                if (value is DateTimeOffset dto)
                    return (T)(object)dto.DateTime;
            }

            if (utype == typeof(DateTimeOffset))
            {
                if (value is DateTime dt)
                    return (T)(object)(DateTimeOffset)dt;
            }

            return (T)Convert.ChangeType(value, utype)!;
        }
    }

    public static object? ChangeType(object? value, Type type)
    {
        if (value == null)
            return null;

        if (type.IsAssignableFrom(value.GetType()))
            return value;
        else
        {
            Type utype = type.UnNullify();

            if (utype.IsEnum)
            {
                if (value is string)
                    return Enum.Parse(utype, (string)value);

                return Enum.ToObject(utype, value);
            }

            if (utype == typeof(Guid) && value is string)
                return Guid.Parse((string)value);

            if (utype == typeof(DateOnly))
            {
                if (value is string s2)
                    return DateOnly.Parse(s2);

                if (value is DateTime dt)
                    return DateOnly.FromDateTime(dt);

                if (value is DateTimeOffset dto)
                    return DateOnly.FromDateTime(dto.DateTime);
            }

            var conv = TypeDescriptor.GetConverter(type);
            if (conv != null && conv.CanConvertFrom(value.GetType()))
                return conv.ConvertFrom(value);

            conv = TypeDescriptor.GetConverter(value.GetType());
            if (conv != null && conv.CanConvertTo(type))
                return conv.ConvertTo(value, type);

            if (type != typeof(string) && value is IEnumerable && typeof(IEnumerable).IsAssignableFrom(type))
            {
                var colType = type.IsInstantiationOf(typeof(IEnumerable<>)) ? typeof(List<>).MakeGenericType(type.GetGenericArguments()) : type;
                IList col = (IList)Activator.CreateInstance(colType)!;
                foreach (var item in (IEnumerable)value)
                {
                    col.Add(item);
                }
                return col;
            }

            if (value is IConvertible c)
                return Convert.ChangeType(c, utype);

            throw new InvalidOperationException($"Unable to convert '{value}' (of type {value.GetType().TypeName()}) to type {utype.TypeName()}");

        }
    }

    public static bool CanChangeType(Type fromType, Type targetType)
    {
        if (fromType == typeof(object))
            return true;

        if (targetType.IsAssignableFrom(fromType))
            return true;
        else
        {
            Type utype = targetType.UnNullify();

            if (utype.IsEnum)
            {
                if (fromType == typeof(string))
                    return true;

                if (ReflectionTools.IsIntegerNumber(utype))
                    return true;

                return false;
            }

            if (utype == typeof(Guid) && fromType == typeof(string))
                return true;

            if (utype == typeof(DateOnly))
            {
                if (fromType == typeof(string))
                    return true;

                if (fromType == typeof(DateTime))
                    return true;

                if (fromType == typeof(DateTimeOffset))
                    return true;
            }

            var conv = TypeDescriptor.GetConverter(targetType);
            if (conv != null && conv.CanConvertFrom(fromType))
                return true;

            conv = TypeDescriptor.GetConverter(fromType);
            if (conv != null && conv.CanConvertTo(targetType))
                return true;

            if (targetType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(fromType) && typeof(IEnumerable).IsAssignableFrom(targetType))
            {
                return CanChangeType(fromType.ElementType()!, targetType.ElementType()!);
            }

            if (typeof(IConvertible).IsAssignableFrom(fromType))
                return true;

            throw new InvalidOperationException($"Unable to convert '{fromType}' to type '{targetType}'");

        }
    }
    

    public static bool IsStatic(this PropertyInfo pi)
    {
        return (pi.CanRead && pi.GetGetMethod()!.IsStatic) ||
              (pi.CanWrite && pi.GetSetMethod()!.IsStatic);
    }
    
    public static DateTime BuildTimeUTC(this Assembly assembly)
    {
        const int peHeaderOffset = 60;
        const int linkerTimestampOffset = 8;
        byte[] bytes = new byte[2048];
        using (FileStream file = new FileStream(assembly.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            file.Read(bytes, 0, bytes.Length);
        }
        int headerPos = BitConverter.ToInt32(bytes, peHeaderOffset);
        int secondsSince1970 = BitConverter.ToInt32(bytes, headerPos + linkerTimestampOffset);
        DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime dateTimeUTC = dt.AddSeconds(secondsSince1970);
        return dateTimeUTC;
    }

    public static bool IsBackingField(this FieldInfo f)
    {
        return f.Name.StartsWith("<") && f.Name.EndsWith(">k__BackingField");
    }
}
