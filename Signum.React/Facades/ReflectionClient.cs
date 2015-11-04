using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web;
using Newtonsoft.Json;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.React.ApiControllers;
using Signum.Utilities;

namespace Signum.React.Facades
{
    public static class ReflectionClient
    {
        public static ConcurrentDictionary<CultureInfo, Dictionary<string, TypeInfoTS>> cache =
         new ConcurrentDictionary<CultureInfo, Dictionary<string, TypeInfoTS>>();

        public static Dictionary<Assembly, HashSet<string>> EntityAssemblies;

        public static void Start()
        {
            DescriptionManager.Invalidated += () => cache.Clear();

            EntityAssemblies = TypeLogic.TypeToEntity.Keys.AgGroupToDictionary(t => t.Assembly, gr => gr.Select(a => a.Namespace).ToHashSet());
        }
        
        const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

        internal static Dictionary<string, TypeInfoTS> GetTypeInfoTS(CultureInfo culture)
        {
            return cache.GetOrAdd(culture, ci =>
            {
                if (!EntityAssemblies.Keys.Any(a => DescriptionManager.GetLocalizedAssembly(a, ci) != null))
                    return GetTypeInfoTS(culture.Parent ?? CultureInfo.GetCultureInfo("en"));
                
                var result = new Dictionary<string, TypeInfoTS>();
                result.AddRange(GetEntities(), "typeInfo");
                result.AddRange(GetSymbolContainers(), "typeInfo");
                result.AddRange(GetEnums(), "typeInfo");
                return result;
            });
        }

        private static Dictionary<string, TypeInfoTS> GetEntities()
        {
            var result = (from type in TypeLogic.TypeToEntity.Keys
                          where !type.IsEnumEntity()
                          let descOptions = LocalizedAssembly.GetDescriptionOptions(type)
                          select KVP.Create(GetTypeName(type), new TypeInfoTS
                          {
                              Kind = KindOfType.Entity,
                              NiceName = descOptions.HasFlag(DescriptionOptions.Description) ? type.NiceName() : null,
                              NicePluralName = descOptions.HasFlag(DescriptionOptions.PluralDescription) ? type.NicePluralName() : null,
                              Gender = descOptions.HasFlag(DescriptionOptions.Gender) ? type.GetGender().ToString() : null,
                              Members = PropertyRoute.GenerateRoutes(type)
                                .ToDictionary(p => p.PropertyString(), p => new MemberInfo
                                {
                                    NiceName = p.PropertyInfo?.NiceName(),
                                    Format = p.PropertyRouteType == PropertyRouteType.FieldOrProperty ? Reflector.FormatString(p) : null,
                                    Unit = p.PropertyInfo?.GetCustomAttribute<UnitAttribute>()?.UnitName,
                                    IsCollection = p.PropertyInfo?.PropertyType.IsMList() ?? false,
                                    IsLite = p.PropertyInfo?.PropertyType.CleanMList().IsLite() ?? false,
                                    IsNullable = p.PropertyInfo?.PropertyType.IsNullable() ?? false,
                                    Type = IsId(p) ? TypeScriptType(PrimaryKey.Type(type)) :
                                            p.TryGetImplementations()?.Key() ?? TypeScriptType(p.Type)
                                })
                          })).ToDictionary("entities");

            return result;
        }

        private static bool IsId(PropertyRoute p)
        {
            return p.PropertyInfo.Name == nameof(Entity.Id) && p.Parent.PropertyRouteType == PropertyRouteType.Root;
        }

        private static Dictionary<string, TypeInfoTS> GetEnums()
        {
            var result = (from a in EntityAssemblies
                          from type in a.Key.GetTypes()
                          where type.IsEnum
                          where a.Value.Contains(type.Namespace)
                          let descOptions = LocalizedAssembly.GetDescriptionOptions(type)
                          where descOptions != DescriptionOptions.None
                          let kind = type.Name.EndsWith("Query") ? KindOfType.Query :
                                     type.Name.EndsWith("Message") ? KindOfType.Message : KindOfType.Enum
                          select KVP.Create(GetTypeName(type), new TypeInfoTS
                          {
                              Kind = kind,
                              NiceName = descOptions.HasFlag(DescriptionOptions.Description) ? type.NiceName() : null,
                              Members = type.GetFields(staticFlags).ToDictionary(m => m.Name, m => new MemberInfo
                              {
                                  NiceName = m.NiceName(),
                              }),
                          })).ToDictionary("enums");

            return result;
        }

        private static Dictionary<string, TypeInfoTS> GetSymbolContainers()
        {
            var result = (from a in EntityAssemblies
                          from type in a.Key.GetTypes()
                          where type.IsStaticClass() && type.HasAttribute<AutoInitAttribute>()
                          where a.Value.Contains(type.Namespace)
                          let descOptions = LocalizedAssembly.GetDescriptionOptions(type)
                          where descOptions != DescriptionOptions.None
                          let kind = type.Name.EndsWith("Query") ? KindOfType.Query :
                                     type.Name.EndsWith("Message") ? KindOfType.Message : KindOfType.Enum
                          select KVP.Create(GetTypeName(type), new TypeInfoTS
                          {
                              Kind = KindOfType.SymbolContainer,
                              Members = type.GetFields(staticFlags).Where(f => GetSymbol(f).IdOrNull.HasValue).ToDictionary(m => m.Name, m => new MemberInfo
                              {
                                  NiceName = m.NiceName(),
                                  Id = GetSymbol(m).Id.Object
                              })
                          })).ToDictionary("symbols");

            return result;
        }

        private static Symbol GetSymbol(FieldInfo m)
        {
            var v = m.GetValue(null);

            if (v is IOperationSymbolContainer)
                v = ((IOperationSymbolContainer)v).Symbol;

            return ((Symbol)v);
        }

        private static string TypeScriptType(Type type)
        {
            type = CleanMList(type);

            type = type.UnNullify().CleanType();

            return BasicType(type) ?? GetTypeName(type) ?? "any";
        }

        private static Type CleanMList(this Type type)
        {
            if (type.IsMList())
                type = type.ElementType();
            return type;
        }

        private static string BasicType(Type type)
        {
            if (type.IsEnum)
                return null;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean: return "boolean";
                case TypeCode.Char: return "string";
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal: return "number";
                case TypeCode.DateTime: return "datetime";
                case TypeCode.String: return "string";
            }
            return null;
        }

        private static string GetTypeName(Type t)
        {
            if (typeof(ModifiableEntity).IsAssignableFrom(t))
                return TypeLogic.TryGetCleanName(t) ?? t.Name;

            return t.Name;
        }
    }

    public class TypeInfoTS
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "kind")]
        public KindOfType Kind { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "niceName")]
        public string NiceName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "nicePluralName")]
        public string NicePluralName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "gender")]
        public string Gender { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "members")]
        public Dictionary<string, MemberInfo> Members { get; set; }
    }

    public class MemberInfo
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "niceName")]
        public string NiceName { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isCollection")]
        public bool IsCollection { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isLite")]
        public bool IsLite { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isNullable")]
        public bool IsNullable { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "unit")]
        public string Unit { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "format")]
        public string Format { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "id")]
        public object Id { get; set; }
    }

    public enum KindOfType
    {
        Entity,
        Enum,
        Message,
        Query,
        SymbolContainer,
    }
}