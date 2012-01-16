using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Hosting;

namespace Signum.Web.PortableAreas
{
    public class CompiledVirtualPathProvider : VirtualPathProvider
    {
        private readonly VirtualPathProvider virtualPathProvider;

        public CompiledVirtualPathProvider(VirtualPathProvider defaultPathProvider)
        {
            this.virtualPathProvider = defaultPathProvider;
        }

        public override bool FileExists(string virtualPath)
        {
            return
                GetCompiledType(virtualPath) != null
                 || virtualPathProvider.FileExists(virtualPath);
        }

        private Type GetCompiledType(string virtualPath)
        {
            return CompiledViews.GetCompiledType(virtualPath);
        }

     
        public override VirtualFile GetFile(string virtualPath)
        {
            if (virtualPathProvider.FileExists(virtualPath))
            {
                return virtualPathProvider.GetFile(virtualPath);
            }
            var compiledType = GetCompiledType(virtualPath);
            if (compiledType != null)
            {
                return new CompiledVirtualFile(virtualPath, compiledType);
            }
            return null;
        }

        public override System.Web.Caching.CacheDependency GetCacheDependency(string virtualPath, System.Collections.IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            return GetCompiledType(virtualPath) != null ? null : base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }

    }

    public class CompiledVirtualFile : VirtualFile
    {
        public CompiledVirtualFile(string virtualPath, Type compiledType)
            : base(virtualPath)
        {
            CompiledType = compiledType;
        }

        public Type CompiledType { get; set; }
        public CompiledVirtualFile Type { get; set; }

        public override Stream Open()
        {
            return Stream.Null;
        }
    }
}