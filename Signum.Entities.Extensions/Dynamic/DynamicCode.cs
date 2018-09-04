using System;
using Signum.Utilities;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Signum.Entities.Authorization;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis;

namespace Signum.Entities.Dynamic
{
    public static class DynamicCode
    {
        public static string AssemblyDirectory = Path.GetDirectoryName(new Uri(typeof(Entity).Assembly.CodeBase).LocalPath);
        public static string CodeGenEntitiesNamespace = "Signum.Entities.CodeGen";
        public static string CodeGenDirectory = "CodeGen";
        public static string CodeGenAssembly = "CodeGenAssymbly.dll";
        public static string CodeGenAssemblyPath;

        public static HashSet<string> Namespaces = new HashSet<string>
        {
            "System",
            "System.IO",
            "System.Linq",
            "System.Reflection",
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
            typeof(System.ComponentModel.IDataErrorInfo),
            typeof(System.ComponentModel.INotifyPropertyChanged),
            typeof(System.Linq.Expressions.Expression),
            typeof(Signum.Utilities.Csv), //  "Signum.Utilities.dll",
            typeof(Newtonsoft.Json.JsonConvert), //"Newtonsoft.Json.dll",
        };

        public static IEnumerable<MetadataReference> GetMetadataReferences()
        {
            return DynamicCode.AssemblyTypes
                .Select(type => MetadataReference.CreateFromFile(type.Assembly.Location))
                .And(DynamicCode.CodeGenAssemblyPath?.Let(s => MetadataReference.CreateFromFile(s)))
                .NotNull();
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