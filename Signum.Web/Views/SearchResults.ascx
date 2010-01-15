<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>

<%
//TODO Anto: Habilitar quickfilters: Controlar campos no filtrables _nf_ (con este método se pueden crear)
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
    bool? allowMultiple = (bool?)ViewData[ViewDataKeys.AllowMultiple];
    Dictionary<int, bool> colVisibility = new Dictionary<int, bool>();
    for (int i = 0; i < queryResult.Columns.Length; i++)
    {
        colVisibility.Add(i, queryResult.Columns[i].Visible);
    }
 %>
<table id="<%=Html.GlobalName("tblResults")%>" class="tblResults">
    <thead>
        <tr>
            <%if (EntityColumnIndex.HasValue && EntityColumnIndex.Value != -1)
              {
                  if (allowMultiple.HasValue)
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
            <tr class="<%=(row % 2 == 1) ? "even" : ""%>" id="<%=Html.GlobalName("trResults_" + row.ToString())%>" name="<%=Html.GlobalName("trResults_" + row.ToString())%>">
                <% Lite entityField = null;
                   if (EntityColumnIndex.HasValue && EntityColumnIndex.Value != -1)
                       entityField = (Lite)queryResult.Rows[row][EntityColumnIndex.Value];
                   
                       if (allowMultiple.HasValue)
                       {
                %>
                <td id="<%=Html.GlobalName("tdRowSelection")%>">
                    <%
                        if (entityField != null)
                        {
            
                    if (allowMultiple.Value)
                    { 
                        %>
                        <input type="checkbox" name="<%=Html.GlobalName("check_" + row.ToString())%>" id="<%=Html.GlobalName("check_" + row.ToString())%>" value="<%= entityField.Id.ToString() + "__" + entityField.RuntimeType.Name + "__" + entityField.ToStr %>" />
                    <%}
                    else
                    { %>
                        <input type="radio" name="<%=Html.GlobalName("rowSelection")%>" id="<%=Html.GlobalName("radio_" + row.ToString())%>" value="<%= entityField.Id.ToString() + "__" + entityField.RuntimeType.Name + "__" + entityField.ToStr %>" />
                        <%
                    }
                        }
                 %>
                 </td>
                 <%} %>
                <td id="<%=Html.GlobalName("tdResults")%>">
                <% if (entityField != null) { %>
                    <a href="<%= Navigator.ViewRoute(entityField.RuntimeType, entityField.Id) %>" title="Ver">Ver</a>
                <% } %>
                </td>
                <%
                   
                    
                    for (int col = 0; col < queryResult.Columns.Length; col++)
                {
                    if (colVisibility[col])
                    {
                        %>
                        <td id="<%=Html.GlobalName("row"+row+"td_" + col.ToString())%>"><%formatters[col](Html, queryResult.Rows[row][col]);%></td>
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
