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
<table id="tblResults" name="tblResults">
    <thead>
        <tr>
            <td></td>
            <td></td>
            <%
                foreach(Column c in queryResult.VisibleColums) 
                {
                    %>
                    <td><%= c.DisplayName %></td>
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
            <tr id="<%="trResults_" + row.ToString()%>" name="<%="trResults_" + row.ToString()%>">
                <% Lazy entityField = (Lazy)queryResult.Data[row][EntityColumnIndex.Value]; %>
                <td id="tdRowSelection" name="tdRowSelection">
                    <%
                    if (allowMultiple)
                    { 
                        %>
                        <input type="checkbox" name="<%="check_" + row.ToString() %>" id="<%="check_" + row.ToString() %>" value="<%= entityField.Id.ToString() + "_" + entityField.RuntimeType.Name + "__" + entityField.ToStr %>" />
                    <%} else{ %>
                        <input type="radio" name="rowSelection" id="<%="radio_" + row.ToString() %>" value="<%= entityField.Id.ToString() + "_" + entityField.RuntimeType.Name + "__" + entityField.ToStr %>" />
                        <%
                    }
                 %>
                 </td>
                <td id="tdResults" name="tdResults">
                    <a href="<%="/View/" + Navigator.TypesToURLNames[entityField.RuntimeType] + "/" + entityField.Id.ToString() %>" title="Navigate">Ver</a>
                </td>
                <%
                    for (int col = 0; col < queryResult.Data[row].Length; col++)
                {
                    if (colVisibility[col])
                    {
                        %>
                        <td id="<%="tdResults_" + col.ToString()%>" name="<%="tdResults_" + col.ToString()%>">
                            <%                        
                            Type colType = queryResult.Columns[col].Type;
                            if (typeof(Lazy).IsAssignableFrom(colType) && queryResult.Data[row][col]!=null)
                            {
                                Lazy lazy = (Lazy)queryResult.Data[row][col];
                                %>
                                <a href="<%="/View/" + Navigator.TypesToURLNames[lazy.RuntimeType] + "/" + lazy.Id.ToString() %>" title="Navigate"><%=lazy.ToStr %></a>   
                                <%
                            }
                            else
                            {
                            %>
                                <%=queryResult.Data[row][col]%>
                            <%} %>
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