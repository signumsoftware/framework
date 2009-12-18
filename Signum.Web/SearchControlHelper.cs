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

namespace Signum.Web
{
    public static class SearchControlHelper
    {
        public delegate WebMenuItem MenuItemForQueryName(object queryName);

        public static event MenuItemForQueryName GetCustomMenuItems;

        public static string GetMenuItems(this HtmlHelper helper, object queryName, object prefix)
        {
            StringBuilder sb = new StringBuilder();
            if (GetCustomMenuItems != null)
            {
                WebMenuItem[] menus = GetCustomMenuItems.GetInvocationList().Cast<MenuItemForQueryName>().Select(d => d(queryName)).NotNull().ToArray();
                foreach (WebMenuItem mi in menus)
                {
                    string onclick = "";
                    string strPrefix = (prefix != null) ? ("'" + prefix.ToString() + "'") : "''";
                        
                    //Add prefix to onclick
                    if (!string.IsNullOrEmpty(mi.OnClick))
                    {
                        if (!string.IsNullOrEmpty(mi.OnServerClickAjax) || !string.IsNullOrEmpty(mi.OnServerClickPost))
                            throw new ArgumentException(Resources.MenuItem0HasOnClickAndAnotherClickDefined.Formato(mi.Id));

                        int lastEnd = mi.OnClick.LastIndexOf(")");
                        int lastStart = mi.OnClick.LastIndexOf("(");
                        if (lastStart == lastEnd -1)
                            onclick = mi.OnClick.Insert(lastEnd, strPrefix);
                        else
                            onclick = mi.OnClick.Insert(lastEnd, ", " + strPrefix);
                    }
                    
                    //Constructo OnServerClick
                    if (!string.IsNullOrEmpty(mi.OnServerClickAjax))
                    {
                        if (!string.IsNullOrEmpty(mi.OnClick) || !string.IsNullOrEmpty(mi.OnServerClickPost))
                            throw new ArgumentException(Resources.MenuItem0HasOnServerClickAjaxAndAnotherClickDefined.Formato(mi.Id));
                        onclick = "CallServer('{0}',{1});".Formato(mi.OnServerClickAjax, strPrefix);
                    }

                    //Constructo OnServerClick
                    if (!string.IsNullOrEmpty(mi.OnServerClickPost))
                    {
                        if (!string.IsNullOrEmpty(mi.OnClick) || !string.IsNullOrEmpty(mi.OnServerClickAjax))
                            throw new ArgumentException(Resources.MenuItem0HasOnServerClickPostAndAnotherClickDefined.Formato(mi.Id));
                        onclick = "PostServer('{0}');".Formato(mi.OnServerClickPost);
                    }

                    //Add cursor pointer to the htmlProps
                    if (!mi.HtmlProps.ContainsKey("style"))
                        mi.HtmlProps.Add("style", "cursor: pointer");
                    else if (mi.HtmlProps["style"].ToString().IndexOf("cursor")==-1)
                        mi.HtmlProps["style"] = "cursor:pointer; " + mi.HtmlProps["style"].ToString();

                   // sb.Append(helper.ImageButton(mi.Id, mi.ImgSrc, mi.AltText, onclick, mi.HtmlProps));
                    sb.Append(helper.Button(mi.Id, mi.AltText, onclick, "", mi.HtmlProps));
                }
            }
            return sb.ToString();
        }

        public static void SearchControl(this HtmlHelper helper, FindOptions findOptions, string prefix, string prefixEnd)
        {
            QueryDescription queryDescription = Navigator.Manager.Queries.QueryDescription(findOptions.QueryName);

            foreach (FilterOptions opt in findOptions.FilterOptions)
            {
                opt.Column = queryDescription.Columns.Where(c => c.Name == opt.ColumnName)
                    .Single(Resources.FilterColumn0NotFoundOrFoundMoreThanOnce.Formato(opt.ColumnName));
            }

            Column entityColumn = queryDescription.Columns.SingleOrDefault(a => a.IsEntity);
            Type entitiesType = entityColumn != null ? Reflector.ExtractLite(entityColumn.Type) : null;

            List<Column> columns = queryDescription.Columns.Where(a => a.Filterable).ToList();

            helper.ViewData[ViewDataKeys.PopupPrefix] = prefix;
            helper.ViewData[ViewDataKeys.PopupSufix] = prefixEnd ?? "";

            helper.ViewData[ViewDataKeys.FilterColumns] = columns;
            helper.ViewData[ViewDataKeys.FindOptions] = findOptions;
            helper.ViewData[ViewDataKeys.Top] = Navigator.Manager.QuerySettings.TryGetC(findOptions.QueryName).ThrowIfNullC(Resources.MissingQuerySettingsForQueryName0.Formato(findOptions.QueryName.ToString())).Top;
            if (helper.ViewData.Keys.Count(s => s == ViewDataKeys.PageTitle) == 0)
                helper.ViewData[ViewDataKeys.PageTitle] = Navigator.Manager.SearchTitle(findOptions.QueryName);
            if (entitiesType != null)
            {
                helper.ViewData[ViewDataKeys.EntityTypeName] = entitiesType.Name;
                helper.ViewData[ViewDataKeys.Create] =
                    (findOptions.Create.HasValue) ?
                    findOptions.Create.Value :
                    Navigator.IsCreable(entitiesType, false);
            }
            else
            {
                helper.ViewData[ViewDataKeys.EntityTypeName] = "";
                helper.ViewData[ViewDataKeys.Create] = false;
            }
            helper.ViewContext.HttpContext.Response.Write(
                helper.RenderPartialToString(Navigator.Manager.SearchControlUrl, helper.ViewData));
        }

