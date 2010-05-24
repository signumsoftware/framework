<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Processes" %>

<%
using (var e = Html.TypeContext<ProcessDN>()) 
{
	Html.ValueLine(e, f => f.Name);
	Html.ValueLine(e, f => f.Key, f => f.ReadOnly = true);
	Html.ValueLine(e, f => f.IdOrNull, f => f.ReadOnly = true);
}
%>
