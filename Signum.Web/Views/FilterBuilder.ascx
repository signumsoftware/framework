<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Engine.DynamicQuery" %>

<%  bool visible = ((FindOptions)ViewData[ViewDataKeys.FindOptions]).FilterMode == FilterMode.Visible;
    Context context = (Context)Model;
    FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions];
    QueryDescription queryDescription = (QueryDescription)ViewData[ViewDataKeys.QueryDescription];
    Type entitiesType = Reflector.ExtractLite(queryDescription.StaticColumns.Single(a => a.IsEntity).Type);
    %>

<%= Html.Hidden(context.Compose(ViewDataKeys.EntityTypeName), entitiesType.Name, new { disbled = "disabled" })%>

<div id="<%=context.Compose("fields-search")%>">
    <div id="<%=context.Compose("fields-list")%>" class="fields-list">
        <a onclick="javascript:toggleFilters('<%=context.Compose("fields-search")%>');" class="filters-header<%= visible ? "" : " close" %>" rev="filters-body"><%= visible ? "Ocultar filtros" : "Mostrar filtros" %></a>
        <div class="filters" <%= visible ? "" : "style='display:none'"%>>
            <div id="<%=context.Compose("filters-body")%>" class="filters-body">
                <label for="<%=context.Compose("ddlNewFilters")%>">Filtrar por campo</label>
                <select id="<%=context.Compose("ddlNewFilters")%>">

                <% 
                    List<StaticColumn> columns = queryDescription.StaticColumns.Where(a => a.Filterable).ToList();
                    foreach (StaticColumn column in columns)
                   {
                       Type type = column.Type.UnNullify();
                       if (typeof(Lite).IsAssignableFrom(type))
                            type = Reflector.ExtractLite(type);
                       string typeName = (Navigator.TypesToURLNames.ContainsKey(type)) ? type.Name : type.AssemblyQualifiedName;
                       %>
                       <option id="<%=context.Compose("option__" + column.Name) %>" value="<%=typeName %>"><%=column.DisplayName%></option>
                   <%
                   } 
                %>
               </select> 
               <%=Html.Button(context.Compose("btnAddFilter"), "+", "AddFilter('{0}');".Formato(context.ControlID), "", null)%>
               <%=Html.Button(context.Compose("btnClearAllFilters"), "Eliminar Filtros", "ClearAllFilters('{0}');".Formato(context.ControlID), "", null)%>
           </div>
    <% List<FilterOption> filterOptions = findOptions.FilterOptions; %>
  
    <div id="<%=context.Compose("filters-list")%>" class="filters-list">
        <span class="explanation" style="<%= (filterOptions == null || filterOptions.Count == 0) ? "" : "display:none;" %>">No se han especificado filtros</span>
        <table id="<%=context.Compose("tblFilters")%>" style="<%= (filterOptions == null || filterOptions.Count == 0) ? "display:none;" : "" %>">            
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
                       %>
                       <%= Html.NewFilter(findOptions.QueryName, filter, context, i)%>
                   <%
                } 
                %>
            </tbody>
        </table>
        </div>
        </div>
    </div>
</div>

