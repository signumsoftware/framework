<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>

<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%@ Import Namespace="Signum.Web.Authorization" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<%= Html.RegisterCss("authAdmin/Content/authAdmin.css")%>
<%
    using (var tc = Html.TypeContext<EntityGroupRulePack>())
    {
        Html.EntityLine(tc, f => f.Role);
%>

<script type="text/javascript" src="authAdmin/Scripts/authAdmin.js"></script>

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
        <td>In<%=Html.Hidden(item.Compose("InBase"), item.Value.AllowedBase.InGroup.ToString())%></td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioBlue.png)">
                <%=Html.RadioButton(item.Compose("In"), "Create", item.Value.Allowed.InGroup == TypeAllowed.Create)%>
            </a>
        </td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioGreen.png)">
                <%=Html.RadioButton(item.Compose("In"), "Modify", item.Value.Allowed.InGroup == TypeAllowed.Modify)%>
            </a>
        </td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioYellow.png)">
                <%=Html.RadioButton(item.Compose("In"), "Read", item.Value.Allowed.InGroup == TypeAllowed.Read)%>
            </a>
        </td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioRed.png)">
                <%=Html.RadioButton(item.Compose("In"), "None", item.Value.Allowed.InGroup == TypeAllowed.None)%>
            </a>
        </td>
        <td>
            <%= Html.CheckBox(item.Compose("InOverriden"), !item.Value.Allowed.InGroup.Equals(item.Value.AllowedBase.InGroup), new { disabled = "disabled", @class = "overriden"})%>
        </td>
    </tr>
    <tr class="second">
        <td>Out<%=Html.Hidden(item.Compose("OutBase"), item.Value.AllowedBase.OutGroup.ToString())%></td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioBlue.png)">
                <%=Html.RadioButton(item.Compose("Out"), "Create", item.Value.Allowed.OutGroup == TypeAllowed.Create)%>
            </a>
        </td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioGreen.png)">
                <%=Html.RadioButton(item.Compose("Out"), "Modify", item.Value.Allowed.OutGroup == TypeAllowed.Modify)%>
            </a>
        </td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioYellow.png)">
                <%=Html.RadioButton(item.Compose("Out"), "Read", item.Value.Allowed.OutGroup == TypeAllowed.Read)%>
            </a>
        </td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioRed.png)">
                <%=Html.RadioButton(item.Compose("Out"), "None", item.Value.Allowed.OutGroup == TypeAllowed.None)%>
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
