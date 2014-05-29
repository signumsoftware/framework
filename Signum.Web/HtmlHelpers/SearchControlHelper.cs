#region usings
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
#endregion

namespace Signum.Web
{
    public class SearchControl
    {
        public string Prefix;
        public ToolBarButton[] ToolBarButton { get; set; }
    }

    public class CountSearchControl
    {
        public bool Navigate { get; set; }
        public string PopupViewPrefix { get; set; }
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

            Navigator.SetTokens(findOptions.FilterOptions, description, false);
            Navigator.SetTokens(findOptions.OrderOptions, description, false);
            Navigator.SetTokens(findOptions.ColumnOptions, description, false);
            Navigator.Manager.SetSearchViewableAndCreable(findOptions, description);
            Navigator.Manager.SetDefaultOrder(findOptions, description);

            var viewData = new ViewDataDictionary(context);
            viewData[ViewDataKeys.FindOptions] = findOptions;
            viewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            viewData[ViewDataKeys.Title] = helper.ViewData.ContainsKey(ViewDataKeys.Title) ?
                helper.ViewData[ViewDataKeys.Title] :
                Navigator.Manager.SearchTitle(findOptions.QueryName);

            if (!options.ToolBarButton.IsNullOrEmpty())
                viewData[ViewDataKeys.ManualToolbarButtons] = options.ToolBarButton;

            return helper.Partial(Navigator.Manager.SearchControlView, viewData);
        }

       

        private static MvcHtmlString CountSearchControlInternal(FindOptions findOptions, Web.CountSearchControl options)
        {
            findOptions.SearchOnLoad = true;

            int count = Navigator.QueryCount(new CountOptions(findOptions.QueryName)
            {
                FilterOptions = findOptions.FilterOptions
            });

            HtmlStringBuilder sb = new HtmlStringBuilder();

            if (options.Navigate)
            {
                sb.Add(new HtmlTag("a")
                    .Class("count-search").Class(count > 0 ? "count-with-results badge" : "count-no-results")
                    .Attr("href", options.Href.HasText() ? options.Href : findOptions.ToString())
                    .SetInnerText(count.ToString()));
            }
            else
            {
                sb.Add(new HtmlTag("span")
                    .Class("count-search").Class(count > 0 ? "count-with-results badge" : "count-no-results")
                    .SetInnerText(count.ToString()));
            }

            if (options.PopupViewPrefix != null)
            {
                sb.Add(new HtmlTag("a", options.PopupViewPrefix + "csbtnView")
                  .Class("sf-line-button sf-view")
                  .Attr("title", EntityControlMessage.View.NiceToString())
                  .Attr("onclick", JsModule.Finder["explore"](findOptions.ToJS(options.PopupViewPrefix)).ToString())
                  .InnerHtml(new HtmlTag("span").Class("glyphicon glyphicon-arrow-right")));
            }

            return sb.ToHtml();
        }


        public static MvcHtmlString CountSearchControlSpan(this HtmlHelper helper, FindOptions findOptions, Action<CountSearchControl> settingsModifier = null)
        {
            var options = new CountSearchControl();
            if (settingsModifier != null)
                settingsModifier(options);

            return "{0} {1}".FormatHtml(
            options.QueryLabelText ?? QueryUtils.GetNiceName(findOptions.QueryName),
            CountSearchControlInternal(findOptions, options));
        }

        public static MvcHtmlString CountSearchControlValue(this HtmlHelper helper, FindOptions findOptions, Action<CountSearchControl> settingsModifier)
        {
            var options = new CountSearchControl();
            if (settingsModifier != null)
                settingsModifier(options);

            return CountSearchControlInternal(findOptions, options);
        }

        public static MvcHtmlString CountSearchControl(this HtmlHelper helper, Context context,  FindOptions findOptions, Action<CountSearchControl> settingsModifier)
        {
            var options = new CountSearchControl();
            if (settingsModifier != null)
                settingsModifier(options);

            var val  = CountSearchControlInternal(findOptions, options); 

            return helper.FormGroup(context, null, options.QueryLabelText ?? QueryUtils.GetNiceName(findOptions.QueryName),
                   new HtmlTag("p").Class("form-control-static").InnerHtml(val));
        }

        public static QueryTokenBuilderSettings GetQueryTokenBuilderSettings(QueryDescription qd)
        {
            return new QueryTokenBuilderSettings
            {
                CanAggregate = false,
                QueryDescription = qd,
                Decorators = new Action<QueryToken, HtmlTag>(CanColumnDecorator) +
                 new Action<QueryToken, HtmlTag>(CanFilterDecorator),
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
            using (sb.Surround(new HtmlTag("th")
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
