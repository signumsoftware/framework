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
using System.Web.Mvc;

namespace Signum.Web.PortableAreas
{
    public class EmbeddedFilesRepository: IFileRepository
    {
        public readonly Assembly Assembly;
        readonly string namespaceName;
        public readonly string VirtualPath;

        readonly Dictionary<string, string> resources;

        public EmbeddedFilesRepository(Assembly assembly, string virtualPath, string namespaceName)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            if (string.IsNullOrEmpty(virtualPath))
                throw new ArgumentNullException("virtualPath");

            if (string.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException("namespaceName");

            this.Assembly = assembly;

            this.VirtualPath = virtualPath.ToLower();
            this.namespaceName = namespaceName.ToLower();

            resources = this.Assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(namespaceName + ".", StringComparison.InvariantCultureIgnoreCase))
                .ToDictionary(name => name.Substring(namespaceName.Length + 1), StringComparer.InvariantCultureIgnoreCase);
        }

        public ActionResult GetFile(string file)
        {
            string resourceName = GetResourceName(file);

            if (resourceName == null)
                throw new FileNotFoundException("{0} does not belong to this repository".FormatWith(file)); 

            string actualResourceName = resources.TryGetC(resourceName);

            if (actualResourceName == null)
                throw new FileNotFoundException("{0} not found".FormatWith(file));

            return new StaticContentResult(this.Assembly.GetManifestResourceStream(actualResourceName).ReadAllBytes(), file);
        }

        public bool FileExists(string file)
        {
            string resourceName = GetResourceName(file);

            return resourceName != null && resources.ContainsKey(resourceName);
        }

        private string GetResourceName(string file)
        {
            file = VirtualPathUtility.ToAppRelative(file).ToLower();

            if (!file.StartsWith(VirtualPath))
                return null;

            return file.Substring(VirtualPath.Length).Replace("/", ".");
        }

        public bool IsEmpty { get { return this.resources.IsEmpty();  } }

        public override string ToString()
        {
            return "EmbeddedResources {0} -> {1}".FormatWith(namespaceName, VirtualPath);
        }
    }



}