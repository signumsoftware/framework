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
   Type entitiesType = Reflector.ExtractLite(queryDescription.StaticColumns.Single(a => a.IsEntity).Type);
   bool viewable = findOptions.View && Navigator.IsNavigable(entitiesType, true);


%>
<%  ResultTable queryResult = (ResultTable)ViewData[ViewDataKeys.Results];
    var entityColumn = (queryResult == null) ? null : queryResult.Columns.OfType<StaticColumn>().Single(c => c.IsEntity);
    Dictionary<int, Action<HtmlHelper, object>> formatters = (Dictionary<int, Action<HtmlHelper, object>>)ViewData[ViewDataKeys.Formatters];

                foreach (var row in queryResult.Rows)
                {
        %>
        <tr class="<%=(row.Index % 2 == 1) ? "even" : ""%>" id="<%=context.Compose("trResults", row.Index.ToString())%>"
            name="<%=context.Compose("trResults", row.Index.ToString())%>">
            <% Lite entityField = (Lite)row[entityColumn];

               if (findOptions.AllowMultiple.HasValue)
               {
            %>
            <td class="tdRowSelection">
                <%
            if (findOptions.AllowMultiple.Value)
            { 
                %>
                <input type="checkbox" name="<%=context.Compose("rowSelection", row.Index.ToString())%>"
                    id="<%=context.Compose("rowSelection", row.Index.ToString())%>" value="<%= entityField.Id.ToString() + "__" + entityField.RuntimeType.Name + "__" + entityField.ToStr %>" />
                <%}
            else
            { %>
                <input type="radio" name="<%=context.Compose("rowSelection")%>" id="<%=context.Compose("radio", row.Index.ToString())%>"
                    value="<%= entityField.Id.ToString() + "__" + entityField.RuntimeType.Name + "__" + entityField.ToStr %>" />
                <%
            }
                %>
            </td>
            <%} %>
            <% if (viewable)
               { %>
            <td class="tdRowEntity">
                <a href="<%= Navigator.ViewRoute(entityField.RuntimeType, entityField.Id) %>" title="<%=Html.Encode(Resources.View) %>">
                    <%=Html.Encode(Resources.View)%></a>
            </td>
            <% } %>
            <%
            foreach (var col in queryResult.VisibleColumns)
            {      
            %>
            <td id="<%=context.Compose("row"+row.Index+"td", col.Index.ToString())%>">
                <%formatters[col.Index](Html, row[col]);%>
            </td>
            <%
            }
            %>
        </tr>
        <%
            }
        %>
