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

            Navigator.SetTokens(findOptions.QueryName, findOptions.FilterOptions);
            Navigator.SetTokens(findOptions.QueryName, findOptions.OrderOptions);
            Navigator.SetViewableAndCreable(findOptions);

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
                    .Attr("href", foptions.FindOptions.ToString())
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
                        sb.AddLine(new HtmlTag("span").Class("sf-filter-token ui-widget-content ui-corner-all").SetInnerText(t.ToString()).ToHtml());
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

        public static MvcHtmlString RootTokensCombo(QueryDescription queryDescription, QueryToken queryToken)
        {
            var columns = new HtmlStringBuilder();
            columns.AddLine(new HtmlTag("option").Attr("value", "").SetInnerText("-").ToHtml());
            foreach (var c in queryDescription.Columns)
            {
                var option = new HtmlTag("option")
                    .Attr("value", c.Name)
                    .SetInnerText(c.DisplayName);

                if (queryToken != null && c.Name == queryToken.Key)
                    option.Attr("selected", "selected");

                columns.AddLine(option.ToHtml());
            }

            return columns.ToHtml();
        }

        public static MvcHtmlString TokensCombo(List<QueryToken> queryTokens, QueryToken selected)
        {
            var options = new HtmlStringBuilder();
            options.AddLine(new HtmlTag("option").Attr("value", "").SetInnerText("-").ToHtml());
            foreach (var qt in queryTokens)
            {
                var option = new HtmlTag("option")
                            .Attr("value", qt.Key)
                            .SetInnerText(qt.ToString());

                if (selected != null && qt.Key == selected.Key)
                    option.Attr("selected", "selected");

                string canColumn = QueryUtils.CanColumn(qt);
                if (canColumn.HasText())
                    option.Attr("data-column", canColumn);

                string canFilter = QueryUtils.CanFilter(qt);
                if (canFilter.HasText())
                    option.Attr("data-filter", canFilter);

                options.AddLine(option.ToHtml());
            }

            return options.ToHtml();
        }

        public static MvcHtmlString TokenOptionsCombo(this HtmlHelper helper, object queryName, MvcHtmlString options, Context context, int index, bool writeExpander)
        {
            MvcHtmlString expander = null;
            if (writeExpander)
                expander = helper.TokensComboExpander(context, index);

            HtmlTag dropdown = new HtmlTag("select").IdName(context.Compose("ddlTokens_" + index))
                .InnerHtml(options)
                .Attr("onchange", "SF.FindNavigator.newSubTokensCombo('{0}','{1}',{2})".Formato(Navigator.ResolveWebQueryName(queryName), context.ControlID, index));

            if (writeExpander)
                dropdown.Attr("style", "display:none");

            MvcHtmlString drop = dropdown.ToHtml();

            return expander == null ? drop : expander.Concat(drop);
        }

        static MvcHtmlString TokensComboExpander(this HtmlHelper helper, Context context, int index)
        {
            return helper.Span(
                context.Compose("lblddlTokens_" + index),
                "[...]",
                "sf-subtokens-expander",
                null);
        }

        public static MvcHtmlString QueryTokenCombo(this HtmlHelper helper, QueryToken queryToken, object queryName, Context context)
        {
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            var tokenPath = queryToken.FollowC(qt => qt.Parent).Reverse().NotNull().ToList();

            if (tokenPath.Count > 0)
                queryToken = tokenPath[0];

            HtmlStringBuilder sb = new HtmlStringBuilder();

            sb.AddLine(SearchControlHelper.TokenOptionsCombo(
                helper, queryName, SearchControlHelper.RootTokensCombo(qd, queryToken), context, 0, false));

            for (int i = 0; i < tokenPath.Count; i++)
            {
                QueryToken t = tokenPath[i];
                List<QueryToken> subtokens = t.SubTokens();
                if (!subtokens.IsEmpty())
                {
                    bool moreTokens = i + 1 < tokenPath.Count;
                    sb.AddLine(SearchControlHelper.TokenOptionsCombo(
                        helper, queryName, SearchControlHelper.TokensCombo(subtokens, moreTokens ? tokenPath[i + 1] : null), context, i + 1, !moreTokens));
                }
            }

            return sb.ToHtml();
        }

        private static MvcHtmlString PrintValueField(HtmlHelper helper, Context parent, FilterOption filterOption)
        {
            var implementations = filterOption.Token.GetImplementations(); 

            if (filterOption.Token.Type.IsLite())
            {
                Lite lite = (Lite)Common.Convert(filterOption.Value, filterOption.Token.Type);
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
                    Implementations = implementations.Value,
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
