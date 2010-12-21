<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Scheduler" %>

<%
    using (var e = Html.TypeContext<CalendarDN>()) 
{
	Html.ValueLine(e, f => f.Name);
    Html.EntityList(e, f => f.Holidays);
}
 %>