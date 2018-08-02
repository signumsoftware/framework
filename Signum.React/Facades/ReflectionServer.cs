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
using Signum.Engine.Operations;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.Entities.Basics;

namespace Signum.React.Facades
{
    public static class ReflectionServer
    {
        public static Func<object> GetContext = GetCurrentValidCulture;

        public static object GetCurrentValidCulture()
        {
            var ci = CultureInfo.CurrentCulture;
            while (ci != CultureInfo.InvariantCulture && !EntityAssemblies.Keys.Any(a => DescriptionManager.GetLocalizedAssembly(a, ci) != null))
                ci = ci.Parent;

            return ci != CultureInfo.InvariantCulture ? ci : CultureInfo.GetCultureInfo("en");
        }

        public static ConcurrentDictionary<object, Dictionary<string, TypeInfoTS>> cache =
         new ConcurrentDictionary<object, Dictionary<string, TypeInfoTS>>();

        public static Dictionary<Assembly, HashSet<string>> EntityAssemblies;

        public static ResetLazy<Dictionary<string, Type>> TypesByName = new ResetLazy<Dictionary<string, Type>>(
            () => GetTypes().Where(t => typeof(ModifiableEntity).IsAssignableFrom(t) ||
            t.IsEnum && !t.Name.EndsWith("Query") && !t.Name.EndsWith("Message"))
            .ToDictionaryEx(GetTypeName, "Types"));

        public static void RegisterLike(Type type)
        {
            TypesByName.Reset();
            EntityAssemblies.GetOrCreate(type.Assembly).Add(type.Namespace);
        }

        internal static void Start()
        {
            DescriptionManager.Invalidated += () => cache.Clear();
            Schema.Current.OnMetadataInvalidated += () => cache.Clear();

            var mainTypes = Schema.Current.Tables.Keys;
            var mixins = mainTypes.SelectMany(t => MixinDeclarations.GetMixinDeclarations(t));
            var operations = OperationLogic.RegisteredOperations.Select(o => o.FieldInfo.DeclaringType);

            EntityAssemblies = mainTypes.Concat(mixins).Concat(operations).AgGroupToDictionary(t => t.Assembly, gr => gr.Select(a => a.Namespace).ToHashSet());
            EntityAssemblies[typeof(PaginationMode).Assembly].Add(typeof(PaginationMode).Namespace);
        }

        const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

        public static event Action<TypeInfoTS, Type> AddTypeExtension;
        static TypeInfoTS OnAddTypeExtension(TypeInfoTS ti, Type t)
        {
            if (ti == null)
                return ti;

            foreach (var a in AddTypeExtension.GetInvocationListTyped())
                a(ti, t);

            return ti;
        }

        public static event Action<MemberInfoTS, PropertyRoute> AddPropertyRouteExtension;
        static MemberInfoTS OnAddPropertyRouteExtension(MemberInfoTS mi, PropertyRoute m)
        {
            if (AddPropertyRouteExtension == null)
                return mi;

            foreach (var a in AddPropertyRouteExtension.GetInvocationListTyped())
                a(mi, m);

            return mi;
        }


        public static event Action<MemberInfoTS, FieldInfo> AddFieldInfoExtension;
        static MemberInfoTS OnAddFieldInfoExtension(MemberInfoTS mi, FieldInfo m)
        {
            if (AddFieldInfoExtension == null)
                return mi;

            foreach (var a in AddFieldInfoExtension.GetInvocationListTyped())
                a(mi, m);

            return mi;
        }

        public static event Action<OperationInfoTS, OperationInfo, Type> AddOperationExtension;
        static OperationInfoTS OnAddOperationExtension(OperationInfoTS oi, OperationInfo o, Type type)
        {
            if (AddOperationExtension == null)
                return oi;

            foreach (var a in AddOperationExtension.GetInvocationListTyped())
                a(oi, o, type);

            return oi;
        }



        public static HashSet<Type> ExcludeTypes = new HashSet<Type>();

