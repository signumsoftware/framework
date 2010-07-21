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
    public static class SearchControlHelper
    {
        public static void SearchControl(this HtmlHelper helper, FindOptions findOptions, Context context)
        {
            Navigator.SetTokens(findOptions.QueryName, findOptions.FilterOptions);
            
            helper.ViewData.Model = context;

            helper.ViewData[ViewDataKeys.FindOptions] = findOptions;
            helper.ViewData[ViewDataKeys.QueryDescription] = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);
            
            if (helper.ViewData.Keys.Count(s => s == ViewDataKeys.PageTitle) == 0)
                helper.ViewData[ViewDataKeys.PageTitle] = Navigator.Manager.SearchTitle(findOptions.QueryName);
            
            helper.Write(
                helper.RenderPartialToString(Navigator.Manager.SearchControlUrl, helper.ViewData));
        }

        public static string CountSearchControl(this HtmlHelper helper, FindOptions findOptions)
        {
            return CountSearchControl(helper, findOptions, null);
        }

        public static string CountSearchControl(this HtmlHelper helper, FindOptions findOptions, string prefix)
        {
            return CountSearchControl(helper, findOptions, prefix, true);
        }

         public static string CountSearchControl(this HtmlHelper helper, FindOptions findOptions, string prefix, bool includeLink)
        {
            int count = Navigator.QueryCount(new CountOptions(findOptions.QueryName)
            {
                FilterOptions = findOptions.FilterOptions
            });            

            JsFindOptions foptions = new JsFindOptions
            {
                Prefix = prefix,
                FindOptions = findOptions
            };

           return includeLink ?
               "<a class=\"count-search valueLine\" onclick=\"javascript:OpenFinder({0});\">{1}</a>"
               .Formato(foptions.ToJS(), count)
               : "<span class=\"count-search valueLine\">{0}</span>".Formato(count);
        }

        public static string NewFilter(this HtmlHelper helper, object queryName, FilterOption filterOptions, Context context, int index)
        {
            StringBuilder sb = new StringBuilder();

            if (filterOptions.Token == null)
            {
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                filterOptions.Token = QueryToken.Parse(qd, filterOptions.ColumnName);
            }

            FilterType filterType = QueryUtils.GetFilterType(filterOptions.Token.Type);
            List<FilterOperation> possibleOperations = QueryUtils.GetFilterOperations(filterType);

            sb.AppendLine("<tr id='{0}' name='{0}'>".Formato(context.Compose("trFilter", index.ToString())));

            sb.AppendLine("<td id='{0}' name='{0}'>".Formato(context.Compose("td" + index.ToString() + "__" + filterOptions.Token.FullKey())));
            sb.AppendLine(filterOptions.Token.FullKey());
            sb.AppendLine("</td>");

            sb.AppendLine("<td>");
            sb.AppendLine(
                helper.DropDownList(
                context.Compose("ddlSelector", index.ToString()),
                possibleOperations.Select(fo =>
                    new SelectListItem
                    {
                        Text = fo.NiceToString(),
                        Value = fo.ToString(),
                        Selected = fo == filterOptions.Operation
                    }),
                    (filterOptions.Frozen) ? new Dictionary<string, object> { { "disabled", "disabled" } } : null).ToHtmlString());
            sb.AppendLine("</td>");
            
            sb.AppendLine("<td>");
            Context valueContext = new Context(context, "value_" + index.ToString());
            
            if (filterOptions.Frozen)
            {
                string txtValue = (filterOptions.Value != null) ? filterOptions.Value.ToString() : "";
                sb.AppendLine(helper.TextBox(valueContext.ControlID, txtValue, new { @readonly = "readonly" }).ToHtmlString());
            }
            else
                sb.AppendLine(PrintValueField(helper, valueContext, filterOptions));
            sb.AppendLine("</td>");

            sb.AppendLine("<td>");
            if (!filterOptions.Frozen)
                sb.AppendLine(helper.Button(context.Compose("btnDelete", index.ToString()), "X", "DeleteFilter('{0}',this);".Formato(context.ControlID), "", null));
            sb.AppendLine("</td>");

            sb.AppendLine("</tr>");
            
            return sb.ToString();
        }

        public static string TokensCombo(this HtmlHelper helper, IEnumerable<SelectListItem> items, Context context, int index)
        {
            string result = "";
            if (index > 0)
            {
                result = helper.Span(context.Compose("lblddlTokens_" + index), "[...]", "",
                new Dictionary<string, object>
                { 
                    { "style", "cursor:pointer;margin-left:5px" },
                    { "onclick", "$('#{0}').remove();$('#{1}').show().focus().click();".Formato(context.Compose("lblddlTokens_" + index), context.Compose("ddlTokens_" + index))}
                });
            }

            result += helper.DropDownList(context.Compose("ddlTokens_" + index), items,
                new
                {
                    style = (index > 0) ? "display:none" : "",
                    onchange = "javascript:NewSubTokensCombo('" + context.ControlID + "'," + index + ");"
                }).ToString();

            return result;
        }

        //public static string QuickFilter(Controller controller, string queryUrlName, int visibleColumnIndex, int filterRowIndex, object value, string prefix, string suffix)
        //{
        //    object queryName = Navigator.ResolveQueryFromUrlName(queryUrlName);
        //    QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
        //    QueryToken token = QueryToken.NewColumn(qd.StaticColumns.Where(c => c.Visible == true).ToList()[visibleColumnIndex]);
        //    FilterOption fo = new FilterOption() { Token = token, ColumnName = null, Operation = FilterOperation.EqualTo, Value = value };
        //    Type type = Reflector.ExtractLite(fo.Token.Type) ?? fo.Token.Type;
            
        //    return NewFilter(controller, type.Name, null, token.FullKey(), filterRowIndex, prefix, suffix, FilterOperation.EqualTo, value);
        //}


        private static string PrintValueField(HtmlHelper helper, Context parent, FilterOption filterOption)
        {
            if (typeof(Lite).IsAssignableFrom(filterOption.Token.Type))
            {
                Type cleanType = Reflector.ExtractLite(filterOption.Token.Type);
                if (Reflector.IsLowPopulation(cleanType) && !cleanType.IsInterface && !(filterOption.Token.Implementations() is ImplementedByAllAttribute) && (cleanType != typeof(IdentifiableEntity)))
                {
                    EntityCombo ec = new EntityCombo(filterOption.Token.Type, filterOption.Value, parent, "", filterOption.Token.GetPropertyRoute())
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
                    EntityLine el = new EntityLine(filterOption.Token.Type, filterOption.Value, parent, "", filterOption.Token.GetPropertyRoute())
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
                        helper,
                        new ValueLine(filterOption.Token.Type, filterOption.Value, parent, "", filterOption.Token.GetPropertyRoute()));
            }

            throw new InvalidOperationException("Invalid filter for type {0}".Formato(filterOption.Token.Type.Name));
        }
    }
}
