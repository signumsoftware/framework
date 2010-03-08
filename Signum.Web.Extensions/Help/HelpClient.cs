using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using System.Reflection;
using Signum.Web.Operations;
using Signum.Entities;
using System.Web.Routing;
using System.Web.Mvc;
namespace Signum.Web.Help
{
    public static class HelpClient
    {
        //pages
        public static string ViewPrefix = "~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.Help.";
        public static string IndexUrl = "Index.aspx";
        public static string ViewEntityUrl = "ViewEntity.aspx";
        public static string ViewAppendixUrl = "ViewAppendix.aspx";
        public static string ViewNamespaceUrl = "ViewNamespace.aspx";
        public static string TodoUrl = "ViewTodo.aspx";
        public static string SearchResults = "Search.aspx";

        //controls
        public static string Menu = "Menu.ascx";
        public static string ViewEntityPropertyUrl = "EntityProperty.ascx";
        public static string NamespaceControlUrl = "NamespaceControl.ascx";

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute(
            "HelpIndex",                                             // Route name
            "Help",                                 // URL with parameters
            new { controller = "Help", action = "Index", }  // Parameter defaults
            );

            routes.MapRoute(
            "EntityHelpSearch",                                             // Route name
            "Help/Search",                           // URL with parameters
            new { controller = "Help", action = "Search" }  // Parameter defaults
            );

            routes.MapRoute(
            "ViewTodo",                                             // Route name
            "Help/ViewTodo",                           // URL with parameters
            new { controller = "Help", action = "ViewTodo" }  // Parameter defaults
            );
            routes.MapRoute(
            "EntityHelp",                                             // Route name
            "Help/{entity}",                           // URL with parameters
            new { controller = "Help", action = "ViewEntity", entity = "" }  // Parameter defaults
            );

            routes.MapRoute(
            "NamespaceHelp",                                             // Route name
            "Help/Namespace/{namespace}",                           // URL with parameters
            new { controller = "Help", action = "ViewNamespace", @namespace = "" }  // Parameter defaults
            );

            routes.MapRoute(
            "AppendixHelp",                                             // Route name
            "Help/Appendix/{appendix}",                           // URL with parameters
            new { controller = "Help", action = "ViewAppendix", appendix = "" }  // Parameter defaults
            );

            routes.MapRoute(
            "EntityHelpSave",                                             // Route name
            "Help/{entity}/Save",                           // URL with parameters
            new { controller = "Help", action = "SaveEntity", entity = "" }  // Parameter defaults
            );

            routes.MapRoute(
             "NamespaceHelpSave",                                             // Route name
             "Help/Namespace/{namespace}/Save",                           // URL with parameters
             new { controller = "Help", action = "SaveNamespace", @namespace = "" }  // Parameter defaults
             );

            routes.MapRoute(
             "AppendixHelpSave",                                             // Route name
             "Help/Appendix/{appendix}/Save",                           // URL with parameters
             new { controller = "Help", action = "SaveAppendix", appendix = "" }  // Parameter defaults
             );
        }
    }
}
