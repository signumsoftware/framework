using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Signum.Engine.Basics;
using Signum.Engine.Operations;
using Signum.Engine.WikiMarkup;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.Engine.Help
{
    public static class HelpWiki
    {
        public static WikiSettings DefaultWikiSettings;
        public static WikiSettings NoLinkWikiSettings;

        static HelpWiki()
        {
            DefaultWikiSettings = new WikiSettings(true);
            DefaultWikiSettings.TokenParser += s => LinkParser(s).Try(wl => wl.ToHtmlString());
            DefaultWikiSettings.TokenParser += ProcessImages;

            NoLinkWikiSettings = new WikiSettings(false) { LineBreaks = false };
            NoLinkWikiSettings.TokenParser += s => LinkParser(s).Try(wl => wl.Text);
            NoLinkWikiSettings.TokenParser += RemoveImages;
        }


        public class MultiWikiLink : WikiLink
        {

            public MultiWikiLink(string text)
                : base(null, text)
            {
            }

            public List<WikiLink> Links = new List<WikiLink>();

            public override string ToHtmlString()
            {
                return "{0} ({1})".FormatWith(Text, Links.CommaAnd(l => l.ToHtmlString()));
            }
        }


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

                        List<Type> types = OperationLogic.FindTypes(operation).Where(TypeLogic.TypeToEntity.ContainsKey).ToList();
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
                        PropertyRoute route = PropertyRoute.Parse(TypeLogic.TryGetType(link.Before('.')), link.After('.'));

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

                
                var fullUrl = HelpUrls.BaseUrl + HelpUrls.ImagesFolder + "/" + url;

                if (footer.HasText())
                {
                    //Has footer
                    return "<div class=\"image{0}\"><img alt=\"{1}\" src=\"{2}\"/><p class=\"imagedescription\">{1}</p></div>".FormatWith(position, footer, fullUrl);
                }
                else
                {
                    return "<div class=\"image{0}\"><img src=\"{1}\"/></div>".FormatWith(position, fullUrl);
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
                return "<a {0} href=\"{1}\">{2}</a>".FormatWith(
                    Class.HasText() ? "class=\"" + Class + "\"" : "",
                    Url,
                    Text);
            }
        }
    }
}
