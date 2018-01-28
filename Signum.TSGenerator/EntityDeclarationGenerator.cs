using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.TSGenerator
{
    public static class EntityDeclarationGenerator
    {
        public class TypeCache
        {
            public Type ModifiableEntity;
            public Type InTypeScriptAttribute;
            public Type ImportInTypeScriptAttribute;
            public Type IEntity;

            public TypeCache(Assembly entities)
            {
                ModifiableEntity = entities.GetType("Signum.Entities.ModifiableEntity", throwOnError: true);
                InTypeScriptAttribute = entities.GetType("Signum.Entities.InTypeScriptAttribute", throwOnError: true);
                ImportInTypeScriptAttribute = entities.GetType("Signum.Entities.ImportInTypeScriptAttribute", throwOnError: true);
                IEntity = entities.GetType("Signum.Entities.IEntity", throwOnError: true);
            }
        }

        static TypeCache Cache;

        public static string Process(Options options)
        {
            StringBuilder sb = new StringBuilder();

            var assembly = Assembly.LoadFrom(options.CurrentAssemblyReference.AssemblyFullPath);
            options.AssemblyReferences.Values.ToList().ForEach(r => Assembly.LoadFrom(r.AssemblyFullPath));

            var entities = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Signum.Entities");

            Cache = new TypeCache(entities);

            GetNamespaceReference(options, Cache.ModifiableEntity);

            var exportedTypes = assembly.ExportedTypes.Where(a => a.Namespace == options.CurrentNamespace).ToList();
            if (exportedTypes.Count == 0)
                throw new InvalidOperationException($"Assembly '{options.CurrentAssembly}' has not types in namespace '{options.CurrentNamespace}'");

            var imported = assembly.GetCustomAttributes(Cache.ImportInTypeScriptAttribute).Select(a => (Type)((dynamic)a).Type).ToList();
            var importedMessage = imported.Where(a => a.Name.EndsWith("Message")).ToList();
            var importedEnums = imported.Except(importedMessage).ToList();

            var entityResults = (from type in exportedTypes
                                 where type.IsClass && (type.InTypeScript() ?? Cache.ModifiableEntity.IsAssignableFrom(type))
                                 select new
                                 {
                                     ns = type.Namespace,
                                     type,
                                     text = EntityInTypeScript(type, options),
                                 }).ToList();

            var interfacesResults = (from type in exportedTypes
                                     where type.IsInterface &&
                                     (type.InTypeScript() ?? Cache.IEntity.IsAssignableFrom(type))
                                     select new
                                     {
                                         ns = type.Namespace,
                                         type,
                                         text = EntityInTypeScript(type, options),
                                     }).ToList();

            var usedEnums = (from type in entityResults.Select(a => a.type)
                             from p in GetProperties(type, declaredOnly: false)
                             let pt = (p.PropertyType.ElementType() ?? p.PropertyType).UnNullify()
                             where pt.IsEnum
                             select pt).Distinct().ToList();

            var symbolResults = (from type in exportedTypes
                                 where type.IsClass && type.IsStaticClass() && type.ContainsAttribute("AutoInitAttribute")
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

            var extrnalEnums = (from type in usedEnums.Where(options.IsExternal).Concat(importedEnums)
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
                .Concat(extrnalEnums)
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
                            sb.AppendLine("    " + line);
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

        private static string EnumInTypeScript(Type type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"export const {type.Name} = new EnumType<{type.Name}>(\"{type.Name}\");");

            sb.AppendLine($"export type {type.Name} =");
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            for (int i = 0; i < fields.Length; i++)
            {
                sb.Append($"    \"{fields[i].Name}\"");
                if (i < fields.Length - 1)
                    sb.AppendLine(" |");
                else
                    sb.AppendLine(";");
            }


            return sb.ToString();
        }

        private static string MessageInTypeScript(Type type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"export module {type.Name} {{");
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var field in fields)
            {
                string context = $"By type {type.Name} and field {field.Name}";
                sb.AppendLine($"    export const {field.Name} = new MessageKey(\"{type.Name}\", \"{field.Name}\");");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private static string QueryInTypeScript(Type type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"export module {type.Name} {{");
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var field in fields)
            {
                string context = $"By type {type.Name} and field {field.Name}";
                sb.AppendLine($"    export const {field.Name} = new QueryKey(\"{type.Name}\", \"{field.Name}\");");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private static string SymbolInTypeScript(Type type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"export module {type.Name} {{");
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (var field in fields)
            {
                string context = $"By type {type.Name} and field {field.Name}";
                var propertyType = TypeScriptName(field.FieldType, type, options, context);

                var cleanType = field.FieldType.IsInterface && field.FieldType.GetInterface("IOperationSymbolContainer") != null ? "Operation" : CleanTypeName(field.FieldType);
                sb.AppendLine($"    export const {field.Name} : {propertyType} = registerSymbol(\"{cleanType}\", \"{type.Name}.{field.Name}\");");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private static string EntityInTypeScript(Type type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            if (!type.IsAbstract)
                sb.AppendLine($"export const {type.Name} = new Type<{type.Name}>(\"{ CleanTypeName(type) }\");");

            List<string> baseTypes = new List<string>();
            if (type.BaseType != null)
                baseTypes.Add(TypeScriptName(type.BaseType, type, options, $"By type {type.Name}"));

            var interfaces = type.GetInterfaces();

            foreach (var i in type.GetInterfaces().Except(type.BaseType?.GetInterfaces() ?? Enumerable.Empty<Type>()).Where(i => Cache.IEntity.IsAssignableFrom(i)))
                baseTypes.Add(TypeScriptName(i, type, options, $"By type {type.Name}"));

            sb.AppendLine($"export interface {TypeScriptName(type, type, options, "declaring " + type.Name)} extends {string.Join(", ", baseTypes)} {{");
            if (!type.IsAbstract && Parents(type.BaseType).All(a => a.IsAbstract))
                sb.AppendLine($"    Type: \"{CleanTypeName(type)}\";");

            var properties = GetProperties(type, declaredOnly: true);

            foreach (var prop in properties)
            {
                string context = $"By type {type.Name} and property {prop.Name}";
                var propertyType = TypeScriptName(prop.PropertyType, type, options, context) +
                    (prop.GetTypescriptNull() ? " | null" : "");

                var undefined = prop.GetTypescriptUndefined() ? "?" : "";

                sb.AppendLine($"    {FirstLower(prop.Name)}{undefined}: {propertyType};");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private static IEnumerable<Type> Parents(Type type)
        {
            while (type != Cache.ModifiableEntity && type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }

        static string CleanTypeName(Type t)
        {
            if (!Cache.IEntity.IsAssignableFrom(t))
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

        private static IEnumerable<PropertyInfo> GetProperties(Type type, bool declaredOnly)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | (declaredOnly ? BindingFlags.DeclaredOnly : 0))
                            .Where(p => (p.InTypeScript() ?? !(p.ContainsAttribute("HiddenPropertyAttribute") || p.ContainsAttribute("ExpressionFieldAttribute"))));
        }

        public static bool ContainsAttribute(this MemberInfo p, string attributeName)
        {
            return p.GetCustomAttributes().Any(a => a.GetType().Name == attributeName);
        }

        public static bool? InTypeScript(this MemberInfo t)
        {
            var attr = t.GetCustomAttribute(Cache.InTypeScriptAttribute, inherit: false);

            if (attr == null)
                return null;

            return (bool?)((dynamic)attr).GetInTypeScript();
        }

        public static bool GetTypescriptUndefined(this PropertyInfo p)
        {
            var attr = p.GetCustomAttribute(Cache.InTypeScriptAttribute, inherit: false);

            var b = attr == null ? null : (bool?)((dynamic)attr).GetUndefined();

            if (b != null)
                return b.Value;

            if (IsCollection(p.PropertyType))
                return false;

            return GetTypescriptUndefined(p.DeclaringType) ?? true;
        }

        private static bool? GetTypescriptUndefined(Type declaringType)
        {
            var attr = declaringType.GetCustomAttribute(Cache.InTypeScriptAttribute, inherit: true);

            return attr == null ? null : (bool?)((dynamic)attr).GetUndefined();
        }

        public static bool GetTypescriptNull(this PropertyInfo p)
        {
            var attr = p.GetCustomAttribute(Cache.InTypeScriptAttribute, inherit: false);

            var b = attr == null ? null : (bool?)((dynamic)attr).GetNull();
            if (b != null)
                return b.Value;

            if (IsCollection(p.PropertyType))
                return false;

            if (GetTypescriptUndefined(p.DeclaringType) == false &&
                p.CustomAttributes.Any(a =>
                a.AttributeType.Name == "NotNullableAttribute" ||
                a.AttributeType.Name == "NotNullValidatorAttribute" ||
                a.AttributeType.Name == "StringLengthValidatorAttribute" && a.NamedArguments.Any(na => na.MemberName == "AllowNulls" && false.Equals(na.TypedValue.Value))))
                return false;

            return p.PropertyType.IsClass || p.PropertyType.IsInterface || Nullable.GetUnderlyingType(p.PropertyType) != null;
        }

        private static string FirstLower(string name)
        {
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        public static Type UnNullify(this Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        private static string TypeScriptName(Type type, Type current, Options options, string errorContext)
        {
            type = type.UnNullify();


            if (!type.IsEnum)
            {
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
                    case TypeCode.Decimal:
                    case TypeCode.Single:
                    case TypeCode.Double: return "number";
                    case TypeCode.String: return "string";
                    case TypeCode.DateTime: return "string";
                }
            }

            if (type.FullName == "System.Guid" || type.FullName == "System.Byte[]" || type.FullName == "System.TimeSpan")
                return "string";

            var relativeName = RelativeName(type, current, options, errorContext);

            if (!type.IsGenericType)
                return relativeName;

            else
                return relativeName + "<" + string.Join(", ", type.GetGenericArguments().Select(a => TypeScriptName(a, current, options, errorContext)).ToList()) + ">";
        }

        private static string RelativeName(Type type, Type current, Options options, string errorContext)
        {
            if (type.IsGenericParameter)
                return type.Name;

            if (type.DeclaringType != null)
                return RelativeName(type.DeclaringType, current, options, errorContext) + "_" + BaseTypeScriptName(type);

            if (type.Assembly.Equals(current.Assembly) && type.Namespace == current.Namespace)
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
                    if (type.GetInterfaces().Contains(typeof(IEnumerable)))
                        return "Array";

                    throw new InvalidOperationException($"{errorContext}:  Type {type.ToString()} is declared in the assembly '{type.Assembly.GetName().Name}.dll', but no React directory for it is found.");
                }

                return CombineNamespace(nsReference.VariableName, BaseTypeScriptName(type));
            }
        }

        public static bool IsCollection(this Type type)
        {
            return type != typeof(byte[]) && type != typeof(string) && type.GetInterfaces().Contains(typeof(IEnumerable));
        }
        public static Type ElementType(this Type ft)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(ft))
                return null;

            var ie = ft.GetInterfaces().FirstOrDefault(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (ie == null)
                return null;
            return ie.GetGenericArguments()[0];
        }

        public static NamespaceTSReference GetNamespaceReference(Options options, Type type)
        {
            AssemblyReference assemblyReference;
            options.AssemblyReferences.TryGetValue(type.Assembly.GetName().Name, out assemblyReference);
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

        private static string FindDeclarationsFile(AssemblyReference assemblyReference, string @namespace, Type typeForError)
        {
            var fileTS = @namespace + ".ts";

            var result = assemblyReference.AllTypescriptFiles.Where(a => Path.GetFileName(a) == fileTS).ToList();

            if (result.Count == 1)
                return result.Single();

            if (result.Count > 1)
                throw new InvalidOperationException($"importing '{typeForError}' required but multiple '{fileTS}' were found inside '{assemblyReference.ReactDirectory}':\r\n{string.Join("\r\n", result.Select(a => "    " + a).ToArray())}");

            var fileT4S = @namespace + ".t4s";

            result = assemblyReference.AllTypescriptFiles.Where(a => Path.GetFileName(a) == fileT4S).ToList();

            if (result.Count == 1)
                return result.Single().RemoveSuffix(".t4s") + ".ts";

            if (result.Count > 1)
                throw new InvalidOperationException($"importing '{typeForError}' required but multiple '{fileT4S}' were found inside '{assemblyReference.ReactDirectory}':\r\n{string.Join("\r\n", result.Select(a => "    " + a).ToArray())}");

            throw new InvalidOperationException($"importing '{typeForError}' required but no '{fileTS}' or '{fileT4S}' found inside '{assemblyReference.ReactDirectory}'");
        }

        private static string BaseTypeScriptName(Type type)
        {
            if (type == Cache.IEntity)
                return "Entity";

            var name = type.Name;

            int pos = name.IndexOf('`');

            if (pos == -1)
                return name;

            return name.Substring(0, pos);
        }

        private static string RelativeNamespace(Type referedType, Type current)
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

        public static bool IsStaticClass(this Type type)
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


        public bool IsExternal(Type type)
        {
            return type.Assembly.GetName().Name != CurrentAssembly &&
                 !AssemblyReferences.ContainsKey(type.Assembly.GetName().Name);
        }
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
