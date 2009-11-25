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
    [AttributeUsage(AttributeTargets.All)]
    public class LocDescriptionAttribute : Attribute
    {
        public bool Auto { get { return resourceKey == null || resourceSource == null; } }

        Type resourceSource;
        string resourceKey;

        public LocDescriptionAttribute()
        {
        }

        public LocDescriptionAttribute(Type resourceSource, string resourceKey)
            : base()
        {
            this.resourceSource = resourceSource;
            this.resourceKey = resourceKey;
        }

        public string Description
        {
            get
            {
                if (Auto)
                    throw new ApplicationException("Use ReflectionTools.GetDescription instead");

                return new ResourceManager(resourceSource).GetString(resourceKey);
            }
        }
    }

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
    public class PluralLocDescriptionAttribute : Attribute
    {
        public bool Auto { get { return resourceKey == null || resourceSource == null; } }

        Type resourceSource;
        string resourceKey;

        public PluralLocDescriptionAttribute()
        {
        }

        public PluralLocDescriptionAttribute(Type resourceSource, string resourceKey)
        {
            this.resourceSource = resourceSource;
            this.resourceKey = resourceKey;
        }

        public string PluralDescription
        {
            get
            {
                if (Auto)
                    throw new ApplicationException("Use ReflectionTools.GetPluralDescription instead");

                return new ResourceManager(resourceSource).GetString(resourceKey);
            }
        }
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
                   Pluralizer.Pluralize(type.NiceName());
        }

        public static string GetDescription(MemberInfo memberInfo)
        {
            LocDescriptionAttribute loc = memberInfo.SingleAttribute<LocDescriptionAttribute>();
            if (loc != null)
            {
                if (loc.Auto)
                {
                    string key = memberInfo.DeclaringType.TryCC(d => d.Name).Add(memberInfo.Name, "_");
                    Assembly assembly = (memberInfo.DeclaringType ?? (Type)memberInfo).Assembly;
                    return assembly.GetDefaultResourceManager().GetString(key, CultureInfo.CurrentCulture);
                }
                else
                    return loc.Description;
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
            PluralLocDescriptionAttribute loc = type.SingleAttribute<PluralLocDescriptionAttribute>();
            if (loc != null)
            {
                if (loc.Auto)
                {
                    string key = type.Name + "_Plural";
                    Assembly assembly = type.Assembly;
                    return assembly.GetDefaultResourceManager().GetString(key, CultureInfo.CurrentCulture);
                }
                else
                    return loc.PluralDescription;
            }

            PluralDescriptionAttribute desc = type.SingleAttribute<PluralDescriptionAttribute>();
            if (desc != null)
            {
                return desc.PluralDescription;
            }

            return null;
        }     

        public static ResourceManager GetDefaultResourceManager(this Assembly assembly)
        {
            string[] resourceFiles = assembly.GetManifestResourceNames();
            string name = resourceFiles.Single(a => a.Contains("Resources.resources"));
            return new ResourceManager(name.Replace(".resources", ""), assembly);
        }
    }
}
