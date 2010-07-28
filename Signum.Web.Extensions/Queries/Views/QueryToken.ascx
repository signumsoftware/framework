<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reports" %>
<%@ Import Namespace="Signum.Web.Queries.Models" %>

<%
using (var e = Html.TypeContext<QueryTokenModel>()) 
{
    %>
    <%= Html.WriteQueryToken(e.Value.QueryUrlName, e.Value.QueryToken.Token, e, 0) %>
    <%
    //Html.ValueLine(e, f => f.DisplayName, f => f.ReadOnly = true);
    //Html.EntityRepeater(e, f => f.Filters);
    //Html.EntityRepeater(e, f => f.Columns);
    //Html.EntityList(e, f => f.Orders);
    //Html.ValueLine(e, f => f.MaxItems);
}
%>
