<%@Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h2><%=ViewData["Title"]%></h2>
    <p><%=ViewData["Message"]%></p>
    <a href="Auth/Login">Go back to login page</a>
</asp:Content>
