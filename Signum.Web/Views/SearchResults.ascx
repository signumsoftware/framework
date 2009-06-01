<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>

<% QueryResult queryResult = (QueryResult)ViewData[ViewDataKeys.Results];%>

<table id="tblResults" name="tblResults">
    <thead>
        <tr>
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
        for (int row=0; row<queryResult.Data.Count; row++)
        {
            %>
            <tr id="<%="trResults_" + row.ToString()%>" name="<%="trResults_" + row.ToString()%>">
                <%
                for (int col=0; col<queryResult.Data[row].Count; col++)
                {
                    %>
                    <td id="<%="tdResults_" + col.ToString()%>" name="<%="tdResults_" + col.ToString()%>">
                        <%=(queryResults.Data[row][col]!=null) ? queryResults.Data[row][col].ToString() : "" %>
                    </td>
                    <%
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