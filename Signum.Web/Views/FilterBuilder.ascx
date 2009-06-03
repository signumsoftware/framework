<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>

<%= Html.Hidden(ViewDataKeys.EntityTypeName, ViewData[ViewDataKeys.EntityTypeName].ToString())%>

<select id="ddlNewFilters" name="ddlNewFilters">
<% foreach (Column column in (List<Column>)ViewData[ViewDataKeys.FilterColumns])
   {
       Type type = column.Type.UnNullify();
       %>
       <option id="<%="option" + column.Name %>" value="<%=typeof(Lazy).IsAssignableFrom(type) ? Reflector.ExtractLazy(type).Name : type.Name %>"><%=column.DisplayName%></option>
   <%
   } 
   %>
   <%=Html.Button("btnAddFilter", "+", "AddFilter('/Signum/AddFilter');","",new Dictionary<string, string>()) %>
   <%=Html.Button("btnClearAllFilters", "XX", "ClearAllFilters();","",new Dictionary<string, string>()) %>
</select>
<br />
<table id="tblFilters" name="tblFilters">
    <thead>
        <tr>
            <td>Campo</td>
            <td>Operación</td>
            <td>Valor</td>
        </tr>  
    </thead>  
    <tbody>
        <% List<FilterOptions> filterOptions = ((FindOptions)ViewData[ViewDataKeys.FindOptions]).FilterOptions;
        for (int i=0; i<filterOptions.Count; i++)
        {
            FilterOptions filter = filterOptions[i];
            Html.NewFilter(filter, i);            
        } 
        %>
    </tbody>
</table>
Top: <%= Html.TextBox(ViewDataKeys.Top, ViewData[ViewDataKeys.Top] ?? "") %>
