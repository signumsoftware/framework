#region usings
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
using System.Text.RegularExpressions;
using Signum.Engine.Help;
using Signum.Engine.Operations;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Engine;
using Signum.Engine.WikiMarkup;
using Signum.Entities.Basics;
using Signum.Web.Omnibox;
using Signum.Entities.Omnibox;
using Signum.Entities.Help;
#endregion

namespace Signum.Web.Help
{
    public static class HelpClient
    {
        public static string ViewPrefix = "~/Help/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Help/Scripts/help"); 

        //pages        
        public static string IndexUrl =  ViewPrefix.Formato("Index");
        public static string ViewEntityUrl = ViewPrefix.Formato("ViewEntity");
        public static string ViewAppendixUrl = ViewPrefix.Formato("ViewAppendix");
        public static string ViewNamespaceUrl = ViewPrefix.Formato("ViewNamespace");
        public static string TodoUrl = ViewPrefix.Formato("ViewTodo");
        public static string SearchResults = ViewPrefix.Formato("Search");

        //controls
        public static string Buttons = ViewPrefix.Formato("Buttons");
        public static string MiniMenu = ViewPrefix.Formato("MiniMenu");
        public static string ViewEntityPropertyUrl = ViewPrefix.Formato("EntityProperty");
        public static string NamespaceControlUrl = ViewPrefix.Formato("NamespaceControl");

        public static void Start(string baseUrl, string imageFolder)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                HelpUrls.BaseUrl = baseUrl;
                HelpUrls.ImagesFolder = imageFolder;

                Navigator.RegisterArea(typeof(HelpClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<EntityHelpDN>(),
                    new EntitySettings<QueryHelpDN>(),
                    new EntitySettings<AppendixHelpDN>(),
                    new EntitySettings<NamespaceHelpDN>(),
                    new EmbeddedEntitySettings<PropertyRouteHelpDN>(),
                    new EntitySettings<OperationHelpDN>(),
                    new EmbeddedEntitySettings<QueryColumnHelpDN>(),
                });

                Navigator.EmbeddedEntitySettings<PropertyRouteHelpDN>().MappingDefault.AsEntityMapping()
                    .SetProperty(a => a.Property, ctx =>
                    {
                        var type = ctx.FindParent<EntityHelpDN>().Value.Type.ToType();
                        return PropertyRoute.Parse(type, ctx.Input).ToPropertyRouteDN();
                    });

                RegisterHelpRoutes();
            }
        }

        private static void RegisterHelpRoutes()
        {
            RouteTable.Routes.MapRoute(null, "Help/Appendix/{appendix}", new { controller = "Help", action = "ViewAppendix" });
            RouteTable.Routes.MapRoute(null, "Help/Namespace/{namespace}", new { controller = "Help", action = "ViewNamespace" });
            RouteTable.Routes.MapRoute(null, "Help/Entity/{entity}", new { controller = "Help", action = "ViewEntity", });
        }
    }


    public class HelpOmniboxProvider : OmniboxClient.OmniboxProvider<HelpModuleOmniboxResult>
    {
        public override OmniboxResultGenerator<HelpModuleOmniboxResult> CreateGenerator()
        {
            return new HelpModuleOmniboxResultGenerator();
        }

        public override MvcHtmlString RenderHtml(HelpModuleOmniboxResult result)
        {
            MvcHtmlString html = result.KeywordMatch.ToHtml();

            if (result.SecondMatch != null)
                html = html.Concat(" {0}".FormatHtml(result.SecondMatch.ToHtml()));
            else if(result.SearchString.HasText())
                html = html.Concat(" \"{0}\"".FormatHtml(result.SearchString));
            else
                html = html.Concat(this.ColoredSpan(typeof(TypeDN).NiceName() + "...", "lightgray"));

            html = Icon().Concat(html);

            return html;
        }

        public override string GetUrl(HelpModuleOmniboxResult result)
        {
            if (result.Type != null)
                return RouteHelper.New().Action((HelpController c) => c.ViewEntity(Navigator.ResolveWebTypeName(result.Type)));

            if (result.SearchString != null)
                return RouteHelper.New().Action((HelpController c) => c.Search(result.SearchString));

            return RouteHelper.New().Action((HelpController c) => c.Index());
        }

        public override MvcHtmlString Icon()
        {
            return ColoredGlyphicon("glyphicon-book", "DarkViolet");
        }
    }
}
