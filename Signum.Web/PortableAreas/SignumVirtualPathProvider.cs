using System;
using System.Linq;
using System.Web.Hosting;
using System.Web;
using System.Diagnostics;
using System.IO;

namespace Signum.Web
{
	public class SignumVirtualPathProvider : VirtualPathProvider
	{
        public override bool FileExists(string virtualPath)
        {
            return base.FileExists(virtualPath) || 
                   AssemblyResourceManager.FileExist(virtualPath) || 
                   LocalizeResourceManager.FileExist(virtualPath);
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            if (base.FileExists(virtualPath))
                return base.GetFile(virtualPath); 

            VirtualFile file = AssemblyResourceManager.GetVirtualFile(virtualPath);
            if (file != null)
                return file;

            file = LocalizeResourceManager.GetVirtualFile(virtualPath);
            if (file != null)
                return file;

            return null;
        }

        public override System.Web.Caching.CacheDependency GetCacheDependency(string virtualPath, System.Collections.IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            if (AssemblyResourceManager.GetVirtualFile(virtualPath) != null)
            {
                return null;
            }

            return base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }

        public override string GetCacheKey(string virtualPath)
        {
            return null;
        }
	}
}