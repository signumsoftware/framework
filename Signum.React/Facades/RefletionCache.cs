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
using Signum.Entities.DynamicQuery;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;

namespace Signum.React.Facades
{
    public static class ReflectionCache
    {
        public static ConcurrentDictionary<CultureInfo, Dictionary<string, TypeInfoTS>> cache =
         new ConcurrentDictionary<CultureInfo, Dictionary<string, TypeInfoTS>>();

        public static Dictionary<Assembly, HashSet<string>> EntityAssemblies;

        public static void Start()
        {
            DescriptionManager.Invalidated += () => cache.Clear();

            EntityAssemblies = TypeLogic.TypeToEntity.Keys.AgGroupToDictionary(t => t.Assembly, gr => gr.Select(a => a.Namespace).ToHashSet());
            EntityAssemblies[typeof(PaginationMode).Assembly].Add(typeof(PaginationMode).Namespace);
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

                var allTypes = GetTypes();
                result.AddRange(GetEntities(allTypes), "typeInfo");
                result.AddRange(GetSymbolContainers(allTypes), "typeInfo");
                result.AddRange(GetEnums(allTypes), "typeInfo");
                return result;
            });
        }

        public static IEnumerable<Type> GetTypes()
        {
            return EntityAssemblies.SelectMany(kvp =>
            {
                var normalTypes = kvp.Key.GetTypes().Where(t => kvp.Value.Contains(t.Namespace));
                var importedTypes = kvp.Key.GetCustomAttributes<ImportInTypeScriptAttribute>().Where(a => kvp.Value.Contains(a.ForNamesace)).Select(a => a.Type);
                return normalTypes.Concat(importedTypes).ToList();
            });
        }

        static MethodInfo miToString = ReflectionTools.GetMethodInfo((object o) => o.ToString());

        private static Dictionary<string, TypeInfoTS> GetEntities(IEnumerable<Type> allTypes)
        {
            var models = (from type in allTypes
                          where typeof(ModelEntity).IsAssignableFrom(type) && !type.IsAbstract
                          select type).ToList();

            var result = (from type in TypeLogic.TypeToEntity.Keys.Concat(models)
                          where !type.IsEnumEntity()
                          let descOptions = LocalizedAssembly.GetDescriptionOptions(type)
                          select KVP.Create(GetTypeName(type), new TypeInfoTS
                          {
                              Kind = KindOfType.Entity,
                              NiceName = descOptions.HasFlag(DescriptionOptions.Description) ? type.NiceName() : null,
                              NicePluralName = descOptions.HasFlag(DescriptionOptions.PluralDescription) ? type.NicePluralName() : null,
                              Gender = descOptions.HasFlag(DescriptionOptions.Gender) ? type.GetGender().ToString() : null,
                              EntityKind = type.IsIEntity() ? EntityKindCache.GetEntityKind(type) : (EntityKind?)null,
                              EntityData = type.IsIEntity() ? EntityKindCache.GetEntityData(type) : (EntityData?)null,
                              IsLowPopulation = type.IsIEntity() ? EntityKindCache.IsLowPopulation(type) : false,
                              ToStringFunction = ToJavascript(ExpressionCleaner.GetFieldExpansion(type, miToString)),
                              Members = PropertyRoute.GenerateRoutes(type)
                                .ToDictionary(p => p.PropertyString(), p => new MemberInfoTS
                                {
                                    NiceName = p.PropertyInfo?.NiceName(),
                                    TypeNiceName = GetTypeNiceName(p.PropertyInfo?.PropertyType),
                                    Format = p.PropertyRouteType == PropertyRouteType.FieldOrProperty ? Reflector.FormatString(p) : null,
                                    IsReadOnly = !IsId(p) && (p.PropertyInfo?.IsReadOnly() ?? false),
                                    Unit = p.PropertyInfo?.GetCustomAttribute<UnitAttribute>()?.UnitName,
                                    Type = new TypeReferenceTS(IsId(p) ? PrimaryKey.Type(type): p.PropertyInfo?.PropertyType, p.TryGetImplementations())
                                })
                          })).ToDictionary("entities");

            return result;
        }

        private static string ToJavascript(LambdaExpression lambdaExpression)
        {
            if (lambdaExpression == null)
                return null;

            var body = ToJavascriptBody(lambdaExpression.Parameters.Single(), lambdaExpression.Body);

            if (body == null)
                return null;

            return "function(e){ return " + body + "; }"; 
        }

        private static string ToJavascriptBody(ParameterExpression param, Expression body)
        {
            if (param == body)
                return "e";

            if (body.NodeType == ExpressionType.MemberAccess)
            {
                var a = ToJavascriptBody(param, ((MemberExpression)body).Expression);

                if (a == null)
                    return null;

                return a + "." + ((MemberExpression)body).Member.Name.FirstLower();
            }

            if (body.NodeType == ExpressionType.Add)
            {
                var a = ToJavascriptBody(param, ((BinaryExpression)body).Left);
                var b = ToJavascriptBody(param, ((BinaryExpression)body).Right);

                if (a != null && b != null)
                    return "(" + a + " + " + b + ")";

                return null;
            }

            if (body.NodeType == ExpressionType.Call && ((MethodCallExpression)body).Method.Name == "ToString")
            {
                var a = ToJavascriptBody(param, ((MethodCallExpression)body).Object);

                if (a == null)
                    return null;

                return a + ".toString()";
            }

            return null;
        }

        static string GetTypeNiceName(Type type)
        {
            if(type.IsModifiableEntity() && !type.IsEntity())
                return type.NiceName();
            return null;
        }

        private static bool IsId(PropertyRoute p)
        {
            return p.PropertyInfo.Name == nameof(Entity.Id) && p.Parent.PropertyRouteType == PropertyRouteType.Root;
        }

        private static Dictionary<string, TypeInfoTS> GetEnums(IEnumerable<Type> allTypes)
        {
            var result = (from type in allTypes
                          where type.IsEnum
                          let descOptions = LocalizedAssembly.GetDescriptionOptions(type)
                          where descOptions != DescriptionOptions.None
                          let kind = type.Name.EndsWith("Query") ? KindOfType.Query :
                                     type.Name.EndsWith("Message") ? KindOfType.Message : KindOfType.Enum
                          select KVP.Create(GetTypeName(type), new TypeInfoTS
                          {
                              Kind = kind,
                              NiceName = descOptions.HasFlag(DescriptionOptions.Description) ? type.NiceName() : null,
                              Members = type.GetFields(staticFlags).ToDictionary(m => m.Name, m => new MemberInfoTS
                              {
                                  NiceName = m.NiceName(),
                                  IsIgnored = kind == KindOfType.Enum && m.HasAttribute<IgnoreAttribute>()
                              }),
                          })).ToDictionary("enums");

            return result;
        }

        private static Dictionary<string, TypeInfoTS> GetSymbolContainers(IEnumerable<Type> allTypes)
        {
            var result = (from type in allTypes
                          where type.IsStaticClass() && type.HasAttribute<AutoInitAttribute>()
                          let descOptions = LocalizedAssembly.GetDescriptionOptions(type)
                          where descOptions != DescriptionOptions.None
                          let kind = type.Name.EndsWith("Query") ? KindOfType.Query :
                                     type.Name.EndsWith("Message") ? KindOfType.Message : KindOfType.Enum
                          select KVP.Create(GetTypeName(type), new TypeInfoTS
                          {
                              Kind = KindOfType.SymbolContainer,
                              Members = type.GetFields(staticFlags).Where(f => GetSymbol(f).IdOrNull.HasValue).ToDictionary(m => m.Name, m => new MemberInfoTS
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

        public static string GetTypeName(Type t)
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
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "entityKind")]
        public EntityKind? EntityKind { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "entityData")]
        public EntityData? EntityData { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isLowPopulation")]
        public bool IsLowPopulation { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "toStringFunction")]
        public string ToStringFunction { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "members")]
        public Dictionary<string, MemberInfoTS> Members { get; set; }
    }

    public class MemberInfoTS
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "type")]
        public TypeReferenceTS Type { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "niceName")]
        public string NiceName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "typeNiceName")]
        public string TypeNiceName { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isReadOnly")]
        public bool IsReadOnly { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "unit")]
        public string Unit { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "format")]
        public string Format { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isIgnored")]
        public bool IsIgnored { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "id")]
        public object Id { get; set; }
    }

    public class TypeReferenceTS
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isEnum")]
        public bool IsEnum { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isCollection")]
        public bool IsCollection { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isLite")]
        public bool IsLite { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isNullable")]
        public bool IsNullable { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isEmbedded")]
        public bool IsEmbedded { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "name")]
        public string Name { get; set; }

        public TypeReferenceTS(Type type, Implementations? implementations)
        {
            this.IsCollection = type != typeof(string) && type.ElementType() != null;
            
            var clean = type == typeof(string) ? type :  (type.ElementType() ?? type);
            this.IsLite = clean.IsLite();
            this.IsNullable = clean.IsNullable();
            this.IsEnum = clean.UnNullify().IsEnum;
            this.IsEmbedded = clean.IsEmbeddedEntity();
            this.Name = implementations?.Key() ?? TypeScriptType(type);
        }

        private static string TypeScriptType(Type type)
        {
            type = CleanMList(type);

            type = type.UnNullify().CleanType();

            return BasicType(type) ?? ReflectionCache.GetTypeName(type) ?? "any";
        }

        private static Type CleanMList(Type type)
        {
            if (type.IsMList())
                type = type.ElementType();
            return type;
        }

        public static string BasicType(Type type)
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
                case TypeCode.UInt64: return "number";
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal: return "decimal";
                case TypeCode.DateTime: return "datetime";
                case TypeCode.String: return "string";
            }
            return null;
        }

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