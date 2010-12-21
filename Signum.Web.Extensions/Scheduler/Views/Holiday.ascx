<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Scheduler" %>

<%
    using (var e = Html.TypeContext<HolidayDN>()) 
{
    Html.ValueLine(e, f => f.Date);
    Html.ValueLine(e, f => f.Name);        
}
 %>