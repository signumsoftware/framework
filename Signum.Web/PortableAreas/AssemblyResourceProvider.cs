using System;
using System.Linq;
using System.Web.Hosting;

namespace Signum.Web
{
	public class AssemblyResourceProvider : VirtualPathProvider
	{
        public override bool FileExists(string virtualPath)
        {
            bool exists = base.FileExists(virtualPath);
            return exists ? exists : AssemblyResourceManager.IsEmbeddedViewResourcePath(virtualPath);
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            if (AssemblyResourceManager.IsEmbeddedViewResourcePath(virtualPath) && !base.FileExists(virtualPath))
            {
                var resourceStore = AssemblyResourceManager.GetResourceStoreFromVirtualPath(virtualPath);
                return new AssemblyResourceVirtualFile(virtualPath, resourceStore);
            }
            else
            {
                return base.GetFile(virtualPath);
            }
        }

        public override System.Web.Caching.CacheDependency GetCacheDependency(string virtualPath, System.Collections.IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            if (AssemblyResourceManager.IsEmbeddedViewResourcePath(virtualPath))
            {
                return null;
            }
            else
            {
                string[] dependencies = virtualPathDependencies.OfType<string>().Where(s => !s.ToLower().Contains("/views/inputbuilders")).ToArray();
                return base.GetCacheDependency(virtualPath, dependencies, utcStart);
            }
        }

        public override string GetCacheKey(string virtualPath)
        {
            return null;
        }
	}
}