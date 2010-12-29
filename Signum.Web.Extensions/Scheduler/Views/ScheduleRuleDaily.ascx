<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Scheduler" %>
<%
    using (var e = Html.TypeContext<ScheduleRuleDailyDN>())
    {
        Html.ValueLine(e, f => f.StartingOn);
        Html.ValueLine(e, f => f.Hour);
        Html.ValueLine(e, f => f.Minute);  
    }
%>