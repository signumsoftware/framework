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
        public static string Menu = ViewPrefix.Formato("Menu");
        public static string ViewEntityPropertyUrl = ViewPrefix.Formato("EntityProperty");
        public static string NamespaceControlUrl = ViewPrefix.Formato("NamespaceControl");

        public static void Start(string wikiUrl, string imagesFolder)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                WikiUrl = wikiUrl;
                ImagesFolder = imagesFolder;

                Navigator.RegisterArea(typeof(HelpClient)); 

                RegisterHelpRoutes();

                DefaultWikiSettings = new WikiSettings(true);
                DefaultWikiSettings.TokenParser += TokenParser;
                DefaultWikiSettings.TokenParser += s =>
                {
                    try
                    {
                        WikiLink wl = LinkParser(s);
                        if (wl != null) 
                            return wl.ToHtmlString();
                    }
                    catch (Exception)
                    {
                        return new WikiLink("#", s, "unavailable").ToHtmlString();
                    }
                    return null;
                };

                DefaultWikiSettings.TokenParser += ProcessImages;

                NoLinkWikiSettings = new WikiSettings(false) { LineBreaks = false };
                NoLinkWikiSettings.TokenParser += TokenParser;
                NoLinkWikiSettings.TokenParser += s =>
                {
                    try
                    {
                        WikiLink wl = LinkParser(s);
                        if (wl != null) return wl.Text;
                    }
                    catch (Exception)
                    {
                        return new WikiLink("#", s, "unavailable").ToHtmlString();
                    }
                    return null;
                };
                NoLinkWikiSettings.TokenParser += RemoveImages;
            }
        }

        private static void RegisterHelpRoutes()
        {
            RouteTable.Routes.MapRoute(null, "Help/Appendix/{appendix}/Save", new { controller = "Help", action = "SaveAppendix"});
            RouteTable.Routes.MapRoute(null, "Help/Namespace/{namespace}/Save", new { controller = "Help", action = "SaveNamespace" });
            RouteTable.Routes.MapRoute(null, "Help/Appendix/{appendix}", new { controller = "Help", action = "ViewAppendix"});
            RouteTable.Routes.MapRoute(null, "Help/Namespace/{namespace}", new { controller = "Help", action = "ViewNamespace" });
            RouteTable.Routes.MapRoute(null, "Help/ViewTodo", new { controller = "Help", action = "ViewTodo" });
            RouteTable.Routes.MapRoute(null, "Help/Search", new { controller = "Help", action = "Search" });
            RouteTable.Routes.MapRoute(null, "Help", new { controller = "Help", action = "Index", });
            RouteTable.Routes.MapRoute(null, "Help/{entity}/Save", new { controller = "Help", action = "SaveEntity" });
            RouteTable.Routes.MapRoute(null, "Help/{entity}", new { controller = "Help", action = "ViewEntity", });
        }

        public static string WikiUrl;
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
                            HelpLogic.EntityUrl(t),
                            text.HasText() ? text : t.NiceName());

                    case WikiFormat.Hyperlink:
                        return new WikiLink(link, text);

                    case WikiFormat.OperationLink:
                        OperationSymbol operation = SymbolLogic<OperationSymbol>.TryToSymbol(link);

                        List<Type> types = OperationLogic.FindTypes(operation).Where(TypeLogic.TypeToDN.ContainsKey).ToList();
                        if (types.Count == 1)
                        {
                            return new WikiLink(
                                HelpLogic.OperationUrl(types[0], operation),
                                text.HasText() ? text : operation.NiceToString());
                        }
                        else
                        {
                            return new MultiWikiLink(operation.NiceToString())
                            {
                                Links = types.Select(currentType =>
                                    new WikiLink(
                                        HelpLogic.OperationUrl(currentType, operation),
                                        currentType.NiceName(), operation.NiceToString())).ToList()
                            };
                        }

                    case WikiFormat.PropertyLink:
                        PropertyRoute route = PropertyRoute.Parse
                            (TypeLogic.TryGetType(link.Before('.')),
                            link.After('.'));
                        //TODO: NiceToString de la propiedad
                        return new WikiLink(
                            HelpLogic.PropertyUrl(route),
                            route.Properties.ToString(p => p.NiceName(), "-"));

                    case WikiFormat.QueryLink:
                        object o = QueryLogic.TryToQueryName(link);
                        if (o as Enum != null)
                        {
                            Enum query = (Enum)o;
                            return new WikiLink(
                                HelpLogic.QueryUrl(query),
                                text.HasText() ? text : QueryUtils.GetNiceName(query));
                        }
                        else
                        {
                            Type query = (Type)o;
                            return new WikiLink(
                                HelpLogic.QueryUrl(query),
                                text.HasText() ? text : QueryUtils.GetNiceName(query));
                        }

                    case WikiFormat.WikiLink:
                        return new WikiLink(WikiUrl + link, text.HasText() ? text : link);

                    case WikiFormat.NamespaceLink:
                        NamespaceHelp nameSpace = HelpLogic.GetNamespace(link);
                        return new WikiLink(
                            HelpLogic.BaseUrl + "/Namespace/" + link,
                            text.HasText() ? text : link,
                            nameSpace != null ? "" : "unavailable");
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
}
