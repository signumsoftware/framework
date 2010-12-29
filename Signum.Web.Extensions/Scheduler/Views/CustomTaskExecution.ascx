<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Scheduler" %>

<%
    using (var e = Html.TypeContext<CustomTaskExecutionDN>()) 
{
    Html.EntityLine(e, f => f.user);
	Html.ValueLine(e, f => f.StartTime);
    Html.ValueLine(e, f => f.EndTime);
    Html.ValueLine(e, f => f.Exception);
}
 %>