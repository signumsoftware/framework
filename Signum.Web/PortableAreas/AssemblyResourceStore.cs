using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Signum.Web
{
    /// <summary>
    /// Stores all the embedded resources for a single assembly/area.
    /// </summary>
    public class AssemblyResourceStore
    {
        public readonly Assembly Assembly;

        readonly Dictionary<string, string> resources;
        readonly string namespaceName;

        public string VirtualPath { get; private set; }

        public AssemblyResourceStore(Type typeToLocateAssembly, string virtualPath) : 
            this(typeToLocateAssembly, virtualPath, typeToLocateAssembly.Namespace)
        {
          
        }

        public AssemblyResourceStore(Type typeToLocateAssembly, string virtualPath, string namespaceName)
        {
            this.Assembly = typeToLocateAssembly.Assembly;
            // should we disallow an empty virtual path?
            this.VirtualPath = virtualPath.ToLower();
            this.namespaceName = namespaceName.ToLower();

            resources = this.Assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(namespaceName))
                .ToDictionary(name => name.ToLower());
        }

        public Stream GetResourceStream(string resourceName)
        {
            var fullResourceName = GetFullyQualifiedTypeFromPath(resourceName);
            string actualResourceName = null;
            if (resources.TryGetValue(fullResourceName, out actualResourceName))
            {
                return this.Assembly.GetManifestResourceStream(actualResourceName);
            }
            else
            {
                return null;
            }
        }

        public string GetFullyQualifiedTypeFromPath(string path)
        {
            string resourceName = path.ToLower().Replace("~", this.namespaceName);
            // we can make this more succinct if we don't have to check for emtpy virtual path (by preventing in constuctor)
            if (!string.IsNullOrEmpty(VirtualPath))
                resourceName = resourceName.Replace(VirtualPath, "");
            return resourceName.Replace("/", ".");
        }

        public bool IsPathResourceStream(string path)
        {
            var fullResourceName = GetFullyQualifiedTypeFromPath(path);
            return resources.ContainsKey(fullResourceName);
        }
    }
}