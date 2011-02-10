using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using System.Web.Routing;
using System.Web.Hosting;
using System.IO;

namespace Signum.Web.PortableAreas
{
    public interface IFileRepository
    {
        bool FileExists(string file);
        ActionResult GetFile(string file);
    }

    public static class FileRepositoryManager
    {
        static HashSet<IFileRepository> Repositories = new HashSet<IFileRepository>();

        public static void Register(IFileRepository repository)
        {
            Repositories.Add(repository);
        }

        public static bool FileExists(string file)
        {
            return Repositories.Any(a => a.FileExists(file)); 
        }

        public static ActionResult GetFile(string file)
        {
            IFileRepository respository = Repositories.FirstOrDefault(a => a.FileExists(file));

            if (respository == null)
                return null;

            return respository.GetFile(file);
        }
    }

    public class EmbeddedFileExist : IRouteConstraint
    {
        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            object file = values[parameterName];
            if (file == null)
                return false;

            return FileRepositoryManager.FileExists("~/" + file.ToString());
        }
    }

    public class FileRepositoryIIS6CompatibilityVirtualPathProvider : VirtualPathProvider
    {
        public string Complete(string virtualPath)
        {
            if (virtualPath.StartsWith("/"))
                return "~" + virtualPath.Substring(1);

            return virtualPath;
        }

        public override bool FileExists(string virtualPath)
        {
            retry:
            try
            {
                File.AppendAllText(HttpContext.Current.Server.MapPath("~/virtualFiles.txt"), "FileExist "+ virtualPath + "\r\n");
            }
            catch
            {
                goto retry; 
            }

            if (base.FileExists(virtualPath))
                return true;

            if (FileRepositoryManager.FileExists(Complete(virtualPath)))
                return true;

            return false;
        }

        public override VirtualFile GetFile(string virtualPath)
        {
              retry:
            try
            {
                File.AppendAllText(HttpContext.Current.Server.MapPath("~/virtualFiles.txt"), "GetFile " + virtualPath + "\r\n");
            }
            catch
            {
                goto retry; 
            }

            if (base.FileExists(virtualPath))
                return base.GetFile(virtualPath);

            ActionResult result = FileRepositoryManager.GetFile(Complete(virtualPath));

            if (!(result is StaticContentResult))
                throw new InvalidOperationException("StaticContentResult expected for {0}".Formato(virtualPath));

            return new StaticContentIIS6CompatibilityVirtualFile((StaticContentResult)result, virtualPath);
        }

        public override System.Web.Caching.CacheDependency GetCacheDependency(string virtualPath, System.Collections.IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            if (FileRepositoryManager.GetFile(virtualPath) != null)
            {
                return null;
            }

            return base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }

        public override string GetCacheKey(string virtualPath)
        {
            return null;
        }

        public class StaticContentIIS6CompatibilityVirtualFile : VirtualFile
        {  
            public StaticContentResult staticResult;

            public StaticContentIIS6CompatibilityVirtualFile(StaticContentResult staticResult, string virtualPath)
                : base(virtualPath)
            {
                this.staticResult = staticResult;
            }

            public override Stream Open()
            {
                return new MemoryStream(staticResult.Uncompressed);
            }
        }
    }
}