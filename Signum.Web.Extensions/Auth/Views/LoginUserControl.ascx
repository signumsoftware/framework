<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%@ Import Namespace="Signum.Web.Authorization" %>
<%
    if (Session[AuthController.SessionUserKey] != null) 
    {
%>
        Usuario: <span class="username"><%=Html.Encode(Session[AuthController.SessionUserKey])%></span>
        <span class="separator">|</span><%= Html.ActionLink("Logout", "Logout", "Auth", null, new {@class = "logout" })%>
<%
    }
    else {
%> 
        <%= Html.ActionLink("Login", "Login", "Auth", null, new {@class = "login" })%>
<%
    }
%>
