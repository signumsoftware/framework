<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>

<%
using (var e = Html.TypeContext<UserDN>()) 
{
    Html.ValueLine(e, f => f.UserName, vl => vl.ValueHtmlProps["size"] = 50);
    Html.ValueLine(e, f => f.Email, vl=>vl.ValueHtmlProps["size"]=30);
    Html.EntityLine(e, f => f.Role);
    Html.ValueLine(e, f => f.State);
    Html.EntityLine(e, f => f.Related);
}
%>
