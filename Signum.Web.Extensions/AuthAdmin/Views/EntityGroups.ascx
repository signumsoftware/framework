<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>

<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%@ Import Namespace="Signum.Web.Authorization" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<%: Html.DynamicCss("~/authAdmin/Content/authAdmin.css") %>
<%: Html.ScriptsJs("~/authAdmin/Scripts/authAdmin.js") %>

<script>
    $(function() {
    magicCheckBoxes($(document));
    });
</script>
<%
    using (var tc = Html.TypeContext<EntityGroupRulePack>())
    {
        Html.EntityLine(tc, f => f.Role);
        Html.ValueLine(tc, f => f.DefaultRule, vl => { vl.UnitText = tc.Value.DefaultLabel; });
%>

<table class="ruleTable">
    <thead>
        <tr>
            <th>
                <%=Html.Encode(Resources.EntityGroupsAscx_EntityGroup) %>
            </th>
            <th></th>
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
                 <%=Html.Encode(Resources.EntityGroupsAscx_Overriden) %>
            </th>
        </tr>
    </thead>
    <%
        foreach (var item in tc.TypeElementContext(p => p.Rules)) {
    %>
    <tr>
        <td  rowspan="2" style="vertical-align:middle">
            <%=Html.Span(null, item.Value.Resource.Name)%>
            <%=Html.HiddenRuntimeInfo(item, i=>i.Resource)%>
        </td>
        <td>In<%=Html.Hidden(item.Compose("InBase"), item.Value.AllowedBase.InGroup.ToStringParts())%></td>
        <td>
            <a class="cbLink create">
                <%=Html.CheckBox(item.Compose("In_Create"), item.Value.Allowed.InGroup.IsActive(TypeAllowedBasic.Create), new { tag = "Create" })%>
            </a>
        </td>
        <td>
            <a class="cbLink modify">
                <%=Html.CheckBox(item.Compose("In_Modify"), item.Value.Allowed.InGroup.IsActive(TypeAllowedBasic.Modify), new { tag = "Modify" })%>
            </a>
        </td>
        <td>
            <a class="cbLink read">
                <%=Html.CheckBox(item.Compose("In_Read"), item.Value.Allowed.InGroup.IsActive(TypeAllowedBasic.Read), new { tag = "Read" })%>
            </a>
        </td>
        <td>
            <a class="cbLink none">
                <%=Html.CheckBox(item.Compose("In_None"), item.Value.Allowed.InGroup.IsActive(TypeAllowedBasic.None), new { tag = "None" })%>
            </a>
        </td>
        <td>
            <%= Html.CheckBox(item.Compose("InOverriden"), !item.Value.Allowed.InGroup.Equals(item.Value.AllowedBase.InGroup), new { disabled = "disabled", @class = "overriden"})%>
        </td>
    </tr>
    <tr class="second">
        <td>Out<%=Html.Hidden(item.Compose("OutBase"), item.Value.AllowedBase.OutGroup.ToStringParts())%></td>
        <td>
            <a class="cbLink create">
                <%=Html.CheckBox(item.Compose("Out_Create"), item.Value.Allowed.OutGroup.IsActive(TypeAllowedBasic.Create), new  { tag = "Create" })%>
            </a>
        </td>
        <td>
            <a class="cbLink modify">
                <%=Html.CheckBox(item.Compose("Out_Modify"), item.Value.Allowed.OutGroup.IsActive(TypeAllowedBasic.Modify), new  { tag = "Modify" })%>
            </a>
        </td>
        <td>
            <a class="cbLink read">
                <%=Html.CheckBox(item.Compose("Out_Read"), item.Value.Allowed.OutGroup.IsActive(TypeAllowedBasic.Read), new { tag = "Read" })%>
            </a>
        </td>
        <td>
            <a class="cbLink none">
                <%=Html.CheckBox(item.Compose("Out_None"), item.Value.Allowed.OutGroup.IsActive(TypeAllowedBasic.None), new { tag = "None" })%>
            </a>
        </td>
        <td>
            <%= Html.CheckBox(item.Compose("OutOverriden"), !item.Value.Allowed.OutGroup.Equals(item.Value.AllowedBase.OutGroup), new { disabled = "disabled", @class = "overriden" })%>
        </td>
    </tr>
    <%
        }
    %>
</table>
<%
    }
%>
