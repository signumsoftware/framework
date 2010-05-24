<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Processes" %>

<%
using (var e = Html.TypeContext<PackageLineDN>()) 
{
	Html.EntityLine(e, f => f.Package, f => f.ReadOnly = true);
	Html.EntityLine(e, f => f.Target, f => f.ReadOnly = true);
	Html.EntityLine(e, f => f.Result, f => f.ReadOnly = true);
	Html.ValueLine(e, f => f.FinishTime, f => f.ReadOnly = true);
	Html.ValueLine(e, f => f.Exception, f => f.ReadOnly = true);
	Html.ValueLine(e, f => f.IdOrNull, f => f.ReadOnly = true);
}
%>
