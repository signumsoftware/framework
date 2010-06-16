using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Utilities.DataStructures;
using System.ComponentModel;
using Signum.Entities.Properties;
using System.Collections.ObjectModel;
using System.Globalization;
using Signum.Utilities.ExpressionTrees;
using System.Text.RegularExpressions;

namespace Signum.Entities.Reflection
{
    /* Fields
     *   Value
     *   Modifiables
     *      MList
     *      EmbeddedEntities
     *      IdentifiableEntities
     *      
     * An identifiable can be accesed thought:
     *   Normal Reference
     *   Interface
     *   Lite
     */


    public static class Reflector
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static Reflector()
        {
            DescriptionManager.CleanTypeName = CleanTypeName; //To allow MyEntityDN
            DescriptionManager.CleanType = t => ExtractLite(t) ?? t; //To allow Lite<T>
        }

        public static string CleanTypeName(Type type)
        {
            if (type.Name.EndsWith("DN"))
                return type.Name.Substring(0, type.Name.Length - 2);

            return type.Name;
        }

        public static bool IsMList(this Type ft)
        {
            return ft.ElementType() != null && IsModifiable(ft);
        }

        public static bool IsModifiable(this Type t)
        {
            return typeof(Modifiable).IsAssignableFrom(t);
        }

        public static bool IsIIdentifiable(this Type type)
        {
            return typeof(IIdentifiable).IsAssignableFrom(type);
        }

        public static bool IsModifiableOrInterface(this Type t)
        {
            return IsModifiable(t) || IsIIdentifiable(t);
        }

        public static bool IsIdentifiableEntity(this Type ft)
        {
            return typeof(IdentifiableEntity).IsAssignableFrom(ft);
        }

        public static bool IsEmbeddedEntity(this Type t)
        {
            return typeof(EmbeddedEntity).IsAssignableFrom(t);
        }    

        public static FieldInfo[] InstanceFieldsInOrder(Type type)
        {
            var result = type.For(t => t != typeof(object), t => t.BaseType)
                .Reverse()
                .SelectMany(t => t.GetFields(flags | BindingFlags.DeclaredOnly).OrderBy(f=>f.MetadataToken)).ToArray();

            return result;
        }

        public static PropertyInfo[] PublicInstancePropertiesInOrder(Type type)
        {
            var result = type.FollowC(t => t.BaseType)
                .Reverse()
                .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                .Where(p => !p.HasAttribute<HiddenPropertyAttribute>())
                                .OrderBy(f => f.MetadataToken))
                 .Distinct(a => a.Name) //Overriden properties
                 .ToArray();

