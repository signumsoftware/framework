using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Web.Controllers;
using Newtonsoft.Json.Linq;

namespace Signum.Web
{
    public class SearchControl
    {
        public string Prefix { get; internal set; }
        public ToolBarButton[] ToolBarButton { get; set; }
        public bool AvoidFullScreenButton { get; set; }
    }

    public class CountSearchControl
    {
        public bool Navigate { get; set; }
        public bool View { get; set; }
        public string QueryLabelText { get; set; }
        public string Href { get; set; }
    }

    public static class SearchControlHelper
    {
        public static MvcHtmlString SearchControl(this HtmlHelper helper, FindOptions findOptions, Context context)
        {
            return SearchControl(helper, findOptions, context, null);
        }

        public static MvcHtmlString SearchControl(this HtmlHelper helper, FindOptions findOptions, Context context, Action<SearchControl> settingsModifier)
        {
            var options = new SearchControl { Prefix = context.Prefix }; 
            if (settingsModifier != null)
                settingsModifier(options);

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            FilterOption.SetFilterTokens(findOptions.FilterOptions, description, false);
            OrderOption.SetOrderTokens(findOptions.OrderOptions, description, false);
            ColumnOption.SetColumnTokens(findOptions.ColumnOptions, description, false);
            Finder.Manager.SetSearchViewableAndCreable(findOptions, description);
            FinderManager.SetDefaultOrder(findOptions, description);

            var viewData = new ViewDataDictionary(context);
            viewData[ViewDataKeys.FindOptions] = findOptions;
            viewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            viewData[ViewDataKeys.Title] = helper.ViewData.ContainsKey(ViewDataKeys.Title) ?
                helper.ViewData[ViewDataKeys.Title] : QueryUtils.GetNiceName(findOptions.QueryName);

            if (!options.ToolBarButton.IsNullOrEmpty())
                viewData[ViewDataKeys.ManualToolbarButtons] = options.ToolBarButton;

            if (options.AvoidFullScreenButton)
                viewData[ViewDataKeys.AvoidFullScreenButton] = true;

            return helper.Partial(Finder.Manager.SearchControlView, viewData);
        }

      

        public static MvcHtmlString CountSearchControlSpan(this HtmlHelper helper, FindOptions findOptions, Context context, Action<CountSearchControl> settingsModifier = null)
        {
            var options = new CountSearchControl();
            if (settingsModifier != null)
                settingsModifier(options);

            return "{0} {1}".FormatHtml(
            options.QueryLabelText ?? QueryUtils.GetNiceName(findOptions.QueryName),
            CountSearchControlInternal(findOptions, options, context));
        }

        public static MvcHtmlString CountSearchControlValue(this HtmlHelper helper, FindOptions findOptions, Context context, Action<CountSearchControl> settingsModifier)
        {
            var options = new CountSearchControl();
            if (settingsModifier != null)
                settingsModifier(options);

            return CountSearchControlInternal(findOptions, options, context);
        }

        public static MvcHtmlString CountSearchControl(this HtmlHelper helper, FindOptions findOptions, Context context, Action<CountSearchControl> settingsModifier)
        {
            var options = new CountSearchControl();
            if (settingsModifier != null)
                settingsModifier(options);

            var val = CountSearchControlInternal(findOptions, options, context); 

            return helper.FormGroup(context, context.Prefix, options.QueryLabelText ?? QueryUtils.GetNiceName(findOptions.QueryName),
                   new HtmlTag("p").Class("form-control-static").InnerHtml(val));
        }

        private static MvcHtmlString CountSearchControlInternal(FindOptions findOptions, Web.CountSearchControl options, Context context)
        {
            findOptions.SearchOnLoad = true;

          

            HtmlStringBuilder sb = new HtmlStringBuilder();

            if (options.Navigate)
            {
                sb.Add(new HtmlTag("a").Id(context.Prefix)
                    .Class("count-search")
                    .Attr("href", options.Href.HasText() ? options.Href : findOptions.ToString())
                    .SetInnerText("..."));
            }
            else
            {
                sb.Add(new HtmlTag("span").Id(context.Prefix)
                    .Class("count-search")
                    .SetInnerText("..."));
            }

            if (options.View)
            {
                sb.Add(new HtmlTag("a", context.Compose("csbtnView"))
                  .Class("sf-line-button sf-view")
                  .Attr("title", EntityControlMessage.View.NiceToString())
                  .Attr("onclick", JsModule.Finder["explore"](findOptions.ToJS(context.Compose("New"))).ToString())
                  .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-arrow-right")));
            }

            var function = new JsFunction(JsModule.Finder, "count",
                findOptions.ToJS(context.Prefix),
                new JRaw("'" + context.Prefix + "'.get()"));

            sb.Add(MvcHtmlString.Create("<script>" + function.ToHtmlString() + "</script>"));

            return sb.ToHtml();
        }


        public static QueryTokenBuilderSettings GetQueryTokenBuilderSettings(QueryDescription qd, SubTokensOptions options)
        {
            return new QueryTokenBuilderSettings(qd, options)
            {
                Decorators = new Action<QueryToken, HtmlTag>(CanColumnDecorator) + new Action<QueryToken, HtmlTag>(CanFilterDecorator),
                ControllerUrl = RouteHelper.New().Action("NewSubTokensCombo", "Finder"),
                RequestExtraJSonData = null
            };
        }

        public static void CanColumnDecorator(QueryToken qt, HtmlTag option)
        {
            string canColumn = QueryUtils.CanColumn(qt);
            if (canColumn.HasText())
                option.Attr("data-column", canColumn);
        }

        public static void CanFilterDecorator(QueryToken qt, HtmlTag option)
        {
            string canFilter = QueryUtils.CanFilter(qt);
            if (canFilter.HasText())
                option.Attr("data-filter", canFilter);
        }


        public static MvcHtmlString Header(Column col, OrderType? orderType)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();
            using (sb.SurroundLine(new HtmlTag("th")
                .Attr("draggable", "true")
                .Attr("data-column-name", col.Name)
                .Attr("data-nice-name", col.Token.NiceName())))
            {
                sb.Add(new HtmlTag("span").Class("sf-header-sort")
                    .Class(orderType == null ? null :
                    orderType == OrderType.Ascending ? "asc" : "desc"));

                sb.Add(new HtmlTag("span").SetInnerText(col.DisplayName));
            }
            return sb.ToHtml();
        }
    }
}
