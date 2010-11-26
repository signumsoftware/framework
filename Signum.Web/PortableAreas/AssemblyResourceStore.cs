using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Signum.Utilities;

namespace Signum.Web
{
    /// <summary>
    /// Stores all the embedded resources for a single assembly/area.
    /// </summary>
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
            
            if(string.IsNullOrEmpty(virtualPath))
                throw new ArgumentNullException("virtualPath");

            if(string.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException("namespaceName");

            this.Assembly = typeToLocateAssembly.Assembly;

            this.VirtualPath = virtualPath.ToLower();
            this.namespaceName = namespaceName.ToLower();

            resources = this.Assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(namespaceName))
                .ToDictionary(name => name.ToLower());
        }

        public Stream GetResourceStream(string resourceName)
        {
            var fullResourceName = GetFullyQualifiedTypeFromPath(resourceName);

            string actualResourceName = resources.TryGetC(fullResourceName);

            if (actualResourceName == null)
                return null;

            return this.Assembly.GetManifestResourceStream(actualResourceName);
        }

        public string GetFullyQualifiedTypeFromPath(string path)
        {
            return path.ToLower().Replace("~", this.namespaceName).Replace(VirtualPath, "").Replace("/", ".");
        }

        public bool IsPathResourceStream(string path)
        {
            if (!path.Contains(VirtualPath))
                return false;

            string fullResourceName = GetFullyQualifiedTypeFromPath(path);
            return resources.ContainsKey(fullResourceName);
        }
    }
}