        public static string NewFilter(Controller controller, string filterTypeName, string columnName, string displayName, int index, string prefix, FilterOperation? filterOperation, object value)
        {
           // Type searchEntityType = Navigator.NameToType[entityTypeName];

            StringBuilder sb = new StringBuilder();
 
            Type columnType;
            if (value != null && value.GetType() != typeof(string)) //No string so ValueTypes are not shown as text
                columnType = Reflector.ExtractLite(value.GetType()) ?? value.GetType();
            else
            {
                columnType = Navigator.ResolveType(filterTypeName);
                if (columnType != null && value != null)
                    value = Convert.ChangeType(value, columnType);
            }

            if (value != null && typeof(Lite).IsAssignableFrom(value.GetType()))
                value = Database.Retrieve((Lite)value);

            //Client doesn't know about Lites, check it ourselves
            if (typeof(IdentifiableEntity).IsAssignableFrom(columnType))
                columnType = Reflector.GenerateLite(columnType);
            FilterType filterType = FilterOperationsUtils.GetFilterType(columnType);
            List<FilterOperation> possibleOperations = FilterOperationsUtils.FilterOperations[filterType];

            sb.AppendLine("<tr id='{0}' name='{0}'>".Formato(prefix + "trFilter_" + index.ToString()));
            
            sb.AppendLine("<td id='{0}' name='{0}'>".Formato(prefix + "td" + index.ToString() + "__" + columnName));
            sb.AppendLine(displayName);
            sb.AppendLine("</td>");
            
            sb.AppendLine("<td>");
            sb.AppendLine("<select id='{0}' name='{0}'>".Formato(prefix + "ddlSelector_" + index.ToString()));
            for (int j=0; j<possibleOperations.Count; j++)
                sb.AppendLine("<option value='{0}'{1}>{2}</option>"
                    .Formato(possibleOperations[j], 
                    (filterOperation.HasValue && filterOperation.Value == possibleOperations[j]) ? " selected='selected'" : "",
                    possibleOperations[j].NiceToString()));
            sb.AppendLine("</select>");
            sb.AppendLine("</td>");
            
            sb.Append("<td>");
            sb.Append(PrintValueField(CreateHtmlHelper(controller), filterType, columnType, prefix + "value_" + index.ToString(), value, columnName));
            sb.Append("</td>");
            
            sb.AppendLine("<td>");
            sb.AppendLine("<input type='button' id='{0}' name='{0}' value='X' onclick=\"DeleteFilter('{1}','{2}');\" />".Formato(prefix + "btnDelete_" + index, prefix, index));
            sb.AppendLine("</td>");
            
            sb.AppendLine("</tr>");
            return sb.ToString();
        }

        public static string QuickFilter(Controller controller, string queryUrlName, int visibleColumnIndex, int filterRowIndex, object value, string prefix)
        {
            QueryDescription qd = Navigator.Manager.Queries.QueryDescription(Navigator.ResolveQueryFromUrlName(queryUrlName));
            Column column = qd.Columns.Where(c => c.Visible == true).ToList()[visibleColumnIndex];
            FilterOptions fo = new FilterOptions() { Column = column, ColumnName = column.Name, Operation = FilterOperation.EqualTo, Value = value };
            Type type = Reflector.ExtractLite(column.Type) ?? column.Type;
            
            return NewFilter(controller, type.Name, column.Name, column.DisplayName, filterRowIndex, prefix, FilterOperation.EqualTo, value);
        }

        private static FilterType GetFilterType(Type type)
        {
            type = type.UnNullify();

            if (type.IsEnum)
                return FilterType.Enum;
            if (typeof(Lite).IsAssignableFrom(type))
                return FilterType.Lite;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.DateTime:
                    return FilterType.DateTime;
                case TypeCode.Boolean:
                    return FilterType.Boolean;
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.Single:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return FilterType.Number;
                case TypeCode.Empty:
                case TypeCode.Object:
                case TypeCode.Char:
                case TypeCode.String:
                default:
                    return FilterType.String;
            }
        }

