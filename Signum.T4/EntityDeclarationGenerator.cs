using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace Signum.T4
{
    public static class EntityDeclarationGenerator
    {
        const string IEntity = "IEntity";

        public static string Process(Options options)
        {
            if (options.ProjectFile.EndsWith("Signum.Entities.csproj"))
                options.EntitiesVariable = "";
            else
                options.EntitiesVariable = options.References.Single(r => r.Assembly == "Signum.Entities").VariableName + ".";

            var proj = MSBuildWorkspace.Create().OpenProjectAsync(options.ProjectFile).Result;


            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Auto-generated from " + Path.GetFileName(options.ProjectFile) + ". Do not modify!");
            sb.AppendLine();
            foreach (var item in options.References)
                sb.AppendLine($"import * as {item.VariableName} from '{item.Path}'");
            sb.AppendLine();
            
            var allSymbol = (from d in proj.Documents
                             where d.SupportsSyntaxTree
                             let tree = d.GetSyntaxTreeAsync().Result
                             let sm = d.GetSemanticModelAsync().Result
                             from dec in tree.GetRoot().DescendantNodes(n => !(n is BaseTypeDeclarationSyntax)).OfType<BaseTypeDeclarationSyntax>()
                             let symbol = sm.GetDeclaredSymbol(dec)
                             select symbol).ToList();

            var entityResults = (from type in allSymbol
                                 where type.TypeKind == TypeKind.Class &&
                                 (type.InTypeScript() ?? type.Is("ModifiableEntity"))
                                 select new
                                 {
                                     ns = type.ContainingNamespace.ToString(),
                                     type,
                                     text = EntityInTypeScript(type, options),
                                 }).ToList();

            var interfacesResults = (from type in allSymbol
                                     where type.TypeKind == TypeKind.Interface && 
                                     (type.InTypeScript() ?? type.AllInterfaces.Any(i => i.Name == IEntity))
                                     select new
                                     {
                                         ns = type.ContainingNamespace.ToString(),
                                         type,
                                         text = EntityInTypeScript(type, options),
                                     }).ToList();

            var usedEnums = (from type in entityResults.Select(a => a.type)
                             from p in type.GetMembers()
                             where p.Kind == SymbolKind.Property && (p.DeclaredAccessibility & Accessibility.Public) == Accessibility.Public
                             let t = ((IPropertySymbol)p).Type.UnNullify()
                             where t.TypeKind == TypeKind.Enum
                             select (INamedTypeSymbol)t).Distinct().ToList();

            var symbolResults = (from type in allSymbol
                                 where type.TypeKind == TypeKind.Class && type.IsStatic && type.ContainsAttribute("AutoInitAttribute") 
                                 && (type.InTypeScript() ?? true)
                                 select new
                                 {
                                     ns = type.ContainingNamespace.ToString(),
                                     type,
                                     text = SymbolInTypeScript(type, options),
                                 }).ToList();

            var enumResult = (from type in allSymbol
                              where type.TypeKind == TypeKind.Enum &&
                              type.ContainingAssembly.Name == options.Assembly &&
                              (type.InTypeScript() ?? usedEnums.Contains(type))
                              select new
                              {
                                  ns = type.ContainingNamespace.ToString(),
                                  type,
                                  text = EnumInTypeScript(type, options),
                              }).ToList();

            var extrnalEnums = (from type in usedEnums
                                where options.IsExternal(type)
                                select new
                                {
                                    ns = options.BaseNamespace + ".External",
                                    type,
                                    text = EnumInTypeScript(type, options),
                                }).ToList();

            var messageResults = (from type in allSymbol
                                  where type.TypeKind == TypeKind.Enum && type.Name.EndsWith("Message")
                                  select new
                                  {
                                      ns = type.ContainingNamespace.ToString(),
                                      type,
                                      text = MessageInTypeScript(type, options),
                                  }).ToList();

            var namespaces = entityResults
                .Concat(interfacesResults)
                .Concat(enumResult)
                .Concat(messageResults)
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

        private static string EnumInTypeScript(INamedTypeSymbol type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"export enum {type.Name} {{");
            
            var fields = type.GetMembers().Where(a => a.Kind == SymbolKind.Field && a.IsStatic &&
            (a.DeclaredAccessibility & Accessibility.Public) == Accessibility.Public);

            long value = 0;
            foreach (IFieldSymbol field in fields)
            {
                string context = $"By type {type.Name} and field {field.Name}";

                var constantValue = Convert.ToInt64(field.ConstantValue);

                if (value == constantValue)
                    sb.AppendLine($"    {field.Name},");
                else
                    sb.AppendLine($"    {field.Name} = {constantValue},");

                value = constantValue + 1;
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private static string MessageInTypeScript(INamedTypeSymbol type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"export module {type.Name} {{");
            var fields = type.GetMembers().Where(a => a.Kind == SymbolKind.Field &&
            (a.DeclaredAccessibility & Accessibility.Public) == Accessibility.Public);

            foreach (IFieldSymbol field in fields)
            {
                string context = $"By type {type.Name} and field {field.Name}";
                sb.AppendLine($"    export const {field.Name} = \"{type.Name}.{field.Name}\"");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private static string SymbolInTypeScript(INamedTypeSymbol type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"export module {type.Name} {{");
            var fields = type.GetMembers().Where(a => a.Kind == SymbolKind.Field && 
            (a.DeclaredAccessibility & Accessibility.Public) == Accessibility.Public);

            foreach (IFieldSymbol field in fields)
            {
                string context = $"By type {type.Name} and field {field.Name}";
                var propertyType = GetTypescriptType(field.Type, type, options, context);
                sb.AppendLine($"    export const {field.Name} : {propertyType} = {{ key: \"{type.Name}.{field.Name}\" }};");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }

        private static string EntityInTypeScript(INamedTypeSymbol type, Options options)
        {
            StringBuilder sb = new StringBuilder();
            if (!type.IsAbstract)
                sb.AppendLine($"export const {type.Name}: {options.EntitiesVariable}Type<{type.Name}> = \"{type.Name}\";");

            List<string> baseTypes = new List<string>();
            if (type.BaseType != null)
                baseTypes.Add(RelativeName(type.BaseType, type, options, $"By type {type.Name}"));

            foreach (var i in type.Interfaces.Where(i => i.Name == IEntity || i.AllInterfaces.Any(a => a.Name == IEntity)))
                baseTypes.Add(RelativeName(i, type, options, $"By type {type.Name}"));

            sb.AppendLine($"export interface {type.Name} extends {string.Join(", ", baseTypes)} {{");

            var properties = type.GetMembers()
                .Where(p => p.Kind == SymbolKind.Property &&
                p.DeclaredAccessibility.HasFlag(Accessibility.Public) &&
                !p.IsStatic && (p.InTypeScript() ?? !(p.ContainsAttribute("HiddenPropertyAttribute") || p.ContainsAttribute("ExpressionFieldAttribute"))));

            foreach (IPropertySymbol prop in properties)
            {
                string context = $"By type {type.Name} and property {prop.Name}";
                var propertyType = GetTypescriptType(prop.Type, type, options, context);
                sb.AppendLine($"    {FirstLower(prop.Name)}?: {propertyType};");
            }
            sb.AppendLine(@"}");

            return sb.ToString();
        }
        
        public static bool ContainsAttribute(this ISymbol p, string attributeName)
        {
            return p.GetAttributes().Any(a => a.AttributeClass.Name == attributeName);
        }

        public static AttributeData GetAttribute(this ISymbol p, string attributeName)
        {
            return p.GetAttributes().SingleOrDefault(a => a.AttributeClass.Name == attributeName);
        }

        public static bool? InTypeScript(this ISymbol p)
        {
            return (bool?)p.GetAttribute("InTypeScriptAttribute")?.ConstructorArguments.SingleOrDefault().Value;
        }

        private static string FirstLower(string name)
        {
            return char.ToLower(name[0]) + name.Substring(1);
        }

        private static string GetTypescriptType(ITypeSymbol type, INamedTypeSymbol s, Options options, string errorContext)
        {
            type = type.UnNullify();

            switch (type.SpecialType)
            {
                case SpecialType.System_Enum: return RelativeName((INamedTypeSymbol)type, s, options, errorContext);
                case SpecialType.System_Boolean: return "boolean";
                case SpecialType.System_Char: return "string";
                case SpecialType.System_SByte: 
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double: return "number";
                case SpecialType.System_String: return "string";
                case SpecialType.System_Nullable_T: return GetTypescriptType(((INamedTypeSymbol)type).TypeArguments.Single(), s, options, errorContext);
                case SpecialType.System_DateTime: return "string";
                case SpecialType.System_Object: return "any";
            }

            var named = type as INamedTypeSymbol;

            if (named == null)
                return "any";

            if (named.ToString() == "System.Guid")
                return "string";

            if (named.IsGenericType)
            {
                if (named.ContainingType != null)
                {
                    if (named.ContainingType.Name == "ConstructSymbol")
                    {
                        var relativeName = RelativeName(named.ContainingType, s, options, errorContext);

                        var types = named.ContainingType.TypeArguments.Select(t => GetTypescriptType(t, s, options, errorContext))
                            .Concat(named.TypeArguments.Select(t => GetTypescriptType(t, s, options, errorContext))).ToList();

                        return relativeName + "_" + named.Name + "<" + string.Join(", ", types) + ">";
                    }
                }
                else
                {
                    var name = RelativeName(named.ConstructedFrom, s, options, errorContext);

                    var types = named.TypeArguments.Select(t => GetTypescriptType(t, s, options, errorContext)).ToList();

                    return name + "<" + string.Join(", ", types) + ">";
                }
            }
       
            return RelativeName(named, s, options, errorContext);
        }

        public static ITypeSymbol UnNullify(this ITypeSymbol type)
        {
            var named = type as INamedTypeSymbol;

            if (named != null && named.IsGenericType && named.ConstructedFrom.ToString() == "System.Nullable<T>")
                return named.TypeArguments.Single();

            return type;
        } 

        private static string RelativeName(INamedTypeSymbol referedType, INamedTypeSymbol current, Options options, string errorContext)
        {
            if (referedType.ContainingType != null)
                throw new InvalidOperationException($"{errorContext}: Nested type {referedType} not supported");

            if (referedType.ContainingAssembly.Equals(current.ContainingAssembly))
            {
                string relativeNamespace = RelativeNamespace(referedType, current);
                
                return CombineNamespace(relativeNamespace, referedType.Name);
            }
            else if(referedType.TypeKind == TypeKind.Enum && options.IsExternal(referedType))
            {
                return "External." + referedType.Name;
            }
            else
            {
                var assembly = options.References.SingleOrDefault(r => r.Assembly == referedType.ContainingAssembly.Name);

                if (assembly == null)
                {
                    if (referedType.AllInterfaces.Any(a => a.ToString() == "System.Collections.Generic.IEnumerable<T>"))
                        return "Array";

                    throw new InvalidOperationException($"{errorContext}:  Type {referedType.ToString()} is declared in the assembly '{referedType.ContainingAssembly.Name}' buy the assembly is not refered");
                }

                var ns = RemoveNamespace(referedType.ContainingNamespace.ToString(), assembly.BaseNamespace);

                return CombineNamespace(assembly.VariableName, ns, referedType.Name);
            }
        }

        private static string RelativeNamespace(INamedTypeSymbol referedType, INamedTypeSymbol current)
        {
            var referedNS = referedType.ContainingNamespace.ToString().Split('.').ToList();
            var currentNS = current.ContainingNamespace.ToString().Split('.').ToList();

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


        static bool Is(this INamedTypeSymbol s, string name)
        {
            if (s.Name == name)
                return true;

            if (s.BaseType == null)
                return false;

            return Is(s.BaseType, name);
        }
    }

    public class Options
    {
        string projectFile; 
        public string ProjectFile
        {
            get { return projectFile; }
            set
            {
                projectFile = value;

                if (Assembly == null)
                    Assembly = Path.GetFileNameWithoutExtension(projectFile);


                if (BaseNamespace == null)
                    BaseNamespace = Path.GetFileNameWithoutExtension(projectFile);
            }
        }

        public string Assembly; 
        public string BaseNamespace;
        public List<Reference> References = new List<Reference>();

        internal string EntitiesVariable;

        internal bool IsExternal(INamedTypeSymbol type)
        {
            return type.ContainingAssembly.Name != Assembly &&
                 !References.Any(r => type.ContainingAssembly.Name == r.Assembly);
        }
    }

    public class Reference
    {
        public string Assembly;
        public string BaseNamespace;
        public string VariableName;
        public string Path;
    }
}
