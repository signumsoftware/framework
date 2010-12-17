using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using $custommessage$.Logic;
using Signum.Engine;
using $custommessage$.Web.Properties;
using Signum.Engine.Maps;
using Signum.Web;

namespace $custommessage$.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
               "view",
               "View/{typeUrlName}/{id}",
               new { controller = "Signum", action = "View", typeFriendlyName = "", id = "" }
            );

            routes.MapRoute(
                "find",
                "Find/{sfQueryUrlName}",
                new { controller = "Signum", action = "Find", sfQueryUrlName = "" }
            );

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );
        }

        protected void Application_Start()
        {
            RegisterRoutes(RouteTable.Routes);

            Starter.Start(UserConnections.Replace(Settings.Default.ConnectionString));

            Schema.Current.Initialize();
            WebStart();
        }

        private void WebStart()
        {
            Navigator.Start(new NavigationManager());
            Constructor.Start(new ConstructorManager());
            
            $custommessage$Client.Start();

            ScriptHtmlHelper.Manager.MainAssembly = typeof($custommessage$Client).Assembly;

            Navigator.Initialize();
        }
    }
}