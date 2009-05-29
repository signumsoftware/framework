<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="System.Collections.Generic" %>

<select id="ddlNewFilters" name="ddlNewFilters">

<% foreach (Column column in (List<Column>)ViewData[ViewDataKeys.Columns])
   {
       string name = column.Name;
       %>
       <option id="<%="option" + column.Name %>" value="<%=column.Type.Name %>"><%=column.DisplayName%></option>
   <%
   } 
   %>
   <%=Html.Button("btnAddFilter", "+", "AddFilter('/Signum/AddFilter')","",new Dictionary<string, string>()) %>

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
        <% List<FilterOptions> filterOptions = ((FindOptions)ViewData[ViewDataKeys.Filters]).FilterOptions;
        for (int i=0; i<filterOptions.Count; i++)
        {
            FilterOptions filter = filterOptions[i];
            //string columnName = filter.ColumnName + "_" + i.ToString();

            //FilterType filterType = FilterOperationsUtils.GetFilterType(filter.Column.Type);
            //List<FilterOperation> possibleOperations = FilterOperationsUtils.FilterOperations[filterType];

            Html.NewFilter(filter);            
        } 
        %>
    </tbody>
</table>