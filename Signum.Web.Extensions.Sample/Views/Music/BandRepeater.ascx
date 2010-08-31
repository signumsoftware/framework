<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Test" %>

<%
using (var e = Html.TypeContext<BandDN>()) 
{
	Html.ValueLine(e, f => f.Name);
	Html.EntityRepeater(e, f => f.Members, er => er.Find = true);
    Html.EntityLine(e, f => f.LastAward);
    Html.EntityRepeater(e, f => f.OtherAwards);
}
%>