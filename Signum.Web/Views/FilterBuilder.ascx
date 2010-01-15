<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>

<% bool visible = ((FindOptions)ViewData[ViewDataKeys.FindOptions]).FilterMode == FilterMode.Visible;%>

<%= Html.Hidden(Html.GlobalName(ViewDataKeys.EntityTypeName), ViewData[ViewDataKeys.EntityTypeName].ToString())%>

<div id="<%=Html.GlobalName("fields-search")%>">
    <div id="<%=Html.GlobalName("fields-list")%>" class="fields-list">
        <a onclick="javascript:toggleFilters('<%=Html.GlobalName("fields-search")%>');" class="filters-header<%= visible ? "" : " close" %>" rev="filters-body"><%= visible ? "Ocultar filtros" : "Mostrar filtros" %></a>
        <div class="filters" <%= visible ? "" : "style='display:none'"%>>
            <div id="<%=Html.GlobalName("filters-body")%>" class="filters-body">
                <label for="<%=Html.GlobalName("ddlNewFilters")%>">Filtrar por campo</label>
                <select id="<%=Html.GlobalName("ddlNewFilters")%>">

                <% foreach (Column column in (List<Column>)ViewData[ViewDataKeys.FilterColumns])
                   {
                       Type type = column.Type.UnNullify();
                       if (typeof(Lite).IsAssignableFrom(type))
                            type = Reflector.ExtractLite(type);
                       string typeName = (Navigator.TypesToURLNames.ContainsKey(type)) ? type.Name : type.AssemblyQualifiedName;
                       %>
                       <option id="<%=Html.GlobalName("option__" + column.Name) %>" value="<%=typeName %>"><%=column.DisplayName%></option>
                   <%
                   } 
                %>
               </select> 
               <%=Html.Button(Html.GlobalName("btnAddFilter"), "+", "AddFilter('{0}');".Formato(ViewData[ViewDataKeys.PopupPrefix] ?? ""), "", new Dictionary<string, object>())%>
               <%=Html.Button(Html.GlobalName("btnClearAllFilters"), "Eliminar Filtros", "ClearAllFilters('{0}');".Formato(ViewData[ViewDataKeys.PopupPrefix] ?? ""), "", new Dictionary<string, object>())%>
           </div>
    <% List<FilterOption> filterOptions = ((FindOptions)ViewData[ViewDataKeys.FindOptions]).FilterOptions; %>
  
    <div id="<%=Html.GlobalName("filters-list")%>" class="filters-list">
        <span class="explanation" style="<%= (filterOptions == null || filterOptions.Count == 0) ? "" : "display:none;" %>">No se han especificado filtros</span>
        <table id="<%=Html.GlobalName("tblFilters")%>" style="<%= (filterOptions == null || filterOptions.Count == 0) ? "display:none;" : "" %>">            
            <thead>
                <tr>
                    <th>Campo</th>
                    <th>Operación</th>
                    <th>Valor</th>
                </tr>  
            </thead>  
            <tbody>
                <% for (int i=0; i<filterOptions.Count; i++)
                {
                    FilterOption filter = filterOptions[i];
                    Html.NewFilter(filter, i);            
                } 
                %>
            </tbody>
        </table>
        </div>
        </div>
    </div>
</div>

