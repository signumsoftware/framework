using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Linq;
using System.Web.Hosting;
using Signum.Utilities;
using System.Reflection;
using System.Resources;
using System.Globalization;
using System.Collections.Concurrent;
using System.Collections;
using System.Text;

namespace Signum.Web
{
    public static class AssemblyResourceManager
    {
        private static Dictionary<string, AssemblyResourceStore> assemblyResourceStores = new Dictionary<string, AssemblyResourceStore>();

        public static VirtualFile GetVirtualFile(string virtualPath)
        {
            string prefix = GetPrefix(virtualPath);

            if (prefix == null)
                return null;

            var store = assemblyResourceStores.TryGetC(prefix.ToLower());
            if (store == null)
                return null;

            if (store.FileExists(virtualPath))
                return new AssemblyResourceVirtualFile(virtualPath, store);

            return null;
        }

        public static bool FileExist(string virtualPath)
        {
            string prefix = GetPrefix(virtualPath);

            if (prefix == null)
                return false;

            var store = assemblyResourceStores.TryGetC(prefix.ToLower());
            if (store == null)
                return false;

            return store.FileExists(virtualPath);
        }

        static string GetPrefix(string virtualPath)
        {
            if (!virtualPath.StartsWith("/"))
                return null;

            int index = virtualPath.IndexOf('/', 1);
            if (index == -1)
                return null;

            return virtualPath.Substring(0, index + 1);
        }

        public static void RegisterAreaResources(AssemblyResourceStore store)
        {
            assemblyResourceStores.Add(store.VirtualPath, store);
        }

        class AssemblyResourceVirtualFile : VirtualFile
        {
            private readonly AssemblyResourceStore store;
            
            public AssemblyResourceVirtualFile(string virtualPath, AssemblyResourceStore store)
                : base(virtualPath)
            {
                this.store = store;
            }

            public override Stream Open()
            {
                return this.store.GetResourceStream(this.VirtualPath);
            }
        }
    }

    public class AssemblyResourceStore
    {
        public readonly Assembly Assembly;
        readonly string namespaceName;
        public readonly string VirtualPath;

        readonly Dictionary<string, string> resources;

        public AssemblyResourceStore(Type typeToLocateAssembly, string virtualPath) :
            this(typeToLocateAssembly, virtualPath, typeToLocateAssembly.Namespace)
        {
        }

        public AssemblyResourceStore(Type typeToLocateAssembly, string virtualPath, string namespaceName)
        {
            if (typeToLocateAssembly == null)
                throw new ArgumentNullException("typeToLocateAssembly");

            if (string.IsNullOrEmpty(virtualPath))
                throw new ArgumentNullException("virtualPath");

            if (string.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException("namespaceName");

            this.Assembly = typeToLocateAssembly.Assembly;

            this.VirtualPath = virtualPath.ToLower();
            this.namespaceName = namespaceName.ToLower();

            resources = this.Assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(namespaceName))
                .ToDictionary(name => name.Substring(namespaceName.Length).ToLowerInvariant());
        }

        public Stream GetResourceStream(string virtualPath)
        {
            string actualResourceName = resources.TryGetC(GetResourceName(virtualPath));

            if (actualResourceName == null)
                throw new FileNotFoundException("{0} not found".Formato(virtualPath));

            return this.Assembly.GetManifestResourceStream(actualResourceName);
        }

        public bool FileExists(string virtualPath)
        {
            return resources.ContainsKey(GetResourceName(virtualPath));
        }

        private string GetResourceName(string virtualPath)
        {
            virtualPath = virtualPath.ToLower();

            if (!virtualPath.StartsWith(VirtualPath))
                throw new InvalidOperationException("virtualPath is not from this store");

            return virtualPath.Substring(VirtualPath.Length).Replace("/", ".");
        }
    }


}