        public static void NewFilter(this HtmlHelper helper, FilterOptions filterOptions, int index)
        {
            StringBuilder sb = new StringBuilder();

            FilterType filterType = FilterOperationsUtils.GetFilterType(filterOptions.Column.Type);
            List<FilterOperation> possibleOperations = FilterOperationsUtils.FilterOperations[filterType];

            sb.Append("<tr id=\"{0}\" name=\"{0}\">\n".Formato(helper.GlobalName("trFilter_" + index.ToString())));
            sb.Append("<td id=\"{0}\" name=\"{0}\">\n".Formato(helper.GlobalName("td" + index.ToString() + "__" + filterOptions.Column.Name)));
            sb.Append(filterOptions.Column.DisplayName);
            sb.Append("</td>\n");

            sb.Append("<td>\n");
            sb.Append("<select{0} id=\"{1}\" name=\"{1}\">\n"
                .Formato(filterOptions.Frozen ? " disabled=\"disabled\"" : "",
                        helper.GlobalName("ddlSelector_" + index.ToString())));
            for (int j = 0; j < possibleOperations.Count; j++)
                sb.Append("<option value=\"{0}\" {1}>{2}</option>\n"
                    .Formato(
                        possibleOperations[j],
                        (possibleOperations[j] == filterOptions.Operation) ? "selected=\"selected\"" : "",
                        possibleOperations[j].NiceToString()));
            sb.Append("</select>\n");

            sb.Append("</td>\n");

            sb.Append("<td>\n");
            string txtId = helper.GlobalName("value_" + index.ToString());
            string txtValue = (filterOptions.Value != null) ? filterOptions.Value.ToString() : "";
            if (filterOptions.Frozen)
                sb.Append("<input type=\"text\" id=\"{0}\" name=\"{0}\" value=\"{1}\" {2}/>\n".Formato(txtId, txtValue, filterOptions.Frozen ? " readonly=\"readonly\"" : ""));
            else
                sb.Append(PrintValueField(helper, filterType, filterOptions.Column.Type, txtId, filterOptions.Value, filterOptions.Column.Name));
            sb.Append("</td>\n");

            sb.Append("<td>\n");
            if (!filterOptions.Frozen)
                sb.Append(helper.Button(helper.GlobalName("btnDelete_" + index), "X", "DeleteFilter('{0}','{1}');".Formato(helper.ViewData[ViewDataKeys.PopupPrefix] ?? "", index), "", new Dictionary<string, object>()));
            sb.Append("</td>\n");

            sb.Append("</tr>\n");
            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        //private static Type GetType(string typeName)
        //{
        //    if (Navigator.NameToType.ContainsKey(typeName))
        //        return Navigator.NameToType[typeName];

        //    return Type.GetType("System." + typeName, false);
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
        
        static MethodInfo mi = typeof(EntityLineHelper).GetMethod("InternalEntityLine", BindingFlags.Static | BindingFlags.NonPublic);

        private static string PrintValueField(HtmlHelper helper, FilterType filterType, Type columnType, string id, object value, string propertyName)
        {
            StringBuilder sb = new StringBuilder();
            if (filterType == FilterType.Lite)
            {
                EntityLine el = new EntityLine(id);
                Navigator.ConfigureEntityBase(el, Reflector.ExtractLite(columnType) ?? columnType, false);
                el.Create = false;

                if (helper.ViewData.ContainsKey(ViewDataKeys.PopupPrefix) && ((string)helper.ViewData[ViewDataKeys.PopupPrefix]).HasText())
                {
                    string prefix = helper.ViewData[ViewDataKeys.PopupPrefix].ToString();
                    if (id.StartsWith(prefix))
                        id = id.RemoveLeft(prefix.Length); //We call GlobalName with it in EntityLine
                }

                if (value != null && (columnType == typeof(IIdentifiable) || Reflector.ExtractLite(columnType) == typeof(IIdentifiable)
                    || columnType == typeof(IdentifiableEntity) || Reflector.ExtractLite(columnType) == typeof(IdentifiableEntity)))
                    columnType = Reflector.ExtractLite(value.GetType()) ?? value.GetType();
                else
                    columnType = Reflector.ExtractLite(columnType) ?? columnType;
                if (value != null && typeof(Lite).IsAssignableFrom(value.GetType()))
                    value = Database.Retrieve((Lite)value);
                Type t = typeof(TypeContext<>);
                var tcreator = t.MakeGenericType(new Type[] {columnType});
                TypeContext tc = (TypeContext)Activator.CreateInstance(tcreator, new object[]{value, id});

                bool loadall = helper.ViewData.Keys.Contains(ViewDataKeys.LoadAll);
                if (loadall)
                    helper.ViewData.Remove(ViewDataKeys.LoadAll);
                string result = (string)mi.MakeGenericMethod(tc.ContextType).Invoke(null, new object[] { helper, tc, el });
                //string result = (string)EntityLineHelper.InternalEntityLine(helper, id, columnType, value, el);
                if (loadall)
                    helper.ViewData.Add(ViewDataKeys.LoadAll, true);

                sb.Append(result);
            }
            else
            {
                ValueLineType vlType = ValueLineHelper.Configurator.GetDefaultValueLineType(columnType);
                sb.Append(
                    ValueLineHelper.Configurator.constructor[vlType](
                        helper,
                        new ValueLineData(
                            id,
                            value,
                            new ValueLine(),
                            columnType)));
            }
            return sb.ToString();
        }
    }
}
