using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using System.Web.WebPages;
using Signum.Utilities;

namespace Signum.Web.PortableAreas
{
    public static class CompiledViews
    {
        static Dictionary<string, Type> Views = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);

        public static void RegisterView(Type viewType)
        {
            PageVirtualPathAttribute attribute = viewType.GetCustomAttribute<PageVirtualPathAttribute>();

            if (attribute == null)
                throw new InvalidOperationException("{0} has not {1}".Formato(viewType.Name, typeof(PageVirtualPathAttribute).Name));

            Views.AddOrThrow(attribute.VirtualPath, viewType, "compiled view {0} already registered");
        }

        public static void RegisterView(Type viewType, string virtualPath)
        {
            Views.AddOrThrow(virtualPath, viewType, "compiled view {0} already registered");
        }


        public static void RegisterViews(Assembly assembly, params string[] views)
        {
            List<Type> viewsInArea = (from t in assembly.GetTypes()
                                      where t.IsSubclassOf(typeof(WebPageRenderingBase))
                                      let att = t.GetCustomAttribute<PageVirtualPathAttribute>()
                                      where views.Contains(att.VirtualPath, StringComparer.InvariantCultureIgnoreCase)
                                      select t).ToList();

            foreach (var t in viewsInArea)
            {
                CompiledViews.RegisterView(t);
            }
        }

        public static void RegisterArea(Assembly assembly, string areaName)
        {
            string prefix = ("~/"+ areaName + "/").ToLowerInvariant();

            var viewsInArea = (from t in assembly.GetTypes()
                               where t.IsSubclassOf(typeof(WebPageRenderingBase))
                               let att = t.GetCustomAttribute<PageVirtualPathAttribute>()
                               where att.VirtualPath.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)
                               select new { att.VirtualPath, Type = t });

            Views.AddRange(viewsInArea, a => a.VirtualPath, a => a.Type, "compiled views");
        }

        public static Type GetCompiledType(string virtualPath)
        {
            virtualPath = VirtualPathUtility.ToAppRelative(virtualPath).ToLower(); 

            return Views.TryGetC(virtualPath); 
        }

    }
}