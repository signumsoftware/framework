using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Signum.TSGenerator
{
    internal class PreloadingAssemblyResolver : DefaultAssemblyResolver
    {
        public AssemblyDefinition SignumUtilities { get; private set; }
        public AssemblyDefinition SignumEntities { get; private set; }

        public PreloadingAssemblyResolver(string[] references)
        {
            foreach (var dll in references)
            {
                var assembly = ModuleDefinition.ReadModule(dll, new ReaderParameters { AssemblyResolver = this }).Assembly;

                if (assembly.Name.Name == "Signum.Entities")
                    SignumEntities = assembly;

                if (assembly.Name.Name == "Signum.Utilities")
                    SignumUtilities = assembly;

                RegisterAssembly(assembly);
            }
        }
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

        internal static string Process(Options options, PreloadingAssemblyResolver resolver)
        {
            StringBuilder sb = new StringBuilder();

            var module = ModuleDefinition.ReadModule(options.CurrentAssemblyReference.AssemblyFullPath, new ReaderParameters { AssemblyResolver = resolver });

            var entities = resolver.SignumEntities;

            Cache = new TypeCache(entities);

            GetNamespaceReference(options, Cache.ModifiableEntity);

            var exportedTypes = module.Types.Where(a => a.Namespace == options.CurrentNamespace).ToList();
            if (exportedTypes.Count == 0)
                throw new InvalidOperationException($"Assembly '{options.CurrentAssembly}' has not types in namespace '{options.CurrentNamespace}'");

            var imported = module.Assembly.CustomAttributes.Where(at => at.AttributeType.FullName == Cache.ImportInTypeScriptAttribute.FullName)
                .Where(at => (string)at.ConstructorArguments[1].Value == options.CurrentNamespace)
                .Select(at => ((TypeReference)at.ConstructorArguments[0].Value).Resolve())
                .ToList();

            var importedMessage = imported.Where(a => a.Name.EndsWith("Message")).ToList();
            var importedEnums = imported.Except(importedMessage).ToList();

            var entityResults = (from type in exportedTypes
                                 where !type.IsValueType && (type.InTypeScript() ?? IsModifiableEntity(type))
                                 select new
                                 {
                                     ns = type.Namespace,
                                     type,
                                     text = EntityInTypeScript(type, options),
                                 }).ToList();

            var interfacesResults = (from type in exportedTypes
                                     where type.IsInterface && (type.InTypeScript() ?? AllInterfaces(type).Any(i => i.FullName ==  Cache.IEntity.FullName))
                                     select new
                                     {
                                         ns = type.Namespace,
                                         type,
                                         text = EntityInTypeScript(type, options),
                                     }).ToList();

            var usedEnums = (from type in entityResults.Select(a => a.type)
                             from p in GetAllProperties(type)
                             let pt = (p.PropertyType.ElementType() ?? p.PropertyType).UnNullify()
                             let def = pt.Resolve()
                             where def != null && def.IsEnum
                             select def).Distinct().ToList();

            var symbolResults = (from type in exportedTypes
                                 where !type.IsValueType && type.IsStaticClass() && type.ContainsAttribute("AutoInitAttribute")
                                 && (type.InTypeScript() ?? true)
                                 select new
                                 {
                                     ns = type.Namespace,
                                     type,
                                     text = SymbolInTypeScript(type, options),
                                 }).ToList();

            var enumResult = (from type in exportedTypes
                              where type.IsEnum && (type.InTypeScript() ?? usedEnums.Contains(type))
                              select new
                              {
                                  ns = type.Namespace,
                                  type,
                                  text = EnumInTypeScript(type, options),
                              }).ToList();

            var externalEnums = (from type in usedEnums.Where(options.IsExternal).Concat(importedEnums)
                                select new
                                {
                                    ns = options.CurrentNamespace + ".External",
                                    type,
                                    text = EnumInTypeScript(type, options),
                                }).ToList();

            var externalMessages = (from type in importedMessage
                                    select new
                                    {
                                        ns = options.CurrentNamespace + ".External",
                                        type,
                                        text = MessageInTypeScript(type, options),
                                    }).ToList();

            var messageResults = (from type in exportedTypes
                                  where type.IsEnum && type.Name.EndsWith("Message")
                                  select new
                                  {
                                      ns = type.Namespace,
                                      type,
                                      text = MessageInTypeScript(type, options),
                                  }).ToList();

            var queryResult = (from type in exportedTypes
                               where type.IsEnum && type.Name.EndsWith("Query")
                               select new
                               {
                                   ns = type.Namespace,
                                   type,
                                   text = QueryInTypeScript(type, options),
                               }).ToList();

            var namespaces = entityResults
                .Concat(interfacesResults)
                .Concat(enumResult)
                .Concat(messageResults)
                .Concat(queryResult)
                .Concat(symbolResults)
                .Concat(externalEnums)
                .Concat(externalMessages)
                .GroupBy(a => a.ns)
                .OrderBy(a => a.Key);


            foreach (var ns in namespaces)
            {
                var key = RemoveNamespace(ns.Key.ToString(), options.CurrentNamespace);

                if (key.Length == 0)
                {
                    foreach (var item in ns.OrderBy(a => a.type.Name))
                    {
                        sb.AppendLine(item.text);
                    }
                }
                else
                {
                    sb.AppendLine("export namespace " + key + " {");
                    sb.AppendLine();

                    foreach (var item in ns.OrderBy(a => a.type.Name))
                    {
                        foreach (var line in item.text.Split(new[] { "\r\n" }, StringSplitOptions.None))
                            sb.AppendLine("  " + line);
                    }

                    sb.AppendLine("}");
                    sb.AppendLine();
                }
            }

            var code = sb.ToString();

            return WriteFillFile(options, code);
        }



        private static string WriteFillFile(Options options, string code)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"//////////////////////////////////");
            sb.AppendLine(@"//Auto-generated. Do NOT modify!//");
            sb.AppendLine(@"//////////////////////////////////");
            sb.AppendLine();
            var path = options.AssemblyReferences.GetOrThrow("Signum.Entities").NamespacesReferences.GetOrThrow("Signum.Entities").Path.Replace("Signum.Entities.ts", "Reflection.ts");
            sb.AppendLine($"import {{ MessageKey, QueryKey, Type, EnumType, registerSymbol }} from '{RelativePath(path, options.TemplateFileName)}'");

            foreach (var a in options.AssemblyReferences.Values)
            {
                foreach (var ns in a.NamespacesReferences.Values)
                {
                    sb.AppendLine($"import * as {ns.VariableName} from '{RelativePath(ns.Path, options.TemplateFileName)}'");
                }
            }
            sb.AppendLine();
            sb.AppendLine(File.ReadAllText(options.TemplateFileName));

            sb.AppendLine(code);

            return sb.ToString();
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

        private static string EnumInTypeScript(TypeDefinition type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"export const {type.Name} = new EnumType<{type.Name}>(\"{type.Name}\");");

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

        private static string MessageInTypeScript(TypeDefinition type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"export module {type.Name} {{");
            var fields = type.Fields.OrderBy(a => a.MetadataToken.RID).Where(a => a.IsPublic && a.IsStatic).ToList();

            foreach (var field in fields)
            {
                string context = $"By type {type.Name} and field {field.Name}";
                sb.AppendLine($"  export const {field.Name} = new MessageKey(\"{type.Name}\", \"{field.Name}\");");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private static string QueryInTypeScript(TypeDefinition type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"export module {type.Name} {{");
            var fields = type.Fields.OrderBy(a => a.MetadataToken.RID).Where(a => a.IsPublic && a.IsStatic).ToList();

            foreach (var field in fields)
            {
                string context = $"By type {type.Name} and field {field.Name}";
                sb.AppendLine($"  export const {field.Name} = new QueryKey(\"{type.Name}\", \"{field.Name}\");");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private static string SymbolInTypeScript(TypeDefinition type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"export module {type.Name} {{");
            var fields = type.Fields.OrderBy(a => a.MetadataToken.RID).Where(a => a.IsPublic && a.IsStatic).ToList();

            foreach (var field in fields)
            {
                string context = $"By type {type.Name} and field {field.Name}";
                var propertyType = TypeScriptName(field.FieldType, type, options, context);

                var fieldTypeDef = field.FieldType.Resolve();
                var cleanType = fieldTypeDef.IsInterface && AllInterfaces(fieldTypeDef).Any(i => i.Name == "IOperationSymbolContainer") ? "Operation" : CleanTypeName(fieldTypeDef);
                sb.AppendLine($"  export const {field.Name} : {propertyType} = registerSymbol(\"{cleanType}\", \"{type.Name}.{field.Name}\");");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private static string EntityInTypeScript(TypeDefinition type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            if (!type.IsAbstract)
                sb.AppendLine($"export const {type.Name} = new Type<{type.Name}>(\"{CleanTypeName(type)}\");");

            List<string> baseTypes = new List<string>();
            if (type.BaseType != null)
                baseTypes.Add(TypeScriptName(type.BaseType, type, options, $"By type {type.Name}"));

            var baseInterfaces = Parents(type.BaseType?.Resolve()).SelectMany(t => t.Resolve()?.Interfaces.Select(a => a.InterfaceType) ?? Enumerable.Empty<TypeReference>()).Select(a => a.FullName).ToHashSet();

            var interfaces = type.Interfaces.Select(i => i.InterfaceType).Where(it => !baseInterfaces.Contains(it.FullName))
                .Where(it => it.FullName == Cache.IEntity.FullName || it.Resolve()?.Interfaces.Any(it2 => it2.InterfaceType.FullName == Cache.IEntity.FullName) == true);

            foreach (var i in interfaces)
                baseTypes.Add(TypeScriptName(i, type, options, $"By type {type.Name}"));

            sb.AppendLine($"export interface {TypeScriptName(type, type, options, "declaring " + type.Name)} extends {string.Join(", ", baseTypes.Distinct())} {{");
            if (!type.IsAbstract && Parents(type.BaseType?.Resolve()).All(a => a.IsAbstract))
                sb.AppendLine($"  Type: \"{CleanTypeName(type)}\";");

            var properties = GetProperties(type);

            foreach (var prop in properties)
            {
                string context = $"By type {type.Name} and property {prop.Name}";
                var propertyType = TypeScriptNameInternal(prop.PropertyType, type, options, context) + (prop.GetTypescriptNull() ? " | null" : "");

                var undefined = prop.GetTypescriptUndefined() ? "?" : "";

                sb.AppendLine($"  {FirstLower(prop.Name)}{undefined}: {propertyType};");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        static bool IsModifiableEntity(TypeDefinition t)
        {
            if (t.IsValueType || t.IsInterface)
                return false;

            if (!InheritsFromModEntity(t))
                return false;

            return true;

            bool InheritsFromModEntity(TypeDefinition td)
            {
                if (td.FullName ==  Cache.ModifiableEntity.FullName)
                    return true;

                if (td.BaseType == null || td.BaseType.FullName == "System.Object")
                    return false;

                var baseType = td.BaseType.Resolve();

                var result = InheritsFromModEntity(baseType);

                return result;
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
            if (!AllInterfaces(t).Any(tr => tr.FullName == Cache.IEntity.FullName))
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
                            .Where(p => p.InTypeScript() ?? !(p.ContainsAttribute("HiddenPropertyAttribute") || p.ContainsAttribute("ExpressionFieldAttribute")));
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
            var attr = p.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == Cache.InTypeScriptAttribute.FullName);

            var b = attr == null ? null : (bool?)attr.Properties.SingleOrDefault(a => a.Name == "Undefined").Argument.Value;

            if (b != null)
                return b.Value;
            
            return GetTypescriptUndefined(p.DeclaringType) ?? false;
        }
        
        private static bool? GetTypescriptUndefined(TypeDefinition declaringType)
        {
            var attr = GetAttributeInherit(declaringType, Cache.InTypeScriptAttribute.FullName);

            return attr == null ? null : (bool?)attr.Properties.SingleOrDefault(a => a.Name == "Undefined").Argument.Value;
        }

        public static bool GetTypescriptNull(this PropertyDefinition p)
        {
            var attr = p.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == Cache.InTypeScriptAttribute.FullName);

            var b = attr == null ? null : (bool?)attr.Properties.SingleOrDefault(a => a.Name == "Null").Argument.Value;
            if (b != null)
                return b.Value;
            
            if (p.PropertyType.IsValueType)
                return p.PropertyType.IsNullable();
            else
                return p.CustomAttributes.Any(a => a.AttributeType.Name == "NullableAttribute" && ((byte)2).Equals(a.ConstructorArguments[0].Value));
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

        public static TypeReference ElementType(this TypeReference type)
        {
            if (!(type is GenericInstanceType gen))
                return null;

            if (type.FullName == typeof(string).FullName || type.FullName == typeof(byte[]).FullName)
                return null;

            var def = type.Resolve();
            if (def == null)
                return null;

            var ienum = AllInterfaces(def).SingleOrDefault(tr => tr is GenericInstanceType git && git.ElementType.Name == "IEnumerable`1");

            if (ienum == null)
                return null;

            return gen.GenericArguments.Single();
        }

        static string TypeScriptName(TypeReference type, TypeDefinition current, Options options, string errorContext)
        {
            var ut = type.UnNullify();
            if (ut != type)
                return TypeScriptNameInternal(ut, current, options, errorContext) + " | null";

            return TypeScriptNameInternal(type, current, options, errorContext);
        }

        private static string TypeScriptNameInternal(TypeReference type, TypeDefinition current, Options options, string errorContext)
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

            if (type.FullName == typeof(DateTime).FullName)
                return "string";

            if (type.FullName == typeof(Guid).FullName ||
                type.FullName == typeof(Byte[]).FullName ||
                type.FullName == typeof(TimeSpan).FullName)
                return "string";

            if (type.IsGenericParameter)
                return type.Name;

            if (type is GenericInstanceType git)
                return RelativeName(type.Resolve(), current, options, errorContext) + "<" + string.Join(", ", git.GenericArguments.Select(a => TypeScriptName(a, current, options, errorContext)).ToList()) + ">";
            else if (type.HasGenericParameters)
                return RelativeName(type.Resolve(), current, options, errorContext) + "<" + string.Join(", ", type.GenericParameters.Select(gp => gp.Name)) + ">";
            else if (type is ArrayType at)
                return TypeScriptName(at.ElementType, current, options, errorContext) + "[]";
            else
                return RelativeName(type.Resolve(), current, options, errorContext);
        }

        private static string RelativeName(TypeDefinition type, TypeDefinition current, Options options, string errorContext)
        {
            if (type.IsGenericParameter)
                return type.Name;

            if (type.DeclaringType != null)
                return RelativeName(type.DeclaringType, current, options, errorContext) + "_" + BaseTypeScriptName(type);

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
                var nsReference = GetNamespaceReference(options, type);
                if (nsReference == null)
                {
                    if (type.Interfaces.Any(i => i.InterfaceType.FullName == typeof(IEnumerable).FullName))
                        return "Array";

                    throw new InvalidOperationException($"{errorContext}:  Type {type.ToString()} is declared in the assembly '{type.Module.Assembly.Name}', but no React directory for it is found.");
                }

                return CombineNamespace(nsReference.VariableName, BaseTypeScriptName(type));
            }
        }

        public static IEnumerable<TypeReference> AllInterfaces(this TypeDefinition type)
        {
            return type.Interfaces.Select(a => a.InterfaceType).Concat(type.BaseType == null ? Enumerable.Empty<TypeReference>() : AllInterfaces(type.BaseType.Resolve()));
        }
        
        public static NamespaceTSReference GetNamespaceReference(Options options, TypeDefinition type)
        {
            AssemblyReference assemblyReference;
            options.AssemblyReferences.TryGetValue(type.Module.Assembly.Name.Name, out assemblyReference);
            if (assemblyReference == null)
                return null;

            NamespaceTSReference nsReference;
            if (!assemblyReference.NamespacesReferences.TryGetValue(type.Namespace, out nsReference))
            {
                nsReference = new NamespaceTSReference
                {
                    Namespace = type.Namespace,
                    Path = FindDeclarationsFile(assemblyReference, type.Namespace, type),
                    VariableName = GetVariableName(options, type.Namespace.Split('.'))
                };

                assemblyReference.NamespacesReferences.Add(nsReference.Namespace, nsReference);
            }

            return nsReference;
        }

        private static string GetVariableName(Options options, string[] nameParts)
        {
            var list = options.AssemblyReferences.Values.SelectMany(a => a.NamespacesReferences.Values.Select(ns => ns.VariableName));

            for (int i = 1; ; i++)
            {
                foreach (var item in nameParts.Reverse())
                {
                    var candidate = item + (i == 1 ? "" : i.ToString());
                    if (!list.Contains(candidate))
                        return candidate;
                }
            }
        }

        private static string FindDeclarationsFile(AssemblyReference assemblyReference, string @namespace, TypeDefinition typeForError)
        {
            var fileTS = @namespace + ".ts";

            var result = assemblyReference.AllTypescriptFiles.Where(a => Path.GetFileName(a) == fileTS).ToList();

            if (result.Count == 1)
                return result.Single();

            if (result.Count > 1)
                throw new InvalidOperationException($"importing '{typeForError}' required but multiple '{fileTS}' were found inside '{assemblyReference.ReactDirectory}':\r\n{string.Join("\r\n", result.Select(a => "  " + a).ToArray())}");

            var fileT4S = @namespace + ".t4s";

            result = assemblyReference.AllTypescriptFiles.Where(a => Path.GetFileName(a) == fileT4S).ToList();

            if (result.Count == 1)
                return result.Single().RemoveSuffix(".t4s") + ".ts";

            if (result.Count > 1)
                throw new InvalidOperationException($"importing '{typeForError}' required but multiple '{fileT4S}' were found inside '{assemblyReference.ReactDirectory}':\r\n{string.Join("\r\n", result.Select(a => "  " + a).ToArray())}");

            throw new InvalidOperationException($"importing '{typeForError}' required but no '{fileTS}' or '{fileT4S}' found inside '{assemblyReference.ReactDirectory}'");
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
    public class Options
    {
        public string CurrentAssembly;
        public string CurrentNamespace;

        public AssemblyReference CurrentAssemblyReference => AssemblyReferences.GetOrThrow(CurrentAssembly);

        public string TemplateFileName { get; internal set; }

        public Dictionary<string, AssemblyReference> AssemblyReferences;

        public Dictionary<string, string> AllReferences { get; internal set; }

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
        public string ReactDirectory;
        public string AssemblyFullPath;
        public string AssemblyName;

        public List<string> AllTypescriptFiles;

        public Dictionary<string, NamespaceTSReference> NamespacesReferences = new Dictionary<string, NamespaceTSReference>();
    }

    public class NamespaceTSReference
    {
        public string Namespace;
        public string Path;
        public string VariableName;
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
}
