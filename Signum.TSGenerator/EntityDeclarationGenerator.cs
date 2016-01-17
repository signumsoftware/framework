using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

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

        public static string Process(StringBuilder sb, Options options)
        {
            var assembly = Assembly.LoadFrom(options.AssemblyFullPath);
            options.References.ForEach(r => Assembly.LoadFrom(r.AssemblyFullPath));

            var entities = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Signum.Entities");

            Cache = new TypeCache(entities);

            var exportedTypes = assembly.ExportedTypes.Where(a => a.Namespace.StartsWith(options.BaseNamespace)).ToList();
            var imported = assembly.GetCustomAttributes(Cache.ImportInTypeScriptAttribute).Select(a => (Type)((dynamic)a).Type).ToList();
            
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
                             let pt = p.PropertyType.UnNullify()
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
                              where type.IsEnum && (type.InTypeScript() ??  usedEnums.Contains(type))
                              select new
                              {
                                  ns = type.Namespace,
                                  type,
                                  text = EnumInTypeScript(type, options),
                              }).ToList();

            var extrnalEnums = (from type in usedEnums.Concat(imported)
                                where options.IsExternal(type)
                                select new
                                {
                                    ns = options.BaseNamespace + ".External",
                                    type,
                                    text = EnumInTypeScript(type, options),
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
                                   text = MessageInTypeScript(type, options),
                               }).ToList();

            var namespaces = entityResults
                .Concat(interfacesResults)
                .Concat(enumResult)
                .Concat(messageResults)
                .Concat(queryResult)
                .Concat(symbolResults)
                .Concat(extrnalEnums)
                .GroupBy(a => a.ns)
                .OrderBy(a => a.Key);


            foreach (var ns in namespaces)
            {
                var key = RemoveNamespace(ns.Key.ToString(), options.BaseNamespace);

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

            return sb.ToString();
        }


        private static string EnumInTypeScript(Type type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"export enum {type.Name} {{");

            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (var field in fields)
            {
                string context = $"By type {type.Name} and field {field.Name}";
                sb.AppendLine($"    {field.Name} = \"{field.Name}\" as any,");
            }
            sb.AppendLine(@"}");
            sb.AppendLine($"export const {type.Name}_Type = new EnumType<{type.Name}>(\"{type.Name}\", {type.Name});");

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
                sb.AppendLine($"    export const {field.Name} : {propertyType} = registerSymbol({{ Type: \"{cleanType}\", key: \"{type.Name}.{field.Name}\" }});");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private static string EntityInTypeScript(Type type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            if (!type.IsAbstract)
                sb.AppendLine($"export const {type.Name}_Type = new Type<{type.Name}>(\"{ CleanTypeName(type) }\");");

            List<string> baseTypes = new List<string>();
            if (type.BaseType != null)
                baseTypes.Add(TypeScriptName(type.BaseType, type, options, $"By type {type.Name}"));

            var interfaces = type.GetInterfaces();

            foreach (var i in type.GetInterfaces().Except(type.BaseType?.GetInterfaces() ?? Enumerable.Empty<Type>()).Where(i => Cache.IEntity.IsAssignableFrom(i)))
                baseTypes.Add(TypeScriptName(i, type, options, $"By type {type.Name}"));
            
            sb.AppendLine($"export interface {TypeScriptName(type, type, options, "declaring " + type.Name)} extends {string.Join(", ", baseTypes)} {{");

            var properties = GetProperties(type, declaredOnly: true);

            foreach (var prop in properties)
            {
                string context = $"By type {type.Name} and property {prop.Name}";
                var propertyType = TypeScriptName(prop.PropertyType, type, options, context);
                sb.AppendLine($"    {FirstLower(prop.Name)}?: {propertyType};");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        static string CleanTypeName(Type t)
        {
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

            return (bool)((dynamic)attr).InTypeScript;
        }

        private static string FirstLower(string name)
        {
            return char.ToLower(name[0]) + name.Substring(1);
        }
        
        private static string BeforeTick(string relativeName)
        {
            int pos = relativeName.IndexOf('`');

            if (pos == -1)
                return relativeName;

            return relativeName.Substring(0, pos);
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

            if (type.FullName == "System.Guid" || type.FullName == "System.Byte[]")
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
                return RelativeName(type.DeclaringType, current, options, errorContext) + "_" + BeforeTick(type.Name);

            if (type.Assembly.Equals(current.Assembly))
            {
                string relativeNamespace = RelativeNamespace(type, current);

                return CombineNamespace(relativeNamespace, BeforeTick(type.Name));
            }
            else if (type.IsEnum && options.IsExternal(type))
            {
                return "External." + BeforeTick(type.Name);
            }
            else
            {
                var assembly = options.References.SingleOrDefault(r => r.AssemblyName == type.Assembly.GetName().Name && type.Namespace.StartsWith(r.BaseNamespace));

                if (assembly == null)
                {
                    if (type.GetInterfaces().Contains(typeof(IEnumerable)))
                        return "Array";

                    throw new InvalidOperationException($"{errorContext}:  Type {type.ToString()} is declared in the assembly '{type.Assembly.GetName().Name}.dll' and namespace '{type.Namespace}', but there is no reference to them.");
                }

                var ns = RemoveNamespace(type.Namespace, assembly.BaseNamespace);

                return CombineNamespace(assembly.VariableName, ns, BeforeTick(type.Name));
            }
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
        public string AssemblyName;
        public string AssemblyFullPath;
        
        public string BaseNamespace;
        public List<Reference> References = new List<Reference>();

        public Options(string assemblyFullPath)
        {
            this.AssemblyFullPath = assemblyFullPath;
            this.AssemblyName = Path.GetFileNameWithoutExtension(assemblyFullPath);
        }

        internal bool IsExternal(Type type)
        {
            return type.Assembly.GetName().Name != AssemblyName &&
                 !References.Any(r => type.Assembly.GetName().Name == r.AssemblyName);
        }
    }

    [Serializable]
    public class Reference
    {
        public string VariableName;
        
        string assemblyFullPath;
        public string AssemblyFullPath
        {
            get { return assemblyFullPath; }
            set
            {
                assemblyFullPath = value;
                if (assemblyFullPath != null)
                    this.AssemblyName = Path.GetFileNameWithoutExtension(assemblyFullPath);
            }
        }

        public Match Match { get; internal set; }

        public string AssemblyName;
        public string BaseNamespace;
    }
}
