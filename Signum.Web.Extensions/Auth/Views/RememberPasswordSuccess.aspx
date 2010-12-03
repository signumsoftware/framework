<%@Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h2><%=ViewData["Title"]%></h2>
    <p><%=ViewData["Message"]%></p>
    <a href="Auth/Login"><%:Resources.GoBackToLoginPage %></a>
</asp:Content>
