<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Web.Properties" %>
<% Context context = (Context)Model;
   FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions];
   QueryDescription queryDescription = (QueryDescription)ViewData[ViewDataKeys.QueryDescription];
   Type entitiesType = Reflector.ExtractLite(queryDescription.Columns.Single(a => a.IsEntity).Type);
   bool viewable = findOptions.View && Navigator.IsNavigable(entitiesType, true);
 
    ResultTable queryResult = (ResultTable)ViewData[ViewDataKeys.Results];
    Dictionary<int, Func<HtmlHelper, object, string>> formatters = (Dictionary<int, Func<HtmlHelper, object, string>>)ViewData[ViewDataKeys.Formatters];

    foreach (var row in queryResult.Rows)
    {
      %>
      <tr>
      <%
        Lite entityField = row.Entity;

        if (findOptions.AllowMultiple.HasValue)
        {
            %>
            <td>
            <%
            if (findOptions.AllowMultiple.Value)
            {
                Response.Write(Html.CheckBox(
                    context.Compose("rowSelection", row.Index.ToString()),
                    new { value = entityField.Id.ToString() + "__" + entityField.RuntimeType.Name + "__" + entityField.ToStr }));

            }
            else
            {
                Response.Write(Html.RadioButton(
                    context.Compose("rowSelection"),
                    entityField.Id.ToString() + "__" + entityField.RuntimeType.Name + "__" + entityField.ToStr).ToHtmlString());
            }  
             %>
            </td>
            <%
        }
        
        if (viewable)
        {
             %>
            <td>
            <%
                Response.Write(Html.Href(Navigator.ViewRoute(entityField.RuntimeType, entityField.Id), Html.Encode(Resources.View)));
             %>
            </td>
            <%
        }

        foreach (var col in queryResult.Columns)
        {
             %>
            <td>
            <%= formatters[col.Index](Html, row[col]) %>
            </td>
            <%
        }
        %>
        </tr>
        <%
    }%>