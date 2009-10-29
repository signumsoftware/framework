<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="System.Configuration" %>

<% FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions];%>
<%=Html.Hidden(Html.GlobalName("sfQueryName"), findOptions.QueryName.ToString())%>
<%=Html.Hidden(Html.GlobalName("sfAllowMultiple"), findOptions.AllowMultiple.ToString())%>

<%= (findOptions.SearchOnLoad) ?
        "<script type=\"text/javascript\">$(document).ready(function() {{ SearchOnLoad('{0}'); }});</script>".Formato(Html.GlobalName("btnSearch")) : 
    ""
%>

<div id="<%=Html.GlobalName("divFilters") %>" style="display:<%= (findOptions.FilterMode == FilterMode.Visible) ? "block" : "none" %>" >
    <%Html.RenderPartial("~/Plugin/Signum.Web.dll/Signum.Web.Views.FilterBuilder.ascx", ViewData); %>
</div>

<div id="<%=Html.GlobalName("divMenuItems") %>" class="buttonBar">
    <label class="OperationDiv" for="<%=Html.GlobalName(ViewDataKeys.Top)%>">Núm.registros</label> 
    <%= Html.TextBox(Html.GlobalName(ViewDataKeys.Top), ViewData[ViewDataKeys.Top] ?? "", new Dictionary<string, object>{{"size","5"},{"class","OperationDiv"}})%>

    <% if (findOptions.FilterMode != FilterMode.AlwaysHidden){%>
        <input class="OperationDiv" type="hidden" onclick="toggleVisibility('<%=Html.GlobalName("divFilters") %>');" value="Filtros" /> 
    <%} %>
        <input class="OperationDiv" id="<%=Html.GlobalName("btnSearch")%>" type="button" onclick="<%="$('#{0}').toggleClass('loading');$('#{0}').val('Buscando...');Search('Signum.aspx/Search','{1}',function(){{$('#{0}').val('Buscar');$('#{0}').toggleClass('loading');}});".Formato(Html.GlobalName("btnSearch"),ViewData[ViewDataKeys.PopupPrefix] ?? "") %>" value="Buscar" /> 
    <%if ((bool)ViewData[ViewDataKeys.Create]){ %>
        <input class="OperationDiv" id="<%=Html.GlobalName("btnCreate")%>" type="button" onclick="<%="SearchCreate('{0}','{1}',function(){{OnSearchCreateOK('{2}','{1}');}},function(){{OnSearchCreateCancel('{1}');}},'false');".Formato("Signum.aspx/PopupView", ViewData[ViewDataKeys.PopupPrefix] ?? "", "Signum.aspx/TrySavePartial")%>" value="+" /> 
    <%} %>
    <%= Html.GetButtonBarElementsForQuery(findOptions.QueryName, (Type)ViewData[ViewDataKeys.EntityType], (string)ViewData[ViewDataKeys.PopupPrefix])%> 
</div>
<div class="clearall"></div>
<div id="<%=Html.GlobalName("divResults")%>" name="<%=Html.GlobalName("divResults")%>" class="divResults">

</div>
<div id="<%=Html.GlobalName("divASustituir")%>"></div>
