using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace Signum.Web
{
    /// <summary>
    /// Manages all .NET assemblies that have registered their embedded resources.
    /// </summary>
    public static class AssemblyResourceManager
    {
        private static Dictionary<string, AssemblyResourceStore> assemblyResourceStores = new Dictionary<string, AssemblyResourceStore>();

        public static AssemblyResourceStore GetResourceStoreFromVirtualPath(string virtualPath)
        {
            var checkPath = VirtualPathUtility.ToAppRelative(virtualPath).ToLower();
            foreach (var resourceStore in assemblyResourceStores)
            {
                if (checkPath.Contains(resourceStore.Key) && resourceStore.Value.IsPathResourceStream(checkPath))
                {
                    return resourceStore.Value;
                }
            }
            return null;
        }

        public static bool IsEmbeddedViewResourcePath(string virtualPath)
        {
            var resourceStore = GetResourceStoreFromVirtualPath(virtualPath);
            return (resourceStore != null);
        }

        public static void RegisterAreaResources(AssemblyResourceStore assemblyResourceStore)
        {
            assemblyResourceStores.Add(assemblyResourceStore.VirtualPath, assemblyResourceStore);
        }
    }
}