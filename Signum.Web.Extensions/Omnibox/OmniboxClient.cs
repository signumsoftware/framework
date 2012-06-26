using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Signum.Entities.Omnibox;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using Signum.Engine;
using Signum.Engine.Maps;
using System.Web.Mvc;
using Signum.Utilities;

namespace Signum.Web.Omnibox
{
    public static class OmniboxClient
    {
        public static Polymorphic<Func<OmniboxResult, MvcHtmlString>> RenderHtml = new Polymorphic<Func<OmniboxResult, MvcHtmlString>>();

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(OmniboxClient));

                OmniboxParser.Manager = new WebOmniboxManager();
            }
        }

        public static void Register<T>(this OmniboxProvider<T> provider) where T : OmniboxResult
        {
            OmniboxParser.Generators.Add(provider.CreateGenerator());
            RenderHtml.Register(new Func<T, MvcHtmlString>(provider.RenderHtml));
        }

        public static MvcHtmlString ToHtml(this OmniboxMatch match)
        {
            MvcHtmlString html = MvcHtmlString.Empty;
            foreach (var item in match.BoldSpans())
            {
                html = html.Concat(item.Item2 ? 
                    new HtmlTag("b").SetInnerText(item.Item1).ToHtml() : 
                    new HtmlTag("span").SetInnerText(item.Item1).ToHtml());
            }
            return html;
        }

        public abstract class OmniboxProvider<T> where T : OmniboxResult
        {
            public abstract OmniboxResultGenerator<T> CreateGenerator();
            public abstract MvcHtmlString RenderHtml(T result);

            public MvcHtmlString ColoredSpan(string text, string colorName)
            { 
                return new HtmlTag("span").InnerHtml(new MvcHtmlString(text)).Attr("style", "color:{0}".Formato(colorName)).ToHtml(); 
            }
        }

        public class WebOmniboxManager : OmniboxManager
        {
            public override bool AllowedType(Type type)
            {
                return Navigator.IsViewable(type, EntitySettingsContext.Admin);
            }

            public override Lite RetrieveLite(Type type, int id)
            {
                if (!Database.Exists(type, id))
                    return null;
                return Database.FillToString(Lite.Create(type, id));
            }

            public override bool AllowedQuery(object queryName)
            {
                return Navigator.IsFindable(queryName);
            }

            public override QueryDescription GetDescription(object queryName)
            {
                return DynamicQueryManager.Current.QueryDescription(queryName);
            }

            public override List<Lite> AutoComplete(Type cleanType, Implementations implementations, string subString, int count)
            {
                if (string.IsNullOrEmpty(subString))
                    return new List<Lite>();

                return AutoCompleteUtils.FindLiteLike(cleanType, implementations, subString, 5);
            }
        }
    }
}