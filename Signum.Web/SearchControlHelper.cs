using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;


namespace Signum.Web
{
    public static class SearchControlHelper
    {
        public static string NewFilter(string filterType, string columnName, string displayName)
        {
            StringBuilder sb = new StringBuilder();
            List<FilterOperation> possibleOperations = FilterOperationsUtils.FilterOperations[FilterOperationsUtils.GetFilterType(GetType(filterType))];
                
            sb.Append("<tr>\n");
            sb.Append("<td id=\"{0}\" name=\"{0}\">{1}</td>\n".Formato("lbl" + columnName, displayName));
            sb.Append("<td>\n");
            sb.Append("<select>\n");
            for (int j=0; j<possibleOperations.Count; j++)
                sb.Append("<option value=\"{0}\">{1}</option>\n"
                    .Formato(possibleOperations[j], possibleOperations[j].NiceToString()));
            sb.Append("</select>\n");
            sb.Append("</td>\n");
            sb.Append("<td>\n");
            sb.Append("<input type=\"text\" id=\"{0}\" name=\"{0}\" value=\"\"></input>\n".Formato(columnName));
            sb.Append("</td>\n");
            sb.Append("</tr>\n");
            return sb.ToString();
        }

        public static Type GetType(string typeName)
        {
            return Type.GetType("System." + typeName, true);
        }

        //public static string NewFilter(this HtmlHelper helper, string filterType, string columnName, string displayName, FilterOperation? selectedOperation, object value)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    List<FilterOperation> possibleOperations = FilterOperationsUtils.FilterOperations[FilterOperationsUtils.GetFilterType(Type.GetType(filterType))];
                
        //    sb.Append("<tr>\n");
        //    sb.Append("<td id=\"{0}\" name=\"{0}\">{1}</td>\n".Formato("lbl" + columnName, displayName));
        //    sb.Append("<td>\n");
        //    sb.Append("<select>\n");
        //    for (int j=0; j<possibleOperations.Count; j++)
        //        sb.Append("<option value=\"{0}\" {1}>{2}</option>\n"
        //            .Formato(
        //                possibleOperations[j],
        //                (selectedOperation.HasValue && possibleOperations[j] == selectedOperation.Value) ? "selected=\"selected\"" : "",
        //                possibleOperations[j].NiceToString()));
        //    sb.Append("</select>\n");
        //    sb.Append("</td>\n");
        //    sb.Append("<td>\n");
        //    sb.Append(helper.TextboxInLine(columnName, (value != null) ? value.ToString() : "", new Dictionary<string,object>()) + "\n");
        //    sb.Append("</td>\n");
        //    sb.Append("</tr>\n");
        //    return sb.ToString();
        //}

        public static void NewFilter(this HtmlHelper helper, FilterOptions filterOptions)
        {
            StringBuilder sb = new StringBuilder();

            FilterType filterType = FilterOperationsUtils.GetFilterType(filterOptions.Column.Type);
            List<FilterOperation> possibleOperations = FilterOperationsUtils.FilterOperations[filterType];
                
            sb.Append("<tr>\n");
            sb.Append("<td id=\"{0}\" name=\"{0}\">{1}</td>\n".Formato("lbl" + filterOptions.Column.Name, filterOptions.Column.DisplayName));
            sb.Append("<td>\n");
            sb.Append("<select>\n");
            for (int j=0; j<possibleOperations.Count; j++)
                sb.Append("<option value=\"{0}\" {1}>{2}</option>\n"
                    .Formato(
                        possibleOperations[j],
                        (possibleOperations[j] == filterOptions.Operation) ? "selected=\"selected\"" : "",
                        possibleOperations[j].NiceToString()));
            sb.Append("</select>\n");
            sb.Append("</td>\n");
            sb.Append("<td>\n");
            sb.Append(helper.TextboxInLine(filterOptions.Column.Name, (filterOptions.Value != null) ? filterOptions.Value.ToString() : "", new Dictionary<string, object>()) + "\n");
            sb.Append("</td>\n");
            sb.Append("</tr>\n");
            helper.ViewContext.HttpContext.Response.Write(sb.ToString()); 
        }

        
    }
}
