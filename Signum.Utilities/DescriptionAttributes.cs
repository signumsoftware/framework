using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.Properties;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;


namespace Signum.Utilities
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PluralDescriptionAttribute : Attribute
    {
        public string PluralDescription { get; private set; }

        public PluralDescriptionAttribute(string pluralDescription)
        {
            this.PluralDescription = pluralDescription;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class GenderAttribute : Attribute
    {
        public char Gender { get; private set; }

        public GenderAttribute(char gender)
        {
            this.Gender = gender;
        }
    }

    [AttributeUsage(AttributeTargets.All, Inherited = true)]
    public class ForceLocalization : Attribute
    {
    }  

    [AttributeUsage(AttributeTargets.All, Inherited = true)]
    public class AvoidLocalization : Attribute
    {
    }

    public static class DescriptionManager
    {
        public static string NiceToString(this Enum a)
        {
            var fi = EnumFieldCache.Get(a.GetType()).TryGetC(a);
            if (fi != null)
                return GetDescription(fi) ?? a.ToString().NiceName();

            return a.ToString().NiceName();
        }

        public static Func<Type, string> CleanTypeName = t => t.Name; //To allow MyEntityDN
        public static Func<Type, Type> CleanType = t => t; //To allow Lite<T>

        public static string NiceName(this Type type)
        {
            type = CleanType(type);

            return GetDescription(type) ??
                CleanTypeName(type).SpacePascal();
        }

        public static string NiceName(this PropertyInfo pi)
        {
            return GetDescription(pi) ??
                (pi.IsDefaultName() ? pi.PropertyType.NiceName() : pi.Name.NiceName());
        }

        public static bool IsDefaultName(this PropertyInfo pi)
        {
            return pi.Name == CleanTypeName(CleanType(pi.PropertyType)); 
        }

        public static string NicePluralName(this Type type)
        {
            return DescriptionManager.GetPluralDescription(type) ??
                   NaturalLanguageTools.Pluralize(type.NiceName());
        }

        public static string GetGenderAwareResource(this ResourceManager resource, string resourceKey, Gender gender)
        {
            string compoundKey = resourceKey + 
                (gender == Gender.Masculine ? "_m" :
                 gender == Gender.Femenine ? "_f" : "_n");

            return resource.GetString(compoundKey) ?? resource.GetString(resourceKey);
        }

        public static string GetGenderAwareResource(this Type type, Expression<Func<string>> resource)
        {
            MemberExpression me = (MemberExpression)resource.Body;
            PropertyInfo pi = me.Member.DeclaringType.GetProperty("ResourceManager", BindingFlags.Static| BindingFlags.NonPublic | BindingFlags.Public);
            ResourceManager rm = (ResourceManager)pi.GetValue(null, null);
            return rm.GetGenderAwareResource(me.Member.Name, type.GetGender());
        }

        static string GetDescription(MemberInfo memberInfo)
        {
            if (memberInfo.DeclaringType == typeof(DayOfWeek))
            {
                return CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)((FieldInfo)memberInfo).GetValue(null)];
            }

            Assembly assembly = (memberInfo as Type ?? memberInfo.DeclaringType).Assembly;

            if (assembly.HasAttribute<LocalizeDescriptionsAttribute>())
            {
                string key = memberInfo.DeclaringType.TryCC(d => d.Name).Add("_", memberInfo.Name);
                string result = assembly.GetDefaultResourceManager().GetString(key);
                if (result != null)
                    return result;
            }

            DescriptionAttribute desc = memberInfo.SingleAttribute<DescriptionAttribute>();
            if (desc != null)
            {
                return desc.Description;
            }

            return null;
        }

        static string GetPluralDescription(Type type)
        {
            Assembly assembly = type.Assembly;
            if (assembly.HasAttribute<LocalizeDescriptionsAttribute>())
            {
                string key = type.Name + "_Plural";
                string result = assembly.GetDefaultResourceManager().GetString(key);
                if (result != null)
                    return result;
            }

            PluralDescriptionAttribute desc = type.SingleAttribute<PluralDescriptionAttribute>();
            if (desc != null)
            {
                return desc.PluralDescription;
            }

            return null;
        }

        public static Gender GetGender(this Type type)
        {
            Assembly assembly = type.Assembly;
            if (assembly.HasAttribute<LocalizeDescriptionsAttribute>())
            {
                string key = type.Name + "_Gender";
                string gender = assembly.GetDefaultResourceManager().GetString(key);
                if (gender != null)
                    return ParseGender(gender);
            }

            var ga = type.SingleAttribute<GenderAttribute>();
            if (ga != null)
                return ga.Gender;

            return NaturalLanguageTools.GetGender(type.NiceName());
        }

        static Gender ParseGender(string str)
        {
            str = str.Trim().ToLower();
            if (str == "m") return Gender.Masculine;
            if (str == "f") return Gender.Femenine;
            if (str == "n") return Gender.Neuter;

            throw new FormatException("{0} is not a valid Gender. Use m, f or n");
        }

        public static ResourceManager GetDefaultResourceManager(this Assembly assembly)
        {
            string[] resourceFiles = assembly.GetManifestResourceNames();
            string name = resourceFiles.SingleEx(a => a.Contains("Resources.resources"));
            return new ResourceManager(name.Replace(".resources", ""), assembly);
        }
    }
}
