<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>

<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Properties" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Engine.DynamicQuery" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="Signum.Web.Properties" %>

<% 
    Context context = (Context)Model;
    FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions];
    QueryDescription queryDescription = (QueryDescription)ViewData[ViewDataKeys.QueryDescription];
    Type entitiesType = Reflector.ExtractLite(queryDescription.StaticColumns.Single(a => a.IsEntity).Type);
    bool viewable = findOptions.View && Navigator.IsNavigable(entitiesType, true);
%>

<div id="<%=context.Compose("divSearchControl") %>" class="searchControl">
<%=Html.Hidden(context.Compose("sfQueryUrlName"), Navigator.Manager.QuerySettings[findOptions.QueryName].UrlName, new { disbled = "disabled" })%>
<%=Html.Hidden(context.Compose(ViewDataKeys.AllowMultiple), findOptions.AllowMultiple.ToString(), new { disbled = "disabled" })%>
<%=Html.Hidden(context.Compose(ViewDataKeys.View), viewable, new { disbled = "disabled" })%>

<%= (findOptions.SearchOnLoad) ?
    "<script type=\"text/javascript\">$(document).ready(function() {{ SearchOnLoad('{0}'); }});</script>".Formato(context.Compose("btnSearch")) : 
    ""
%>

<div id="<%=context.Compose("divFilters") %>" style="display:<%= (findOptions.FilterMode != FilterMode.AlwaysHidden) ? "block" : "none" %>" >
    <%Html.RenderPartial(Navigator.Manager.FilterBuilderUrl, ViewData); %>
</div>

<div class="search-footer">
    <label for="<%=context.Compose(ViewDataKeys.Top)%>"><%=Html.Encode(Resources.NumberOfRows) %></label> 
    <%= Html.TextBox(context.Compose(ViewDataKeys.Top), Navigator.Manager.QuerySettings.GetOrThrow(findOptions.QueryName, Resources.MissingQuerySettingsForQueryName0).Top.TryToString(), new Dictionary<string, object> { { "size", "5" }, { "onkeydown", "return validator.number(event)" } })%>

    <input class="btnSearch" id="<%=context.Compose("btnSearch")%>" type="button" onclick="<%="Search({{prefix:'{0}'}});".Formato(context.ControlID) %>" value="<%=Html.Encode(Resources.Search) %>" /> 
    <% if (findOptions.Create && Navigator.IsCreable(entitiesType, true) && viewable)
       { %>
        <input type="button" value="+" class="lineButton create" onclick="<%="SearchCreate({{prefix:'{0}'}});".Formato(context.ControlID)%>" />
    <%} %>
    <%= ButtonBarQueryHelper.GetButtonBarElementsForQuery(this.ViewContext, findOptions.QueryName, entitiesType, context.ControlID).ToString(Html)%> 
</div>
<div class="clearall"></div>
<div id="<%=context.Compose("divResults")%>" class="divResults"></div>
</div>
