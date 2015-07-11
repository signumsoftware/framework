using Mono.Cecil;

namespace Signum.MSBuildTask
{
    internal class PreloadingAssemblyResolver : DefaultAssemblyResolver
    {
        public AssemblyDefinition SignumEntities { get; private set; } 

        public PreloadingAssemblyResolver(string references)
        {
            foreach (var dll in references.Split(';'))
            {
                var assembly = ModuleDefinition.ReadModule(dll, new ReaderParameters { AssemblyResolver = this }).Assembly;

                if (assembly.Name.Name == "Signum.Entities")
                    SignumEntities = assembly;

                RegisterAssembly(assembly);
            }
        }
    }
}