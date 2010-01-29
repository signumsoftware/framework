<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Test.LinqProvider" %>

<%
using (var e = Html.TypeContext<PersonalAwardDN>()) 
{
	Html.ValueLine(e, f => f.Year);
	Html.ValueLine(e, f => f.Category);
	Html.ValueLine(e, f => f.Result);
}
%>
