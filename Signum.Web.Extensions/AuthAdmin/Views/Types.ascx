<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>

<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Web.Authorization" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%@ Import Namespace="Signum.Engine.Operations" %>
<%@ Import Namespace="Signum.Engine.DynamicQuery" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<%= Html.RegisterCss("authAdmin/Content/authAdmin.css")%>
<%
    using (var tc = Html.TypeContext<TypeRulePack>())
    {
        Html.EntityLine(tc, f => f.Role);
%>

<script type="text/javascript" src="authAdmin/Scripts/authAdmin.js"></script>

<table class="ruleTable">
    <thead>
        <tr>
            <th>
                <%=Html.Encode(Resources.TypesAscx_Type) %>
            </th>
            <th>
                <%=Html.Encode(Resources.TypesAscx_Create) %>
            </th>
            <th>
                <%=Html.Encode(Resources.TypesAscx_Modify) %>
            </th>
            <th>
                <%=Html.Encode(Resources.TypesAscx_Read) %>
            </th>
            <th>
                <%=Html.Encode(Resources.TypesAscx_None) %>
            </th>
            <th>
                <%=Html.Encode(Resources.TypesAscx_Overriden) %>
            </th>
            <% if (Navigator.Manager.EntitySettings.ContainsKey(typeof(PermissionRulePack)))
               { %>
            <th>
                <%=Html.Encode(Resources.TypesAscx_Properties) %>
            </th>
            <%} %>
            <% if (Navigator.Manager.EntitySettings.ContainsKey(typeof(OperationRulePack)))
               { %>
            <th>
                <%=Html.Encode(Resources.TypesAscx_Operations) %>
            </th>
            <%} %>
            <% if (Navigator.Manager.EntitySettings.ContainsKey(typeof(QueryRulePack)))
               { %>
            <th>
                <%=Html.Encode(Resources.TypesAscx_Queries) %>
            </th>
            <%} %>
        </tr>
    </thead>
    <%
        foreach (var iter in tc.TypeElementContext(p=>p.Rules).GroupBy(a => a.Value.Resource.Namespace).OrderBy(a => a.Key).Iterate())
        {
    %>
    <tr>
        <td colspan="6">
            <a class="namespace">
                <div class="treeView <%=iter.IsLast?"tvExpandedLast": "tvExpanded" %>">
                </div>
                <img src="authAdmin/Images/namespace.png" />
                <%=Html.Span(null, iter.Value.Key, "namespace") %>
            </a>
        </td>
    </tr>
    <%
        foreach (var iter2 in iter.Value.OrderBy(a => a.Value.Resource.FriendlyName).Iterate())
        {
            var item = iter2.Value;
  
    %>
    <tr>
        <td>
            <div class="treeView <%=iter.IsLast?"tvBlank": "tvLine" %>">
            </div>
            <div class="treeView <%=iter2.IsLast?"tvLeafLast": "tvLeaf" %>">
            </div>
            <img src="authAdmin/Images/class.png" />
            <%=Html.Span(null, item.Value.Resource.FriendlyName)%>
            <%=Html.HiddenRuntimeInfo(item, i => i.Resource)%>
            <%=Html.Hidden(item.Compose("AllowedBase"), item.Value.AllowedBase)%>
            <%=Html.Span(null, iter.Value.Key, "namespace", new Dictionary<string, object> { {"style", "display:none"}})%>
        </td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioBlue.png)">
                <%=Html.RadioButton(item.Compose("Allowed"), TypeAllowed.Create.ToString(), item.Value.Allowed == TypeAllowed.Create)%></a>
        </td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioGreen.png)">
                <%=Html.RadioButton(item.Compose("Allowed"), TypeAllowed.Modify.ToString(), item.Value.Allowed == TypeAllowed.Modify)%></a>
        </td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioYellow.png)">
                <%=Html.RadioButton(item.Compose("Allowed"), TypeAllowed.Read.ToString(), item.Value.Allowed == TypeAllowed.Read)%></a>
        </td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioRed.png)">
                <%=Html.RadioButton(item.Compose("Allowed"), TypeAllowed.None.ToString(), item.Value.Allowed == TypeAllowed.None)%>
            </a>
        </td>
        <td>
            <%= Html.CheckBox(item.Compose("Overriden"), item.Value.Overriden, new {disabled = "disabled",  @class = "overriden"}) %>
        </td>
        <% if (Navigator.Manager.EntitySettings.ContainsKey(typeof(PropertyRulePack)))
           { 
               %>
        <td>
         <%if (item.Value.Properties.HasValue)
              {%>
            <a href="javascript:openDialog('AuthAdmin/Properties', {role:<%=tc.Value.Role.Id%>, type:<%=item.Value.Resource.Id%>});">
               <div style="background-image:url(authAdmin/images/property.png); background-repeat:no-repeat">
                    <div class="thumb <%=item.Value.Properties.ToString().ToLower()%>"></div>
                </div>
            </a>
              <%} %>
        </td>
        <%} %>
        <% if (Navigator.Manager.EntitySettings.ContainsKey(typeof(OperationRulePack)))
           { 
               %>
        <td>
            <%if (item.Value.Operations.HasValue)
              {%>
            <a href="javascript:openDialog('AuthAdmin/Operations', {role:<%=tc.Value.Role.Id%>, type:<%=item.Value.Resource.Id%>});">
                <div style="background-image:url(authAdmin/images/operation.png); background-repeat:no-repeat">
                    <div class="thumb <%=item.Value.Operations.ToString().ToLower()%>"></div>
                </div>
            </a>
            <%} %>
        </td>
        <%} %>
        <% if (Navigator.Manager.EntitySettings.ContainsKey(typeof(QueryRulePack)))
           {
               %>
        <td>
            <%if (item.Value.Queries.HasValue)
              {%>
            <a href="javascript:openDialog('AuthAdmin/Queries', {role:<%=tc.Value.Role.Id%>, type:<%=item.Value.Resource.Id%>});">
                <div style="background-image:url(authAdmin/images/query.png); background-repeat:no-repeat">
                    <div class="thumb <%=item.Value.Queries.ToString().ToLower()%>"></div>
                </div>
            </a>
            <%} %>
        </td>
        <%} %>
    </tr>
    <%
        }
        }
    %>
</table>
<%
    }
%>
