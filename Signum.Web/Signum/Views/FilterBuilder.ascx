<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Engine.DynamicQuery" %>
<%@ Import Namespace="Signum.Web.Properties" %>

<%  bool visible = ((FindOptions)ViewData[ViewDataKeys.FindOptions]).FilterMode == FilterMode.Visible;
    Context context = (Context)Model;
    FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions];
    QueryDescription queryDescription = (QueryDescription)ViewData[ViewDataKeys.QueryDescription];
    Type entitiesType = Reflector.ExtractLite(queryDescription.StaticColumns.Single(a => a.IsEntity).Type);
    %>

<%= Html.Hidden(context.Compose(ViewDataKeys.EntityTypeName), entitiesType.Name, new { disbled = "disabled" })%>

<div id="<%=context.Compose("fields-search")%>">
    <div id="<%=context.Compose("fields-list")%>" class="fields-list">
        <a onclick="toggleFilters(this)" class="filters-header<%= visible ? "" : " close" %>" rev="filters-body"><%= visible ? Html.Encode(Resources.HideFilters) : Html.Encode(Resources.ShowFilters) %></a>
        <div class="filters" <%= visible ? "" : "style='display:none'"%>>
            <div id="<%=context.Compose("filters-body")%>" class="filters-body">
                <label for="<%=context.Compose("ddlTokens_0")%>"><%=Html.Encode(Resources.FilterByField) %></label>
                <% var columns = queryDescription.StaticColumns
                        .Where(a => a.Filterable)
                        .Select(c => new SelectListItem { Text = c.DisplayName, Value = c.Name, Selected = false })
                        .ToList();
                   columns.Insert(0, new SelectListItem { Text = "-", Selected = true, Value = "" });
                %>
               <%= Html.TokensCombo(columns, context, 0) %>
               <%= Html.Button(context.Compose("btnAddFilter"), "+", "AddFilter('{0}');".Formato(context.ControlID), "addFilter", new Dictionary<string, object> { { "title", "Add Filter" }})%>
               <% if (findOptions.AllowUserColumns.HasValue ? findOptions.AllowUserColumns.Value : Navigator.Manager.AllowUserColumns(context.ControlID))
                  { %>
               <%= Html.Button(context.Compose("btnAddColumn"), "+", "AddColumn('{0}');".Formato(context.ControlID), "addColumn", null)%>
               <% } %>
               <%= Html.Button(context.Compose("btnEditColumns"), Html.Encode(Resources.UserColumnsEdit), "EditColumns('{0}');".Formato(context.ControlID), "", findOptions.UserColumnOptions.Any() ? new Dictionary<string, object>() : new Dictionary<string, object> { { "style", "display:none;" } })%>
               <%= Html.Button(context.Compose("btnEditColumnsFinish"), Html.Encode(Resources.EditColumnsFinishEdit), "EditColumnsFinish('{0}');".Formato(context.ControlID), "", new Dictionary<string, object> { { "style", "display:none;" } })%>
               <%= Html.Button(context.Compose("btnClearAllFilters"), Html.Encode(Resources.DeleteFilters), "ClearAllFilters('{0}');".Formato(context.ControlID), "", findOptions.FilterOptions.Any() ? new Dictionary<string, object>() : new Dictionary<string, object> { { "style", "display:none;" } })%>
           </div>
    <% List<FilterOption> filterOptions = findOptions.FilterOptions; %>
  
    <div id="<%=context.Compose("filters-list")%>" class="filters-list">
        <span class="explanation" style="<%= (filterOptions == null || filterOptions.Count == 0) ? "" : "display:none;" %>"><%=Html.Encode(Resources.NoFiltersSpecified)%></span>
        <table id="<%=context.Compose("tblFilters")%>" style="<%= (filterOptions == null || filterOptions.Count == 0) ? "display:none;" : "" %>">            
            <thead>
                <tr>
                    <th><%=Html.Encode(Resources.Field)%></th>
                    <th><%=Html.Encode(Resources.Operation)%></th>
                    <th><%=Html.Encode(Resources.Value)%></th>
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

