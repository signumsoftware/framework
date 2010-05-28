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
        <a onclick="javascript:toggleFilters('<%=context.Compose("fields-search")%>');" class="filters-header<%= visible ? "" : " close" %>" rev="filters-body"><%= visible ? Html.Encode(Resources.HideFilters) : Html.Encode(Resources.ShowFilters) %></a>
        <div class="filters" <%= visible ? "" : "style='display:none'"%>>
            <div id="<%=context.Compose("filters-body")%>" class="filters-body">
                <label for="<%=context.Compose("ddlNewFilters")%>"><%=Html.Encode(Resources.FilterByField) %></label>
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
               <%=Html.Button(context.Compose("btnClearAllFilters"), Html.Encode(Resources.DeleteFilters), "ClearAllFilters('{0}');".Formato(context.ControlID), "", findOptions.FilterOptions.Count == 0 ? new Dictionary<string, object> { { "style", "display:none;" } } : new Dictionary<string, object>())%>
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

