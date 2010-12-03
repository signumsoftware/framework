<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reports" %>

<%
using (var e = Html.TypeContext<QueryColumnDN>()) 
{
    using (var style = e.SubContext())
    {
        style.OnlyValue = true;
        Html.ValueLine(style, f => f.DisplayName, vl => vl.ValueHtmlProps["size"] = 20);
    %>
    <div style="float:left">
    <%: Html.QueryTokenCombo(e.Value.Token, e)%>
    </div>
    <%  
    }
}
%>

