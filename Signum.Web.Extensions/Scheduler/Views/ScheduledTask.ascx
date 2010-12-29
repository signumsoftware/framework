<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Scheduler" %>
<%
    using (var e = Html.TypeContext<ScheduledTaskDN>())
    {
        Html.ValueLine(e, f => f.NextDate);
        Html.ValueLine(e, f => f.Suspended);
        Html.EntityLine(e, f => f.Task);
        Html.EntityLine(e, f => f.Rule);
    }
%>