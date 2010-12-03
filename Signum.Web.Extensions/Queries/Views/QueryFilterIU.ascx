<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reports" %>
<%@ Import Namespace="Signum.Web.Queries" %>

<%
using (var e = Html.TypeContext<QueryFilterDN>()) 
{
    using (var style = e.SubContext())
    {
        style.OnlyValue = true;
    %>
    <div style="float:left">
    <%: Html.QueryTokenCombo(e.Value.Token, e)%>
    </div>
    <%
        Html.ValueLine(style, f => f.Operation);
        Html.ValueLine(style, f => f.ValueString, vl => vl.ValueHtmlProps["size"] = 20);
    }
}
%>

