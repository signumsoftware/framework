using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using System.Reflection;
using System.CodeDom;
using Microsoft.CSharp;
using System.IO;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using Signum.Utilities.DataStructures;

namespace Signum.Utilities.ExpressionTrees
{
    public static class CSharpRenderer
    {
        public static string GenerateCSharpCode(this Expression expression, string[] importedNamespaces)
        {
            return new CSharpTreeVisitor { ImportedNamespaces = importedNamespaces }.VisitReal(expression);
        }

        public static string GenerateCSharpCode(this Expression expression)
        {
            return new CSharpTreeVisitor().VisitReal(expression);
        }

        /// <summary>
        /// Dummy method to collapse un collection o object initializer in one single line
        /// </summary>
        public static T Collapse<T>(this T obj)
        {
            return obj;
        }

        /// <summary>
        /// Allows to write string litterals in an expression tree, be carefull with parenthesis
        /// </summary>
        public static T Literal<T>(string literal)
        {
            return default(T);
        }

        public static bool IsBasicType(Type t) 
        {
            return basicTypes.ContainsKey(Type.GetTypeCode(t));
        }

        static Dictionary<TypeCode, string> basicTypes = new Dictionary<TypeCode, string>
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
            return "{0} {1}".Formato(pi.ParameterType.TypeName(), pi.Name);
        }

        public static string PropertyName(this PropertyInfo pi)
        {
            return "{0} {1}".Formato(pi.PropertyType.TypeName(), pi.Name);
        }

        public static string FieldName(this FieldInfo pi)
        {
            return "{0} {1}".Formato(pi.FieldType.TypeName(), pi.Name);
        }

        public static string MethodName(this MethodInfo method)
        {
            if (method.IsGenericMethod)
                return "{0}<{1}>".Formato(method.Name.Split('`')[0], method.GetGenericArguments().ToString(t => TypeName(t), ","));

            return method.Name;
        }

        public static string ConstructorSignature(this ConstructorInfo constructor)
        {
            return "{0}({1})".Formato(
                constructor.DeclaringType.TypeName(),
                constructor.GetParameters().ToString(p => p.ParameterSignature(), ", "));
        }

        public static string MethodSignature(this MethodInfo method)
        {
            return "{0} {1}({2})".Formato(
                method.ReturnType.TypeName(),
                method.MethodName(),
                method.GetParameters().ToString(p => p.ParameterSignature(), ", "));
        }

        public static string MemberName(this MemberInfo mi)
        {
            return mi is PropertyInfo ? ((PropertyInfo)mi).PropertyName() :
             mi is FieldInfo ? ((FieldInfo)mi).FieldName() :
             mi is MethodInfo ? ((MethodInfo)mi).MethodName() :
             new InvalidOperationException("MethodInfo mi should be a PropertyInfo, FieldInfo or MethodInfo").Throw<string>();
        }

        public static string TypeName(this Type type)
        {
            if (type == typeof(object))
                return "object";

            if (type.IsEnum)
                return type.Name; 

            string result = basicTypes.TryGetC(Type.GetTypeCode(type));
            if (result != null)
                return result;

            if (type.IsArray)
                return "{0}[{1}]".Formato(type.GetElementType().TypeName(), new string(',', type.GetArrayRank() - 1));

            Type ut = Nullable.GetUnderlyingType(type);
            if (ut != null)
                return "{0}?".Formato(ut.TypeName());

            if (type.IsGenericType)
            {
                if (type.IsGenericTypeDefinition)
                    return "{0}<{1}>".Formato(type.Name.Split('`')[0], type.GetGenericArguments().ToString(_ => "", ","));

                return "{0}<{1}>".Formato(type.Name.Split('`')[0], type.GetGenericArguments().ToString(t => TypeName(t), ","));

            }

            return type.Name;
        }

        public static string CleanIdentifiers(this string str)
        {
            return str
                .Replace("<>f__AnonymousType", "α")
                .Replace("<>h__TransparentIdentifier", "τ");
        }

        static CSharpCodeProvider provider = new CSharpCodeProvider();
        static CodeGeneratorOptions options = new CodeGeneratorOptions();

