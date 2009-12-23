<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Test" %>

<%
using (var e = Html.TypeContext<ArtistDN>()) 
{
	Html.ValueLine(e, f => f.Name);
	Html.ValueLine(e, f => f.Dead);
	Html.ValueLine(e, f => f.Sex);
	Html.ValueLine(e, f => f.IsMale);
	Html.EntityLine(e, f => f.LastAward);
	Html.EntityList(e, f => f.Friends);
}
%>
