<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Web.Extensions.Sample" %>

<%
using (var e = Html.TypeContext<AlbumFromBandModel>()) 
{
	Html.EntityLine(e, f => f.Band);
	Html.ValueLine(e, f => f.Name);
	Html.ValueLine(e, f => f.Year);
	Html.EntityLine(e, f => f.Label);
}
%>
