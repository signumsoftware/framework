using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Linq;

namespace Signum.Web
{
    /// <summary>
    /// Manages all .NET assemblies that have registered their embedded resources.
    /// </summary>
    public static class AssemblyResourceManager
    {
        private static List<AssemblyResourceStore> assemblyResourceStores = new List<AssemblyResourceStore>();

        public static AssemblyResourceStore GetResourceStoreFromVirtualPath(string virtualPath)
        {
            var checkPath = VirtualPathUtility.ToAppRelative(virtualPath).ToLower();

            return assemblyResourceStores.SingleOrDefault(rs=> rs.IsPathResourceStream(checkPath)); 
        }

        public static void RegisterAreaResources(AssemblyResourceStore assemblyResourceStore)
        {
            assemblyResourceStores.Add(assemblyResourceStore);
        }
    }
}