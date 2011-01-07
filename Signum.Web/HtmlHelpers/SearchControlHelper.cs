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
        public static void SearchControl(this HtmlHelper helper, FindOptions findOptions, Context context)
        {
            Navigator.SetTokens(findOptions.QueryName, findOptions.FilterOptions);
            Navigator.SetTokens(findOptions.QueryName, findOptions.OrderOptions);

            helper.ViewData.Model = context;

            helper.ViewData[ViewDataKeys.FindOptions] = findOptions;
            helper.ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);
            
            if (helper.ViewData.Keys.Count(s => s == ViewDataKeys.PageTitle) == 0)
                helper.ViewData[ViewDataKeys.PageTitle] = Navigator.Manager.SearchTitle(findOptions.QueryName);
            
            helper.Write(helper.Partial(Navigator.Manager.SearchControlUrl, helper.ViewData));
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
               "<a class=\"count-search valueLine\" href=\"{0}\">{1}</a>".Formato(foptions.FindOptions.ToString(), count) :
               "<span class=\"count-search valueLine\">{0}</span>".Formato(count);

           if (options.PopupView)
               result += helper.Button(prefix + "csbtnView",
                  "->",
                  "javascript:OpenFinder({0});".Formato(foptions.ToJS()),
                  "lineButton go", null);

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
                using (sb.Surround(new HtmlTag("td"))) //.IdName(context.Compose("td" + index.ToString() + "__" + filterOptions.Token.FullKey()))))
                {
                    sb.AddLine(helper.HiddenAnonymous(filterOptions.Token.FullKey()));

                    foreach(var t in filterOptions.Token.FollowC(tok => tok.Parent).Reverse())
                       sb.AddLine(new HtmlTag("span").Class("filtertoken").SetInnerText(t.ToString()).ToHtml());
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

                    if (filterOptions.Frozen)
                    {
                        string txtValue = (filterOptions.Value != null) ? filterOptions.Value.ToString() : "";
                        sb.AddLine(helper.TextBox(valueContext.ControlID, txtValue, new { @readonly = "readonly" }));
                    }
                    else
                        sb.AddLine(PrintValueField(helper, valueContext, filterOptions));
                }

                using (sb.Surround("td"))
                {
                    if (!filterOptions.Frozen)
                        sb.AddLine(helper.Button(context.Compose("btnDelete", index.ToString()), "X", "DeleteFilter('{0}',this);".Formato(context.ControlID), "", null));
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
                    onchange = "javascript:NewSubTokensCombo({{prefix:\"{0}\",webQueryName:\"{1}\"}}".Formato(context.ControlID, Navigator.ResolveWebQueryName(queryName)) + "," + index + ",'" + RouteHelper.New().SignumAction("NewSubTokensCombo") + "');"
                });

            return expander == null? drop: expander.Concat(drop);
        }

        private static MvcHtmlString TokensComboExpander(this HtmlHelper helper, Context context, int index)
        { 
            return helper.Span(
                context.Compose("lblddlTokens_" + index), "[...]", "",
                new Dictionary<string, object>
                { 
                    { "style", "cursor:pointer;margin-left:5px" },
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
                Lite lite = (Lite)filterOption.Value; 
                if (string.IsNullOrEmpty(lite.ToStr))
                    Database.FillToStr(lite);

                Type cleanType = Reflector.ExtractLite(filterOption.Token.Type);
                if (Reflector.IsLowPopulation(cleanType) && !cleanType.IsInterface && !(filterOption.Token.Implementations() is ImplementedByAllAttribute) && (cleanType != typeof(IdentifiableEntity)))
                {
                    EntityCombo ec = new EntityCombo(filterOption.Token.Type, lite, parent, "", filterOption.Token.GetPropertyRoute())
                    {
                        LabelVisible = false,
                        BreakLine = false,
                        Implementations = filterOption.Token.Implementations()
                    };
                    EntityBaseHelper.ConfigureEntityBase(ec, filterOption.Token.Type.CleanType());
                    return EntityComboHelper.InternalEntityCombo(helper, ec);
                }
                else
                {
                    EntityLine el = new EntityLine(filterOption.Token.Type, lite, parent, "", filterOption.Token.GetPropertyRoute())
                    {
                        LabelVisible = false,
                        BreakLine = false,
                        Create = false,
                        Implementations = filterOption.Token.Implementations()
                    };
                    EntityBaseHelper.ConfigureEntityBase(el, filterOption.Token.Type.CleanType());
                    el.Create = false;

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
