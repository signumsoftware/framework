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
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using System.Collections.Concurrent;
using System.IO;
using System.Xml.Linq;


namespace Signum.Utilities
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface, Inherited = true)]
    public class DescriptionOptionsAttribute : Attribute
    {
        public DescriptionOptions Options { get; set; }

        public DescriptionOptionsAttribute(DescriptionOptions options)
        {
            this.Options = options;
        }
    }

    [Flags]
    public enum DescriptionOptions
    {
        None = 0,

        Members = 1,
        Description = 2,
        PluralDescription = 4,
        Gender = 8,

        All = Members | Description | PluralDescription | Gender,
    }

    public static class DescriptionOptionsExtensions
    {
        public static bool IsSetAssert(this DescriptionOptions opts, DescriptionOptions flag, MemberInfo member)
        {
            if ((opts.IsSet(DescriptionOptions.PluralDescription) || opts.IsSet(DescriptionOptions.Gender)) && !opts.IsSet(DescriptionOptions.Description))
                throw new InvalidOperationException("{0} has {1} set also requires {2}".FormatWith(member.Name, opts, DescriptionOptions.Description));

            if ((member is PropertyInfo || member is FieldInfo) &&
                (opts.IsSet(DescriptionOptions.PluralDescription) ||
                 opts.IsSet(DescriptionOptions.Gender) ||
                 opts.IsSet(DescriptionOptions.Members)))
                throw new InvalidOperationException("Member {0} has {1} set".FormatWith(member.Name, opts));

            return opts.IsSet(flag);
        }

        public static bool IsSet(this DescriptionOptions opts, DescriptionOptions flag)
        {
            return (opts & flag) == flag;
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

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class FormatAttribute : Attribute
    {
        public string Format { get; private set; }
        public FormatAttribute(string format)
        {
            this.Format = format;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TimeSpanDateFormatAttribute : Attribute
    {
        public string Format { get; private set; }

        public TimeSpanDateFormatAttribute(string format)
        {
            Format = format;
        }
    }

    public static class DescriptionManager
    {
        public static Func<Type, string> CleanTypeName = t => t.Name; //To allow MyEntityEntity
        public static Func<Type, Type> CleanType = t => t; //To allow Lite<T>

        public static string TranslationDirectory = Path.Combine(Path.GetDirectoryName(new Uri(typeof(DescriptionManager).Assembly.CodeBase).LocalPath), "Translations");

        public static event Func<Type, DescriptionOptions?> DefaultDescriptionOptions = t => t.IsEnum && t.Name.EndsWith("Message") ? DescriptionOptions.Members : (DescriptionOptions?)null;
        public static event Func<MemberInfo, bool> ShouldLocalizeMemeber = m => true;
        public static event Action<CultureInfo,Type,MemberInfo> NotLocalizedMemeber ;

        public static Dictionary<Type, Func<MemberInfo, string>> ExternalEnums = new Dictionary<Type, Func<MemberInfo, string>>
        {
            { typeof(DayOfWeek), m => CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)((FieldInfo)m).GetValue(null)] }
        };


        static string Fallback(Type type, Func<LocalizedType, string> typeValue, Action<LocalizedType> notLocalized)
        {
            var cc = CultureInfo.CurrentUICulture;
            {
                var loc = GetLocalizedType(type, cc);
                if (loc != null)
                {
                    string result = typeValue(loc);
                    if (result != null)
                        return result;                  
                }
            }

            if (cc.Parent.Name.HasText())
            {
                var loc = GetLocalizedType(type, cc.Parent);
                if (loc != null)
                {
                    string result = typeValue(loc);
                    if (result != null)
                        return result;              
                }
             
            }

  

            var defaultCulture = LocalizedAssembly.GetDefaultAssemblyCulture(type.Assembly);
            //if (defaultCulture != null)
            {
                var loc = GetLocalizedType(type, CultureInfo.GetCultureInfo(defaultCulture));
                if (loc == null)
                    throw new InvalidOperationException("Type {0} is not localizable".FormatWith(type.TypeName()));

                if (notLocalized != null)
                    notLocalized.Invoke(loc);
                        
                return typeValue(loc);
            }

            //return null;
        }


        public static string NiceName(this Type type)
        {
            type = CleanType(type);

            if (!LocalizedAssembly.HasDefaultAssemblyCulture(type.Assembly))
            {
                return type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? type.Name.NiceName();
            }

            var result = Fallback(type, lt => lt.Description, lt => OnNotLocalizedMemeber(type, null));

            if (result != null)
                return result;

            return DefaultTypeDescription(type);
        }

     
        public static string NicePluralName(this Type type)
        {
            type = CleanType(type);

            var result = Fallback(type, lt => lt.PluralDescription, lt => OnNotLocalizedMemeber(type,null));

            if (result != null)
                return result;

            return type.GetCustomAttribute<PluralDescriptionAttribute>()?.PluralDescription ??
                NaturalLanguageTools.Pluralize(DefaultTypeDescription(type)); 
        }

        public static string NiceToString(this Enum a, params object[] args)
        {
            return a.NiceToString().FormatWith(args);
        }

        public static string NiceToString(this Enum a)
        {
            if (a == null)
                return null;

            var fi = EnumFieldCache.Get(a.GetType()).TryGetC(a);
            if (fi != null)
                return GetMemberNiceName(fi) ?? DefaultMemberDescription(fi);

            return a.ToString().NiceName();
        }

        public static string NiceName<R>(Expression<Func<R>> expressionToProperty)
        {
            return ReflectionTools.GetPropertyInfo(expressionToProperty).NiceName();
        }

        public static string NiceName<T, R>(Expression<Func<T, R>> expressionToProperty)
        {
            return ReflectionTools.GetPropertyInfo(expressionToProperty).NiceName();
        }

        public static string NiceName(this FieldInfo fi)
        {
            return GetMemberNiceName(fi) ?? DefaultMemberDescription(fi);
        }

        public static string NiceName(this PropertyInfo pi)
        {
            return GetMemberNiceName(pi) ??
                (pi.IsDefaultName() ? pi.PropertyType.NiceName() : DefaultMemberDescription(pi));
        }

        public static bool IsDefaultName(this PropertyInfo pi)
        {
            return pi.Name == CleanTypeName(CleanType(pi.PropertyType)); 
        }

        static string GetMemberNiceName(MemberInfo memberInfo)
        {
            //var cc = CultureInfo.CurrentUICulture;
            var type = memberInfo.DeclaringType;

            if (!LocalizedAssembly.HasDefaultAssemblyCulture(type.Assembly))
            {
                var f = ExternalEnums.TryGetC(type);

                if (f != null)
                    return f(memberInfo);

                return memberInfo.GetCustomAttribute<DescriptionAttribute>()?.Description ?? memberInfo.Name.NiceName();
            }
       
            var result = Fallback(type, lt => lt.Members.TryGetC(memberInfo.Name), lt => OnNotLocalizedMemeber(null,memberInfo));
            if (result != null)
                return result;



            return result;
        }

        private static void OnNotLocalizedMemeber(Type  type, MemberInfo memberInfo)
        {
            if (NotLocalizedMemeber != null)
            {
                var cc = CultureInfo.CurrentUICulture;
                NotLocalizedMemeber.Invoke(cc, type, memberInfo);
            }
        }

        public static char? GetGender(this Type type)
        {
            type = CleanType(type);

            var cc = CultureInfo.CurrentUICulture;

            if (!LocalizedAssembly.HasDefaultAssemblyCulture(type.Assembly))
            {
                return type.GetCustomAttribute<GenderAttribute>()?.Gender ?? NaturalLanguageTools.GetGender(type.NiceName());
            }

            var lt = GetLocalizedType(type, cc);
            if (lt != null && lt.Gender != null)
                return lt.Gender;

            if (cc.Parent.Name.HasText())
            {
                lt = GetLocalizedType(type, cc.Parent);
                if (lt != null)
                    return lt.Gender;
            }

            return null;
        }

        static ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Assembly, LocalizedAssembly>> localizations = 
            new ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Assembly, LocalizedAssembly>>();

        public static LocalizedType GetLocalizedType(Type type, CultureInfo cultureInfo)
        {
            var la = GetLocalizedAssembly(type.Assembly, cultureInfo);

            if (la == null)
                return null;

            var result = la.Types.TryGetC(type);
            
            if(result != null)
                return result;

            if(type.IsGenericType && !type.IsGenericTypeDefinition)
                return la.Types.TryGetC(type.GetGenericTypeDefinition());

            return null;
        }

        public static LocalizedAssembly GetLocalizedAssembly(Assembly assembly, CultureInfo cultureInfo)
        {
            return localizations
                .GetOrAdd(cultureInfo, ci => new ConcurrentDictionary<Assembly, LocalizedAssembly>())
                .GetOrAdd(assembly, (Assembly a) => LocalizedAssembly.ImportXml(assembly, cultureInfo, forceCreate : false));
        }

        internal static DescriptionOptions? OnDefaultDescriptionOptions(Type type)
        {
            if (DescriptionManager.DefaultDescriptionOptions == null)
                return null;

            foreach (var func in DescriptionManager.DefaultDescriptionOptions.GetInvocationListTyped())
            {
                var result = func(type);
                if (result != null)
                    return result.Value;
            }

            return null;
        }


        public static bool OnShouldLocalizeMember(MemberInfo m)
        {
            if (ShouldLocalizeMemeber == null)
                return true;

            foreach (var func in ShouldLocalizeMemeber.GetInvocationListTyped())
            {
                if (!func(m))
                    return false;
            }

            return true;
        }

        public static Action Invalidated;
        public static void Invalidate()
        {
            localizations.Clear();

            Invalidated?.Invoke();
        }

        internal static string DefaultTypeDescription(Type type)
        {
            return type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? DescriptionManager.CleanTypeName(type).SpacePascal();
        }

        internal static string DefaultMemberDescription(MemberInfo m)
        {
            return m.GetCustomAttribute<DescriptionAttribute>()?.Description ?? m.Name.NiceName();
        }
    }


    public class LocalizedAssembly
    {
        public Assembly Assembly;
        public CultureInfo Culture;
        public bool IsDefault;

        private LocalizedAssembly() { }

        public Dictionary<Type, LocalizedType> Types = new Dictionary<Type, LocalizedType>();

        public static string TranslationFileName(Assembly assembly, CultureInfo cultureInfo)
        {
            return Path.Combine(DescriptionManager.TranslationDirectory, "{0}.{1}.xml".FormatWith(assembly.GetName().Name, cultureInfo.Name));
        }

        public static DescriptionOptions GetDescriptionOptions(Type type)
        {
            var doa = type.GetCustomAttribute<DescriptionOptionsAttribute>(true);
            if (doa != null)
                return type.IsGenericTypeDefinition ? doa.Options & DescriptionOptions.Members : doa.Options;

            DescriptionOptions? def = DescriptionManager.OnDefaultDescriptionOptions(type);
            if (def != null)
                return type.IsGenericTypeDefinition ? def.Value & DescriptionOptions.Members : def.Value;

            if (DescriptionManager.ExternalEnums.ContainsKey(type))
                return DescriptionOptions.Members;

            return DescriptionOptions.None;
        }

        public static string GetDefaultAssemblyCulture(Assembly assembly)
        {
            var defaultLoc = assembly.GetCustomAttribute<DefaultAssemblyCultureAttribute>();

            if (defaultLoc == null)
                throw new InvalidOperationException($"No {nameof(DefaultAssemblyCultureAttribute)} found in {assembly.GetName().Name}");

            return defaultLoc.DefaultCulture;
        }

        public static bool HasDefaultAssemblyCulture(Assembly assembly)
        {
            var defaultLoc = assembly.GetCustomAttribute<DefaultAssemblyCultureAttribute>();

            return defaultLoc != null;
        }

        public void ExportXml()
        {
            var doc = ToXml();

            string fileName = TranslationFileName(Assembly, Culture);

            doc.Save(fileName);

            DescriptionManager.Invalidate();
        }

        public XDocument ToXml()
        {
            var doc = new XDocument(new XDeclaration("1.0", "UTF8", "yes"),
                new XElement("Translations",
                    from lt in Types.Values
                    let doa = GetDescriptionOptions(lt.Type)
                    where doa != DescriptionOptions.None
                    orderby lt.Type.Name
                    select lt.ExportXml()
                )
            );
            return doc;
        }
  
        public static LocalizedAssembly ImportXml(Assembly assembly, CultureInfo cultureInfo, bool forceCreate)
        {
            var defaultCulture = GetDefaultAssemblyCulture(assembly);

            if(defaultCulture == null)
                return null;

            bool isDefault = cultureInfo.Name == defaultCulture;

            string fileName = TranslationFileName(assembly, cultureInfo);

            XDocument doc = !File.Exists(fileName) ? null : XDocument.Load(fileName);

            if (!isDefault && !forceCreate && doc == null)
                return null;

            return FromXml(assembly, cultureInfo, doc, null);
        }

        public static LocalizedAssembly FromXml(Assembly assembly, CultureInfo cultureInfo, XDocument doc, Dictionary<string, string> replacements /*new -> old*/)
        {
            Dictionary<string, XElement> file = doc?.Element("Translations").Elements("Type")
                .Select(x => KVP.Create(x.Attribute("Name").Value, x))
                .Distinct(x => x.Key)
                .ToDictionary();

            var result = new LocalizedAssembly
            {
                Assembly = assembly,
                Culture = cultureInfo,
                IsDefault = GetDefaultAssemblyCulture(assembly) == cultureInfo.Name
            };

            result.Types = (from t in assembly.GetTypes()
                            let opts = GetDescriptionOptions(t)
                            where opts != DescriptionOptions.None
                            let x = file?.TryGetC(replacements?.TryGetC(t.Name) ?? t.Name)
                            select LocalizedType.ImportXml(t, opts, result, x))
                            .ToDictionary(lt => lt.Type);

            return result;
        }

        public override string ToString()
        {
            return "Localized {0}".FormatWith(Assembly.GetName().Name);
        }
    }

    public class LocalizedType
    {
        public Type Type { get; private set; }
        public LocalizedAssembly Assembly { get; private set; }
        public DescriptionOptions Options { get; private set; }

        public string Description { get; set; }
        public string PluralDescription { get; set; }
        public char? Gender { get; set; }

        public Dictionary<string, string> Members = new Dictionary<string, string>();

        LocalizedType() { }

        public XElement ExportXml()
        {
            return new XElement("Type",
                    new XAttribute("Name", Type.Name),

                    !Options.IsSetAssert(DescriptionOptions.Description, Type) ||
                    Description == null ||
                    (Assembly.IsDefault && Description == DescriptionManager.DefaultTypeDescription(Type)) ? null :
                    new XAttribute("Description", Description),

                    !Options.IsSetAssert(DescriptionOptions.PluralDescription, Type) ||
                    PluralDescription == null ||
                    (PluralDescription == NaturalLanguageTools.Pluralize(Description, Assembly.Culture)) ? null :
                    new XAttribute("PluralDescription", PluralDescription),

                    !Options.IsSetAssert(DescriptionOptions.Gender, Type) ||
                    Gender == null ||
                    (Gender == NaturalLanguageTools.GetGender(Description, Assembly.Culture)) ? null :
                    new XAttribute("Gender", Gender.ToString()),

                    !Options.IsSetAssert(DescriptionOptions.Members, Type) ? null :
                     (from m in GetMembers(Type)
                      where DescriptionManager.OnShouldLocalizeMember(m)
                      orderby m.Name
                      let value = Members.TryGetC(m.Name)
                      where value != null && !(Assembly.IsDefault && (DescriptionManager.DefaultMemberDescription(m) == value))
                      select new XElement("Member", new XAttribute("Name", m.Name), new XAttribute("Description", value)))
                );
        }

        const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

        public static IEnumerable<MemberInfo> GetMembers(Type type)
        {
            if (type.IsEnum)
                return EnumFieldCache.Get(type).Values;
            else if (type.IsAbstract && type.IsSealed) // static 
                return type.GetFields(staticFlags).Cast<MemberInfo>();
            else
                return type.GetProperties(instanceFlags).Concat(type.GetFields(instanceFlags).Cast<MemberInfo>());
        }

        internal static LocalizedType ImportXml(Type type, DescriptionOptions opts, LocalizedAssembly assembly, XElement x)
        {
            string description = !opts.IsSetAssert(DescriptionOptions.Description, type) ? null :
                (x == null || x.Attribute("Name").Value != type.Name ? null : x.Attribute("Description")?.Value) ??
                (!assembly.IsDefault ? null : DescriptionManager.DefaultTypeDescription(type));

            var xMembers = x?.Elements("Member")
                .Select(m => KVP.Create(m.Attribute("Name").Value, m.Attribute("Description").Value))
                .Distinct(m => m.Key)
                .ToDictionary();

            LocalizedType result = new LocalizedType
            {
                Type = type,
                Options = opts,
                Assembly = assembly,

                Description = description,
                PluralDescription = !opts.IsSetAssert(DescriptionOptions.PluralDescription, type) ? null :
                             ((x == null || x.Attribute("Name").Value != type.Name ? null : x.Attribute("PluralDescription")?.Value) ??
                             (!assembly.IsDefault ? null : type.GetCustomAttribute<PluralDescriptionAttribute>()?.PluralDescription) ??
                             (description == null ? null : NaturalLanguageTools.Pluralize(description, assembly.Culture))),

                Gender = !opts.IsSetAssert(DescriptionOptions.Gender, type) ? null :
                         ((x?.Attribute("Gender")?.Value.Single()) ??
                         (!assembly.IsDefault ? null : type.GetCustomAttribute<GenderAttribute>()?.Gender) ??
                         (description == null ? null : NaturalLanguageTools.GetGender(description, assembly.Culture))),

                Members = !opts.IsSetAssert(DescriptionOptions.Members, type) ? null :
                          (from m in GetMembers(type)
                           where DescriptionManager.OnShouldLocalizeMember(m)
                           let value = xMembers?.TryGetC(m.Name) ?? (!assembly.IsDefault ? null : DescriptionManager.DefaultMemberDescription(m))
                           where value != null
                           select KVP.Create(m.Name, value))
                           .ToDictionary()
            };

            return result;
        }

        public override string ToString()
        {
            return "Localized {0}".FormatWith(Type.Name);
        }

        public bool Contains(string text)
        {
            return ContainsDescription(text) ||
                this.Members != null && this.Members.Any(m => m.Key.Contains(text, StringComparison.InvariantCultureIgnoreCase) || m.Value.Contains(text, StringComparison.InvariantCultureIgnoreCase)); 
        }

        public bool ContainsDescription(string text)
        {
            return this.Type.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase) ||
                            this.Description != null && this.Description.Contains(text, StringComparison.InvariantCultureIgnoreCase) ||
                            this.PluralDescription != null && this.Description.Contains(text, StringComparison.InvariantCultureIgnoreCase);
        }
    }

}
