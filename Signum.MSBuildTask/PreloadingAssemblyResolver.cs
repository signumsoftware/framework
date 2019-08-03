using Mono.Cecil;
using System.Linq.Expressions;

namespace Signum.MSBuildTask
{
    internal class PreloadingAssemblyResolver : DefaultAssemblyResolver
    {
        public AssemblyDefinition SystemRuntime { get; private set; }
        public AssemblyDefinition SystemLinqExpressions { get; private set; }
        public AssemblyDefinition SignumUtilities { get; private set; }
        public AssemblyDefinition SignumEntities { get; private set; }

        public PreloadingAssemblyResolver(string[] references)
        {
            foreach (var dll in references)
            {
                var assembly = ModuleDefinition.ReadModule(dll, new ReaderParameters { AssemblyResolver = this }).Assembly;

                if (assembly.Name.Name == "System.Runtime")
                    SystemRuntime = assembly;

                if (assembly.Name.Name == "System.Linq.Expressions")
                    SystemLinqExpressions = assembly;

                if (assembly.Name.Name == "Signum.Entities")
                    SignumEntities = assembly;

                if (assembly.Name.Name == "Signum.Utilities")
                    SignumUtilities = assembly;

                RegisterAssembly(assembly);
            }
        }
    }
}
