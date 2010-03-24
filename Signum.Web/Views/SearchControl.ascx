<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="System.Configuration" %>

<% FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions];
   string sufix = (string)ViewData[ViewDataKeys.PopupSufix] ?? "";
    %>
<div id="<%=Html.GlobalName("divSearchControl" + sufix) %>" class="searchControl">
<%=Html.Hidden(Html.GlobalName("sfQueryUrlName" + sufix), Navigator.Manager.QuerySettings[findOptions.QueryName].UrlName, new { disbled = "disabled" })%>
<%=Html.Hidden(Html.GlobalName(ViewDataKeys.AllowMultiple + sufix), findOptions.AllowMultiple.ToString(), new { disbled = "disabled" })%>
<%=Html.Hidden(Html.GlobalName(ViewDataKeys.View + sufix), (bool)ViewData[ViewDataKeys.View], new { disbled = "disabled" })%>
<% 
    string popupPrefix = (string)ViewData[ViewDataKeys.PopupPrefix]; %>

<%= (findOptions.SearchOnLoad) ?
            "<script type=\"text/javascript\">$(document).ready(function() {{ SearchOnLoad('{0}'); }});</script>".Formato(Html.GlobalName("btnSearch" + sufix)) : 
    ""
%>

<div id="<%=Html.GlobalName("divFilters" + sufix) %>" style="display:<%= (findOptions.FilterMode != FilterMode.AlwaysHidden) ? "block" : "none" %>" >
    <%Html.RenderPartial(Navigator.Manager.FilterBuilderUrl, ViewData); %>
</div>

<div id="<%=Html.GlobalName("divMenuItems" + sufix) %>" class="buttonBar">
    <label class="OperationDiv" for="<%=Html.GlobalName(ViewDataKeys.Top + sufix)%>">Núm.registros</label> 
    <%= Html.TextBox(Html.GlobalName(ViewDataKeys.Top + sufix), ViewData[ViewDataKeys.Top] ?? "", new Dictionary<string, object> { { "size", "5" }, { "class", "OperationDiv" }, { "onkeydown", "return validator.number(event)" } })%>

    <input class="OperationDiv btnSearch" id="<%=Html.GlobalName("btnSearch" + sufix)%>" type="button" onclick="<%="Search({{prefix:'{0}',suffix:'{1}'}});".Formato(ViewData[ViewDataKeys.PopupPrefix] ?? "", sufix) %>" value="Buscar" /> 
    <% if ((bool)ViewData[ViewDataKeys.Create] && (bool)ViewData[ViewDataKeys.View])
       { %>
        <input type="button" value="+" class="lineButton create" onclick="<%="SearchCreate({{prefix:'{0}'}},'{1}');".Formato(popupPrefix ?? "", sufix)%>" />
    <%} %>
    <%= Html.GetButtonBarElementsForQuery(findOptions.QueryName, (Type)ViewData[ViewDataKeys.EntityType], popupPrefix)%> 
</div>
<div class="clearall"></div>
<div id="<%=Html.GlobalName("divResults" + sufix)%>" class="divResults"></div>
</div>