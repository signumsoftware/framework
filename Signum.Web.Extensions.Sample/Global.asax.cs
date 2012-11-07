using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Signum.Test;
using System.Web.Hosting;
using Signum.Web.Extensions.Sample.Properties;
using Signum.Engine.Maps;
using Signum.Web.Operations;
using Signum.Entities.Authorization;
using System.Threading;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Web.UserQueries;
using Signum.Web.Reports;
using Signum.Web.Auth;
using Signum.Web.AuthAdmin;
using Signum.Web.ControlPanel;
using Signum.Entities.ControlPanel;
using Signum.Web.Combine;
using System.Reflection;
using Signum.Web.PortableAreas;
using Signum.Web.Widgets;
using Signum.Web.Chart;
using Signum.Utilities;
using Signum.Web.Files;
using Signum.Web.Processes;
using Signum.Web.Basic;
using Signum.Engine.Processes;

namespace Signum.Web.Extensions.Sample
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
               Navigator.NavigateRouteName,
               "View/{webTypeName}/{id}",
               new { controller = "Signum", action = "View", webTypeName = "", id = "" }
            );

            routes.MapRoute(
                Navigator.FindRouteName,
                "Find/{webQueryName}",
                new { controller = "Signum", action = "Find", webQueryName = "" }
            );

            RouteTable.Routes.MapRoute(
                 "EmbeddedResources",
                 "{*file}",
                 new { controller = "Resources", action = "GetFile" },
                 new { file = new EmbeddedFileExist() }
            );
            
            routes.MapRoute(
                "Default",                                              // Route name
                "{controller}/{action}/{id}",                           // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }  // Parameter defaults
            );
        }

        protected void Application_Start()
        {   
            Signum.Test.Extensions.Starter.Start(UserConnections.Replace(Settings.Default.ConnectionString));

            Statics.SessionFactory = new ScopeSessionFactory(new AspNetSessionFactory());

            using (AuthLogic.Disable())
            {
                Schema.Current.Initialize();
                WebStart();
            }

            RegisterRoutes(RouteTable.Routes);
        }

        private void WebStart()
        {
            Navigator.Start(new NavigationManager());
            Constructor.Start(new ConstructorManager());
            OperationsClient.Start(new OperationManager(), true);

            AuthClient.Start(
                types: true, 
                property: true, 
                queries: true, 
                resetPassword: true,
                passwordExpiration: false);

            AuthClient.CookieName = "sfUserMusicSample";

            AuthAdminClient.Start(
                types: true, 
                properties: true, 
                queries: true, 
                operations: true, 
                permissions: true, 
                facadeMethods: false);

            ContextualItemsHelper.Start();

            QueryClient.Start();
            UserQueriesClient.Start();
            ControlPanelClient.Start();

            FilesClient.Start(true, true, true);
            ChartClient.Start();
            ReportsClient.Start(true, true);

            ProcessesClient.Start(packages: true, packageOperations: true);
            ProcessLogic.StartBackgroundProcess(5 * 1000);

            QuickLinkWidgetHelper.Start();
            NotesClient.Start();
            AlertsClient.Start();

            MusicClient.Start();

            ScriptHtmlHelper.Manager.MainAssembly = typeof(MusicClient).Assembly;
            SignumControllerFactory.MainAssembly = Assembly.GetExecutingAssembly();

            Navigator.Initialize();

            SignumControllerFactory.EveryController().AddFilters(
                new SignumExceptionHandlerAttribute());

            SignumControllerFactory.EveryController().AddFilters(
                ctx => ctx.FilterInfo.AuthorizationFilters.OfType<AuthenticationRequiredAttribute>().Any() ? null : new AuthenticationRequiredAttribute());
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            if (!AuthController.LoginFromCookie())
                UserDN.Current = AuthLogic.AnonymousUser;
        }
    }
}