        internal static Dictionary<string, TypeInfoTS> GetTypeInfoTS()
        {
            return cache.GetOrAdd(GetContext(), ci =>
            {
                var result = new Dictionary<string, TypeInfoTS>();

                var allTypes = GetTypes();
                allTypes = allTypes.Except(ExcludeTypes).ToList();

                result.AddRange(GetEntities(allTypes), "typeInfo");
                result.AddRange(GetSymbolContainers(allTypes), "typeInfo");
                result.AddRange(GetEnums(allTypes), "typeInfo");

                return result;
            });
        }

        public static List<Type> GetTypes()
        {
            return EntityAssemblies.SelectMany(kvp =>
            {
                var normalTypes = kvp.Key.GetTypes().Where(t => kvp.Value.Contains(t.Namespace));

                var usedEnums = (from type in normalTypes
                                 where typeof(ModifiableEntity).IsAssignableFrom(type)
                                 from p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                 let pt = (p.PropertyType.ElementType() ?? p.PropertyType).UnNullify()
                                 where pt.IsEnum && !EntityAssemblies.ContainsKey(pt.Assembly)
                                 select pt).ToList();


                var importedTypes = kvp.Key.GetCustomAttributes<ImportInTypeScriptAttribute>().Where(a => kvp.Value.Contains(a.ForNamesace)).Select(a => a.Type);
                return normalTypes.Concat(importedTypes).Concat(usedEnums);
            }).Distinct().ToList();
        }

        static MethodInfo miToString = ReflectionTools.GetMethodInfo((object o) => o.ToString());

        public static Dictionary<string, TypeInfoTS> GetEntities(IEnumerable<Type> allTypes)
        {
            var models = (from type in allTypes
                          where typeof(ModelEntity).IsAssignableFrom(type) && !type.IsAbstract
                          select type).ToList();

            var queries = QueryLogic.Queries;

            var schema = Schema.Current;
            var settings = Schema.Current.Settings;

            var result = (from type in TypeLogic.TypeToEntity.Keys.Concat(models)
                          where !type.IsEnumEntity() && !ReflectionServer.ExcludeTypes.Contains(type)
                          let descOptions = LocalizedAssembly.GetDescriptionOptions(type)
                          let allOperations = !type.IsEntity() ? null : OperationLogic.GetAllOperationInfos(type)
                          select KVP.Create(GetTypeName(type), OnAddTypeExtension(new TypeInfoTS
                          {
                              Kind = KindOfType.Entity,
                              FullName = type.FullName,
                              NiceName = descOptions.HasFlag(DescriptionOptions.Description) ? type.NiceName() : null,
                              NicePluralName = descOptions.HasFlag(DescriptionOptions.PluralDescription) ? type.NicePluralName() : null,
                              Gender = descOptions.HasFlag(DescriptionOptions.Gender) ? type.GetGender().ToString() : null,
                              EntityKind = type.IsIEntity() ? EntityKindCache.GetEntityKind(type) : (EntityKind?)null,
                              EntityData = type.IsIEntity() ? EntityKindCache.GetEntityData(type) : (EntityData?)null,
                              IsLowPopulation = type.IsIEntity() ? EntityKindCache.IsLowPopulation(type) : false,
                              IsSystemVersioned = type.IsIEntity() ? schema.Table(type).SystemVersioned != null : false,
                              ToStringFunction = typeof(Symbol).IsAssignableFrom(type) ? null : LambdaToJavascriptConverter.ToJavascript(ExpressionCleaner.GetFieldExpansion(type, miToString)),
                              QueryDefined = queries.QueryDefined(type),
                              Members = PropertyRoute.GenerateRoutes(type).Where(pr => InTypeScript(pr))
                                .ToDictionary(p => p.PropertyString(), p =>
                                {
                                    var mi = new MemberInfoTS
                                    {
                                        NiceName = p.PropertyInfo?.NiceName(),
                                        TypeNiceName = GetTypeNiceName(p.PropertyInfo?.PropertyType),
                                        Format = p.PropertyRouteType == PropertyRouteType.FieldOrProperty ? Reflector.FormatString(p) : null,
                                        IsReadOnly = !IsId(p) && (p.PropertyInfo?.IsReadOnly() ?? false),
                                        Unit = UnitAttribute.GetTranslation(p.PropertyInfo?.GetCustomAttribute<UnitAttribute>()?.UnitName),
                                        Type = new TypeReferenceTS(IsId(p) ? PrimaryKey.Type(type).Nullify() : p.PropertyInfo?.PropertyType, p.Type.IsMList() ? p.Add("Item").TryGetImplementations() : p.TryGetImplementations()),
                                        IsMultiline = Validator.TryGetPropertyValidator(p)?.Validators.OfType<StringLengthValidatorAttribute>().FirstOrDefault()?.MultiLine ?? false,
                                        MaxLength = Validator.TryGetPropertyValidator(p)?.Validators.OfType<StringLengthValidatorAttribute>().FirstOrDefault()?.Max.DefaultToNull(-1),
                                        PreserveOrder = settings.FieldAttributes(p)?.OfType<PreserveOrderAttribute>().Any() ?? false,
                                    };

                                    return OnAddPropertyRouteExtension(mi, p);
                                }),

                              Operations = allOperations == null ? null : allOperations.ToDictionary(oi => oi.OperationSymbol.Key, oi => OnAddOperationExtension(new OperationInfoTS(oi), oi, type)),

                              RequiresEntityPack = allOperations != null && allOperations.Any(oi => oi.HasCanExecute != null),

                          }, type))).ToDictionaryEx("entities");

            return result;
        }

