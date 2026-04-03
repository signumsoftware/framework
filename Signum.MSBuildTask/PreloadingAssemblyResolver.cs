using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.MSBuildTask;

internal class PreloadingAssemblyResolver : DefaultAssemblyResolver
{
    public AssemblyDefinition SystemRuntime { get; private set; }
    public AssemblyDefinition SystemLinqExpressions { get; private set; }
    public AssemblyDefinition SignumUtilities { get; private set; }
    public AssemblyDefinition Signum { get; private set; }

    Dictionary<string, string> assemblyLocations;

    public PreloadingAssemblyResolver(string[] references)
    {
        this.assemblyLocations = references.ToDictionary(a => Path.GetFileNameWithoutExtension(a));

        foreach (var dll in references.Where(r => r.Contains("Signum") || r.Contains("System.Runtime") || r.Contains("System.Linq.Expressions")))
        {
            var assembly = ModuleDefinition.ReadModule(dll, new ReaderParameters { AssemblyResolver = this }).Assembly;

            if (assembly.Name.Name == "System.Runtime")
                SystemRuntime = assembly;

            if (assembly.Name.Name == "System.Linq.Expressions")
                SystemLinqExpressions = assembly;

            if (assembly.Name.Name == "Signum")
                Signum = assembly;

            if (assembly.Name.Name == "Signum.Utilities")
                SignumUtilities = assembly;

            RegisterAssembly(assembly);
        }
    }


    public override AssemblyDefinition Resolve(AssemblyNameReference name)
    {
        var assembly = ModuleDefinition.ReadModule(this.assemblyLocations[name.Name], new ReaderParameters { AssemblyResolver = this }).Assembly;

        this.RegisterAssembly(assembly);

        return assembly;
    }
}
