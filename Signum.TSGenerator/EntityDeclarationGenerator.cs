using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Signum.TSGenerator;

public class PreloadingAssemblyResolver : DefaultAssemblyResolver
{
    public AssemblyDefinition SignumUtilities { get; private set; }
    public AssemblyDefinition Signum { get; private set; }

    Dictionary<string, string> assemblyLocations;
    readonly ReaderParameters rp;

    public PreloadingAssemblyResolver(Dictionary<string, string> assemblyLocations)
    {
        this.rp = new ReaderParameters { AssemblyResolver = this };

        this.assemblyLocations = assemblyLocations;

        this.RegisterAssembly(this.SignumUtilities = ModuleDefinition.ReadModule(assemblyLocations["Signum.Utilities"], rp).Assembly);
        this.RegisterAssembly(this.Signum = ModuleDefinition.ReadModule(assemblyLocations["Signum"], rp).Assembly);
    }

    public override AssemblyDefinition Resolve(AssemblyNameReference name)
    {
        var assembly = ModuleDefinition.ReadModule(this.assemblyLocations[name.Name], rp).Assembly;

        this.RegisterAssembly(assembly);

        return assembly;
    }
}

public class TSType 
{
    public required string Namespace;
    public required TypeDefinition Type;
    public required Func<Dictionary<string, Dictionary<string, NamespaceTSReference>>, string> GetText;

    public override string ToString() => Type.Name;
}

static class EntityDeclarationGenerator
{
    internal class TypeCache
    {
        public TypeDefinition ModifiableEntity;
        public TypeDefinition InTypeScriptAttribute;
        public TypeDefinition ImportInTypeScriptAttribute;
        public TypeDefinition IEntity;

        public TypeCache(AssemblyDefinition signumEntities)
        {
            ModifiableEntity = signumEntities.MainModule.GetType("Signum.Entities", "ModifiableEntity");
            InTypeScriptAttribute = signumEntities.MainModule.GetType("Signum.Entities", "InTypeScriptAttribute");
            ImportInTypeScriptAttribute = signumEntities.MainModule.GetType("Signum.Entities", "ImportInTypeScriptAttribute");
            IEntity = signumEntities.MainModule.GetType("Signum.Entities", "IEntity");
        }
    }

    static TypeCache Cache;


    static ConcurrentDictionary<TypeReference, bool> IEntityCache = new ConcurrentDictionary<TypeReference, bool>();