            return result;
        }

        internal static IEnumerable<Type> Identifiables(Assembly assembly)
        {
            return assembly.GetTypes().Where(t => IsIdentifiableEntity(t));
        }

        internal static Type GenerateEnumProxy(Type enumType)
        {
            return typeof(EnumProxy<>).MakeGenericType(enumType); 
        }

        internal static Type ExtractEnumProxy(Type enumProxyType)
        {
            if (enumProxyType.IsGenericType && enumProxyType.GetGenericTypeDefinition() == typeof(EnumProxy<>))
                return enumProxyType.GetGenericArguments()[0];
            return null;
        }

        public static Type GenerateLite(Type identificableType)
        {
            return typeof(Lite<>).MakeGenericType(identificableType);
        }

        public static Type ExtractLite(Type liteType)
        {
            if (liteType.IsGenericType && liteType.GetGenericTypeDefinition() == typeof(Lite<>))
                return liteType.GetGenericArguments()[0];
            return null;
        }

        public static bool IsLite(this Type t)
        {
            return typeof(Lite).IsAssignableFrom(t);
        }

        public static Type CleanType(this Type t)
        {
            return ExtractLite(t) ?? t;
        }    

        public static MemberInfo[] GetMemberList<T>(Expression<Func<T, object>> lambdaToField)
        {
            Expression e = lambdaToField.Body;

            UnaryExpression ue = e as UnaryExpression;
            if (ue != null && ue.NodeType == ExpressionType.Convert && ue.Type == typeof(object))
                e = ue.Operand;

            MemberInfo[] result = e.FollowC(NextExpression).Select(a => GetMember(a)).NotNull().Reverse().ToArray();

            return result;          
        }

        static Expression NextExpression(Expression e)
        {
            switch (e.NodeType)
            {
                case ExpressionType.MemberAccess: return ((MemberExpression)e).Expression;
                case ExpressionType.Call:
                    {
                        MethodCallExpression mce = (MethodCallExpression)e;

                        if (mce.Method.DeclaringType == typeof(MListExtensions) && mce.Method.Name == "Element")
                            return mce.Arguments.Single();

                        return ((MethodCallExpression)e).Arguments.Single(Resources.OnlyOneArgumentAllowed);

                    }
                case ExpressionType.Convert: return ((UnaryExpression)e).Operand;
                case ExpressionType.Parameter: return null;
                default: throw new InvalidCastException(Resources._0NotSupported.Formato(e.NodeType));
            }
        }

        static readonly string[] collectionMethods = new[] { "Element" };

        static MemberInfo GetMember(Expression e)
        {
            switch (e.NodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        MemberExpression me = (MemberExpression)e;  
                        if(typeof(Lite).IsAssignableFrom(me.Member.DeclaringType) && me.Member.Name.StartsWith("Entity"))
                            return null;
                        
                        return me.Member;
                    }
                case ExpressionType.Call:
                    {
                        MethodCallExpression mce = (MethodCallExpression)e;

                        if (mce.Method.DeclaringType == typeof(MListExtensions) && mce.Method.Name == "Element")
                            return mce.Arguments.Single().Type.GetProperty("Item");

                        return mce.Method;
                    }
                case ExpressionType.Convert: return ((UnaryExpression)e).Type;
                case ExpressionType.Parameter: return null;
                default: throw new InvalidCastException(Resources._0NotSupported.Formato(e.NodeType)); 
            }
        }

        internal static FieldInfo FindFieldInfo(Type type, MemberInfo value, bool throws)
        {
            return value as FieldInfo ?? Reflector.FindFieldInfo(type, (PropertyInfo)value, throws);
        }

        public static FieldInfo FindFieldInfo(Type type, PropertyInfo pi, bool throws)
        {
            FieldInfo fi=null;
            for (Type tempType = type; tempType != null && fi == null; tempType = tempType.BaseType)
            {
                fi = (tempType.GetField(pi.Name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic) ??
                tempType.GetField("m" + pi.Name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic) ??
                tempType.GetField("_" + pi, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic));
            }

            if (throws && fi == null)
                throw new NullReferenceException(Resources.FieldForPropertyNotFound.Formato(pi.Name));

            return fi;
        }

        public static PropertyInfo FindPropertyInfo(MemberInfo mi)
        {
            return mi as PropertyInfo ?? FindPropertyInfo((FieldInfo)mi); 
        }

        public static PropertyInfo FindPropertyInfo(FieldInfo fi)
        {
            return (fi.DeclaringType.GetProperty(CleanFieldName(fi.Name), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public));
        }

        public static string CleanFieldName(string name)
        {
            if (name.Length > 2)
            {
                if (name.StartsWith("_"))
                    name = name.Substring(1);
                else if (name.StartsWith("m") && char.IsUpper(name[1]))
                    name = Char.ToLower(name[1]) + name.Substring(2);
            }

            return name.FirstUpper();
        }

        public static bool QueryableProperty(Type type, PropertyInfo pi)
        {
            QueryablePropertyAttribute spa = pi.SingleAttribute<QueryablePropertyAttribute>();
            if (spa != null)
                return spa.AvailableForQueries;

            FieldInfo fi = FindFieldInfo(type, pi, false);
            if (fi != null && !fi.HasAttribute<IgnoreAttribute>())
                return true;

            if (ExpressionExpander.GetExpansion(type, pi) != null)
                return true;

            return false;
        }

        public static bool IsLowPopulation(Type type)
        {
            if (!typeof(IIdentifiable).IsAssignableFrom(type))
                throw new ArgumentException(Resources._0DoesNotInheritFromIdentifiableEntity.Formato(type));

            LowPopulationAttribute lpa = type.SingleAttribute<LowPopulationAttribute>();
            if (lpa != null)
                return lpa.Low;

            return !typeof(Entity).IsAssignableFrom(type);
        }

    

        public static Func<IFormattable, string> GetPropertyFormatter(string format, string unitName)
        {
            if (format != null)
            {
                if (unitName != null)
                    return a => a == null ? null : a.ToString(format, CultureInfo.CurrentCulture) + " " + unitName;
                else
                    return a => a == null ? null : a.ToString(format, CultureInfo.CurrentCulture);
            }
            else
            {
                if (unitName != null)
                    return a => a == null ? null : a.ToString() + " " + unitName;
                else
                    return a => a == null ? null : a.ToString();
            }
        }

        public static string FormatString(PropertyRoute route)
        {
            if (route.PropertyRouteType != PropertyRouteType.Property)
                throw new InvalidOperationException("PropertyRoute of type Property expected");

            FormatAttribute format = route.PropertyInfo.SingleAttribute<FormatAttribute>();
            if(format != null)
                return format.Format;

            var pp = Validator.GetOrCreatePropertyPack(route);
            if (pp != null)
            {
                DateTimePrecissionValidatorAttribute datetimePrecission = pp.Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefault();
                if (datetimePrecission != null)
                    return datetimePrecission.FormatString;

                DecimalsValidatorAttribute decimals = pp.Validators.OfType<DecimalsValidatorAttribute>().SingleOrDefault();
                if (decimals != null)
                    return "N" + decimals.DecimalPlaces;

                StringCaseValidatorAttribute stringCase = pp.Validators.OfType<StringCaseValidatorAttribute>().SingleOrDefault();
                if (stringCase != null)
                    return stringCase.TextCase == Case.Lowercase ? "L" : "U";
            }

            return FormatString(route.Type);
        }

        public static string FormatString(Type type)
        {
            type = type.UnNullify();
            if (type.IsEnum)
                return null;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.DateTime:
                    return "g";
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return "D"; 
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return "N2"; 
            }
            return null;
        }

        public static bool IsNumber(Type type)
        {
            type = type.UnNullify();
            if (type.IsEnum)
                return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
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

        static readonly Regex validIdentifier = new Regex(@"^[_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}][_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}\p{Nd}]*$");
        public static bool ValidIdentifier(string identifier)
        {
            return validIdentifier.IsMatch(identifier);
        }

        public static void AssertValidIdentifier(string step)
        {
            if (!ValidIdentifier(step))
                throw new FormatException(Resources._0IsNotAValidIdentifier.Formato(step));
        }
    }
}
