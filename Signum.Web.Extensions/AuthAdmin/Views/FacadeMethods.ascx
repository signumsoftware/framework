<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>

<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%@ Import Namespace="Signum.Web.Authorization" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<%= Html.RegisterCss("authAdmin/Content/authAdmin.css") %>
<%
    using (var tc = Html.TypeContext<FacadeMethodRulePack>())
    {
        Html.EntityLine(tc, f => f.Role);
%>

<script type="text/javascript" src="authAdmin/Scripts/authAdmin.js"></script>

<table class="ruleTable">
    <thead>
        <tr>
            <th>
                <%=Html.Encode(Resources.FacadeMethodsAscx_FacadeMethod) %>
            </th>
            <th>
                <%=Html.Encode(Resources.FacadeMethodsAscx_Allow) %>
            </th>
            <th>
                <%=Html.Encode(Resources.FacadeMethodsAscx_Deny) %>
            </th>
            <th style="display: none">
                <%=Html.Encode(Resources.FacadeMethodsAscx_Overriden) %>
            </th>
        </tr>
    </thead>
    <%
        foreach (var item in tc.TypeElementContext(p => p.Rules)) {
    %>
    <tr>
        <td>
            <%=Html.Span(null, item.Value.Resource.Name)%>
            <%=Html.Hidden(item.Compose("Resource_Name"),  item.Value.Resource.Name)%>
            <%=Html.Hidden(item.Compose("AllowedBase"), item.Value.AllowedBase)%>
        </td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioGreen.png)">
                <%=Html.RadioButton(item.Compose("Allowed"), "True", item.Value.Allowed)%>
            </a>
        </td>
        <td>
            <a class="cbLink" style="background-image: url(authAdmin/images/radioRed.png)">
                <%=Html.RadioButton(item.Compose("Allowed"), "False", !item.Value.Allowed)%>
            </a>
        </td>
        <td>
            <%= Html.CheckBox(item.Compose("Overriden"), item.Value.Overriden, new {disabled = "disabled",  @class = "overriden"}) %>
        </td>
    </tr>
    <%
        }
    %>
</table>
<%
    }
%>
