<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Test" %>

<%
using (var e = Html.TypeContext<NoteDN>()) 
{
    Html.ValueLine(e, f => f.CreationTime, vl => vl.DatePickerOptions = new DatePickerOptions { ChangeYear = false, Format = "dd/MM/yyyy" });
	Html.EntityLine(e, f => f.Target);
    Html.ValueLine(e, f => f.Text);
}
%>