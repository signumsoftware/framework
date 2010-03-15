<%@Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="changePasswordSuccessContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2><%=ViewData["Title"]%></h2>
    <p>
        <%=ViewData["Message"]%>
    </p>
</asp:Content>
