using System.IO;
using Microsoft.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace Signum.Eval;

public static class EvalLogic
{
    public static string AssemblyDirectory = Path.GetDirectoryName(typeof(Entity).Assembly.Location)!;

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
        "System.ComponentModel",
        "System.Collections",
        "System.Collections.Generic",
        "System.Linq.Expressions",
        "Signum.Utilities",
        "Signum.Utilities.Reflection",
        "DocumentFormat.OpenXml",
        "DocumentFormat.OpenXml.Packaging",
    };

    public static HashSet<Type> AssemblyTypes = new HashSet<Type>
    {
        typeof(object),
        typeof(System.IO.File),
        typeof(System.Attribute),
        typeof(System.Runtime.AmbiguousImplementationException), 
        typeof(System.Linq.Enumerable),
        typeof(System.Linq.Queryable),
        typeof(System.Collections.Generic.List<>),
        typeof(System.ComponentModel.Component),
        typeof(System.ComponentModel.DescriptionAttribute),
        typeof(System.ComponentModel.IDataErrorInfo),
        typeof(System.ComponentModel.INotifyPropertyChanged),
        typeof(System.Net.HttpWebRequest),
        typeof(System.Linq.Expressions.Expression),
        typeof(Signum.Utilities.Csv), //  "Signum.Utilities.dll",
        typeof(JsonConverter), //"System.Text.Json.dll",
        typeof(DocumentFormat.OpenXml.AlternateContent), //"DocumentFormat.OpenXml.dll",
        typeof(System.Text.RegularExpressions.Regex),
    };

    public static Func<string?> GetCodeGenAssemblyPath = () => null; 

    public static IEnumerable<MetadataReference> GetMetadataReferences(bool needsCodeGenAssembly = true)
    {
        var result = EvalLogic.AssemblyTypes
                .Select(type => MetadataReference.CreateFromFile(type.Assembly.Location))
                .And(needsCodeGenAssembly ? GetCodeGenAssemblyPath()?.Let(s => MetadataReference.CreateFromFile(s)) : null)
                .NotNull().ToList();

        return result;
    }


    public static HashSet<string> CoreAssemblyNames = new HashSet<string>
    {
        "mscorlib.dll",
        "netstandard.dll",
        "System.Runtime.dll",
        "System.Collections.dll",
        "System.IO.dll",
    };

    public static IEnumerable<MetadataReference> GetCoreMetadataReferences()
    {
        string dd = typeof(Enumerable).GetType().Assembly.Location;
        var coreDir = Directory.GetParent(dd)!;

        return CoreAssemblyNames.Select(name => MetadataReference.CreateFromFile(Path.Combine(coreDir.FullName, name))).ToArray();
    }

    public static string GetUsingNamespaces()
    {
        return EvalLogic.CreateUsings(GetNamespaces());
    }

    public static IEnumerable<string> GetNamespaces()
    {
        return EvalLogic.Namespaces.NotNull();
    }

    public static string CreateUsings(IEnumerable<string> namespaces)
    {
        return namespaces.ToString(ns => "using {0};\n".FormatWith(ns), "");
    }

    public static Func<string, List<CustomCompilerError>> GetCustomErrors;

    public static Action OnInvalidated { get; internal set; }

    public static void AddFullAssembly(Type type)
    {
        Namespaces.AddRange(type.Assembly.ExportedTypes.Select(a => a.Namespace).NotNull());
        AssemblyTypes.Add(type);
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        PermissionLogic.RegisterPermissions(EvalPanelPermission.ViewDynamicPanel);

        if(sb.WebServerBuilder != null)
        {
            ReflectionServer.RegisterLike(typeof(EvalPanelPermission), () => EvalPanelPermission.ViewDynamicPanel.IsAuthorized());
        }    
    }
}

public class CustomCompilerError
{
    public int Line { get; set; }
    public string ErrorText { get; set; }
}
