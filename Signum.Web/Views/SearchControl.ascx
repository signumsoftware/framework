<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="System.Configuration" %>

<% FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions];%>
<%=Html.Hidden(Html.GlobalName("sfQueryUrlName"), Navigator.Manager.QuerySettings[findOptions.QueryName].UrlName)%>
<%=Html.Hidden(Html.GlobalName("sfAllowMultiple"), findOptions.AllowMultiple.ToString())%>
<% string popupPrefix = (string)ViewData[ViewDataKeys.PopupPrefix]; %>

<%= (findOptions.SearchOnLoad) ?
        "<script type=\"text/javascript\">$(document).ready(function() {{ SearchOnLoad('{0}'); }});</script>".Formato(Html.GlobalName("btnSearch")) : 
    ""
%>

<div id="<%=Html.GlobalName("divFilters") %>" style="display:<%= (findOptions.FilterMode != FilterMode.AlwaysHidden) ? "block" : "none" %>" >
    <%Html.RenderPartial("~/Plugin/Signum.Web.dll/Signum.Web.Views.FilterBuilder.ascx", ViewData); %>
</div>

<div id="<%=Html.GlobalName("divMenuItems") %>" class="buttonBar">
    <label class="OperationDiv" for="<%=Html.GlobalName(ViewDataKeys.Top)%>">Núm.registros</label> 
    <%= Html.TextBox(Html.GlobalName(ViewDataKeys.Top), ViewData[ViewDataKeys.Top] ?? "", new Dictionary<string, object>{{"size","5"},{"class","OperationDiv"}})%>

    <input class="OperationDiv btnSearch" id="<%=Html.GlobalName("btnSearch")%>" type="button" onclick="<%="Search({{prefix:'{0}'}});".Formato(ViewData[ViewDataKeys.PopupPrefix] ?? "") %>" value="Buscar" /> 
    <%if ((bool)ViewData[ViewDataKeys.Create]){ %>
        <input type="button" value="+" class="lineButton create" onclick="<%="SearchCreate({{prefix:'{0}'}});".Formato(popupPrefix ?? "")%>" />
    <%} %>
    <%= Html.GetButtonBarElementsForQuery(findOptions.QueryName, (Type)ViewData[ViewDataKeys.EntityType], popupPrefix)%> 
</div>
<div class="clearall"></div>
<div id="<%=Html.GlobalName("divResults")%>" class="divResults"></div>
<div id="<%=Html.GlobalName("divASustituir")%>"></div>