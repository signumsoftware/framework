<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.ControlPanel" %>
<%@ Import Namespace="Signum.Web.ControlPanel" %>
<%@ Import Namespace="Signum.Entities.Reports" %>

<% 
    using (var tc = Html.TypeContext<CountSearchControlPartDN>())
    {
        tc.BreakLine = true;
        tc.ValueFirst = false;

        Html.EntityRepeater(tc, p => p.UserQueries);
    }
%>