<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>

<%= Html.Hidden(Html.GlobalName(ViewDataKeys.EntityTypeName), ViewData[ViewDataKeys.EntityTypeName].ToString())%>

<script type="text/javascript">
  function toggleFilters(id){
        var elem = $('#'+id+' .filters-header');
        var R = elem.attr('rev');
	    var D = $('#'+R);
	    D.toggle('fast');
	    $('#'+id+' .filters').toggle('fast');
	    elem.toggleClass('close');
	    if (elem.hasClass('close')) elem.html('Mostrar filtros');else elem.html('Ocultar filtros');
	    return false;
  }
</script>
<div class="fields-search" id="<%=Html.GlobalName("")%>">
    <div class="fields-list">
        <a onclick="javascript:toggleFilters('<%=Html.GlobalName("")%>');" class="filters-header" rev="<%=Html.GlobalName("filters-body")%>">Ocultar filtros</a>
        <div class="filters">
        <div class="filters-body">
            <label for="<%=Html.GlobalName("ddlNewFilters")%>">Filtrar por campo</label>
            <select id="<%=Html.GlobalName("ddlNewFilters")%>">

            <% foreach (Column column in (List<Column>)ViewData[ViewDataKeys.FilterColumns])
               {
                   Type type = column.Type.UnNullify();
                   %>
                   <option id="<%=Html.GlobalName("option__" + column.Name) %>" value="<%=typeof(Lite).IsAssignableFrom(type) ? Reflector.ExtractLite(type).Name : type.Name %>"><%=column.DisplayName%></option>
               <%
               } 
            %>
           </select> 
           <%=Html.Button(Html.GlobalName("btnAddFilter"), "+", "AddFilter('Signum.aspx/AddFilter','{0}');".Formato(ViewData[ViewDataKeys.PopupPrefix] ?? ""), "", new Dictionary<string, object>())%>
           <%=Html.Button(Html.GlobalName("btnClearAllFilters"), "Eliminar Filtros", "ClearAllFilters('{0}');".Formato(ViewData[ViewDataKeys.PopupPrefix] ?? ""), "", new Dictionary<string, object>())%>
       </div>
    <% List<FilterOptions> filterOptions = ((FindOptions)ViewData[ViewDataKeys.FindOptions]).FilterOptions; %>
  
    <div class="filters-list">
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
                FilterOptions filter = filterOptions[i];
                Html.NewFilter(filter, i);            
            } 
            %>
        </tbody>
    </table>
    </div>
    </div>
    </div>
</div>

