<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.ControlPanel" %>
<%@ Import Namespace="Signum.Web.ControlPanel" %>
<%@ Import Namespace="System.Reflection" %>

<% 
    using (var tc = Html.TypeContext<ControlPanelDN>())
    {
        Html.EntityLine(tc, cp => cp.Related);
        Html.ValueLine(tc, cp => cp.DisplayName);
        Html.ValueLine(tc, cp => cp.HomePage);
        Html.ValueLine(tc, cp => cp.NumberOfColumns);
        Html.EntityRepeater(tc, cp => cp.Parts);
    }
%>