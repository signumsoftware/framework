<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reports" %>
<%@ Import Namespace="Signum.Entities.Basics" %>

<%
using (var e = Html.TypeContext<UserQueryDN>()) 
{
    Html.EntityLine(e, f => f.Related, el => el.Create = false);
    using (var query = e.SubContext(f => f.Query))
    {
        Html.WriteEntityInfo(query);
        %>
        <%= Html.Span("Query", "Query", "labelLine") %>
        <%= Html.Href("hrefQuery", query.Value.DisplayName, Navigator.FindRoute(ViewData[ViewDataKeys.QueryName]), "", "valueLine", null) %>
        
        <div class="clearall"></div>
        
        <%= Html.Hidden(query.Compose("Key"), query.Value.Key)%>
        <%= Html.Hidden(query.Compose("DisplayName"), query.Value.DisplayName)%>
    <%
    }
    Html.ValueLine(e, f => f.DisplayName);
	%>
	<br />
	<%    
        Html.EntityRepeater(e, f => f.Filters, er => { er.PreserveViewData = true; er.ForceNewInUI = true; });
    %>
	<br />
	<%
        Html.ValueLine(e, f => f.ColumnsMode);
        Html.EntityRepeater(e, f => f.Columns, er => { er.PreserveViewData = true; er.ForceNewInUI = true; });
    %>
	<br />
	<%    
        Html.EntityRepeater(e, f => f.Orders, er => { er.PreserveViewData = true; er.ForceNewInUI = true; });
    %>
	<br />
	<%   
    Html.ValueLine(e, f => f.MaxItems);
}
%>
