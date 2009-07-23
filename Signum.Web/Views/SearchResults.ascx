<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>

<%  QueryResult queryResult = (QueryResult)ViewData[ViewDataKeys.Results];
    int? EntityColumnIndex = (int?)ViewData[ViewDataKeys.EntityColumnIndex];
    bool allowMultiple = (bool)ViewData[ViewDataKeys.AllowMultiple];
    Dictionary<int, bool> colVisibility = new Dictionary<int, bool>();
    for (int i = 0; i < queryResult.Columns.Count; i++)
    {
        colVisibility.Add(i, queryResult.Columns[i].Visible);
    }
 %>
<table id="<%=Html.GlobalName("tblResults")%>" name="<%=Html.GlobalName("tblResults")%>">
    <thead>
        <tr>
            <%if (EntityColumnIndex.HasValue && EntityColumnIndex.Value != -1)
              { %>
            <th></th>
            <th></th>
            <%} %>
            <%
                foreach(Column c in queryResult.VisibleColums) 
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
        for (int row=0; row<queryResult.Data.Length; row++)
        {
            %>
            <tr class="<%=(row % 2 == 1) ? "even" : ""%>" id="<%=Html.GlobalName("trResults_" + row.ToString())%>" name="<%=Html.GlobalName("trResults_" + row.ToString())%>">
                <% Lazy entityField = null;
                   if (EntityColumnIndex.HasValue && EntityColumnIndex.Value != -1)
                       entityField = (Lazy)queryResult.Data[row][EntityColumnIndex.Value];
                   if (entityField != null)
                   {
                %>
                <td id="<%=Html.GlobalName("tdRowSelection")%>" name="<%=Html.GlobalName("tdRowSelection")%>">
                    <%
            
                    if (allowMultiple)
                    { 
                        %>
                        <input type="checkbox" name="<%=Html.GlobalName("check_" + row.ToString())%>" id="<%=Html.GlobalName("check_" + row.ToString())%>" value="<%= entityField.Id.ToString() + "__" + entityField.RuntimeType.Name + "__" + entityField.ToStr %>" />
                    <%}
                    else
                    { %>
                        <input type="radio" name="<%=Html.GlobalName("rowSelection")%>" id="<%=Html.GlobalName("radio_" + row.ToString())%>" value="<%= entityField.Id.ToString() + "__" + entityField.RuntimeType.Name + "__" + entityField.ToStr %>" />
                        <%
                    }
                 %>
                 </td>
                <td id="<%=Html.GlobalName("tdResults")%>" name="<%=Html.GlobalName("tdResults")%>">
                    <a href="<%= Navigator.ViewRoute(entityField.RuntimeType, entityField.Id) %>" title="Navigate">Ver</a>
                </td>
                <%
                    }
                    for (int col = 0; col < queryResult.Data[row].Length; col++)
                {
                    if (colVisibility[col])
                    {
                        %>
                        <td id="<%=Html.GlobalName("tdResults_" + col.ToString())%>" name="<%=Html.GlobalName("tdResults_" + col.ToString())%>">
                            <%                        
                            Type colType = queryResult.Columns[col].Type;
                            if (QueryDecorators.DecoratorsByName.ContainsKey(queryResult.Columns[col].Name))
                            {%>
                                <%=QueryDecorators.DecoratorsByName[queryResult.Columns[col].Name](queryResult.Data[row][col])%>
                            <%}
                            else
                            {
                                if (typeof(Lazy).IsAssignableFrom(colType) && queryResult.Data[row][col] != null)
                                {
                                    Lazy lazy = (Lazy)queryResult.Data[row][col];
                                    Html.LightEntityLine(lazy, false);
                                }
                                else
                                {
                            %>
                                <%=queryResult.Data[row][col]%>
                            <%  }
                            } %>
                        </td>
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