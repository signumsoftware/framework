using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.Web
{
    public static class ModuleResources
    {
        static Dictionary<string, string> moduleResources = new Dictionary<string, string>();

        public static Func<string, bool, string> ResourceToUrl = (r, a) => r;

        public static void RegisterModule(string module, string resource, bool area)
        {
            if (module == null)
                throw new ArgumentNullException("module");

            if (resource == null)
                throw new ArgumentNullException("resource");

            moduleResources.Add(module, ResourceToUrl(resource, area));
        }

        public static void RegisterBasics()
        {
            ModuleResources.RegisterModule("autocomplete",
                "signum/Scripts/SF_autocomplete.js",
                true);
            ModuleResources.RegisterModule("draganddrop",
                "signum/Scripts/SF_DragAndDrop.js",
                true);
        }

        public static string ResourceForModule(string module)
        {
            return moduleResources[module];
        }

    }
}