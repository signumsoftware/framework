<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>

<% Context context = (Context)Model;
   FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions];
   QueryDescription queryDescription = (QueryDescription)ViewData[ViewDataKeys.QueryDescription];
   Type entitiesType = Reflector.ExtractLite(queryDescription.StaticColumns.Single(a => a.IsEntity).Type);
   bool viewable = findOptions.View && Navigator.IsNavigable(entitiesType, true);
    
//TODO Anto: Habilitar quickfilters: Controlar campos no filtrables  (con este método se pueden crear)
//"<script type=\"text/javascript\">" + 
//    "$(document).ready(function() {" + 
//        "$('.tblResults td').bind('dblclick', function(e) {" + 
//            "QuickFilter('" + Html.GlobalName("") + "', this.id);" + 
//        "});" + 
//    "});" + 
//"</script>"
%>

<%  ResultTable queryResult = (ResultTable)ViewData[ViewDataKeys.Results];
    int? EntityColumnIndex = (int?)ViewData[ViewDataKeys.EntityColumnIndex];
    Dictionary<int, bool> colVisibility = new Dictionary<int, bool>();
    for (int i = 0; i < queryResult.Columns.Length; i++)
    {
        colVisibility.Add(i, queryResult.Columns[i].Visible);
    }
 %>
<table id="<%=context.Compose("tblResults")%>" class="tblResults">
    <thead>
        <tr>
            
            <%if (EntityColumnIndex.HasValue && EntityColumnIndex.Value != -1 && viewable)
              {
                  if (findOptions.AllowMultiple.HasValue)
                  {%>
                  <th></th>
                  <%} %>
            <th></th>
            <%} %>
            <%
                foreach (Column c in queryResult.VisibleColumns) 
                {
                    %>
                    <th><%= c.DisplayName %></th>
                    <%
                }      
            %>
        </tr>
    </thead>    
    <tbody>
    <%
        List<Action<HtmlHelper, object>> formatters = (List<Action<HtmlHelper, object>>)ViewData[ViewDataKeys.Formatters];
        for (int row=0; row<queryResult.Rows.Length; row++)
        {
            %>
            <tr class="<%=(row % 2 == 1) ? "even" : ""%>" id="<%=context.Compose("trResults", row.ToString())%>" name="<%=context.Compose("trResults", row.ToString())%>">
                <% Lite entityField = null;
                   if (EntityColumnIndex.HasValue && EntityColumnIndex.Value != -1)
                       entityField = (Lite)queryResult.Rows[row][EntityColumnIndex.Value];
                   
                       if (findOptions.AllowMultiple.HasValue)
                       {
                %>
                <td class="<%=context.Compose("tdRowSelection")%>">
                    <%
                        if (entityField != null)
                        {

                            if (findOptions.AllowMultiple.Value)
                    { 
                        %>
                        <input type="checkbox" name="<%=context.Compose("rowSelection", row.ToString())%>" id="<%=context.Compose("rowSelection", row.ToString())%>" value="<%= entityField.Id.ToString() + "__" + entityField.RuntimeType.Name + "__" + entityField.ToStr %>" />
                    <%}
                    else
                    { %>
                        <input type="radio" name="<%=context.Compose("rowSelection")%>" id="<%=context.Compose("radio", row.ToString())%>" value="<%= entityField.Id.ToString() + "__" + entityField.RuntimeType.Name + "__" + entityField.ToStr %>" />
                        <%
                    }
                        }
                 %>
                 </td>
                 <%} %>
                <% if (entityField != null && viewable)
                   { %>
                   <td id="<%=context.Compose("tdResults")%>">
                    <a href="<%= Navigator.ViewRoute(entityField.RuntimeType, entityField.Id) %>" title="Ver">Ver</a>
                </td>
                <% } %>
                <%
                    for (int col = 0; col < queryResult.Columns.Length; col++)
                {
                    if (colVisibility[col])
                    {
                        %>
                        <td id="<%=context.Compose("row"+row+"td", col.ToString())%>"><%formatters[col](Html, queryResult.Rows[row][col]);%></td>
                        <%
                    }
                }
                 %>
            </tr>
            <%
        }
         %>
    </tbody>
    <tfoot>
        
    </tfoot>
</table>
