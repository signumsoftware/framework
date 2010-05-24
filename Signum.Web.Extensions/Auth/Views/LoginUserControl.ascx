<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%@ Import Namespace="Signum.Web.Authorization" %>
<%
    if (Session[AuthController.SessionUserKey] != null) 
    {
%>
        Usuario: <b><%=Html.Encode(Session[AuthController.SessionUserKey])%></b>
        [ <%= Html.ActionLink("Logout", "Logout", "Auth") %> ]
<%
    }
    else {
%> 
        [ <%= Html.ActionLink("Login", "Login", "Auth")%> ]
<%
    }
%>
