<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Test" %>

<%
using (var e = Html.TypeContext<AlbumDN>()) 
{
	Html.ValueLine(e, f => f.Name);
	Html.ValueLine(e, f => f.Year);
	Html.EntityLine(e, f => f.Author);
	Html.EntityList(e, f => f.Songs);
	Html.EntityCombo(e, f => f.Label);
}
%>
