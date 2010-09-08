<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.ControlPanel" %>
<%@ Import Namespace="Signum.Web.ControlPanel" %>
<%@ Import Namespace="Signum.Entities.Reports" %>

<% 
    using (var tc = Html.TypeContext<LinkElement>())
    {
        tc.BreakLine = false;
        tc.ValueFirst = true;
        
        Html.ValueLine(tc, l => l.Label);
        Html.ValueLine(tc, l => l.Link);
    }
%>