    internal static List<TSType> GetAllTSTypes(AssemblyOptions options)
    {
        var entities = options.Resolver.Signum;

        Cache = new TypeCache(entities);

        var exportedTypes = options.ModuleDefinition.Types.ToList();

        var imported = options.ModuleDefinition.Assembly.CustomAttributes.Where(at => at.AttributeType.FullName == Cache.ImportInTypeScriptAttribute.FullName)
            .Select(at => ((TypeReference)at.ConstructorArguments[0].Value).Resolve())
            .ToList();

        var importedMessage = imported.Where(a => a.Name.EndsWith("Message")).ToList();
        var importedEnums = imported.Except(importedMessage).ToList();

        var entityResults = (from type in exportedTypes
                             where !type.IsValueType && (type.InTypeScript() ?? IsModifiableEntity(type))
                             select new TSType
                             {
                                 Namespace = type.Namespace,
                                 Type = type,
                                 GetText = nsr => EntityInTypeScript(type, options, nsr),
                             }).ToList();

        var interfacesResults = (from type in exportedTypes
                                 where type.IsInterface && (type.InTypeScript() ?? type.AnyInterfaces(IEntityCache, i => i.FullName == Cache.IEntity.FullName))
                                 select new TSType
                                 {
                                     Namespace = type.Namespace,
                                     Type = type,
                                     GetText = nsr => EntityInTypeScript(type, options, nsr),
                                 }).ToList();


        var usedEnums = (from type in entityResults.Select(a => a.Type)
                         from p in GetAllProperties(type)
                         let pt = (p.PropertyType.ElementType() ?? p.PropertyType).UnNullify()
                         let def = pt.Resolve()
                         where def != null && def.IsEnum
                         select def).Distinct().ToList();

        var symbolResults = (from type in exportedTypes
                             where !type.IsValueType && type.IsStaticClass() && type.ContainsAttribute("AutoInitAttribute")
                             && (type.InTypeScript() ?? true)
                             select new TSType
                             {
                                 Namespace = type.Namespace,
                                 Type = type,
                                 GetText = nsr => SymbolInTypeScript(type, options, nsr),
                             }).ToList();

        var enumResult = (from type in exportedTypes
                          where type.IsEnum && (type.InTypeScript() ?? usedEnums.Contains(type))
                          select new TSType
                          {
                              Namespace = type.Namespace,
                              Type = type,
                              GetText = nsr => EnumInTypeScript(type, options),
                          }).ToList();

        var externalNamespace = Path.GetFileNameWithoutExtension(options.CurrentAssembly) + ".External";

        var externalEnums = (from type in usedEnums.Where(td => options.IsExternal(td)).Concat(importedEnums)
                             select new TSType
                             {
                                 Namespace = externalNamespace,
                                 Type = type,
                                 GetText = nsr => EnumInTypeScript(type, options),
                             }).ToList();

        var externalMessages = (from type in importedMessage
                                select new TSType
                                {
                                    Namespace = externalNamespace,
                                    Type = type,
                                    GetText = nsr => MessageInTypeScript(type, options),
                                }).ToList();

        var messageResults = (from type in exportedTypes
                              where type.IsEnum && type.Name.EndsWith("Message")
                              select new TSType
                              {
                                  Namespace = type.Namespace,
                                  Type = type,
                                  GetText = nsr => MessageInTypeScript(type, options),
                              }).ToList();

        var queryResult = (from type in exportedTypes
                           where type.IsEnum && type.Name.EndsWith("Query")
                           select new TSType
                           {
                               Namespace = type.Namespace,
                               Type = type,
                               GetText = nsr => QueryInTypeScript(type, options),
                           }).ToList();

        var allTypes = entityResults
            .Concat(interfacesResults)
            .Concat(enumResult)
            .Concat(messageResults)
            .Concat(queryResult)
            .Concat(symbolResults)
            .Concat(externalEnums)
            .Concat(externalMessages)
            .ToList();

        return allTypes;
    }


    internal static string WriteNamespaceFile(AssemblyOptions options, string templateFileName, string currentNamespace, List<TSType> types)
    {
        var namespacesReferences = new Dictionary<string, Dictionary<string, NamespaceTSReference>>();

        namespacesReferences.GetNamespaceReference(options, Cache.ModifiableEntity);
        
        var texts = types.Select(a => new
        {
            a.Type.Name,
            Text = a.GetText(namespacesReferences)
        }).ToList();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(@"//////////////////////////////////");
        sb.AppendLine(@"//Auto-generated. Do NOT modify!//");
        sb.AppendLine(@"//////////////////////////////////");
        sb.AppendLine();
        var path = namespacesReferences.GetOrThrow("Signum").GetOrThrow("Signum.Entities").FullPath.Replace("Signum.Entities.ts", "Reflection.ts");
        sb.AppendLine($"import {{ MessageKey, QueryKey, Type, EnumType, registerSymbol }} from '{RelativePath(path, templateFileName)}'");

        foreach (var assRef in namespacesReferences.Values)
        {
            foreach (var nsRef in assRef.Values)
            {
                sb.AppendLine($"import * as {nsRef.VariableName} from '{RelativePath(nsRef.FullPath, templateFileName)}'");
            }
        }
        sb.AppendLine();
        sb.AppendLine(File.ReadAllText(templateFileName));

        foreach (var t in texts.OrderBy(a => a.Name))
        {
            sb.Append(t.Text);
            sb.AppendLine();
        }


        return sb.ToString().Replace("\n", "\n");

    }



