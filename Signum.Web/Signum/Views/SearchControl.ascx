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
    Type entitiesType = Reflector.ExtractLite(queryDescription.Columns.Single(a => a.IsEntity).Type);
    bool viewable = findOptions.View && Navigator.IsNavigable(entitiesType, true);
%>

<div id="<%=context.Compose("divSearchControl") %>" class="searchControl">
<%=Html.Hidden(context.Compose("sfQueryUrlName"), Navigator.Manager.QuerySettings[findOptions.QueryName].UrlName, new { disabled = "disabled" })%>
<%=Html.Hidden(context.Compose(ViewDataKeys.AllowMultiple), findOptions.AllowMultiple.ToString(), new { disabled = "disabled" })%>
<%=Html.Hidden(context.Compose(ViewDataKeys.View), viewable, new { disabled = "disabled" })%>
<%= Html.Hidden(context.Compose(ViewDataKeys.EntityTypeName), entitiesType.Name, new { disabled = "disabled" })%>
<% if (findOptions.EntityContextMenu)
   {
       Response.Write("<script type=\"text/javascript\">var " + context.Compose("EntityContextMenu") + " = true;</script>");
   }
%>

<%= (findOptions.SearchOnLoad) ?
    "<script type=\"text/javascript\">$(document).ready(function() {{ SearchOnLoad('{0}'); }});</script>".Formato(context.ControlID) : 
    ""
%>

<div id="<%=context.Compose("divFilters") %>" style="display:<%= (findOptions.FilterMode != FilterMode.AlwaysHidden && findOptions.FilterMode != FilterMode.OnlyResults) ? "block" : "none" %>" >
    <%Html.RenderPartial(Navigator.Manager.FilterBuilderUrl, ViewData); %>
</div>

<div class="search-footer" style="display:<%= (findOptions.FilterMode != FilterMode.OnlyResults) ? "block" : "none" %>">
    <%= Html.Label(null, Resources.NumberOfRows, context.Compose(ViewDataKeys.Top), null) %>
    <% int? top = findOptions.Top ?? Navigator.Manager.QuerySettings.GetOrThrow(findOptions.QueryName, "Missing QuerySettings for QueryName {0}").Top; %>
    <%= HtmlHelperExtenders.InputType("text", context.Compose(ViewDataKeys.Top), top.TryToString(), new Dictionary<string, object> { { "size", "5" }, { "onkeydown", "return validator.number(event)" } })%>

    <%= Html.Hidden(context.Compose("OrderBy"), findOptions.OrderOptions == null ? "" :
                (findOptions.OrderOptions.ToString(oo => (oo.OrderType == OrderType.Ascending ? "" : "-") + oo.Token.FullKey(), ",")))%>

    <input class="btnSearch" id="<%=context.Compose("btnSearch")%>" type="button" onclick="<%="Search({{prefix:'{0}'}});".Formato(context.ControlID) %>" value="<%: Resources.Search %>" /> 
    <% if (findOptions.Create && Navigator.IsCreable(entitiesType, true) && viewable)
       { %>
        <input type="button" value="+" class="lineButton create" onclick="<%= findOptions.Creating.HasText() ? findOptions.Creating : "SearchCreate({{prefix:'{0}'}});".Formato(context.ControlID)%>" />
    <%} %>
    <ul class="button-bar">
    <%= ButtonBarQueryHelper.GetButtonBarElementsForQuery(this.ViewContext, findOptions.QueryName, entitiesType, context.ControlID).ToString(Html)%> 
    </ul>
</div>
<%if (findOptions.FilterMode != FilterMode.OnlyResults)
  { %>
<div class="clearall"></div>
<% } %>
<div id="<%=context.Compose("divResults")%>" class="divResults">

<table id="<%=context.Compose("tblResults")%>" class="tblResults">
    <thead>
        <tr>
            <%if (findOptions.AllowMultiple.HasValue)
              {
            %>
            <th class="thRowSelection">
                <%if (findOptions.AllowMultiple.Value)
                  {
                  %>
                    <%= Html.CheckBox(context.Compose("cbSelectAll"), false, new { onclick = "javascript:ToggleSelectAll('{0}');".Formato(context.ControlID) })%>
                  <% } %>
            </th>
            <%}
              if (viewable)
              {
             %>
            <th class="thRowEntity">
            </th>
            <%}

              foreach (var col in findOptions.MergeColumns())
              {
                  var order = findOptions.OrderOptions.FirstOrDefault(oo => oo.Token.FullKey() == col.Name);
                  OrderType? orderType = null;
                  if (order != null)
                  {
                    orderType = order.OrderType;
                  }
            %>
            <th class="<%= (orderType == null) ? "" : (orderType == OrderType.Ascending ? "headerSortDown" : "headerSortUp") %>">
                <input type="hidden" value="<%= col.Name %>" />
                <%= col.DisplayName%>
            </th>
            <%
              }
            %>
        </tr>
    </thead>
    <tbody>
    </tbody>
    <tfoot>
    </tfoot>
</table>

</div>
</div>
