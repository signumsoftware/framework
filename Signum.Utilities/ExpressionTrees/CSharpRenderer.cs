
namespace Signum.Utilities.ExpressionTrees;

public static class CSharpRenderer
{
    public static bool IsBasicType(Type t)
    {
        return BasicTypeNames.ContainsKey(Type.GetTypeCode(t));
    }

    public static Dictionary<TypeCode, string> BasicTypeNames = new Dictionary<TypeCode, string>
    {
        { TypeCode.Boolean, "bool"},
        { TypeCode.Byte, "byte"},
        { TypeCode.Char, "char"},
        { TypeCode.Decimal, "decimal"},
        { TypeCode.Double, "double"},
        { TypeCode.Int16, "short"},
        { TypeCode.Int32, "int"},
        { TypeCode.Int64, "long"},
        { TypeCode.SByte, "sbyte"},
        { TypeCode.Single, "float"},
        { TypeCode.String, "string"},
        { TypeCode.UInt16, "ushort"},
        { TypeCode.UInt32, "uint"},
        { TypeCode.UInt64, "ulong"},
    };


    public static string ParameterSignature(this ParameterInfo pi)
    {
        return "{0} {1}".FormatWith(pi.ParameterType.TypeName(), pi.Name);
    }

    public static string PropertyName(this PropertyInfo pi)
    {
        return "{0} {1}".FormatWith(pi.PropertyType.TypeName(), pi.Name);
    }

    public static string FieldName(this FieldInfo pi)
    {
        return "{0} {1}".FormatWith(pi.FieldType.TypeName(), pi.Name);
    }

    public static string MethodName(this MethodInfo method)
    {
        if (method.IsGenericMethod)
            return "{0}<{1}>".FormatWith(method.Name.Split('`')[0], method.GetGenericArguments().ToString(t => TypeName(t), ","));

        return method.Name;
    }

    public static string ConstructorSignature(this ConstructorInfo constructor)
    {
        return "{0}({1})".FormatWith(
            constructor.DeclaringType!.TypeName(),
            constructor.GetParameters().ToString(p => p.ParameterSignature(), ", "));
    }

    public static string MethodSignature(this MethodInfo method)
    {
        return "{0} {1}({2})".FormatWith(
            method.ReturnType.TypeName(),
            method.MethodName(),
            method.GetParameters().ToString(p => p.ParameterSignature(), ", "));
    }

    public static string MemberName(this MemberInfo mi)
    {
        return mi is PropertyInfo pi ? pi.PropertyName() :
         mi is FieldInfo fi ? fi.FieldName() :
         mi is MethodInfo mti ? mti.MethodName() :
         throw new InvalidOperationException("MethodInfo mi should be a PropertyInfo, FieldInfo or MethodInfo");
    }

    public static string TypeName(this Type type)
    {
        List<Type> arguments = type.IsGenericType ? type.GetGenericArguments().ToList() : new List<Type>();

        StringBuilder sb = new StringBuilder();
        foreach (var item in type.Follow(a => a.IsNested ? a.DeclaringType : null).Reverse())
        {
            if (sb.Length > 0)
                sb.Append('.');

            sb.Append(TypeNameSimple(item, arguments));
        }

        return sb.ToString();
    }

    static string TypeNameSimple(Type type, List<Type> globalGenericArguments)
    {
        if (type == typeof(object))
            return "object";

        if (type.IsEnum)
            return type.Name;

        string? result = BasicTypeNames.TryGetC(Type.GetTypeCode(type));
        if (result != null)
            return result;

        if (type.IsArray)
            return "{0}[{1}]".FormatWith(type.GetElementType()!.TypeName(), new string(',', type.GetArrayRank() - 1));

        Type? ut = Nullable.GetUnderlyingType(type);
        if (ut != null)
            return "{0}?".FormatWith(ut.TypeName());

        if (type.IsGenericType && globalGenericArguments.Count > 0)
        {
            var args = globalGenericArguments.Take(type.GetGenericArguments().Length).ToList();

            globalGenericArguments.RemoveRange(0, args.Count);

            return "{0}<{1}>".FormatWith(type.Name.Before('`'), args.ToString(t => TypeName(t), ","));
        }

        return type.Name;
    }

    public static string CleanIdentifiers(this string str)
    {
        return str
            .Replace("<>f__AnonymousType", "α")
            .Replace("<>h__TransparentIdentifier", "τ");
    }

    public static string Value(object? obj)
    {
        if (obj == null)
            return "null";

        if (obj is bool b)
            return b ? "true" : "false";

        if (obj is string s)
            return ToSrtringLiteral(s);
        
        if (obj.GetType().IsEnum)
            return $"{obj.GetType().FullName}.{obj}";

        return obj.ToString()!;
    }

    static string ToSrtringLiteral(string input)
    {
        StringBuilder literal = new StringBuilder(input.Length + 2);
        literal.Append('"');
        foreach (var c in input)
        {
            switch (c)
            {
                case '\'': literal.Append(@"\'"); break;
                case '\"': literal.Append("\\\""); break;
                case '\\': literal.Append(@"\\"); break;
                case '\0': literal.Append(@"\0"); break;
                case '\a': literal.Append(@"\a"); break;
                case '\b': literal.Append(@"\b"); break;
                case '\f': literal.Append(@"\f"); break;
                case '\n': literal.Append(@"\n"); break;
                case '\r': literal.Append(@"\r"); break;
                case '\t': literal.Append(@"\t"); break;
                case '\v': literal.Append(@"\v"); break;
                default:
                    // ASCII printable character
                    if (c >= 0x20 && c <= 0x7e)
                    {
                        literal.Append(c);
                        // As UTF16 escaped character
                    }
                    else
                    {
                        literal.Append(@"\u");
                        literal.Append(((int)c).ToString("x4"));
                    }
                    break;
            }
        }
        literal.Append('"');
        return literal.ToString();
    }

    public static HashSet<string> Keywords = new HashSet<string>(@"abstract as base bool break byte case catch char checked class const continue decimal default
delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long
namespace new null object operator out out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static
string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while".Split(' ', '\r', '\n').NotNull());

    public static string EscapeIdentifier(string p)
    {
        if (Keywords.Contains(p))
            return "@" + p;

        return p;
    }
}
