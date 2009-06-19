<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>

<%= Html.Hidden(Html.GlobalName(ViewDataKeys.EntityTypeName), ViewData[ViewDataKeys.EntityTypeName].ToString())%>

<select id="<%=Html.GlobalName("ddlNewFilters")%>" name="<%=Html.GlobalName("ddlNewFilters")%>">
<% foreach (Column column in (List<Column>)ViewData[ViewDataKeys.FilterColumns])
   {
       Type type = column.Type.UnNullify();
       %>
       <option id="<%=Html.GlobalName("option__" + column.Name) %>" value="<%=typeof(Lazy).IsAssignableFrom(type) ? Reflector.ExtractLazy(type).Name : type.Name %>"><%=column.DisplayName%></option>
   <%
   } 
   %>
   <%=Html.Button(Html.GlobalName("btnAddFilter"), "+", "AddFilter('{0}','{1}');".Formato((ConfigurationManager.AppSettings["RoutePrefix"] ?? "") + "/Signum/AddFilter", ViewData[ViewDataKeys.PopupPrefix] ?? ""), "", new Dictionary<string, string>())%>
   <%=Html.Button(Html.GlobalName("btnClearAllFilters"), "XX", "ClearAllFilters('{0}');".Formato(ViewData[ViewDataKeys.PopupPrefix] ?? ""), "", new Dictionary<string, string>())%>
</select>
<br />
<table id="<%=Html.GlobalName("tblFilters")%>" name="<%=Html.GlobalName("tblFilters")%>">
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
            Html.NewFilter(filter, i, ViewData[ViewDataKeys.EntityTypeName].ToString());            
        } 
        %>
    </tbody>
</table>
Top: <%= Html.TextBox(Html.GlobalName(ViewDataKeys.Top), ViewData[ViewDataKeys.Top] ?? "") %>
