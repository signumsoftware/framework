using Signum.Utilities;
using System;
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
            "System.Linq",
            "System.Reflection",
            "System.Collections.Generic",
            "System.Linq.Expressions",
            "Signum.Engine",
            "Signum.Entities",
            "Signum.Entities.Basics",
            "Signum.Engine.DynamicQuery",
            "Signum.Engine.Maps",
            "Signum.Engine.Basics",
            "Signum.Engine.Operations",
            "Signum.Engine.Workflow",
            "Signum.Utilities",
            "Signum.Engine.Authorization",
            "Signum.Engine.Notes",
            "Signum.Engine.Alerts",
            "Signum.Engine.Cache",
            "Signum.Engine.Chart",
            "Signum.Engine.Dashboard",
            "Signum.Engine.DiffLog",
            "Signum.Engine.Dynamic",
            "Signum.Engine.Excel",
            "Signum.Engine.Files",
            "Signum.Engine.Mailing",
            "Signum.Engine.Map",
            "Signum.Engine.Migrations",
            "Signum.Engine.Processes",
            "Signum.Engine.Profiler",
            "Signum.Engine.Scheduler",
            "Signum.Engine.Toolbar",
            "Signum.Engine.Translation",
            "Signum.Engine.UserQueries",
            "Signum.Engine.ViewLog",
            "Signum.Engine.Word",
            "Signum.Engine.Tree",
            "Signum.Entities.Authorization",
            "Signum.Entities.Notes",
            "Signum.Entities.Alerts",
            "Signum.Entities.Chart",
            "Signum.Entities.Dashboard",
            "Signum.Entities.Dynamic",
            "Signum.Entities.Excel",
            "Signum.Entities.Files",
            "Signum.Entities.Mailing",
            "Signum.Entities.Migrations",
            "Signum.Entities.Processes",
            "Signum.Entities.Scheduler",
            "Signum.Entities.Toolbar",
            "Signum.Entities.Translation",
            "Signum.Entities.UserQueries",
            "Signum.Entities.ViewLog",
            "Signum.Entities.Word",
            "Signum.Entities.Workflow",
            "Signum.Entities.Tree",
        };

        public static HashSet<Type> AssemblyTypes = new HashSet<Type>
        {   
            typeof(object),
            typeof(Csv), //  "Signum.Utilities.dll",
            typeof(Entity), //"Signum.Entities.dll",
            typeof(UserEntity), //"Signum.Entities.Extensions.dll",
            typeof(JsonConvert), //"Newtonsoft.Json.dll",
        };

        public static IEnumerable<MetadataReference> GetMetadataReferences()
        {
            return DynamicCode.AssemblyTypes
                .Select(type => MetadataReference.CreateFromFile(type.Assembly.Location))
                .And(MetadataReference.CreateFromFile(DynamicCode.CodeGenAssemblyPath))
                .NotNull()
                .EmptyIfNull();
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