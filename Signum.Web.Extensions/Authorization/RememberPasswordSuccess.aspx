<%@Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="rememberPasswordSuccessContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2><%=ViewData["Title"]%></h2>
    <p><%=ViewData["Message"]%></p>
    <a href="Auth/Login">Volver a la página de identificación</a>
 
</asp:Content>