    private static string RelativePath(string path, string fileName)
    {
        Uri pathUri = new Uri(path.RemoveSuffix(".ts"), UriKind.Absolute);
        Uri fileNameUri = new Uri(fileName, UriKind.Absolute);

        string relPath = fileNameUri.MakeRelativeUri(pathUri).ToString();

        var result = relPath.Replace(@"\", "/");

        if (!result.StartsWith(".."))
            return "./" + result;

        return result;
    }

    private static string EnumInTypeScript(TypeDefinition type, AssemblyOptions options)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"export const {type.Name}: EnumType<{type.Name}> = new EnumType<{type.Name}>(\"{type.Name}\");");

        sb.AppendLine($"export type {type.Name} =");
        var fields = type.Fields.OrderBy(a => a.Constant as IComparable).Where(a => a.IsPublic && a.IsStatic).ToList();
        for (int i = 0; i < fields.Count; i++)
        {
            sb.Append($"  \"{fields[i].Name}\"");
            if (i < fields.Count - 1)
                sb.AppendLine(" |");
            else
                sb.AppendLine(";");
        }


        return sb.ToString();
    }

    private static string MessageInTypeScript(TypeDefinition type, AssemblyOptions options)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"export namespace {type.Name} {{");
        var fields = type.Fields.OrderBy(a => a.MetadataToken.RID).Where(a => a.IsPublic && a.IsStatic).ToList();

        foreach (var field in fields)
        {
            string context = $"By type {type.Name} and field {field.Name}";
            sb.AppendLine($"  export const {field.Name}: MessageKey = new MessageKey(\"{type.Name}\", \"{field.Name}\");");
        }
        sb.AppendLine(@"}");

        return sb.ToString();
    }

    private static string QueryInTypeScript(TypeDefinition type, AssemblyOptions options)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"export namespace {type.Name} {{");
        var fields = type.Fields.OrderBy(a => a.MetadataToken.RID).Where(a => a.IsPublic && a.IsStatic).ToList();

        foreach (var field in fields)
        {
            string context = $"By type {type.Name} and field {field.Name}";
            sb.AppendLine($"  export const {field.Name}: QueryKey = new QueryKey(\"{type.Name}\", \"{field.Name}\");");
        }
        sb.AppendLine(@"}");

        return sb.ToString();
    }

    static ConcurrentDictionary<TypeReference, bool> IOperationSymbolCache = new ConcurrentDictionary<TypeReference, bool>();

    private static string SymbolInTypeScript(TypeDefinition type, AssemblyOptions options, Dictionary<string, Dictionary<string, NamespaceTSReference>> namespacesReferences)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"export namespace {type.Name} {{");
        var fields = type.Fields.OrderBy(a => a.MetadataToken.RID).Where(a => a.IsPublic && a.IsStatic).ToList();

        foreach (var field in fields)
        {
            string context = $"By type {type.Name} and field {field.Name}";
            var propertyType = TypeScriptName(field.FieldType, type, options, namespacesReferences, context);

            var fieldTypeDef = field.FieldType.Resolve();
            var cleanType = fieldTypeDef.IsInterface && fieldTypeDef.AnyInterfaces(IOperationSymbolCache, i => i.Name == "IOperationSymbolContainer") ? "Operation" : CleanTypeName(fieldTypeDef);
            sb.AppendLine($"  export const {field.Name} : {propertyType} = registerSymbol(\"{cleanType}\", \"{type.Name}.{field.Name}\");");
        }
        sb.AppendLine(@"}");

        return sb.ToString();
    }

    enum ConstraintKind
    {
        Enum,
        ModifiableEntity
    }

    private static string EntityInTypeScript(TypeDefinition type, AssemblyOptions options, Dictionary<string, Dictionary<string, NamespaceTSReference>> namespacesReferences)
    {
        StringBuilder sb = new StringBuilder();
        if (!type.IsAbstract)
        {
            if (!type.HasGenericParameters)
                sb.AppendLine($"export const {type.Name}: Type<{type.Name}> = new Type<{type.Name}>(\"{CleanTypeName(type)}\");");
            else
            {
                var parameters = type.GenericParameters.Select(a => new
                {
                    a.Name,
                    Type = 
                    a.Constraints.Any(c => c.ConstraintType.FullName == "System.Enum") ? ConstraintKind.Enum :
                    a.Constraints.Any(c => IsModifiableEntity(c.ConstraintType)) ? ConstraintKind.ModifiableEntity :
                     throw new Exception($"GenericParameter {a.Name} of {type.ToString()} is not an enum or a ModifiableEntity")
                }).ToList();

                var genericParametersDefinitions = string.Join(", ", parameters.Select(a => $"{a.Name} extends " +
                    (a.Type == ConstraintKind.Enum ? "string" : "ModifiableEntity")));

                var genericParameters = string.Join(", ", parameters.Select(a => $"{a.Name}"));

                var functionParameters = string.Join(", ", parameters.Select(a => $"{FirstLower(a.Name)} : " +
                    (a.Type == ConstraintKind.Enum ? $"EnumType<{a.Name}>" : $"Type<{a.Name}>")));

                var cleanName = type.Name.Substring(0, type.Name.IndexOf('`'));
                var typeNameParts = string.Join(" + \"_\" + ", parameters.Select(a => FirstLower(a.Name) + ".typeName"));

                sb.AppendLine(@$"export function {cleanName}<{genericParametersDefinitions}>({functionParameters}): Type<{cleanName}<{genericParameters}>> {{
    return new Type<{cleanName}<{genericParameters}>>(""{cleanName}_"" + {typeNameParts});
}}");
            }
        }

        List<string> baseTypes = new List<string>();
        if (type.BaseType != null)
            baseTypes.Add(TypeScriptName(type.BaseType, type, options, namespacesReferences, $"By type {type.Name}"));

        var baseInterfaces = Parents(type.BaseType?.Resolve()).SelectMany(t => t.Resolve()?.Interfaces.Select(a => a.InterfaceType) ?? Enumerable.Empty<TypeReference>()).Select(a => a.FullName).ToHashSet();

        var interfaces = type.Interfaces.Select(i => i.InterfaceType).Where(it => !baseInterfaces.Contains(it.FullName))
            .Where(it => it.FullName == Cache.IEntity.FullName || it.Resolve()?.Interfaces.Any(it2 => it2.InterfaceType.FullName == Cache.IEntity.FullName) == true);

        foreach (var i in interfaces)
            baseTypes.Add(TypeScriptName(i, type, options, namespacesReferences, $"By type {type.Name}"));

        sb.AppendLine($"export interface {TypeScriptName(type, type, options, namespacesReferences, "declaring " + type.Name)} extends {string.Join(", ", baseTypes.Distinct())} {{");
        if (!type.IsAbstract && !type.HasGenericParameters && Parents(type.BaseType?.Resolve()).All(a => a.IsAbstract))
            sb.AppendLine($"  Type: \"{CleanTypeName(type)}\";");

        var properties = GetProperties(type);

        var defaultNullableCustomAttribute = type.NullableContextAttribute();


        foreach (var prop in properties)
        {
            string context = $"Generating {type.Name}.{prop.Name}";
            var propertyType = TypeScriptNameInternal(prop.PropertyType, type, options, namespacesReferences, context) + (prop.GetTypescriptNull(defaultNullableCustomAttribute) ? " | null" : "");

            var undefined = prop.GetTypescriptUndefined() ? "?" : "";

            sb.AppendLine($"  {FirstLower(prop.Name)}{undefined}: {propertyType};");
        }
        sb.AppendLine(@"}");

        return sb.ToString();
    }

    static CustomAttribute NullableContextAttribute(this TypeDefinition type)
    {
        return type.CustomAttributes.SingleOrDefault(a => a.AttributeType.Name == "NullableContextAttribute") ?? type.DeclaringType?.NullableContextAttribute();
    }


    static ConcurrentDictionary<TypeReference, bool> IsModifiableDictionary = new ConcurrentDictionary<TypeReference, bool>(); 

    static bool IsModifiableEntity(TypeReference t)
    {
        if (t.IsValueType || t.Resolve().IsInterface)
            return false;

        if (!InheritsFromModEntity(t))
            return false;

        return true;

        bool InheritsFromModEntity(TypeReference tr)
        {
            return IsModifiableDictionary.GetOrAdd(tr, tr =>
            {
                if (tr.FullName == Cache.ModifiableEntity.FullName)
                    return true;

                var td = tr.Resolve();

                if (td.BaseType == null || td.BaseType.FullName == "System.Object")
                    return false;

                return InheritsFromModEntity(td.BaseType);
            });
        }
    }

    private static IEnumerable<TypeDefinition> Parents(TypeDefinition type)
    {
        while (type != null && type.FullName != Cache.ModifiableEntity.FullName)
        {
            yield return type;
            type = type.BaseType?.Resolve();
        }
    }

    static string CleanTypeName(TypeDefinition t)
    {
        if (!t.AnyInterfaces(IEntityCache, tr => tr.FullName == Cache.IEntity.FullName))
            return t.Name;

        if (t.Name.EndsWith("Entity"))
            return t.Name.RemoveSuffix("Entity");

        if (t.Name.EndsWith("Model"))
            return t.Name.RemoveSuffix("Model");

        if (t.Name.EndsWith("Symbol"))
            return t.Name.RemoveSuffix("Symbol");

        return t.Name;
    }

    static string RemoveSuffix(this string text, string postfix)
    {
        if (text.EndsWith(postfix) && text != postfix)
            return text.Substring(0, text.Length - postfix.Length);

        return text;
    }

    private static IEnumerable<PropertyDefinition> GetAllProperties(TypeDefinition type) {

        return GetProperties(type).Concat(type.BaseType == null ? Enumerable.Empty<PropertyDefinition>() : GetAllProperties(type.BaseType.Resolve()));
    }

    private static IEnumerable<PropertyDefinition> GetProperties(TypeDefinition type)
    {
        return type.Properties.Where(p => p.HasThis && p.GetMethod.IsPublic)
                        .Where(p => p.InTypeScript() ?? !(p.ContainsAttribute("HiddenPropertyAttribute") || p.ContainsAttribute("ExpressionFieldAttribute") || p.ContainsAttribute("AutoExpressionFieldAttribute")));
    }

    public static bool ContainsAttribute(this IMemberDefinition p, string attributeName)
    {
        return p.CustomAttributes.Any(a => a.AttributeType.Name == attributeName);
    }

    public static CustomAttribute GetAttributeInherit(this TypeDefinition type, string attributeName)
    {
        if (type == null)
            return null;

        var att = type.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == attributeName);
        if (att != null)
            return att;

        return GetAttributeInherit(type.BaseType?.Resolve(), attributeName);
    }

    public static bool? InTypeScript(this MemberReference mr)
    {
        var attr = mr.Resolve().CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == Cache.InTypeScriptAttribute.FullName);

        if (attr == null)
            return null;

        return (bool?)attr.ConstructorArguments.FirstOrDefault().Value;
    }

    public static bool GetTypescriptUndefined(this PropertyDefinition p)
    {
        var ainTSAttr = p.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == Cache.InTypeScriptAttribute.FullName);

        var b = (bool?)ainTSAttr?.Properties.SingleOrDefault(a => a.Name == "Undefined").Argument.Value;

        if (b != null)
            return b.Value;
        
        return GetTypescriptUndefined(p.DeclaringType) ?? false;
    }
    
    private static bool? GetTypescriptUndefined(TypeDefinition declaringType)
    {
        var inTSAttr = GetAttributeInherit(declaringType, Cache.InTypeScriptAttribute.FullName);

        return (bool?)inTSAttr?.Properties.SingleOrDefault(a => a.Name == "Undefined").Argument.Value;
    }

    public static bool GetTypescriptNull(this PropertyDefinition p, CustomAttribute defaultCustomAttribute)
    {
        var inTSAttr = p.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == Cache.InTypeScriptAttribute.FullName);

        var b = (bool?)inTSAttr?.Properties.SingleOrDefault(a => a.Name == "Null").Argument.Value;
        if (b != null)
            return b.Value;
        
        if (p.PropertyType.IsValueType)
            return p.PropertyType.IsNullable();
        else
        {
            var nullableAttr = p.CustomAttributes.SingleOrDefault(a => a.AttributeType.Name == "NullableAttribute");

            if (nullableAttr == null)
                nullableAttr = defaultCustomAttribute;

            if (nullableAttr == null)
                return false;

            var arg = nullableAttr.ConstructorArguments[0].Value;

            if (arg is byte val)
                return val == 2;

            if (arg is CustomAttributeArgument[] args)
                return ((byte)args[0].Value) == 2;

            throw new InvalidOperationException("Unexpected value of type " + arg.GetType() + " in NullableAttribute constructor");
        }
    }

    private static string FirstLower(string name)
    {
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    public static bool IsNullable(this TypeReference type)
    {
        return type is GenericInstanceType gtype && gtype.ElementType.Name == "Nullable`1";
    }

    public static TypeReference UnNullify(this TypeReference type)
    {
        return type is GenericInstanceType gtype && gtype.ElementType.Name == "Nullable`1" ? gtype.GenericArguments.Single() : type;
    }

    static ConcurrentDictionary<TypeReference, bool> IEnumerableCache = new ConcurrentDictionary<TypeReference, bool>();

    public static TypeReference ElementType(this TypeReference type)
    {
        if (!(type is GenericInstanceType gen))
            return null;

        if (type.FullName == typeof(string).FullName || type.FullName == typeof(byte[]).FullName)
            return null;

        var def = type.Resolve();
        if (def == null)
            return null;

        if (!gen.AnyInterfaces(IEnumerableCache, tr => tr is GenericInstanceType git && git.ElementType.Name == "IEnumerable`1"))
            return null;

        return gen.GenericArguments.Single();
    }

    static string TypeScriptName(TypeReference type, TypeDefinition current, AssemblyOptions options, Dictionary<string, Dictionary<string, NamespaceTSReference>> namespacesReferences, string errorContext)
    {
        var ut = type.UnNullify();
        if (ut != type)
            return TypeScriptNameInternal(ut, current, options, namespacesReferences, errorContext) + " | null";

        return TypeScriptNameInternal(type, current, options, namespacesReferences, errorContext);
    }

    private static string TypeScriptNameInternal(TypeReference type, TypeDefinition current, AssemblyOptions options, Dictionary<string, Dictionary<string, NamespaceTSReference>> namespacesReferences, string errorContext)
    {
        type = type.UnNullify();

        if (type.FullName == typeof(Boolean).FullName)
            return "boolean";

        if (type.FullName == typeof(Char).FullName)
            return "string";

        if (type.FullName == typeof(SByte).FullName ||
            type.FullName == typeof(Byte).FullName ||
            type.FullName == typeof(Int16).FullName ||
            type.FullName == typeof(UInt16).FullName ||
            type.FullName == typeof(Int32).FullName ||
            type.FullName == typeof(UInt32).FullName ||
            type.FullName == typeof(Int64).FullName ||
            type.FullName == typeof(UInt64).FullName ||
            type.FullName == typeof(Decimal).FullName ||
            type.FullName == typeof(Single).FullName ||
            type.FullName == typeof(Double).FullName)
            return "number";

        if (type.FullName == typeof(String).FullName)
            return "string";

        if (type.FullName == typeof(DateTime).FullName ||
            type.FullName == typeof(DateOnly).FullName ||
            type.FullName == typeof(DateTimeOffset).FullName ||
            type.FullName == typeof(TimeSpan).FullName ||
            type.FullName == typeof(TimeOnly).FullName ||
            type.FullName == typeof(Guid).FullName)
            return "string /*" + type.Name + "*/";

        if (type.FullName == typeof(Byte[]).FullName)
            return "string /*Byte[]*/";

        if (type.IsGenericParameter)
            return type.Name;

        if (type is GenericInstanceType git)
            return RelativeName(type.Resolve(), current, options, namespacesReferences, errorContext) + "<" + string.Join(", ", git.GenericArguments.Select(a => TypeScriptName(a, current, options, namespacesReferences, errorContext)).ToList()) + ">";
        else if (type.HasGenericParameters)
            return RelativeName(type.Resolve(), current, options, namespacesReferences, errorContext) + "<" + string.Join(", ", type.GenericParameters.Select(gp => gp.Name)) + ">";
        else if (type is ArrayType at)
            return TypeScriptName(at.ElementType, current, options, namespacesReferences, errorContext) + "[]";
        else
            return RelativeName(type.Resolve(), current, options, namespacesReferences, errorContext);
    }

    private static string RelativeName(TypeDefinition type, TypeDefinition current, AssemblyOptions options, Dictionary<string, Dictionary<string, NamespaceTSReference>> namespacesReferences, string errorContext)
    {
        if (type.IsGenericParameter)
            return type.Name;

        if (type.DeclaringType != null)
            return RelativeName(type.DeclaringType, current, options, namespacesReferences, errorContext) + "_" + BaseTypeScriptName(type);

        if (type.Module.Assembly.Equals(current.Module.Assembly) && type.Namespace == current.Namespace)
        {
            string relativeNamespace = RelativeNamespace(type, current);

            return CombineNamespace(relativeNamespace, BaseTypeScriptName(type));
        }
        else if (type.IsEnum && options.IsExternal(type))
        {
            return "External." + BaseTypeScriptName(type);
        }
        else
        {
            var nsReference = GetNamespaceReference(namespacesReferences, options, type);
            if (nsReference == null)
            {
                if (type.Interfaces.Any(i => i.InterfaceType.FullName == typeof(IEnumerable).FullName))
                    return "Array";

                throw new InvalidOperationException($"{errorContext}: Declaring a {type} requires a reference to '{type.Module.Assembly.Name.Name}.csproj'");
            }

            return CombineNamespace(nsReference.VariableName, BaseTypeScriptName(type));
        }
    }



    public static bool AnyInterfaces(this TypeReference type, ConcurrentDictionary<TypeReference, bool> cache, Func<TypeReference, bool> func)
    {
        return cache.GetOrAdd(type, type =>
        {
            var td = type.Resolve();

            foreach (var item in td.Interfaces)
            {
                if (func(item.InterfaceType))
                    return true;
            }

            if (td.BaseType == null)
                return false;

            return td.BaseType.AnyInterfaces(cache, func);
        });
    }
    
    public static NamespaceTSReference GetNamespaceReference(this Dictionary<string, Dictionary<string, NamespaceTSReference>> references,  AssemblyOptions options, TypeDefinition type)
    {
        AssemblyReference assemblyReference;
        options.AssemblyReferences.TryGetValue(type.Module.Assembly.Name.Name, out assemblyReference);
        if (assemblyReference == null)
            return null;


        return references.GetOrCreate(type.Module.Assembly.Name.Name, () => new Dictionary<string, NamespaceTSReference>())
            .GetOrCreate(type.Namespace, () => new NamespaceTSReference
            {
                Namespace = type.Namespace,
                FullPath = FindDeclarationsFile(assemblyReference, type.Namespace, type),
                VariableName = GetVariableName(references, type.Namespace.Split('.'))
            });
    }

    static T GetOrCreate<K, T>(this Dictionary<K, T> dictionary, K key, Func<T> create)
    {
        if (dictionary.TryGetValue(key, out var result))
            return result;

        return dictionary[key] = create();
    }

    private static string GetVariableName(Dictionary<string, Dictionary<string, NamespaceTSReference>> namespaceReferences, string[] nameParts)
    {
        var list = namespaceReferences.Values.SelectMany(a => a.Values.Select(ns => ns.VariableName));

        for (int i = 1; ; i++)
        {
            foreach (var item in nameParts.AsEnumerable().Reverse())
            {
                var candidate = item + (i == 1 ? "" : i.ToString());
                if (!list.Contains(candidate))
                    return candidate;
            }
        }
    }

    private static string FindDeclarationsFile(AssemblyReference assemblyReference, string @namespace, TypeDefinition typeForError)
    {
        var fileT4S = @namespace + ".t4s";

        var result = assemblyReference.AllTypescriptFiles.Where(a => Path.GetFileName(a) == fileT4S).ToList();

        if (result.Count == 1)
            return result.Single().RemoveSuffix(".t4s") + ".ts";

        if (result.Count > 1)
            throw new InvalidOperationException($"importing '{typeForError}' required but multiple '{fileT4S}' were found inside '{assemblyReference.Directory}':\n{string.Join("\n", result.Select(a => "  " + a).ToArray())}");

        throw new InvalidOperationException($"importing '{typeForError}' required but no '{fileT4S}' found inside '{assemblyReference.Directory}'");
    }

    private static string BaseTypeScriptName(TypeDefinition type)
    {
        if (type.FullName == Cache.IEntity.FullName)
            return "Entity";

        var name = type.Name;

        int pos = name.IndexOf('`');

        if (pos == -1)
            return name;

        return name.Substring(0, pos);
    }

    private static string RelativeNamespace(TypeDefinition referedType, TypeDefinition current)
    {
        var referedNS = referedType.Namespace.Split('.').ToList();
        var currentNS = current.Namespace.Split('.').ToList();

        var equal = referedNS.Zip(currentNS, (a, b) => new { a, b }).Where(p => p.a == p.b).Count();

        referedNS.RemoveRange(0, equal);
        return string.Join(".", referedNS);
    }

    private static string CombineNamespace(params string[] parts)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var p in parts)
        {
            if (!string.IsNullOrEmpty(p))
            {
                if (sb.Length > 0)
                    sb.Append(".");

                sb.Append(p);
            }
        }
        return sb.ToString();
    }

    private static string RemoveNamespace(string v, string baseNamespace)
    {
        if (v == baseNamespace)
            return "";

        if (v.StartsWith(baseNamespace + "."))
            return v.Substring((baseNamespace + ".").Length);

        return v;
    }

    public static bool IsStaticClass(this TypeDefinition type)
    {
        return type.IsAbstract && type.IsSealed;
    }
}

