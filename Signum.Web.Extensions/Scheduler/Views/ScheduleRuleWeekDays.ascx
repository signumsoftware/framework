<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Scheduler" %>
<%
    using (var e = Html.TypeContext<ScheduleRuleWeekDaysDN>())
    {
     
        //Html.EmbeddedControl(e, f => (ScheduleRuleDayDN)f);
        Html.ValueLine(e, f => f.StartingOn);
        Html.ValueLine(e, f => f.Hour);
        Html.ValueLine(e, f => f.Minute);  
        Html.ValueLine(e, f => f.Sunday);
        Html.ValueLine(e, f => f.Monday);
        Html.ValueLine(e, f => f.Tuesday);
        Html.ValueLine(e, f => f.Wednesday);
        Html.ValueLine(e, f => f.Thursday);
        Html.ValueLine(e, f => f.Friday);
        Html.ValueLine(e, f => f.Saturday);
        
    }
%>