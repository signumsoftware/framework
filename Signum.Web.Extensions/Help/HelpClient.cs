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
        public static string Menu = ViewPrefix.Formato("Buttons");
        public static string MiniMenu = ViewPrefix.Formato("MiniMenu");
        public static string ViewEntityPropertyUrl = ViewPrefix.Formato("EntityProperty");
        public static string NamespaceControlUrl = ViewPrefix.Formato("NamespaceControl");

        public static void Start(string imagesFolder)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ImagesFolder = imagesFolder;

                HelpUrls.EntityUrl = t => RouteHelper.New().Action((HelpController c) => c.ViewEntity(Navigator.ResolveWebTypeName(t)));
                HelpUrls.NamespaceUrl = ns => RouteHelper.New().Action((HelpController c) => c.ViewNamespace(ns));
                HelpUrls.AppendixUrl = ap => RouteHelper.New().Action((HelpController c) => c.ViewAppendix(ap));

                Navigator.RegisterArea(typeof(HelpClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<EntityHelpDN>(),
                    new EntitySettings<QueryHelpDN>(),
                    new EntitySettings<AppendixHelpDN>(),
                    new EntitySettings<NamespaceHelpDN>(),
                    new EmbeddedEntitySettings<PropertyRouteHelpDN>(),
                    new EmbeddedEntitySettings<OperationHelpDN>(),
                    new EmbeddedEntitySettings<QueryColumnHelpDN>(),
                });
                Navigator.EmbeddedEntitySettings<PropertyRouteHelpDN>().MappingDefault.AsEntityMapping()
                    .SetProperty(a => a.Property, ctx =>
                    {
                        var type = ctx.FindParent<EntityHelpDN>().Value.Type.ToType();
                        return PropertyRoute.Parse(type, ctx.Input).ToPropertyRouteDN();
                    });

                RegisterHelpRoutes();

                DefaultWikiSettings = new WikiSettings(true);
                DefaultWikiSettings.TokenParser += TokenParser;
                DefaultWikiSettings.TokenParser += s => LinkParser(s).Try(wl => wl.ToHtmlString());
                DefaultWikiSettings.TokenParser += ProcessImages;

                NoLinkWikiSettings = new WikiSettings(false) { LineBreaks = false };
                NoLinkWikiSettings.TokenParser += TokenParser;
                NoLinkWikiSettings.TokenParser += s => LinkParser(s).Try(wl => wl.Text);
                NoLinkWikiSettings.TokenParser += RemoveImages;
            }
        }

        private static void RegisterHelpRoutes()
        {
            RouteTable.Routes.MapRoute(null, "Help/Appendix/{appendix}", new { controller = "Help", action = "ViewAppendix" });
            RouteTable.Routes.MapRoute(null, "Help/Namespace/{namespace}", new { controller = "Help", action = "ViewNamespace" });
            RouteTable.Routes.MapRoute(null, "Help/Entity/{entity}", new { controller = "Help", action = "ViewEntity", });
        }

        public static string ImagesFolder;

        public class WikiLink
        {
            public string Text { get; set; }
            public string Url { get; set; }
            public string Class { get; set; }

            public WikiLink(string url)
            {
                this.Url = url;
            }

            public WikiLink(string url, string text)
            {
                this.Url = url;
                this.Text = text;
            }

            public WikiLink(string url, string text, string @class)
            {
                this.Url = url;
                this.Text = text;
                this.Class = @class;
            }

            public virtual string ToHtmlString()
            {
                return "<a {0} href=\"{1}\">{2}</a>".Formato(
                    Class.HasText() ? "class=\"" + Class + "\"" : "",
                    Url,
                    Text);
            }
        }

        public class MultiWikiLink : WikiLink {

            public MultiWikiLink(string text) : base(null, text)
            {
            }

            public List<WikiLink> Links = new List<WikiLink>();

            public override string ToHtmlString()
            {
                return "{0} ({1})".Formato(Text, Links.CommaAnd(l=>l.ToHtmlString()));
            }
        }

        public static WikiSettings DefaultWikiSettings;
        public static WikiSettings NoLinkWikiSettings;
        public static Func<string, string> TokenParser;

        public static WikiLink LinkParser(string content)
        {
            Match m = HelpLogic.HelpLinkRegex.Match(content);

            if (m.Success)
            {
                string letter = m.Groups["letter"].ToString();
                string link = m.Groups["link"].ToString();
                string text = m.Groups["text"].ToString();

                switch (letter)
                {
                    case WikiFormat.EntityLink:
                        Type t = TypeLogic.TryGetType(link);
                        return new WikiLink(
                            HelpUrls.EntityUrl(t),
                            text.HasText() ? text : t.NiceName());

                    case WikiFormat.Hyperlink:
                        return new WikiLink(link, text);

                    case WikiFormat.OperationLink:
                        OperationSymbol operation = SymbolLogic<OperationSymbol>.TryToSymbol(link);

                        List<Type> types = OperationLogic.FindTypes(operation).Where(TypeLogic.TypeToDN.ContainsKey).ToList();
                        if (types.Count == 1)
                        {
                            return new WikiLink(
                                HelpUrls.OperationUrl(types[0], operation),
                                text.HasText() ? text : operation.NiceToString());
                        }
                        else
                        {
                            return new MultiWikiLink(operation.NiceToString())
                            {
                                Links = types.Select(currentType =>
                                    new WikiLink(
                                        HelpUrls.OperationUrl(currentType, operation),
                                        currentType.NiceName(), operation.NiceToString())).ToList()
                            };
                        }

                    case WikiFormat.PropertyLink:
                        PropertyRoute route = PropertyRoute.Parse
                            (TypeLogic.TryGetType(link.Before('.')),
                            link.After('.'));

                        while (route.PropertyRouteType == PropertyRouteType.LiteEntity || 
                               route.PropertyRouteType == PropertyRouteType.Mixin || 
                               route.PropertyRouteType == PropertyRouteType.MListItems)
                            route = route.Parent;

                        return new WikiLink(HelpUrls.PropertyUrl(route), route.PropertyInfo.NiceName());

                    case WikiFormat.QueryLink:
                        object o = QueryLogic.TryToQueryName(link);
                        if (o as Enum != null)
                        {
                            Enum query = (Enum)o;
                            return new WikiLink(
                                HelpUrls.QueryUrl(query),
                                text.HasText() ? text : QueryUtils.GetNiceName(query));
                        }
                        else
                        {
                            Type query = (Type)o;
                            return new WikiLink(
                                HelpUrls.QueryUrl(query),
                                text.HasText() ? text : QueryUtils.GetNiceName(query));
                        }

                    case WikiFormat.NamespaceLink:
                        NamespaceHelp nameSpace = HelpLogic.GetNamespaceHelp(link);
                        return new WikiLink(
                            HelpUrls.NamespaceUrl(link),
                            text.HasText() ? text : link,
                            nameSpace != null ? "" : "unavailable");

                    case WikiFormat.AppendixLink:
                        AppendixHelp appendix = HelpLogic.GetAppendixHelp(link);
                        return new WikiLink(
                            HelpUrls.AppendixUrl(link),
                            text.HasText() ? text : link,
                            appendix != null ? "" : "unavailable");
                }
            }
            return null;
        }

        static Regex ImageRegex = new Regex(@"^image(?<position>[^\|]+)\|(?<url>[^\|\]]*)(\|(?<footer>.*))?$");

        public static string ProcessImages(string content)
        {
            Match m = ImageRegex.Match(content);

            if (m.Success)
            {
                string position = m.Groups["position"].ToString();
                string url = m.Groups["url"].ToString();
                string footer = m.Groups["footer"].ToString();

                if (footer.HasText())
                {
                    //Has footer
                    return "<div class=\"image{0}\"><img alt=\"{1}\" src=\"{2}{3}\"/><p class=\"imagedescription\">{1}</p></div>".Formato(position, url, ImagesFolder, footer);
                }
                else
                {
                    return "<div class=\"image{0}\"><img src=\"{1}{2}\"/></div>".Formato(position, ImagesFolder, url);
                }
            }
            return null;
        }

        public static string RemoveImages(string content)
        {
            Match m = ImageRegex.Match(content);

            if (m.Success)
            {
                return "";
            }
            return null;
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
