<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Mailing" %>

<%
using (var e = Html.TypeContext<EmailMessageDN>()) 
{
	Html.EntityLine(e, f => f.Recipient);
	Html.EntityLine(e, f => f.Template, f => f.ReadOnly = true);
	Html.ValueLine(e, f => f.Sent, f => f.ReadOnly = true);
	Html.ValueLine(e, f => f.Received, f => f.ReadOnly = true);
	Html.ValueLine(e, f => f.Exception, f => f.ReadOnly = true);
	Html.ValueLine(e, f => f.State, f => f.ReadOnly = true);
	Html.EntityLine(e, f => f.Package, f => f.ReadOnly = true);
    Html.ValueLine(e, f => f.Subject);
    
    %>
    <h3>Message:</h3>
    <div>
    <%= e.Value.Body%>
    </div>
    <%
    
}
%>
