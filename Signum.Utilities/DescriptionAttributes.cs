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
using System.Collections.Concurrent;
using System.IO;
using System.Xml.Linq;


namespace Signum.Utilities
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property |AttributeTargets.Field | AttributeTargets.Enum, Inherited = true)]
    public class DescriptionOptionsAttribute : Attribute
    {
        public DescriptionOptions Options { get; set; }

        public DescriptionOptionsAttribute(DescriptionOptions options)
        {
            this.Options = options;
        }
    }

    public enum DescriptionOptions
    {
        None = 0,

        Members = 1,
        Description = 2,
        PluralDescription = 4,
        Gender = 8,

        All = Members | Description | PluralDescription | Gender,
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
    public class GenderAttribute : Attribute
    {
        public char Gender { get; set; }

        public GenderAttribute(char gender)
        {
            this.Gender = gender;
        }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = true)]
    public class DefaultAssemblyCultureAttribute : Attribute
    {
        public string DefaultCulture { get; private set; }

        public DefaultAssemblyCultureAttribute(string defaultCulture)
        {
            this.DefaultCulture = defaultCulture;
        }
    }



    public static class DescriptionManager
    {
        public static Func<Type, string> CleanTypeName = t => t.Name; //To allow MyEntityDN
        public static Func<Type, Type> CleanType = t => t; //To allow Lite<T>

        public static string NiceName(this Type type)
        {
            type = CleanType(type);

            var cc = CultureInfo.CurrentUICulture;

            return GetLocalizedType(type, cc).TryCC(lt => lt.PluralDescription) ??
                (cc.Parent.Name.HasText() ? GetLocalizedType(type, cc.Parent).TryCC(lt => lt.PluralDescription) : null) ??
                GetLocalizedType(type, CultureInfo.GetCultureInfo(GetDefaultAssemblyCulture(type.Assembly))).PluralDescription;
        }

        public static string NicePluralName(this Type type)
        {
            type = CleanType(type);

            var cc = CultureInfo.CurrentUICulture;

            return GetLocalizedType(type, cc).TryCC(lt => lt.PluralDescription) ??
                (cc.Parent.Name.HasText() ? GetLocalizedType(type, cc.Parent).TryCC(lt => lt.PluralDescription) : null) ??
                GetLocalizedType(type, CultureInfo.GetCultureInfo(GetDefaultAssemblyCulture(type.Assembly))).PluralDescription;
        }

        public static string NiceToString(this Enum a)
        {
            var fi = EnumFieldCache.Get(a.GetType()).TryGetC(a);
            if (fi != null)
                return GetMemberNiceName(fi);

            return a.ToString().NiceName();
        }

        public static string NiceName(this PropertyInfo pi)
        {
            return GetMemberNiceName(pi) ??
                (pi.IsDefaultName() ? pi.PropertyType.NiceName() : pi.Name.NiceName());
        }

        public static bool IsDefaultName(this PropertyInfo pi)
        {
            return pi.Name == CleanTypeName(CleanType(pi.PropertyType)); 
        }

        static string GetMemberNiceName(MemberInfo memberInfo)
        {
            if (memberInfo.DeclaringType == typeof(DayOfWeek))
            {
                return CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)((FieldInfo)memberInfo).GetValue(null)];
            }

            var cc = CultureInfo.CurrentUICulture;

            var type = memberInfo.DeclaringType;

            return GetLocalizedType(type, cc).TryCC(lt => lt.Members.TryGetC(memberInfo.Name)) ??
                (cc.Parent.Name.HasText() ? GetLocalizedType(type, cc.Parent).TryCC(lt => lt.Members.TryGetC(memberInfo.Name)) : null) ??
                GetLocalizedType(type, CultureInfo.GetCultureInfo(GetDefaultAssemblyCulture(type.Assembly))).Members.TryGetC(memberInfo.Name);
        }


        private static string GetDefaultAssemblyCulture(Assembly assembly)
        {
            var defaultLoc = assembly.SingleAttribute<DefaultAssemblyCultureAttribute>();

            if (defaultLoc == null)
                throw new InvalidOperationException("Assembly {0} does not have {1}".Formato(assembly.GetName().Name, typeof(DefaultAssemblyCultureAttribute).Name));

            return defaultLoc.DefaultCulture;
        }

        public static char? GetGender(this Type type)
        {
            type = CleanType(type);

            var cc = CultureInfo.CurrentUICulture;

            return GetLocalizedType(type, cc).TryCS(lt => lt.Gender) ??
                (cc.Parent.Name.HasText() ? GetLocalizedType(type, cc.Parent).TryCS(lt => lt.Gender) : null);
        }

        static ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Assembly, Dictionary<Type, LocalizedType>>> localizations = 
            new ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Assembly, Dictionary<Type, LocalizedType>>>();

        public static LocalizedType GetLocalizedType(Type type, CultureInfo cultureInfo)
        {
            return GetLocalizedAssembly(type.Assembly, cultureInfo).TryGetC(type); 
        }

        private static Dictionary<Type, LocalizedType> GetLocalizedAssembly(Assembly assembly, CultureInfo cultureInfo)
        {
            return localizations
                .GetOrAdd(cultureInfo, ci => new ConcurrentDictionary<Assembly, Dictionary<Type, LocalizedType>>())
                .GetOrAdd(assembly, (Assembly a) => LoadTranslatedAssembly(assembly, cultureInfo));
        }

        public static string TranslationDirectory = Path.Combine(Path.GetDirectoryName(new Uri(typeof(DescriptionManager).Assembly.CodeBase).LocalPath), "Translations");

        public static string TranslationFileName(Assembly assembly, CultureInfo cultureInfo)
        {
            return Path.Combine(TranslationDirectory, "{0}.{1}.xml".Formato(assembly.GetName().Name, cultureInfo.Name));
        }

        public static event Func<Type, DescriptionOptions?> DefaultDescriptionOptions = t => t.Name.EndsWith("Message") ? DescriptionOptions.Members : null; 

        static Dictionary<Type, LocalizedType> LoadTranslatedAssembly(Assembly assembly, CultureInfo cultureInfo)
        {
            bool isDefault = cultureInfo.Name == GetDefaultAssemblyCulture(assembly);

            string fileName = TranslationFileName(assembly, cultureInfo);

            Dictionary<string, XElement> file = !File.Exists(fileName) ? null :
                XDocument.Load(fileName).Element("Translations").Elements("Type")
                .Select(x => KVP.Create(x.Attribute("Name").Value, x))
                .Distinct(x => x.Key)
                .ToDictionary();

            if (!isDefault && file == null)
                return null;

            return (from t in assembly.GetTypes()
                    let opts = GetDescriptionOptions(t)
                    let x = file.TryGetC(t.Name)
                    where opts != DescriptionOptions.None && (x != null || isDefault)
                    select LoadTranslatedType(t, opts, cultureInfo, x, isDefault))
                    .ToDictionary(lt => lt.Type);
        }

        static DescriptionOptions GetDescriptionOptions(Type type)
        {
            var doa = type.SingleAttributeInherit<DescriptionOptionsAttribute>();
            if (doa != null)
                return doa.Options;

            if (DefaultDescriptionOptions == null)
                return DescriptionOptions.None;

            foreach (Func<Type, DescriptionOptions?> action in DefaultDescriptionOptions.GetInvocationList())
            {
                var result = action(type);
                if (result != null)
                    return result;
            }
        }

        const BindingFlags bf = BindingFlags.Public | BindingFlags.Instance;

        static LocalizedType LoadTranslatedType(Type type, DescriptionOptions opts, CultureInfo cultureInfo, XElement x, bool isDefault)
        {
            string name = !opts.IsSetAssert(DescriptionOptions.Description, type) ? null :
                (x == null ? null : x.Attribute("Description").TryCC(xa => xa.Value)) ??
                (isDefault ? null : DefaultTypeDescription(type));

            var members = x == null ? null : x.Elements("Member")
                .Select(m => KVP.Create(m.Attribute("Name").Value, m.Attribute("Description").Value))
                .Distinct(m => m.Key)
                .ToDictionary();

            LocalizedType result = new LocalizedType(type)
            {
                Description = name,
                PluralDescription = !opts.IsSetAssert(DescriptionOptions.PluralDescription, type) ? null :
                             ((x == null ? null : x.Attribute("PluralDescription").TryCC(xa => xa.Value)) ??
                             (isDefault ? type.SingleAttribute<PluralDescriptionAttribute>().TryCC(t => t.PluralDescription) : null) ??
                             (name == null ? null : NaturalLanguageTools.Pluralize(name, cultureInfo))),

                Gender = !opts.IsSetAssert(DescriptionOptions.Gender, type) ? null :
                         ((x == null ? null : x.Attribute("Gender").TryCS(xa => xa.Value.Single())) ??
                         (isDefault ? type.SingleAttribute<GenderAttribute>().TryCS(t => t.Gender) : null) ??
                         (name == null ? null : NaturalLanguageTools.GetGender(name, cultureInfo))),

                Members = !opts.IsSetAssert(DescriptionOptions.Members, type) ? null :
                          (from m in type.GetProperties(bf).Cast<MemberInfo>().Concat(type.GetFields(bf))
                           let mta = m.SingleAttribute<DescriptionOptionsAttribute>()
                           where mta == null || mta.Options.IsSetAssert(DescriptionOptions.Description, m)
                           let value = members.TryGetC(m.Name) ?? (isDefault ? DefaultMemberDescription(m) : null)
                           where value != null
                           select KVP.Create(m.Name, value))
                           .ToDictionary()
            };

            return result;
        }

        private static string DefaultTypeDescription(Type type)
        {
            return type.SingleAttribute<DescriptionAttribute>().TryCC(t => t.Description) ?? CleanTypeName(type).SpacePascal();
        }

        private static string DefaultMemberDescription(MemberInfo m)
        {
            return (m.SingleAttribute<DescriptionAttribute>().TryCC(t => t.Description) ?? m.Name.NiceName());
        }

        static bool IsSetAssert(this DescriptionOptions opts, DescriptionOptions flag, MemberInfo member)
        {
            if ((opts.IsSet(DescriptionOptions.PluralDescription) || opts.IsSet(DescriptionOptions.Gender)) && !opts.IsSet(DescriptionOptions.Description))
                throw new InvalidOperationException("{0} has {1} set also requires {2}".Formato(member.Name, opts, DescriptionOptions.Description));

            if ((member is PropertyInfo || member is FieldInfo) &&
                (opts.IsSet(DescriptionOptions.PluralDescription) ||
                 opts.IsSet(DescriptionOptions.Gender) ||
                 opts.IsSet(DescriptionOptions.Members)))
                throw new InvalidOperationException("Member {0} has {1} set".Formato(member.Name, opts));

            return opts.IsSet(flag);
        }

        private static bool IsSet(this DescriptionOptions opts, DescriptionOptions flag)
        {
            return (opts & flag) == flag;
        }

        static void SaveTranslatedAssembly(Assembly assembly, CultureInfo cultureInfo)
        {
            bool isDefault = cultureInfo.Name == GetDefaultAssemblyCulture(assembly);

            string fileName = TranslationFileName(assembly, cultureInfo);

            Dictionary<Type, LocalizedType> localizedAssembly = GetLocalizedAssembly(assembly, cultureInfo);

            var doc = new XDocument(new XDeclaration("1.0", "UTF8", "yes"),
                new XElement("Translations",
                    from kvp in localizedAssembly
                    let type = kvp.Key
                    let tt = kvp.Value
                    let doa = GetDescriptionOptions(type)
                    where doa != DescriptionOptions.None
                    select new XElement("Type",
                        new XAttribute("Name", type.Name),

                        !doa.IsSetAssert(DescriptionOptions.Description, type) ||
                        tt.Description == null ||
                        (isDefault && tt.Description == (type.SingleAttribute<DescriptionAttribute>().TryCC(t => t.Description) ?? CleanTypeName(type).SpacePascal())) ? null : 
                        new XAttribute("Description", tt.Description),

                        !doa.IsSetAssert(DescriptionOptions.PluralDescription, type) ||
                        tt.PluralDescription == null ||
                        (tt.PluralDescription == NaturalLanguageTools.Pluralize(tt.Description, cultureInfo)) ? null :
                        new XAttribute("PluralDescription", tt.PluralDescription),

                        !doa.IsSetAssert(DescriptionOptions.Gender, type) ||
                        tt.Gender == null ||
                        (tt.Gender == NaturalLanguageTools.GetGender(tt.Description, cultureInfo)) ? null : 
                        new XAttribute("Gender", tt.Gender),

                        !doa.IsSetAssert(DescriptionOptions.Members, type) ? null :
                         (from m in type.GetProperties(bf).Cast<MemberInfo>().Concat(type.GetFields(bf))
                          let doam = m.SingleAttribute<DescriptionOptionsAttribute>()
                          where doa == null || doam.Options.IsSetAssert(DescriptionOptions.Description, m)
                          let value = tt.Members.TryGetC(m.Name)
                          where value != null && (!isDefault || ((type.SingleAttribute<DescriptionAttribute>().TryCC(t => t.Description) ?? m.Name.NiceName()) != value))
                          select new XElement("Member", new XAttribute("MemberName", m.Name), new XAttribute("Name", value)))
                    )
                )
            );

            doc.Save(fileName);
        }

        public class LocalizedType
        {
            public Type Type { get; private set; }
            public string Description { get; set; }
            public string PluralDescription { get; set; }
            public char? Gender { get; set; }

            public Dictionary<string, string> Members = new Dictionary<string, string>();

            public LocalizedType(Type type)
            {
                this.Type = type;
            }
        }
    }
}
