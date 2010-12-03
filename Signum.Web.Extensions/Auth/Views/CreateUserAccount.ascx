<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<p>
<%: Resources.WeReceivedARequestToCreateAnAccountYouCanCreateItFollowingTheLinkBelow %>
</p>
<% if (ViewData.ContainsKey("Link")) { %>
<%= Html.Href(ViewData["link"].ToString(), ViewData["link"].ToString())%>
<% } %>
