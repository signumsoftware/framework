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
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;

namespace Signum.Web.Omnibox
{
    public static class OmniboxClient
    {
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Omnibox/Scripts/Omnibox");
        static Dictionary<Type, OmniboxProviderBase> Providers = new Dictionary<Type, OmniboxProviderBase>();

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
            Providers[typeof(T)] = provider;
        }

        public static MvcHtmlString Render(OmniboxResult result)
        {
            var helpResult = result as HelpOmniboxResult;
            if (helpResult != null)
            {
                var innerHtml = MvcHtmlString.Create(helpResult.Text.Replace("(", "<b>").Replace(")", "</b>"));
                
                if (helpResult.OmniboxResultType != null)
                {
                    var icon = Providers[helpResult.OmniboxResultType].Icon();
                    innerHtml = icon.Concat(innerHtml);
                }

                return new HtmlTag("span").InnerHtml(innerHtml)
                    .Attr("style", "font-style: italic;")
                    .ToHtml();
            }
            else
                return Providers[result.GetType()].RenderHtmlUntyped(result);
        }

        public static string GetUrl(OmniboxResult result)
        {
           if(result is HelpOmniboxResult)
               return null;

           return Providers[result.GetType()].GetUrlUntyped(result);
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

        public abstract class OmniboxProviderBase
        {
            public abstract MvcHtmlString Icon();

            public abstract MvcHtmlString RenderHtmlUntyped(OmniboxResult result);
            public abstract string GetUrlUntyped(OmniboxResult result);

            public MvcHtmlString ColoredSpan(string text, string colorName)
            {
                return new HtmlTag("span")
                    .InnerHtml(new MvcHtmlString(text))
                    .Attr("style", "color:{0}; padding: .2em .4em; line-height: 1.6em;".Formato(colorName)).ToHtml();
            }

            public MvcHtmlString ColoredGlyphicon(string icon, string colorName)
            {
                return new HtmlTag("span")
                    .Class("glyphicon")
                    .Class(icon)
                    .Attr("style", "color:{0}".Formato(colorName))
                    .ToHtml();
            }
        }

        public abstract class OmniboxProvider<T>: OmniboxProviderBase where T : OmniboxResult
        {
            public abstract OmniboxResultGenerator<T> CreateGenerator();
            public abstract MvcHtmlString RenderHtml(T result);
            public abstract string GetUrl(T result);
            
            public override MvcHtmlString RenderHtmlUntyped(OmniboxResult result)
            {
                return RenderHtml((T)result);
            }

            public override string GetUrlUntyped(OmniboxResult result)
            {
                return GetUrl((T)result);
            }
        }

        public class WebOmniboxManager : OmniboxManager
        {
            public override bool AllowedType(Type type)
            {
                return Navigator.IsNavigable(type, null, isSearch: true);
            }

            public override bool AllowedPermission(PermissionSymbol permission)
            {
                return permission.IsAuthorized();
            }

            public override bool AllowedQuery(object queryName)
            {
                return Finder.IsFindable(queryName);
            }

            public override Lite<IdentifiableEntity> RetrieveLite(Type type, int id)
            {
                if (!Database.Exists(type, id))
                    return null;
                return Database.FillToString(Lite.Create(type, id));
            }

            public override QueryDescription GetDescription(object queryName)
            {
                return DynamicQueryManager.Current.QueryDescription(queryName);
            }

            public override List<Lite<IdentifiableEntity>> Autocomplete(Implementations implementations, string subString, int count)
            {
                if (string.IsNullOrEmpty(subString))
                    return new List<Lite<IdentifiableEntity>>();

                return AutocompleteUtils.FindLiteLike(implementations, subString, 5);
            }

            protected override IEnumerable<object> GetAllQueryNames()
            {
                return DynamicQueryManager.Current.GetQueryNames();
            }

            protected override IEnumerable<Type> GetAllTypes()
            {
                return Schema.Current.Tables.Keys;
            }
        }
    }
}