<%@Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h2><%=ViewData["Title"]%></h2>
    <p><%=ViewData["Message"]%></p>
    <a href="<%= Url.Action("Login", "Auth") %>"><%:Resources.GoBackToLoginPage %></a>
</asp:Content>
