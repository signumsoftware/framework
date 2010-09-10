<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reports" %>

<%
using (var e = Html.TypeContext<QueryTokenDN>()) 
{
    %>
    <%= Html.WriteQueryToken(e.Value.Token, e)%>
    <%
}
%>
