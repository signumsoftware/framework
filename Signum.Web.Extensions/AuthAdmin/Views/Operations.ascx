<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>

<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%@ Import Namespace="Signum.Web.Authorization" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<%: Html.DynamicCss("~/authAdmin/Content/authAdmin.css") %>

<%
    using (var tc = Html.TypeContext<OperationRulePack>())
    {
        Html.EntityLine(tc, f => f.Role);
        Html.ValueLine(tc, f => f.DefaultRule, vl => { vl.UnitText = tc.Value.DefaultLabel; }); 
        Html.EntityLine(tc, f => f.Type);
        
%>

<table class="ruleTable" id="operations">
    <thead>
        <tr>
            <th>
                <%: Resources.OperationsAscx_Operation %>
            </th>
            <th>
                <%: Resources.OperationsAscx_Allow %>
            </th>
            <th>
                <%: Resources.OperationsAscx_Deny %>
            </th>
            <th>
                <%: Resources.OperationsAscx_Overriden %>
            </th>
        </tr>
    </thead>
    <%
        foreach (var item in tc.TypeElementContext(p => p.Rules)) {
    %>
    <tr>
        <td>
            <%=Html.Span(null, item.Value.Resource.Name)%>
            <%=Html.HiddenRuntimeInfo(item, i=>i.Resource)%>
            <%=Html.Hidden(item.Compose("AllowedBase"), item.Value.AllowedBase)%>
        </td>
        <td>
            <a class="cbLink allowed">
            <%=Html.RadioButton(item.Compose("Allowed"), "True", item.Value.Allowed)%>
            </a>
        </td>
        <td>
            <a class="cbLink not-allowed">
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
