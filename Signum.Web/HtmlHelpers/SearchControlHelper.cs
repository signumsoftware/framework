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
        public ToolBarButton[] QueryButtons { get; set; }
    }

    public class CountSearchControl
    {
        public bool Navigate { get; set; }
        public string PopupViewPrefix { get; set; }
        public WriteQueryName WriteQueryName { get; set; }
        public string QueryLabelText { get; set; }
        public string Href { get; set; }
    }

    public enum WriteQueryName
    {
        No,
        Span,
        Field
    }

    public static class SearchControlHelper
    {
        public static MvcHtmlString SearchControl(this HtmlHelper helper, FindOptions findOptions, Context context)
        {
            return SearchControl(helper, findOptions, context, null);
        }

        public static MvcHtmlString SearchControl(this HtmlHelper helper, FindOptions findOptions, Context context, Action<SearchControl> settingsModifier)
        {
            var options = new SearchControl();
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

            if (!options.QueryButtons.IsNullOrEmpty())
                viewData[ViewDataKeys.ManualToolbarButtons] = options.QueryButtons;

            return helper.Partial(Navigator.Manager.SearchControlView, viewData);
        }

        public static MvcHtmlString CountSearchControl(this HtmlHelper helper, FindOptions findOptions, Action<CountSearchControl> settingsModifier)
        {
            var options = new CountSearchControl();
            if (settingsModifier != null)
                settingsModifier(options);

            findOptions.SearchOnLoad = true;

            int count = Navigator.QueryCount(new CountOptions(findOptions.QueryName)
            {
                FilterOptions = findOptions.FilterOptions
            });

            HtmlStringBuilder sb = new HtmlStringBuilder();

            if (options.WriteQueryName == WriteQueryName.Span)
                sb.Add(new HtmlTag("span")
                    .Class("count-search")
                    .Class("count-search").Class(count > 0 ? "count-with-results" : "count-no-results")
                    .SetInnerText(options.QueryLabelText ?? QueryUtils.GetNiceName(findOptions.QueryName)));

            if (options.Navigate)
            {
                sb.Add(new HtmlTag("a")
                    .Class("count-search").Class(count > 0 ? "count-with-results" : "count-no-results")
                    .Attr("href", options.Href.HasText() ? options.Href : findOptions.ToString())
                    .SetInnerText(count.ToString()));
            }
            else
            {
                sb.Add(new HtmlTag("span")
                    .Class("count-search").Class(count > 0 ? "count-with-results" : "count-no-results")
                    .SetInnerText(options.WriteQueryName == WriteQueryName.Span ? " (" + count.ToString() + ")" : count.ToString()));
            }

            if (options.PopupViewPrefix != null)
            {
                var htmlAttr = new Dictionary<string, object>
                {
                    { "onclick", new JsFunction(JsFunction.FinderModule, "explore", findOptions.ToJS(options.PopupViewPrefix)) },
                    { "data-icon", "ui-icon-circle-arrow-e" },
                    { "data-text", false}
                };

                sb.Add(helper.Href(options.PopupViewPrefix + "csbtnView",
                      EntityControlMessage.View.NiceToString(),
                      "",
                      EntityControlMessage.View.NiceToString(),
                      "sf-line-button sf-view",
                      htmlAttr));
            }

            if (options.WriteQueryName == WriteQueryName.Field)
                return helper.Field(options.QueryLabelText ?? QueryUtils.GetNiceName(findOptions.QueryName), sb.ToHtml());

            return sb.ToHtml();
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

    }
}