[Serializable]
public class AssemblyOptions
{
    public string CurrentAssembly;

    public Dictionary<string, AssemblyReference> AssemblyReferences;

    public Dictionary<string, string> AllReferences { get; internal set; }
    
    public PreloadingAssemblyResolver Resolver { get; internal set; }
    public ModuleDefinition ModuleDefinition { get; internal set; }

    public bool IsExternal(TypeDefinition type)
    {
        return type.Module.Assembly.Name.Name != CurrentAssembly &&
             !AssemblyReferences.ContainsKey(type.Module.Assembly.Name.Name);
    }

    //public Assembly Default_Resolving(AssemblyLoadContext arg1, AssemblyName arg2)
    //{
    //    Console.WriteLine(arg2.Name);
    //    if (AllReferences.TryGetValue(arg2.Name, out string path))
    //        return arg1.LoadFromAssemblyPath(path);

    //    return null;
    //}

}

[Serializable]
public class AssemblyReference
{
    public string Directory;
    //public string AssemblyFullPath;
    public string AssemblyName;

    public List<string> AllTypescriptFiles;
}

public class NamespaceTSReference
{
    public string Namespace;
    public string FullPath;
    public string VariableName;

    public override string ToString() => $"{Namespace} => {VariableName} {FullPath}";
}

public static class DictionaryExtensions
{
    public static V GetOrThrow<K, V>(this Dictionary<K, V> dictionary, K key)
    {
        V result;
        if (!dictionary.TryGetValue(key, out result))
            throw new KeyNotFoundException($"Key '{key}' not found");

        return result;
    }
}
