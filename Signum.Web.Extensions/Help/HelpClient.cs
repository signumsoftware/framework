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
using Signum.Entities.Operations;
using Signum.Engine.Operations;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Engine;
#endregion

namespace Signum.Web.Help
{
    public static class HelpClient
    {
        //pages
        public static string ViewPrefix = "help/Views/";
        
        public static string IndexUrl = "Index";
        public static string ViewEntityUrl = "ViewEntity";
        public static string ViewAppendixUrl = "ViewAppendix";
        public static string ViewNamespaceUrl = "ViewNamespace";
        public static string TodoUrl = "ViewTodo";
        public static string SearchResults = "Search";

        //controls
        public static string Menu = "Menu";
        public static string ViewEntityPropertyUrl = "EntityProperty";
        public static string NamespaceControlUrl = "NamespaceControl";

        public static void Start(string wikiUrl, string imagesFolder)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                WikiUrl = wikiUrl;
                ImagesFolder = imagesFolder;

                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(HelpClient), "~/help/", "Signum.Web.Extensions.Help."));

                RouteTable.Routes.InsertRouteAt0("help/{resourcesFolder}/{*resourceName}",
                    new { controller = "Resources", action = "Index", area = "help" },
                    new { resourcesFolder = new InArray(new string[] { "Scripts", "Content", "Images" }) });

                RegisterHelpRoutes();

                DefaultWikiSettings = new WikiSettings(true) { TokenParser = TokenParser };
                DefaultWikiSettings.TokenParser += s =>
                {
                    try
                    {
                        WikiLink wl = LinkParser(s);
                        if (wl != null) return wl.ToHtmlString();
                    }
                    catch (Exception)
                    {
                        return new WikiLink("#", s, "unavailable").ToHtmlString();
                    }
                    return null;
                };

                DefaultWikiSettings.TokenParser += ProcessImages;

                NoLinkWikiSettings = new WikiSettings(false) { TokenParser = TokenParser, LineBreaks = false };
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
            RouteTable.Routes.InsertRouteAt0("Help/Appendix/{appendix}/Save", new { controller = "Help", action = "SaveAppendix", appendix = "" });
            RouteTable.Routes.InsertRouteAt0("Help/Namespace/{namespace}/Save", new { controller = "Help", action = "SaveNamespace", @namespace = "" });
            RouteTable.Routes.InsertRouteAt0("Help/{entity}/Save", new { controller = "Help", action = "SaveEntity", entity = "" });
            RouteTable.Routes.InsertRouteAt0("Help/Appendix/{appendix}", new { controller = "Help", action = "ViewAppendix", appendix = "" });
            RouteTable.Routes.InsertRouteAt0("Help/Namespace/{namespace}", new { controller = "Help", action = "ViewNamespace", @namespace = "" });
            RouteTable.Routes.InsertRouteAt0("Help/{entity}", new { controller = "Help", action = "ViewEntity", entity = "" });
            RouteTable.Routes.InsertRouteAt0("Help/ViewTodo", new { controller = "Help", action = "ViewTodo" });
            RouteTable.Routes.InsertRouteAt0("Help/Search", new { controller = "Help", action = "Search" });
            RouteTable.Routes.InsertRouteAt0("Help", new { controller = "Help", action = "Index", });
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

        public static class WikiFormat
        {
            public const string EntityLink = "e";
            public const string PropertyLink = "p";
            public const string QueryLink = "q";
            public const string OperationLink = "o";
            public const string HyperLink = "h";
            public const string WikiLink = "w";
            public const string NameSpaceLink = "n";

            public const string Separator = ":";
        }

        public static WikiSettings DefaultWikiSettings;
        public static WikiSettings NoLinkWikiSettings;
        public static Func<string, string> TokenParser;

        public static WikiLink LinkParser(string content)
        {
            Match m = Regex.Match(content,
                        @"\[(?<letter>[^:]+):(?<link>.*?)(\|(?<text>.*?))?\]");

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

                    case WikiFormat.HyperLink:
                        return new WikiLink(link, text);

                    case WikiFormat.OperationLink:
                        Enum operation = EnumLogic<OperationDN>.TryToEnum(link);

                            Type[] types = OperationLogic.FindTypes(operation);
                            if (types.Length == 1)
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
                        string[] parts = link.Split('.');

                        Type type = TypeLogic.GetType(parts[0]);
                        //TODO: NiceToString de la propiedad
                        return new WikiLink(
                            HelpLogic.PropertyUrl(type, parts[1]),
                            text.HasText() ? text : parts[1].NiceName());

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

                    case WikiFormat.NameSpaceLink:
                        NamespaceHelp nameSpace = HelpLogic.GetNamespace(link);
                        return new WikiLink(
                            HelpLogic.BaseUrl + "/Namespace/" + link,
                            text.HasText() ? text : link,
                            nameSpace != null ? "" : "unavailable");
                }
            }
            return null;
        }

        static Regex ImageRegex = new Regex(@"\[image(?<position>[^\|]+)\|(?<url>[^\|\]]*)(\|(?<footer>.*))?\]");

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
