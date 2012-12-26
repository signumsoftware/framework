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
using Signum.Web.Properties;
using Signum.Engine;
using Signum.Engine.DynamicQuery;
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


            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            Navigator.SetTokens(queryDescription, findOptions.FilterOptions);
            Navigator.SetTokens(queryDescription, findOptions.OrderOptions);
            Navigator.SetTokens(queryDescription, findOptions.ColumnOptions);
            Navigator.SetSearchViewableAndCreable(findOptions);

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

            JsFindOptions foptions = new JsFindOptions
            {
                Prefix = options.PopupViewPrefix,
                FindOptions = findOptions
            };

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
                    .Attr("href", options.Href.HasText() ? options.Href : foptions.FindOptions.ToString())
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
                        { "onclick", "SF.FindNavigator.openFinder({0});".Formato(foptions.ToJS()) },
                        { "data-icon", "ui-icon-circle-arrow-e" },
                        { "data-text", false}
                    };

                sb.Add(helper.Href(options.PopupViewPrefix + "csbtnView",
                      Resources.LineButton_View,
                      "",
                      Resources.LineButton_View,
                      "sf-line-button sf-view",
                      htmlAttr));
            }

            if (options.WriteQueryName == WriteQueryName.Field)
                return helper.Field(QueryUtils.GetNiceName(findOptions.QueryName), sb.ToHtml());

            return sb.ToHtml();
        }

        public static MvcHtmlString NewFilter(this HtmlHelper helper, object queryName, FilterOption filterOptions, Context context, int index)
        {
            HtmlStringBuilder sb = new HtmlStringBuilder();

            if (filterOptions.Token == null)
            {
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                filterOptions.Token = QueryUtils.Parse(filterOptions.ColumnName, qd);
            }

            FilterType filterType = QueryUtils.GetFilterType(filterOptions.Token.Type);
            List<FilterOperation> possibleOperations = QueryUtils.GetFilterOperations(filterType);

            using (sb.Surround(new HtmlTag("tr").IdName(context.Compose("trFilter", index.ToString()))))
            {
                using (sb.Surround("td"))
                {
                    if (!filterOptions.Frozen)
                    {
                        var htmlAttr = new Dictionary<string, object>
                        {
                            { "data-icon", "ui-icon-close" },
                            { "data-text", false},
                            { "onclick", "SF.FindNavigator.deleteFilter(this)" },
                        };
                        sb.AddLine(helper.Href(
                            context.Compose("btnDelete", index.ToString()),
                            Resources.FilterBuilder_DeleteFilter,
                            "",
                            Resources.FilterBuilder_DeleteFilter,
                            "sf-button",
                            htmlAttr));
                    }
                }

                using (sb.Surround(new HtmlTag("td")))
                {
                    sb.AddLine(helper.HiddenAnonymous(filterOptions.Token.FullKey()));

                    foreach (var t in filterOptions.Token.FollowC(tok => tok.Parent).Reverse())
                    {
                        sb.AddLine(new HtmlTag("span")
                            .Class("sf-filter-token ui-widget-content ui-corner-all")
                            .Attr("title", t.NiceTypeName)
                            .Attr("style", "color:" + t.TypeColor)
                            .SetInnerText(t.ToString()).ToHtml());
                    }
                }

                using (sb.Surround("td"))
                {
                    sb.AddLine(
                        helper.DropDownList(
                        context.Compose("ddlSelector", index.ToString()),
                        possibleOperations.Select(fo =>
                            new SelectListItem
                            {
                                Text = fo.NiceToString(),
                                Value = fo.ToString(),
                                Selected = fo == filterOptions.Operation
                            }),
                            (filterOptions.Frozen) ? new Dictionary<string, object> { { "disabled", "disabled" } } : null));
                }

                using (sb.Surround("td"))
                {
                    Context valueContext = new Context(context, "value_" + index.ToString());

                    if (filterOptions.Frozen && !filterOptions.Token.Type.IsLite())
                    {
                        string txtValue = (filterOptions.Value != null) ? filterOptions.Value.ToString() : "";
                        sb.AddLine(helper.TextBox(valueContext.ControlID, txtValue, new { @readonly = "readonly" }));
                    }
                    else
                        sb.AddLine(PrintValueField(helper, valueContext, filterOptions));
                }
            }

            return sb.ToHtml();
        }

        public static Func<QueryToken, bool> AllowSubTokens = null; 

        public static MvcHtmlString QueryTokenCombo(this HtmlHelper helper, QueryToken previous, QueryToken selected, Context context, int index, object queryName, Func<QueryToken, List<QueryToken>> subTokens)
        {
            if (previous != null && AllowSubTokens != null && !AllowSubTokens(previous))
                return MvcHtmlString.Create("");

            var queryTokens = subTokens(previous);

            if (queryTokens.IsEmpty())
                return MvcHtmlString.Create("");

            var options = new HtmlStringBuilder();
            options.AddLine(new HtmlTag("option").Attr("value", "").SetInnerText("-").ToHtml());
            foreach (var qt in queryTokens)
            {
                var option = new HtmlTag("option")
                            .Attr("value", qt.Key)
                            .SetInnerText(qt.SubordinatedToString);

                if (selected != null && qt.Key == selected.Key)
                    option.Attr("selected", "selected");

                option.Attr("title", qt.NiceTypeName);
                option.Attr("style", "color:" + qt.TypeColor);

                string canColumn = QueryUtils.CanColumn(qt);
                if (canColumn.HasText())
                    option.Attr("data-column", canColumn);

                string canFilter = QueryUtils.CanFilter(qt);
                if (canFilter.HasText())
                    option.Attr("data-filter", canFilter);

                options.AddLine(option.ToHtml());
            }

            HtmlTag dropdown = new HtmlTag("select")
                .IdName(context.Compose("ddlTokens_" + index))
                .InnerHtml(options.ToHtml())
                .Attr("onchange", "SF.FindNavigator.newSubTokensCombo('{0}','{1}',{2})".Formato(Navigator.ResolveWebQueryName(queryName), context.ControlID, index));

            if(selected != null)
            {
                dropdown.Attr("title", selected.NiceTypeName);
                dropdown.Attr("style", "color:" + selected.TypeColor);
            }

            return dropdown.ToHtml();
        }

        public static MvcHtmlString QueryTokenBuilder(this HtmlHelper helper, QueryToken queryToken, Context context, QueryDescription desc)
        {
            return helper.QueryTokenBuilder(queryToken, context, desc.QueryName, qt => QueryUtils.SubTokens(qt, desc.Columns));
        }

        public static MvcHtmlString QueryTokenBuilder(this HtmlHelper helper, QueryToken queryToken, Context context, object queryName, Func<QueryToken, List<QueryToken>> subTokens)
        {
            var tokenPath = queryToken.FollowC(qt => qt.Parent).Reverse().NotNull().ToList();

            HtmlStringBuilder sb = new HtmlStringBuilder();

            for (int i = 0; i < tokenPath.Count; i++)
            {
                sb.AddLine(helper.QueryTokenCombo(i == 0 ? null : tokenPath[i - 1], tokenPath[i], context, i, queryName, subTokens));
            }

            sb.AddLine(helper.QueryTokenCombo(queryToken, null, context, tokenPath.Count, queryName, subTokens));

            return sb.ToHtml();
        }

        private static MvcHtmlString PrintValueField(HtmlHelper helper, Context parent, FilterOption filterOption)
        {
            var implementations = filterOption.Token.GetImplementations(); 

            if (filterOption.Token.Type.IsLite())
            {
                Lite<IIdentifiable> lite = (Lite<IIdentifiable>)Common.Convert(filterOption.Value, filterOption.Token.Type);
                if (lite != null && string.IsNullOrEmpty(lite.ToString()))
                    Database.FillToString(lite);

                Type cleanType = Lite.Extract(filterOption.Token.Type);
                if (Reflector.IsLowPopulation(cleanType) && !cleanType.IsInterface && !implementations.Value.IsByAll)
                {
                    EntityCombo ec = new EntityCombo(filterOption.Token.Type, lite, parent, "", filterOption.Token.GetPropertyRoute())
                    {
                        Implementations = implementations.Value,
                    };
                    EntityBaseHelper.ConfigureEntityButtons(ec, filterOption.Token.Type.CleanType());
                    ec.LabelVisible = false;
                    ec.Create = false;
                    ec.ReadOnly = filterOption.Frozen;
                    return EntityComboHelper.InternalEntityCombo(helper, ec);
                }
                else
                {
                    EntityLine el = new EntityLine(filterOption.Token.Type, lite, parent, "", filterOption.Token.GetPropertyRoute())
                    {
                        Implementations = implementations.Value,
                    };

                    if (el.Implementations.Value.IsByAll)
                        el.Autocomplete = false;

                    EntityBaseHelper.ConfigureEntityButtons(el, filterOption.Token.Type.CleanType());
                    el.LabelVisible = false;
                    el.Create = false;
                    el.ReadOnly = filterOption.Frozen;

                    return EntityLineHelper.InternalEntityLine(helper, el);
                }
            }
            else if (filterOption.Token.Type.IsEmbeddedEntity())
            {
                EmbeddedEntity lite = (EmbeddedEntity)Common.Convert(filterOption.Value, filterOption.Token.Type);
                EntityLine el = new EntityLine(filterOption.Token.Type, lite, parent, "", filterOption.Token.GetPropertyRoute())
                {
                    Implementations = null,
                };
                EntityBaseHelper.ConfigureEntityButtons(el, filterOption.Token.Type.CleanType());
                el.LabelVisible = false;
                el.Create = false;
                el.ReadOnly = filterOption.Frozen;

                return EntityLineHelper.InternalEntityLine(helper, el);
            }
            else
            {
                ValueLineType vlType = ValueLineHelper.Configurator.GetDefaultValueLineType(filterOption.Token.Type);
                return ValueLineHelper.Configurator.Constructor[vlType](
                        helper, new ValueLine(filterOption.Token.Type, filterOption.Value, parent, "", filterOption.Token.GetPropertyRoute()));
            }

            throw new InvalidOperationException("Invalid filter for type {0}".Formato(filterOption.Token.Type.Name));
        }
    }
}
