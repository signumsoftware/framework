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
	Html.EntityListDetail(e, f => f.Members);
    Html.EntityLineDetail(e, f => f.LastAward);
    Html.EntityListDetail(e, f => f.OtherAwards);
}
%>