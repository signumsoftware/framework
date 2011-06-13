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
    public class CountSearchControlOptions
    {
        public CountSearchControlOptions()
        {
            Navigate = true;
            PopupView = false;
        }

        public bool Navigate { get; set; }
        public bool PopupView { get; set; }
    }

    public static class SearchControlHelper
    {
        public static MvcHtmlString SearchControl(this HtmlHelper helper, FindOptions findOptions, Context context)
        {
            Navigator.SetTokens(findOptions.QueryName, findOptions.FilterOptions);
            Navigator.SetTokens(findOptions.QueryName, findOptions.OrderOptions);

            var viewData = new ViewDataDictionary(context);
            viewData[ViewDataKeys.FindOptions] = findOptions;
            viewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            viewData[ViewDataKeys.Title] = helper.ViewData.ContainsKey(ViewDataKeys.Title) ?
                helper.ViewData[ViewDataKeys.Title] :
                Navigator.Manager.SearchTitle(findOptions.QueryName);

            //helper.ViewData.Model = context;

            //helper.ViewData[ViewDataKeys.FindOptions] = findOptions;
            //helper.ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);
            
            //if (!helper.ViewData.ContainsKey(ViewDataKeys.Title))
            //    helper.ViewData[ViewDataKeys.Title] = Navigator.Manager.SearchTitle(findOptions.QueryName);

            return helper.Partial(Navigator.Manager.SearchControlView, viewData);
        }

        public static MvcHtmlString CountSearchControl(this HtmlHelper helper, FindOptions findOptions)
        {
            return CountSearchControl(helper, findOptions, null);
        }

        public static MvcHtmlString CountSearchControl(this HtmlHelper helper, FindOptions findOptions, string prefix)
        {
            return CountSearchControl(helper, findOptions, prefix, null);
        }

        public static MvcHtmlString CountSearchControl(this HtmlHelper helper, FindOptions findOptions, string prefix, CountSearchControlOptions options)
        {
            if (options == null)
                options = new CountSearchControlOptions();

            int count = Navigator.QueryCount(new CountOptions(findOptions.QueryName)
            {
                FilterOptions = findOptions.FilterOptions
            });

            JsFindOptions foptions = new JsFindOptions
            {
                Prefix = prefix,
                FindOptions = findOptions            
            };

           string result = options.Navigate ?
               "<a class=\"count-search sf-value-line\" href='{0}'>{1}</a>".Formato(foptions.FindOptions.ToString(), count) :
               "<span class=\"count-search sf-value-line\">{0}</span>".Formato(count);

           if (options.PopupView)
           {
                var htmlAttr = new Dictionary<string, object>
                {
                    { "onclick", "javascript:new SF.FindNavigator({0}).openFinder();".Formato(foptions.ToJS()) },
                    { "data-icon", "ui-icon-circle-arrow-e" },
                    { "data-text", false}
                };

               result += helper.Href(prefix + "csbtnView",
                     Resources.LineButton_View,
                     "",
                     Resources.LineButton_View,
                     "sf-line-button sf-view",
                     htmlAttr);
           }

           return MvcHtmlString.Create(result);
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
                            { "onclick", "new SF.FindNavigator({{prefix:\"{0}\"}}).deleteFilter(this);".Formato(context.ControlID) },
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

                    foreach(var t in filterOptions.Token.FollowC(tok => tok.Parent).Reverse())
                       sb.AddLine(new HtmlTag("span").Class("sf-filter-token").SetInnerText(t.ToString()).ToHtml());
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

        public static MvcHtmlString TokensCombo(this HtmlHelper helper, object queryName, IEnumerable<SelectListItem> items, Context context, int index, bool writeExpander)
        {
            MvcHtmlString expander = null;
            if (writeExpander)
                expander = helper.TokensComboExpander(context, index);

            MvcHtmlString drop = helper.DropDownList(context.Compose("ddlTokens_" + index), items,
                new
                {
                    style = (writeExpander) ? "display:none" : "",
                    onchange = "javascript:new SF.FindNavigator({{prefix:\"{0}\",webQueryName:\"{1}\"}})".Formato(context.ControlID, Navigator.ResolveWebQueryName(queryName)) + 
                               ".newSubTokensCombo(" + index + ",'" + RouteHelper.New().SignumAction("NewSubTokensCombo") + "');"
                });

            return expander == null? drop: expander.Concat(drop);
        }

        private static MvcHtmlString TokensComboExpander(this HtmlHelper helper, Context context, int index)
        { 
            return helper.Span(
                context.Compose("lblddlTokens_" + index), 
                "[...]",
                "sf-subtokens-expander",
                new Dictionary<string, object>
                { 
                    { "onclick", "$('#{0}').remove();$('#{1}').show().focus().click();".Formato(context.Compose("lblddlTokens_" + index), context.Compose("ddlTokens_" + index))}
                });
        }

        public static MvcHtmlString QueryTokenCombo(this HtmlHelper helper, QueryToken queryToken, Context context)
        {
            var queryName = helper.ViewData[ViewDataKeys.QueryName];
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            
            var tokenPath = queryToken.FollowC(qt => qt.Parent).Reverse().NotNull().ToList();

            if (tokenPath.Count > 0)
                queryToken = tokenPath[0];

            HtmlStringBuilder sb = new HtmlStringBuilder();

            var items = qd.Columns.Select(c => new SelectListItem
            {
                Text = c.DisplayName,
                Value = c.Name,
                Selected = queryToken != null && c.Name == queryToken.Key
            }).ToList();

            items.Insert(0, new SelectListItem { Text = "-", Selected = true, Value = "" });
            sb.AddLine(SearchControlHelper.TokensCombo(helper, queryName, items, context, 0, false));
            
            for (int i = 0; i < tokenPath.Count; i++)
            {
                QueryToken t = tokenPath[i];
                QueryToken[] subtokens = t.SubTokens();
                if (subtokens != null)
                {
                    var subitems = subtokens.Select(qt => new SelectListItem
                    {
                        Text = qt.ToString(),
                        Value = qt.Key,
                        Selected = i + 1 < tokenPath.Count && qt.Key == tokenPath[i+1].Key
                    }).ToList();
                    subitems.Insert(0, new SelectListItem { Text = "-", Selected = true, Value = "" });
                    sb.AddLine(SearchControlHelper.TokensCombo(helper, queryName, subitems, context, i + 1, (i + 1 >= tokenPath.Count)));
                }
            }
            
            return sb.ToHtml();
        }

        private static MvcHtmlString PrintValueField(HtmlHelper helper, Context parent, FilterOption filterOption)
        {
            if (filterOption.Token.Type.IsLite())
            {
                Lite lite = (Lite)Common.Convert(filterOption.Value, filterOption.Token.Type); 
                if (lite != null && string.IsNullOrEmpty(lite.ToStr))
                    Database.FillToStr(lite);

                Type cleanType = Reflector.ExtractLite(filterOption.Token.Type);
                if (Reflector.IsLowPopulation(cleanType) && !cleanType.IsInterface && !(filterOption.Token.Implementations() is ImplementedByAllAttribute) && (cleanType != typeof(IdentifiableEntity)))
                {
                    EntityCombo ec = new EntityCombo(filterOption.Token.Type, lite, parent, "", filterOption.Token.GetPropertyRoute())
                    {
                        Implementations = filterOption.Token.Implementations(),
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
                         Implementations = filterOption.Token.Implementations(),
                    };
                    EntityBaseHelper.ConfigureEntityButtons(el, filterOption.Token.Type.CleanType());
                    el.LabelVisible = false;
                    el.Create = false;
                    el.ReadOnly = filterOption.Frozen;

                    return EntityLineHelper.InternalEntityLine(helper, el);
                }
            }
            else
            {
                ValueLineType vlType = ValueLineHelper.Configurator.GetDefaultValueLineType(filterOption.Token.Type);
                return ValueLineHelper.Configurator.Constructor[vlType](
                        helper,  new ValueLine(filterOption.Token.Type, filterOption.Value, parent, "", filterOption.Token.GetPropertyRoute()));
            }

            throw new InvalidOperationException("Invalid filter for type {0}".Formato(filterOption.Token.Type.Name));
        }
    }
}
