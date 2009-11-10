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
        var elem = $('#' + id + " .filters-header");
        var R = elem.attr('rev');
	    var D = $('#'+R);
	    D.toggle('fast');
	    $('#'+id+' .filters').toggle('fast');
	    elem.toggleClass('close');
	    if (elem.hasClass('close')) elem.html('Mostrar filtros');else elem.html('Ocultar filtros');
	    return false;
  }
  /*
  $(document).ready(function() {
    $('#filters-header').click(function(){
	    var R = $(this).attr('rev');
	    var D = $('#'+R);
	    D.toggle('fast');
	    $('#filters-list').toggle('fast');
	    $(this).toggleClass('close');
	    if ($(this).hasClass('close')) $(this).html('Mostrar filtros');else $(this).html('Ocultar filtros');
	    return false;
    });
  });*/
</script>
<div id="<%=Html.GlobalName("fields-search")%>">
    <div id="<%=Html.GlobalName("fields-list")%>" class="fields-list">
        <a onclick="javascript:toggleFilters('<%=Html.GlobalName("fields-search")%>');" class="filters-header" rev="filters-body">Ocultar filtros</a>
        <div class="filters">
            <div id="<%=Html.GlobalName("filters-body")%>" class="filters-body">
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

