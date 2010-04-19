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

namespace Signum.Web
{
    public static class SearchControlHelper
    {
        public delegate ToolBarButton MenuItemForQueryName(object queryName);

        public static event MenuItemForQueryName GetCustomMenuItems;

        public static string GetMenuItems(this HtmlHelper helper, object queryName, object prefix)
        {
            StringBuilder sb = new StringBuilder();
            if (GetCustomMenuItems != null)
            {
                ToolBarButton[] menus = GetCustomMenuItems.GetInvocationList().Cast<MenuItemForQueryName>().Select(d => d(queryName)).NotNull().ToArray();
                foreach (ToolBarButton mi in menus)
                {
                    throw new NotImplementedException("ConstructorFromMany operations are not supported yet");
                   // string onclick = "";
                   // string strPrefix = (prefix != null) ? ("'" + prefix.ToString() + "'") : "''";
                        
                   // //Add prefix to onclick
                   // if (!string.IsNullOrEmpty(mi.OnClick))
                   // {
                   //     if (!string.IsNullOrEmpty(mi.OnServerClickAjax) || !string.IsNullOrEmpty(mi.OnServerClickPost))
                   //         throw new ArgumentException(Resources.MenuItem0HasOnClickAndAnotherClickDefined.Formato(mi.Id));

                   //     int lastEnd = mi.OnClick.LastIndexOf(")");
                   //     int lastStart = mi.OnClick.LastIndexOf("(");
                   //     if (lastStart == lastEnd -1)
                   //         onclick = mi.OnClick.Insert(lastEnd, strPrefix);
                   //     else
                   //         onclick = mi.OnClick.Insert(lastEnd, ", " + strPrefix);
                   // }
                    
                   // //Constructo OnServerClick
                   // if (!string.IsNullOrEmpty(mi.OnServerClickAjax))
                   // {
                   //     if (!string.IsNullOrEmpty(mi.OnClick) || !string.IsNullOrEmpty(mi.OnServerClickPost))
                   //         throw new ArgumentException(Resources.MenuItem0HasOnServerClickAjaxAndAnotherClickDefined.Formato(mi.Id));
                   //     onclick = "CallServer('{0}',{1});".Formato(mi.OnServerClickAjax, strPrefix);
                   // }

                   // //Constructo OnServerClick
                   // if (!string.IsNullOrEmpty(mi.OnServerClickPost))
                   // {
                   //     if (!string.IsNullOrEmpty(mi.OnClick) || !string.IsNullOrEmpty(mi.OnServerClickAjax))
                   //         throw new ArgumentException(Resources.MenuItem0HasOnServerClickPostAndAnotherClickDefined.Formato(mi.Id));
                   //     onclick = "PostServer('{0}');".Formato(mi.OnServerClickPost);
                   // }

                   // //Add cursor pointer to the htmlProps
                   // if (!mi.HtmlProps.ContainsKey("style"))
                   //     mi.HtmlProps.Add("style", "cursor: pointer");
                   // else if (mi.HtmlProps["style"].ToString().IndexOf("cursor")==-1)
                   //     mi.HtmlProps["style"] = "cursor:pointer; " + mi.HtmlProps["style"].ToString();

                   // if(!mi.HtmlProps.ContainsKey("title"))
                   //     mi.HtmlProps["title"] = mi.AltText ?? "";

                   //// sb.Append(helper.ImageButton(mi.Id, mi.ImgSrc, mi.AltText, onclick, mi.HtmlProps));
                   // sb.Append(helper.Button(mi.Id, mi.Text, onclick, "", mi.HtmlProps));
                }
            }
            return sb.ToString();
        }

        public static void SearchControl(this HtmlHelper helper, FindOptions findOptions, Context context)
        {
            QueryDescription queryDescription =  DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);

            foreach (FilterOption opt in findOptions.FilterOptions)
                opt.Token = QueryToken.Parse(queryDescription, opt.ColumnName);
            
            helper.ViewData.Model = context;

            helper.ViewData[ViewDataKeys.FindOptions] = findOptions;
            helper.ViewData[ViewDataKeys.QueryDescription] = queryDescription;
            
            if (helper.ViewData.Keys.Count(s => s == ViewDataKeys.PageTitle) == 0)
                helper.ViewData[ViewDataKeys.PageTitle] = Navigator.Manager.SearchTitle(findOptions.QueryName);
            
            helper.Write(
                helper.RenderPartialToString(Navigator.Manager.SearchControlUrl, helper.ViewData));
        }

        public static string CountSearchControl(this HtmlHelper helper, FindOptions findOptions)
        {
            int count = Navigator.QueryCount(new CountOptions(findOptions.QueryName)
            {
                FilterOptions = findOptions.FilterOptions
            });            

            JsFindOptions foptions = new JsFindOptions
            {
                FindOptions = findOptions
            };

           return "<a class=\"count-search\" onclick=\"javascript:OpenFinder({0});\">{1}</a>".Formato(foptions.ToJS(), count);
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
                    (filterOptions.Frozen) ? new Dictionary<string, object>{{"disabled","disabled"}} : null));
            sb.AppendLine("</td>");
            
            sb.AppendLine("<td>");
            Context valueContext = new Context(context, "value_" + index.ToString());
            
            if (filterOptions.Frozen)
            {
                string txtValue = (filterOptions.Value != null) ? filterOptions.Value.ToString() : "";
                sb.AppendLine(helper.TextBox(valueContext.ControlID, txtValue, new { @readonly = "readonly" }));
            }
            else
                sb.AppendLine(PrintValueField(helper, valueContext, filterOptions));
            sb.AppendLine("</td>");

            sb.AppendLine("<td>");
            if (!filterOptions.Frozen)
                sb.AppendLine(helper.Button(context.Compose("btnDelete", index.ToString()), "X", "DeleteFilter('{0}','{1}');".Formato(context.ControlID, index), "", null));
            sb.AppendLine("</td>");

            sb.AppendLine("</tr>");
            
            return sb.ToString();
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


        private static HtmlHelper CreateHtmlHelper(Controller c)
        {
            return new HtmlHelper(
                        new ViewContext(
                            c.ControllerContext,
                            new WebFormView(c.ControllerContext.RequestContext.HttpContext.Request.FilePath),
                            c.ViewData,
                            c.TempData),
                        new ViewPage()); 
        }        

        private static string PrintValueField(HtmlHelper helper, Context parent, FilterOption filterOption)
        {
            if (typeof(Lite).IsAssignableFrom(filterOption.Token.Type))
            {
                Type cleanType = Reflector.ExtractLite(filterOption.Token.Type);
                if (Reflector.IsLowPopulation(cleanType) && !(filterOption.Token.Implementations() is ImplementedByAllAttribute))
                {
                    EntityCombo ec = new EntityCombo(filterOption.Token.Type, filterOption.Value, parent, "", filterOption.Token.GetPropertyRoute())
                    {
                        LabelVisible = false,
                        BreakLine = false
                    };
                    Navigator.ConfigureEntityBase(ec, filterOption.Token.Type.CleanType(), false);
                    return EntityComboHelper.InternalEntityCombo(helper, ec);
                }
                else
                {
                    EntityLine el = new EntityLine(filterOption.Token.Type, filterOption.Value, parent, "", filterOption.Token.GetPropertyRoute())
                    {
                        LabelVisible = false,
                        BreakLine = false
                    };
                    Navigator.ConfigureEntityBase(el, filterOption.Token.Type.CleanType(), false);
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
