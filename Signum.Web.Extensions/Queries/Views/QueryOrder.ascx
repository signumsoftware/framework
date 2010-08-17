<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reports" %>
<%@ Import Namespace="Signum.Web.Queries.Models" %>

<%
using (var e = Html.TypeContext<QueryOrderModel>()) 
{
    using (var style = e.SubContext())
    {
        style.OnlyValue = true;
    %>
    <div style="float:left">
    <%
        using (var queryToken = e.SubContext(f => f.QueryToken))
        {
            Html.WriteEntityInfo(queryToken); %>
            <%= Html.WriteQueryToken(queryToken.Value.QueryNameToStr, queryToken.Value.QueryToken.Token, queryToken, 0)%>
    <% } %>
    </div>
    <%  
        Html.ValueLine(style, f => f.OrderType);
    }
}
%>

