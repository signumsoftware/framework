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
        public static string NewFilter(Controller controller, string filterType, string columnName, string displayName, int index)
        {
            StringBuilder sb = new StringBuilder();
            Type columnType = GetType(filterType);
            List<FilterOperation> possibleOperations = FilterOperationsUtils.FilterOperations[FilterOperationsUtils.GetFilterType(columnType)];

            sb.Append("<tr id=\"{0}\" name=\"{0}\">\n".Formato("trFilter_" + index.ToString()));
            sb.Append("<td id=\"{0}\" name=\"{0}\">{1}</td>\n".Formato("td" + index.ToString() + "_" + columnName, displayName));
            sb.Append("<td>\n");
            sb.Append("<select id=\"{0}\" name=\"{0}\">\n".Formato("ddlSelector_" + index.ToString()));
            for (int j=0; j<possibleOperations.Count; j++)
                sb.Append("<option value=\"{0}\">{1}</option>\n"
                    .Formato(possibleOperations[j], possibleOperations[j].NiceToString()));
            sb.Append("</select>\n");
            sb.Append("</td>\n");
            sb.Append("<td>\n");

            ValueLineType vlType = ValueLineHelper.Configurator.GetDefaultValueLineType(columnType);
            sb.Append(
                ValueLineHelper.Configurator.constructor[vlType](
                    CreateHtmlHelper(controller), 
                    new ValueLineData("value_" + index.ToString(), null, new Dictionary<string, object>()))); 

            sb.Append("</td>\n");
            sb.Append("<td>\n");
            sb.Append("<input type=\"button\" id=\"{0}\" name=\"{0}\" value=\"X\" onclick=\"DeleteFilter('" + index + "');\" />\n".Formato("btnDelete_" + index));
            sb.Append("</td>\n");
            sb.Append("</tr>\n");
            return sb.ToString();
        }

        private static Type GetType(string typeName)
        {
            return Type.GetType("System." + typeName, true);
        }

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

        public static void NewFilter(this HtmlHelper helper, FilterOptions filterOptions, int index)
        {
            StringBuilder sb = new StringBuilder();

            FilterType filterType = FilterOperationsUtils.GetFilterType(filterOptions.Column.Type);
            List<FilterOperation> possibleOperations = FilterOperationsUtils.FilterOperations[filterType];
                
            sb.Append("<tr id=\"{0}\" name=\"{0}\">\n".Formato("trFilter_" + index.ToString()));
            sb.Append("<td id=\"{0}\" name=\"{0}\">{1}</td>\n".Formato("td" + index.ToString() + "_" + filterOptions.Column.Name, filterOptions.Column.DisplayName));
            sb.Append("<td>\n");
            sb.Append("<select{0} id=\"{1}\" name=\"{1}\">\n"
                .Formato(filterOptions.Frozen ? " disabled=\"disabled\"" : "",
                        "ddlSelector_" + index.ToString()));
            for (int j=0; j<possibleOperations.Count; j++)
                sb.Append("<option value=\"{0}\" {1}>{2}</option>\n"
                    .Formato(
                        possibleOperations[j],
                        (possibleOperations[j] == filterOptions.Operation) ? "selected=\"selected\"" : "",
                        possibleOperations[j].NiceToString()));
            sb.Append("</select>\n");
            sb.Append("</td>\n");
            sb.Append("<td>\n");

            string txtId = "value_" + index.ToString();
            string txtValue = (filterOptions.Value != null) ? filterOptions.Value.ToString() : "";
            if (filterOptions.Frozen)
                sb.Append("<input type=\"text\" id=\"{0}\" name=\"{0}\" value=\"{1}\" {2}/>\n".Formato(txtId, txtValue, filterOptions.Frozen ? " readonly=\"readonly\"" : ""));
            else
            {
                ValueLineType vlType = ValueLineHelper.Configurator.GetDefaultValueLineType(filterOptions.Column.Type);
                sb.Append(
                    ValueLineHelper.Configurator.constructor[vlType](
                        helper,
                        new ValueLineData(
                            txtId,
                            filterOptions.Value,
                            new Dictionary<string, object>())));
            }
            sb.Append("</td>\n");
            sb.Append("<td>\n");
            if (!filterOptions.Frozen)
                sb.Append(helper.Button("btnDelete_" + index, "X", "DeleteFilter('" + index + "');", "", new Dictionary<string, string>()));
            sb.Append("</td>\n");
            sb.Append("</tr>\n");
            helper.ViewContext.HttpContext.Response.Write(sb.ToString()); 
        }

        
    }
}