        public static bool InTypeScript(PropertyRoute pr)
        {
            return (pr.Parent == null || InTypeScript(pr.Parent)) && (pr.PropertyInfo == null || (pr.PropertyInfo.GetCustomAttribute<InTypeScriptAttribute>()?.GetInTypeScript() ?? !IsExpression(pr.Parent.Type, pr.PropertyInfo)));
        }

        private static bool IsExpression(Type type, PropertyInfo propertyInfo)
        {
            return propertyInfo.SetMethod == null && ExpressionCleaner.HasExpansions(type, propertyInfo);
        }

        static string GetTypeNiceName(Type type)
        {
            if (type.IsModifiableEntity() && !type.IsEntity())
                return type.NiceName();
            return null;
        }

        public static bool IsId(PropertyRoute p)
        {
            return p.PropertyRouteType == PropertyRouteType.FieldOrProperty &&
                p.PropertyInfo.Name == nameof(Entity.Id) &&
                p.Parent.PropertyRouteType == PropertyRouteType.Root;
        }

        public static Dictionary<string, TypeInfoTS> GetEnums(IEnumerable<Type> allTypes)
        {
            var queries = QueryLogic.Queries;

            var result = (from type in allTypes
                          where type.IsEnum
                          let descOptions = LocalizedAssembly.GetDescriptionOptions(type)
                          where descOptions != DescriptionOptions.None
                          let kind = type.Name.EndsWith("Query") ? KindOfType.Query :
                                     type.Name.EndsWith("Message") ? KindOfType.Message : KindOfType.Enum
                          select KVP.Create(GetTypeName(type), OnAddTypeExtension(new TypeInfoTS
                          {
                              Kind = kind,
                              FullName = type.FullName,
                              NiceName = descOptions.HasFlag(DescriptionOptions.Description) ? type.NiceName() : null,
                              Members = type.GetFields(staticFlags)
                              .Where(fi => kind != KindOfType.Query || queries.QueryDefined(fi.GetValue(null)))
                              .ToDictionary(fi => fi.Name, fi => OnAddFieldInfoExtension(new MemberInfoTS
                              {
                                  NiceName = fi.NiceName(),
                                  IsIgnoredEnum = kind == KindOfType.Enum && fi.HasAttribute<IgnoreAttribute>()
                              }, fi)),
                          }, type))).ToDictionaryEx("enums");

            return result;
        }

