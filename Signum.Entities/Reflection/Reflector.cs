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

        public static bool IsMList(Type ft)
        {
            return ReflectionTools.CollectionType(ft) != null && IsModifiable(ft);
        }

        public static bool IsModifiable(Type t)
        {
            return typeof(Modifiable).IsAssignableFrom(t);
        }

        public static bool IsIIdentifiable(Type type)
        {
            return typeof(IIdentifiable).IsAssignableFrom(type);
        }

        public static bool IsModifiableOnly(Type t)
        {
            return IsModifiable(t) && !IsIdentifiableEntity(t);
        }

        public static bool IsModifiableOrInterface(Type t)
        {
            return IsModifiable(t) || IsIIdentifiable(t);
        }

        public static bool IsIdentifiableEntity(Type ft)
        {
            return typeof(IdentifiableEntity).IsAssignableFrom(ft);
        }

        public static bool IsEmbeddedEntity(Type t)
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

        public static PropertyInfo[] InstancePropertiesInOrder(Type type)
        {
            var result = type.For(t => t != typeof(object), t => t.BaseType)
                .Reverse()
                .SelectMany(t => t.GetProperties(flags | BindingFlags.DeclaredOnly).OrderBy(f => f.MetadataToken)).ToArray();

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

        internal static MemberInfo[] GetMemberList<T>(Expression<Func<T, object>> lambdaToField, bool throws)
        {
            Expression e = lambdaToField.Body;

            UnaryExpression ue = e as UnaryExpression;
            if (ue != null && ue.NodeType == ExpressionType.Convert && ue.Type == typeof(object))
                e = ue.Operand;

            MemberInfo[] result = e.FollowC(NextExpression).Select(a => GetMember(a, throws)).NotNull().Reverse().ToArray();

            return result;          
        }

        static Expression NextExpression(Expression e)
        {
            switch (e.NodeType)
            {
                case ExpressionType.MemberAccess: return ((MemberExpression)e).Expression;
                case ExpressionType.Call: return ((MethodCallExpression)e).Arguments.Single(Resources.OnlyOneArgumentAllowed);
                case ExpressionType.Convert: return ((UnaryExpression)e).Operand;
                case ExpressionType.Parameter: return null;
                default: throw new InvalidCastException(Resources._0NotSupported.Formato(e.NodeType));
            }
        }

        static MemberInfo GetMember(Expression e, bool throws)
        {
            switch (e.NodeType)
            {
                case ExpressionType.MemberAccess: return ((MemberExpression)e).Map(me => me.Member.MemberType == MemberTypes.Field ? me.Member :
                                                                                        me.Member.Name == "EntityOrNull" ? null :
                                                                                        FindFieldInfo(me.Expression.Type, (PropertyInfo)me.Member, throws));
                case ExpressionType.Call: return ((MethodCallExpression)e).Method;
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
            for (Type tempType = type; tempType != typeof(object) && fi == null; tempType = tempType.BaseType)
            {
                fi = (tempType.GetField(pi.Name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic) ??
                tempType.GetField("m" + pi.Name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic) ??
                tempType.GetField("_" + pi, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic));
            }

            if (throws && fi == null)
                throw new NullReferenceException(Resources.FieldForPropertyNotFound.Formato(pi.Name));

            return fi;
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


        public static string NiceName(this Type type)
        {
            return ReflectionTools.GetDescription(type) ??
                type.Name.Map(n => n.EndsWith("DN") ? n.RemoveRight(2) : n).SpacePascal(true);
        }

        public static bool IsLowPopulation(Type type)
        {
            if (!typeof(IdentifiableEntity).IsAssignableFrom(type))
                throw new ApplicationException(Resources._0DoesNotInheritFromIdentifiableEntity);

            LowPopulationAttribute lpa = type.SingleAttribute<LowPopulationAttribute>();
            if (lpa != null)
                return lpa.Low;

            return !typeof(Entity).IsAssignableFrom(type);
        }

        static Dictionary<Type, Dictionary<string, PropertyPack>> validators = new Dictionary<Type, Dictionary<string, PropertyPack>>();

        public static Dictionary<string, PropertyPack> GetPropertyValidators(Type type)
        {
            lock (validators)
            {
                return validators.GetOrCreate(type, () =>
                    MemberEntryFactory.GenerateIList(type, MemberOptions.Properties | MemberOptions.Getter | MemberOptions.Setters | MemberOptions.Untyped)
                    .Cast<IMemberEntry>()
                    .Where(p => !Attribute.IsDefined(p.MemberInfo, typeof(HiddenPropertyAttribute)))
                    .ToDictionary(p => p.Name, p => new PropertyPack((PropertyInfo)p.MemberInfo, p.UntypedGetter, p.UntypedSetter)));
            }
        }

        public static string NiceName(this PropertyInfo pi)
        {
            return ReflectionTools.GetDescription(pi) ?? pi.Name.NiceName();
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

        public static string FormatString(PropertyInfo property)
        {
            FormatAttribute format = property.SingleAttribute<FormatAttribute>();
            if(format != null)
                return format.Format; 

            DateOnlyValidatorAttribute dateOnly = property.SingleAttribute<DateOnlyValidatorAttribute>();
            if (dateOnly != null)
                return "d"; 

            DecimalsValidatorAttribute decimals = property.SingleAttribute<DecimalsValidatorAttribute>();
            if(decimals != null)
                return "N" + decimals.DecimalPlaces;

            return FormatString(property.PropertyType);
        }

        public static string FormatString(Type type)
        {
            type = type.UnNullify();
            if (type.IsEnum)
                return null;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.DateTime:
                    return CultureInfo.CurrentCulture.DateTimeFormat.ShortDateTimePattern();
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
    }

    public class PropertyPack
    {
        public PropertyPack(PropertyInfo pi, Func<object, object> getValue, Action<object, object> setValue)
        {
            this.PropertyInfo = pi;
            Validators = pi.GetCustomAttributes(typeof(ValidatorAttribute), true).OfType<ValidatorAttribute>().ToReadOnly();
            this.GetValue = getValue;
            this.SetValue = setValue;

        }

        public readonly Func<object, object> GetValue;
        public readonly Action<object, object> SetValue;
        public readonly PropertyInfo PropertyInfo;
        public readonly ReadOnlyCollection<ValidatorAttribute> Validators;
    }
}
