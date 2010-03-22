<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>

<%  string sufix = (string)ViewData[ViewDataKeys.PopupSufix];
    bool visible = ((FindOptions)ViewData[ViewDataKeys.FindOptions]).FilterMode == FilterMode.Visible;
    %>

<%= Html.Hidden(Html.GlobalName(ViewDataKeys.EntityTypeName + sufix), ViewData[ViewDataKeys.EntityTypeName].ToString(), new { disbled = "disabled" })%>

<div id="<%=Html.GlobalName("fields-search" + sufix)%>">
    <div id="<%=Html.GlobalName("fields-list" + sufix)%>" class="fields-list">
        <a onclick="javascript:toggleFilters('<%=Html.GlobalName("fields-search" + sufix)%>');" class="filters-header<%= visible ? "" : " close" %>" rev="filters-body"><%= visible ? "Ocultar filtros" : "Mostrar filtros" %></a>
        <div class="filters" <%= visible ? "" : "style='display:none'"%>>
            <div id="<%=Html.GlobalName("filters-body" + sufix)%>" class="filters-body">
                <label for="<%=Html.GlobalName("ddlNewFilters" + sufix)%>">Filtrar por campo</label>
                <select id="<%=Html.GlobalName("ddlNewFilters" + sufix)%>">

                <% foreach (StaticColumn column in (List<StaticColumn>)ViewData[ViewDataKeys.FilterColumns])
                   {
                       Type type = column.Type.UnNullify();
                       if (typeof(Lite).IsAssignableFrom(type))
                            type = Reflector.ExtractLite(type);
                       string typeName = (Navigator.TypesToURLNames.ContainsKey(type)) ? type.Name : type.AssemblyQualifiedName;
                       %>
                       <option id="<%=Html.GlobalName("option__" + column.Name + sufix) %>" value="<%=typeName %>"><%=column.DisplayName%></option>
                   <%
                   } 
                %>
               </select> 
               <%=Html.Button(Html.GlobalName("btnAddFilter" + sufix), "+", "AddFilter('{0}','{1}');".Formato(ViewData[ViewDataKeys.PopupPrefix] ?? "", sufix ?? ""), "", new Dictionary<string, object>())%>
               <%=Html.Button(Html.GlobalName("btnClearAllFilters" + sufix), "Eliminar Filtros", "ClearAllFilters('{0}','{1}');".Formato(ViewData[ViewDataKeys.PopupPrefix] ?? "", sufix ?? ""), "", new Dictionary<string, object>())%>
           </div>
    <% List<FilterOption> filterOptions = ((FindOptions)ViewData[ViewDataKeys.FindOptions]).FilterOptions; %>
  
    <div id="<%=Html.GlobalName("filters-list" + sufix)%>" class="filters-list">
        <span class="explanation" style="<%= (filterOptions == null || filterOptions.Count == 0) ? "" : "display:none;" %>">No se han especificado filtros</span>
        <table id="<%=Html.GlobalName("tblFilters" + sufix)%>" style="<%= (filterOptions == null || filterOptions.Count == 0) ? "display:none;" : "" %>">            
            <thead>
                <tr>
                    <th>Campo</th>
                    <th>Operación</th>
                    <th>Valor</th>
                    <th></th>
                </tr>  
            </thead>  
            <tbody>
                <% for (int i=0; i<filterOptions.Count; i++)
                {
                    FilterOption filter = filterOptions[i];
                    Html.NewFilter(((FindOptions)ViewData[ViewDataKeys.FindOptions]).QueryName, filter, i);            
                } 
                %>
            </tbody>
        </table>
        </div>
        </div>
    </div>
</div>

