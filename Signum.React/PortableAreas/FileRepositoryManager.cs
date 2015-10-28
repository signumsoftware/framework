using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using System.Web.Routing;
using System.Web.Hosting;
using System.IO;

namespace Signum.React.PortableAreas
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
            var result = Repositories.Any(a => a.FileExists(file));
            return result;
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
}