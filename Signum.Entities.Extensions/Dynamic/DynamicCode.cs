using System;
using Signum.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Signum.Entities.Dynamic
{
    public static class DynamicCode
    {
        public static string AssemblyDirectory = Path.GetDirectoryName(new Uri(typeof(Entity).Assembly.CodeBase).LocalPath);
        public static string CodeGenEntitiesNamespace = "Signum.Entities.CodeGen";
        public static string CodeGenDirectory = "CodeGen";
        public static string CodeGenAssembly = "CodeGenAssembly.dll";
        public static string CodeGenGeneratedAssembly = "CodeGenAssembly.dll";
        public static string CodeGenAssemblyPath;
        public static Action OnApplicationServerRestarted;

        public static HashSet<Type> RegisteredDynamicTypes = new HashSet<Type>();
        public static HashSet<string> Namespaces = new HashSet<string>
        {
            "System",
            "System.IO",
            "System.Globalization",
            "System.Net",
            "System.Text",
            "System.Linq",
            "System.Reflection",
            "System.Collections",
            "System.Collections.Generic",
            "System.Linq.Expressions",
            "Signum.Utilities",
            "DocumentFormat.OpenXml",
            "DocumentFormat.OpenXml.Packaging",
            "DocumentFormat.OpenXml.Spreadsheet",
        };

        public static HashSet<Type> AssemblyTypes = new HashSet<Type>
        {
            typeof(object),
            typeof(System.Attribute),
            typeof(System.Runtime.ConstrainedExecution.PrePrepareMethodAttribute), 
            typeof(System.Linq.Enumerable),
            typeof(System.Linq.Queryable),
            typeof(System.Collections.Generic.List<>),
            typeof(System.ComponentModel.Component),
            typeof(System.ComponentModel.IDataErrorInfo),
            typeof(System.ComponentModel.INotifyPropertyChanged),
            typeof(System.Net.HttpWebRequest),
            typeof(System.Linq.Expressions.Expression),
            typeof(Signum.Utilities.Csv), //  "Signum.Utilities.dll",
            typeof(Newtonsoft.Json.JsonConvert), //"Newtonsoft.Json.dll",
            typeof(DocumentFormat.OpenXml.AlternateContent), //"DocumentFormat.OpenXml.dll",
            typeof(System.Text.RegularExpressions.Regex),
        };

        public static IEnumerable<MetadataReference> GetMetadataReferences(bool needsCodeGenAssembly = true)
        {
            var result = DynamicCode.AssemblyTypes
                .Select(type => MetadataReference.CreateFromFile(type.Assembly.Location))
                .And(needsCodeGenAssembly ? DynamicCode.CodeGenAssemblyPath?.Let(s => MetadataReference.CreateFromFile(s)) : null)
                .NotNull().ToList();

            return result;
        }


        public static HashSet<string> CoreAssemblyNames = new HashSet<string>
        {
            "mscorlib.dll",
            "System.Runtime.dll",
            "System.Collections.dll",
            "System.IO.dll",
        };

        public static IEnumerable<MetadataReference> GetCoreMetadataReferences()
        {
            string dd = typeof(Enumerable).GetType().Assembly.Location;
            var coreDir = Directory.GetParent(dd);

            return CoreAssemblyNames.Select(name => MetadataReference.CreateFromFile(Path.Combine(coreDir.FullName, name))).ToArray();
        }

        public static string GetUsingNamespaces()
        {
            return DynamicCode.CreateUsings(GetNamespaces());
        }

        public static IEnumerable<string> GetNamespaces()
        {
            return DynamicCode.Namespaces
                .And(DynamicCode.CodeGenAssemblyPath == null ? null : DynamicCode.CodeGenEntitiesNamespace)
                .NotNull();
        }

        public static string CreateUsings(IEnumerable<string> namespaces)
        {
            return namespaces.ToString(ns => "using {0};\r\n".FormatWith(ns), "");
        }

        public static Func<string, List<CustomCompilerError>> GetCustomErrors;

        public static Action OnInvalidated { get; internal set; }

        public static void AddFullAssembly(Type type)
        {
            Namespaces.AddRange(type.Assembly.ExportedTypes.Select(a => a.Namespace));
            AssemblyTypes.Add(type);
        }
    }

    public class CustomCompilerError
    {
        public int Line { get; set; }
        public string ErrorText { get; set; }
    }
}
