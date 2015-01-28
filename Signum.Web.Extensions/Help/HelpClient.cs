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

namespace Signum.Web.Help
{
    public static class HelpClient
    {
        public static string ViewPrefix = "~/Help/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Help/Scripts/help");
        public static JsModule WidgetModule = new JsModule("Extensions/Signum.Web.Extensions/Help/Scripts/helpWidget");

        //pages        
        public static string IndexUrl = ViewPrefix.FormatWith("Index");
        public static string ViewEntityUrl = ViewPrefix.FormatWith("ViewEntity");
        public static string ViewAppendixUrl = ViewPrefix.FormatWith("ViewAppendix");
        public static string ViewNamespaceUrl = ViewPrefix.FormatWith("ViewNamespace");
        public static string TodoUrl = ViewPrefix.FormatWith("ViewTodo");
        public static string SearchResults = ViewPrefix.FormatWith("Search");

        //controls
        public static string Buttons = ViewPrefix.FormatWith("Buttons");
        public static string MiniMenu = ViewPrefix.FormatWith("MiniMenu");
        public static string ViewEntityPropertyUrl = ViewPrefix.FormatWith("EntityProperty");
        public static string NamespaceControlUrl = ViewPrefix.FormatWith("NamespaceControl");

        public static void Start(string imageFolder,string baseUrl)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                HelpUrls.BaseUrl = baseUrl;
                HelpUrls.ImagesFolder = imageFolder;

                Navigator.RegisterArea(typeof(HelpClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<EntityHelpEntity>(),
                    new EntitySettings<QueryHelpEntity>(),
                    new EntitySettings<AppendixHelpEntity>(),
                    new EntitySettings<NamespaceHelpEntity>(),
                    new EmbeddedEntitySettings<PropertyRouteHelpEntity>(),
                    new EntitySettings<OperationHelpEntity>(),
                    new EmbeddedEntitySettings<QueryColumnHelpEntity>(),
                });

                Navigator.EmbeddedEntitySettings<PropertyRouteHelpEntity>().MappingDefault.AsEntityMapping()
                    .SetProperty(a => a.Property, ctx =>
                    {
                        var type = ctx.FindParent<EntityHelpEntity>().Value.Type.ToType();
                        return PropertyRoute.Parse(type, ctx.Input).ToPropertyRouteEntity();
                    });

                RegisterHelpRoutes();

                Common.CommonTask += Common_CommonTask;

                WidgetsHelper.GetWidget += WidgetsHelper_GetWidget;

                ButtonBarQueryHelper.RegisterGlobalButtons(ButtonBarQueryHelper_RegisterGlobalButtons);
            }
        }

        static void Common_CommonTask(LineBase line)
        {
            if (line.PropertyRoute != null)
                line.FormGroupHtmlProps["data-route"] = line.PropertyRoute.ToString();
        }

        static ToolBarButton[] ButtonBarQueryHelper_RegisterGlobalButtons(QueryButtonContext ctx)
        {
            HeloToolBarButton btn = new HeloToolBarButton(ctx.Prefix, "helpButton")
            {
                QueryName = ctx.QueryName,
                Order = 1000,
            };

            return new ToolBarButton[] { btn };
        }

        public class HeloToolBarButton : ToolBarButton
        {
            public object QueryName;
            public string Prefix;

            public HeloToolBarButton(string prefix, string idToAppend)
                : base(prefix, idToAppend)
            {
                this.Prefix = prefix;
            }

            public override MvcHtmlString ToHtml(HtmlHelper helper)
            {
                var a = new HtmlTag("button").Id(this.Id)
                    .Class("btn btn-default btn-help")
                    .Class(HelpLogic.GetQueryHelp(QueryName).HasEntity ? "hasItems" : null)
                    .Attr("type", "button")
                    .SetInnerText("?");

                var query = HelpLogic.GetQueryHelpService(this.QueryName);

                var jsType = new
                {
                    QueryName = QueryUtils.GetQueryUniqueKey(query.QueryName),
                    Info = query.Info,
                    Columns = query.Columns,
                };

                var result = new HtmlTag("div").Class("btn-group").InnerHtml(a).ToHtml();

                result = result.Concat(helper.ScriptCss("~/Help/Content/helpWidget.css"));
                result = result.Concat(MvcHtmlString.Create("<script>$('#" + this.Id + "').on('mouseup', function(event){ if(event.which == 3) return; " +
                        HelpClient.WidgetModule["searchClick"](JsFunction.This, this.Prefix, jsType, helper.UrlHelper().Action((HelpController c) => c.ComplexColumns())).ToString() +
                        " })</script>"));

                return result;
            }

        }

        static IWidget WidgetsHelper_GetWidget(WidgetContext ctx)
        {
            if (ctx.Entity is Entity)
                return new HelpButton { Prefix = ctx.Prefix, RootType = ctx.TypeContext.PropertyRoute.RootType };
            return null;
        }

        class HelpButton : IWidget
        {
            public string Prefix;
            public Type RootType;

            public MvcHtmlString ToHtml(HtmlHelper helper)
            {
                HtmlStringBuilder sb = new HtmlStringBuilder();
                using (sb.SurroundLine("li"))
                {
                    sb.Add(helper.ScriptCss("~/Help/Content/helpWidget.css"));

                    var id = TypeContextUtilities.Compose(Prefix, "helpButton");

                    sb.Add(new HtmlTag("button").Id(id)
                        .Class("btn btn-xs btn-help btn-help-widget")
                        .Class(HelpLogic.GetEntityHelp(RootType).HasEntity ? "hasItems" : null)
                        .Attr("type", "button")
                        .SetInnerText("?"));

                    var type = HelpLogic.GetEntityHelpService(this.RootType);

                    var jsType = new
                    {
                        Type = TypeLogic.GetCleanName(type.Type),
                        Info = type.Info,
                        Operations = type.Operations.ToDictionary(a => a.Key.Key, a => a.Value),
                        Properties = type.Properties.ToDictionary(a => a.Key.ToString(), a => a.Value),
                    };

                    sb.Add(MvcHtmlString.Create("<script>$('#" + id + "').on('mouseup', function(event){ if(event.which == 3) return; " +
                        HelpClient.WidgetModule["entityClick"](JsFunction.This, this.Prefix, jsType, helper.UrlHelper().Action((HelpController c) => c.PropertyRoutes())).ToString() +
                        " })</script>"));
                }

                return sb.ToHtml();
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
            else if (result.SearchString.HasText())
                html = html.Concat(" \"{0}\"".FormatHtml(result.SearchString));
            else
                html = html.Concat(this.ColoredSpan(typeof(TypeEntity).NiceName() + "...", "lightgray"));

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