        public static string Value(object value, Type type, string[] importedNamespaces)
        {
            var expr = GetRightExpressionForValue(value, type, importedNamespaces);
            if (expr == null)
                return value.ToString();

            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb))
                provider.GenerateCodeFromExpression(expr, sw, options);

            return sb.ToString();
        }

        public static CodeExpression GetRightExpressionForValue(object value, Type type, string[] importedNamespaces)
        {
            if (value is DBNull || value == null)
            {
                return new CodePrimitiveExpression(null);
            }

            if (type == typeof(decimal))
            {
                return new CodePrimitiveExpression(value);
            }
       
            if ((type == typeof(string)) && (value is string))
            {
                return new CodePrimitiveExpression((string)value);
            }
            if (type.IsPrimitive)
            {
                if (type != value.GetType())
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(type);
                    if (converter.CanConvertFrom(value.GetType()))
                    {
                        return new CodePrimitiveExpression(converter.ConvertFrom(value));
                    }
                }
                return new CodePrimitiveExpression(value);
            }
            if (type.IsArray)
            {
                Array array = (Array)value;
                CodeArrayCreateExpression expression = new CodeArrayCreateExpression();
                expression.CreateType = TypeReference(type.GetElementType(), importedNamespaces);
                if (array != null)
                {
                    foreach (object obj2 in array)
                    {
                        expression.Initializers.Add(GetRightExpressionForValue(obj2, type.GetElementType(), importedNamespaces));
                    }
                }
                return expression;
            }
            TypeConverter converter2 = null;
            converter2 = TypeDescriptor.GetConverter(type);
            if (type.IsEnum && (value is string))
            {
                value = converter2.ConvertFromString(value.ToString());
            }
            if (converter2 != null)
            {
                InstanceDescriptor descriptor = null;
                if (converter2.CanConvertTo(typeof(InstanceDescriptor)))
                {
                    descriptor = (InstanceDescriptor)converter2.ConvertTo(value, typeof(InstanceDescriptor));
                }
                if (descriptor != null)
                {
                    if (descriptor.MemberInfo is FieldInfo)
                    {
                        return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(TypeReference( descriptor.MemberInfo.DeclaringType, importedNamespaces)), descriptor.MemberInfo.Name);
                    }
                    if (descriptor.MemberInfo is PropertyInfo)
                    {
                        return new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(TypeReference(descriptor.MemberInfo.DeclaringType, importedNamespaces)), descriptor.MemberInfo.Name);
                    }
                    object[] objArray = new object[descriptor.Arguments.Count];
                    descriptor.Arguments.CopyTo(objArray, 0);
                    CodeExpression[] expressionArray = new CodeExpression[objArray.Length];
                    if (descriptor.MemberInfo is MethodInfo)
                    {
                        ParameterInfo[] parameters = ((MethodInfo)descriptor.MemberInfo).GetParameters();
                        for (int i = 0; i < objArray.Length; i++)
                        {
                            expressionArray[i] = GetRightExpressionForValue(objArray[i], parameters[i].ParameterType, importedNamespaces);
                        }
                        CodeMethodInvokeExpression expression4 = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(descriptor.MemberInfo.DeclaringType.FullName), descriptor.MemberInfo.Name, new CodeExpression[0]);
                        foreach (CodeExpression expression5 in expressionArray)
                        {
                            expression4.Parameters.Add(expression5);
                        }
                        return expression4;
                    }
                    if (descriptor.MemberInfo is ConstructorInfo)
                    {
                        ParameterInfo[] infoArray2 = ((ConstructorInfo)descriptor.MemberInfo).GetParameters();
                        for (int j = 0; j < objArray.Length; j++)
                        {
                            expressionArray[j] = GetRightExpressionForValue(objArray[j], infoArray2[j].ParameterType, importedNamespaces);
                        }
                        CodeObjectCreateExpression expression6 = new CodeObjectCreateExpression(descriptor.MemberInfo.DeclaringType.FullName, new CodeExpression[0]);
                        foreach (CodeExpression expression7 in expressionArray)
                        {
                            expression6.Parameters.Add(expression7);
                        }
                        return expression6;
                    }
                }
            }
            return null;
        }

        public static CodeTypeReference TypeReference(Type type, string[] importedNamespaces)
        {
            if (!type.IsGenericType && !type.IsArray && importedNamespaces != null && importedNamespaces.Contains(type.Namespace))
                return new CodeTypeReference(type.Name);
            else
                return new CodeTypeReference(type); ;
        }

    }
}
