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

    [AttributeUsage(AttributeTargets.Assembly)]
    public class LocalizeDescriptionsAttribute : Attribute
    {

    }

    public static class DescriptionManager
    {
        internal static class EnumDescriptionCache
        {
            static Dictionary<Type, Dictionary<Enum, FieldInfo>> enumCache = new Dictionary<Type, Dictionary<Enum, FieldInfo>>();

            public static FieldInfo Get(Enum value)
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                return Create(value.GetType())[value];
            }

            static Dictionary<Enum, FieldInfo> Create(Type type)
            {
                if (!type.IsEnum)
                    throw new ApplicationException(Resources.IsNotAnEnum.Formato(type));

                lock (enumCache)
                    return enumCache.GetOrCreate(type, () => type.GetFields().Skip(1).ToDictionary(
                        fi => (Enum)fi.GetValue(null),
                        fi => fi));
            }
        }

        public static string NiceToString(this Enum a)
        {
            return DescriptionManager.GetDescription(EnumDescriptionCache.Get(a)) ??
                a.ToString().NiceName();
        }

        public static string NiceName(this Type type)
        {
            return DescriptionManager.GetDescription(type) ??
                type.Name.Map(n => n.EndsWith("DN") ? n.RemoveRight(2) : n).SpacePascal();
        }

        public static string NiceName(this PropertyInfo pi)
        {
            return DescriptionManager.GetDescription(pi) ?? pi.Name.NiceName();
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

            return resource.GetString(compoundKey) ?? resource.GetString(compoundKey);
        }


        public static string GetDescription(MemberInfo memberInfo)
        {
            Assembly assembly = (memberInfo.DeclaringType ?? (Type)memberInfo).Assembly;
            if (assembly.HasAttribute<LocalizeDescriptionsAttribute>())
            {
                string key = memberInfo.DeclaringType.TryCC(d => d.Name).Add(memberInfo.Name, "_");
                string result = assembly.GetDefaultResourceManager().GetString(key, CultureInfo.CurrentCulture);
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

        public static string GetPluralDescription(Type type)
        {
            Assembly assembly = type.Assembly;
            if (assembly.HasAttribute<LocalizeDescriptionsAttribute>())
            {
                string key = type.Name + "_Plural";
                string result = assembly.GetDefaultResourceManager().GetString(key, CultureInfo.CurrentCulture);
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
                string result = assembly.GetDefaultResourceManager().GetString(key, CultureInfo.CurrentCulture);
                if (result != null)
                {
                    if (result.Equals("m", StringComparison.InvariantCultureIgnoreCase) ||
                        result.Equals("male", StringComparison.InvariantCultureIgnoreCase) ||
                        result.Equals("masculine", StringComparison.InvariantCultureIgnoreCase))
                        return Gender.Masculine;

                    if (result.Equals("f", StringComparison.InvariantCultureIgnoreCase) ||
                        result.Equals("female", StringComparison.InvariantCultureIgnoreCase) ||
                        result.Equals("femenine", StringComparison.InvariantCultureIgnoreCase))
                        return Gender.Femenine;

                    if (result.Equals("n", StringComparison.InvariantCultureIgnoreCase) ||
                        result.Equals("neuter", StringComparison.InvariantCultureIgnoreCase))
                        return Gender.Neuter;
                }
            }

            return NaturalLanguageTools.GetGender(type.NiceName());
        }     

        public static ResourceManager GetDefaultResourceManager(this Assembly assembly)
        {
            string[] resourceFiles = assembly.GetManifestResourceNames();
            string name = resourceFiles.Single(a => a.Contains("Resources.resources"));
            return new ResourceManager(name.Replace(".resources", ""), assembly);
        }
    }
}
