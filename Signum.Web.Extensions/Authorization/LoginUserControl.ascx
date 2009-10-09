<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%
    if (Session["user"] != null) 
    {
%>
        Usuario: <b><%=Html.Encode(Session["user"])%></b>
        [ <%= Html.ActionLink("Logout", "Logout", "Auth") %> ]
<%
    }
    else {
%> 
        [ <%= Html.ActionLink("Login", "Login", "Auth")%> ]
<%
    }
%>
