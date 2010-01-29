using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Hosting;
using Signum.Utilities;
using Signum.Test.LinqProvider;
using Signum.Engine.Maps;
using Signum.Web.Sample.Properties;

namespace Signum.Web.Sample
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
               "v",
               "View/{typeUrlName}/{id}",
               new { controller = "Signum", action = "View", typeFriendlyName = "", id = "" }
           );

            routes.MapRoute(
                "f",
                "Find/{sfQueryUrlName}",
                new { controller = "Signum", action = "Find", sfQueryUrlName = "" }
            );

            routes.MapRoute(
                "Default",                                              // Route name
                "{controller}/{action}/{id}",                           // URL with parameters
                new { controller = "Home", action = "Index", id = "" }  // Parameter defaults
            );
        }

        protected void Application_Start()
        {
            //System.Diagnostics.Debugger.Break();

            RegisterRoutes(RouteTable.Routes);
            //RouteDebug.RouteDebugger.RewriteRoutesForTesting(RouteTable.Routes);

            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new SignumViewEngine());

            HostingEnvironment.RegisterVirtualPathProvider(new AssemblyResourceProvider());

            Starter.Start(Settings.Default.ConnectionString, Queries.Web);

            Schema.Current.Initialize();
            LinkTypesAndViews();

            AuthenticationRequiredAttribute.Authenticate = null;
            //Thread.CurrentPrincipal = Database.Query<UserDN>().Where(u => u.UserName == "externo").Single();
        }

        private void LinkTypesAndViews()
        {
            Navigator.Manager = new NavigationManager
            {
                EntitySettings = new Dictionary<Type, EntitySettings>
                {
                    // {typeof(EfColaboradoraDN), new EntitySettings(false){PartialViewName="Views/Home/EfColaboradoraIU" }},
                },
                Queries = Queries.Web,
            };

            Constructor.Start(new ConstructorManager
            {
                Constructors = new Dictionary<Type, Func<Controller, object>>
                {
                    //{ typeof(FuturoTomadorDN), c => new FuturoTomadorDN{DatosContacto = new DatosContactoDN().ToLiteFat() }},
                }
            });

            MusicClient.Start();

            Navigator.Manager.NormalPageUrl = "Views/Shared/NormalPage";
            Navigator.Start();
        }

    }
}