        public static Dictionary<string, TypeInfoTS> GetSymbolContainers(IEnumerable<Type> allTypes)
        {
            SymbolLogic.LoadAll();

            var result = (from type in allTypes
                          where type.IsStaticClass() && type.HasAttribute<AutoInitAttribute>()
                          select KVP.Create(GetTypeName(type), OnAddTypeExtension(new TypeInfoTS
                          {
                              Kind = KindOfType.SymbolContainer,
                              FullName = type.FullName,
                              Members = type.GetFields(staticFlags)
                                  .Select(f => GetSymbolInfo(f))
                                  .Where(s =>
                                  s.FieldInfo != null && /*Duplicated like in Dynamic*/
                                  s.IdOrNull.HasValue /*Not registered*/)
                                  .ToDictionary(s => s.FieldInfo.Name, s => OnAddFieldInfoExtension(new MemberInfoTS
                                  {
                                      NiceName = s.FieldInfo.NiceName(),
                                      Id = s.IdOrNull.Value.Object
                                  }, s.FieldInfo))
                          }, type)))
                          .Where(a => a.Value.Members.Any())
                          .ToDictionaryEx("symbols");

            return result;
        }

        private static (FieldInfo FieldInfo, PrimaryKey? IdOrNull) GetSymbolInfo(FieldInfo m)
        {
            object v = m.GetValue(null);
            if (v is IOperationSymbolContainer osc)
                v = osc.Symbol;
            
            if (v is Symbol s)
                return (s.FieldInfo, s.IdOrNull);

            if(v is SemiSymbol semiS)
                return (semiS.FieldInfo, semiS.IdOrNull);

            throw new InvalidOperationException();
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
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "fullName")]
        public string FullName { get; set; }
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
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isSystemVersioned")]
        public bool IsSystemVersioned { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "toStringFunction")]
        public string ToStringFunction { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "queryDefined")]
        public bool QueryDefined { get; internal set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "members")]
        public Dictionary<string, MemberInfoTS> Members { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "operations")]
        public Dictionary<string, OperationInfoTS> Operations { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "requiresEntityPack")]
        public bool RequiresEntityPack { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Extension { get; set; } = new Dictionary<string, object>();


        public override string ToString() => $"{Kind} {NiceName} {EntityKind} {EntityData}";
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
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isIgnoredEnum")]
        public bool IsIgnoredEnum { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "maxLength")]
        public int? MaxLength { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isMultiline")]
        public bool IsMultiline { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "preserveOrder")]
        public bool PreserveOrder { get; internal set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "id")]
        public object Id { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Extension { get; set; } = new Dictionary<string, object>();
    }

    public class OperationInfoTS
    {
        [JsonProperty(PropertyName = "operationType")]
        private OperationType OperationType;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, PropertyName = "canBeNew")]
        private bool? CanBeNew;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, PropertyName = "hasCanExecute")]
        private bool? HasCanExecute;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, PropertyName = "hasStates")]
        private bool? HasStates;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, PropertyName = "canBeModified")]
        private bool? CanBeModified;

        [JsonExtensionData]
        public Dictionary<string, object> Extension { get; set; } = new Dictionary<string, object>();

        public OperationInfoTS(OperationInfo oper)
        {
            this.CanBeNew = oper.CanBeNew;
            this.HasCanExecute = oper.HasCanExecute;
            this.HasStates = oper.HasStates;
            this.OperationType = oper.OperationType;
            this.CanBeModified = oper.CanBeModified;
        }
    }

    public class TypeReferenceTS
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isCollection")]
        public bool IsCollection { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isLite")]
        public bool IsLite { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isNotNullable")]
        public bool IsNotNullable { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, PropertyName = "isEmbedded")]
        public bool IsEmbedded { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "typeNiceName")]
        public string TypeNiceName { get; set; }

        public TypeReferenceTS() { }
        public TypeReferenceTS(Type type, Implementations? implementations)
        {
            this.IsCollection = type != typeof(string) && type != typeof(byte[]) && type.ElementType() != null;

            var clean = type == typeof(string) ? type : (type.ElementType() ?? type);
            this.IsLite = clean.IsLite();
            this.IsNotNullable = clean.IsValueType && !clean.IsNullable();
            this.IsEmbedded = clean.IsEmbeddedEntity();

            if (this.IsEmbedded && !this.IsCollection)
                this.TypeNiceName = type.NiceName();

            this.Name = implementations?.Key() ?? TypeScriptType(type);
        }

        private static string TypeScriptType(Type type)
        {
            type = CleanMList(type);

            type = type.UnNullify().CleanType();

            return BasicType(type) ?? ReflectionServer.GetTypeName(type) ?? "any";
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