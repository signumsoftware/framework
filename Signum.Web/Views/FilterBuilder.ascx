<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="System.Collections.Generic" %>

<select id="Table1" name="results">

<% foreach (Column column in (List<Column>)ViewData[ViewDataKeys.Columns])
   {
       string name = column.Name;
       %>
       <option id="Option1"><%=column.DisplayName%></option>
   <%
   } 
   %>

</select>
<br />
<table id="tblFilters" name="tblFilters">
    <tr>
        <td>Campo</td>
        <td>Operación</td>
        <td>Valor</td>
    </tr>    
        <% List<FilterOptions> filterOptions = ((FindOptions)ViewData[ViewDataKeys.Filters]).FilterOptions;
        for (int i=0; i<filterOptions.Count; i++)
        {
            FilterOptions filter = filterOptions[i];
            string columnName = filter.ColumnName + "_" + i.ToString();

            FilterType filterType = FilterOperationsUtils.GetFilterType(filter.Column.Type);
            List<FilterOperation> possibleOperations = FilterOperationsUtils.FilterOperations[filterType];
            
        %>
            <tr>
                <td id="<%=columnName %>" name="<%=columnName %>"><%=filter.Column.DisplayName%></td>
                <td>
                    <select>
                    <% for (int j=0; j<possibleOperations.Count; j++)
                    {
                        %>
                        <option <%= possibleOperations[j]==filter.Operation ? "selected=\"selected\"" : "" %> ><%= possibleOperations[j] %></option>
                        <%
                    }
                    %>
                    </select>
                </td>
                <td>
                    <input type="text" class="filterOption filterWidth" value="<%=filter.Value.ToString()%>" />
                </td>
            </tr>
        <%
        } 
        %>
    
</table>