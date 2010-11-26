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
            if(base.FileExists(virtualPath))
            return true;
            
            AssemblyResourceStore store = AssemblyResourceManager.GetResourceStoreFromVirtualPath(virtualPath);
            return store != null;
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            if (base.FileExists(virtualPath))
                return base.GetFile(virtualPath); 

            AssemblyResourceStore store = AssemblyResourceManager.GetResourceStoreFromVirtualPath(virtualPath);
            if (store != null)
                return new AssemblyResourceVirtualFile(virtualPath, store);

            return null;
        }

        public override System.Web.Caching.CacheDependency GetCacheDependency(string virtualPath, System.Collections.IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            if (AssemblyResourceManager.GetResourceStoreFromVirtualPath(virtualPath) != null)
            {
                return null;
            }

            return base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }

        public override string GetCacheKey(string virtualPath)
        {
            return null;
        }

        class AssemblyResourceVirtualFile : VirtualFile
        {
            private readonly AssemblyResourceStore resourceStore;
            private readonly string path;

            public AssemblyResourceVirtualFile(string virtualPath, AssemblyResourceStore resourceStore)
                : base(virtualPath)
            {
                this.resourceStore = resourceStore;
                path = VirtualPathUtility.ToAppRelative(virtualPath);
            }

            public override Stream Open()
            {
                Trace.WriteLine("Opening " + path);
                return this.resourceStore.GetResourceStream(this.path);
            }
        }
	}
}