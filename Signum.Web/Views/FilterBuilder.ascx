<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>

<%= Html.Hidden(Html.GlobalName(ViewDataKeys.EntityTypeName), ViewData[ViewDataKeys.EntityTypeName].ToString())%>

<script type="text/javascript">
  $(document).ready(function() {
    //$('.menu ul li .ext').css('display', 'none'); //ocultamos las capas por js, de esta manera, si el usuario navega con el javascript desactivado, verá las capas
    $('#filtros-header').click(function(){
	    var R = $(this).attr('rev');
	    var D = $('#'+R);
	    D.toggle('fast');
	    $('#filters-list').toggle('fast');
	    $(this).toggleClass('close');
	    if ($(this).hasClass('close')) $(this).html('Mostrar filtros');else $(this).html('Ocultar filtros');
	    return false;
    });
  });
</script>
<div id="buscar-campos">
    <div id="lista-campos">
        <a href="#" id="filtros-header" rev="filtros-body">Ocultar filtros</a>
        <div id="filtros-body">
            <label for="campos">Filtrar por campo</label>
            <select id="<%=Html.GlobalName("ddlNewFilters")%>" name="<%=Html.GlobalName("ddlNewFilters")%>">

            <% foreach (Column column in (List<Column>)ViewData[ViewDataKeys.FilterColumns])
               {
                   Type type = column.Type.UnNullify();
                   %>
                   <option id="<%=Html.GlobalName("option__" + column.Name) %>" value="<%=typeof(Lazy).IsAssignableFrom(type) ? Reflector.ExtractLazy(type).Name : type.Name %>"><%=column.DisplayName%></option>
               <%
               } 
            %>
           <%=Html.Button(Html.GlobalName("btnAddFilter"), "+", "AddFilter('Signum/AddFilter','{0}');".Formato(ViewData[ViewDataKeys.PopupPrefix] ?? ""), "", new Dictionary<string, object>())%>
           <%=Html.Button(Html.GlobalName("btnClearAllFilters"), "Eliminar Filtros", "ClearAllFilters('{0}');".Formato(ViewData[ViewDataKeys.PopupPrefix] ?? ""), "", new Dictionary<string, object>())%>
       <!--    <span class="separator"></span>
           <label for="<%=Html.GlobalName(ViewDataKeys.Top)%>">Núm.registros</label> <%= Html.TextBox(Html.GlobalName(ViewDataKeys.Top), ViewData[ViewDataKeys.Top] ?? "", new {size = "5" })%>
        --></select>
        </div>
    <% List<FilterOptions> filterOptions = ((FindOptions)ViewData[ViewDataKeys.FindOptions]).FilterOptions; %>
  
    <div id="filters-list">
    <span class="explanation" style="style="<%= (filterOptions == null || filterOptions.Count == 0) ? "display:none;" : "" %>">No se han especificado filtros</span>
    <table id="<%=Html.GlobalName("tblFilters")%>" name="<%=Html.GlobalName("tblFilters")%>" style="<%= (filterOptions == null || filterOptions.Count == 0) ? "display:none;" : "" %>">            
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
                Html.NewFilter(filter, i, ViewData[ViewDataKeys.EntityTypeName].ToString());            
            } 
            %>
        </tbody>
    </table>
    </div>
    </div>
</div>

