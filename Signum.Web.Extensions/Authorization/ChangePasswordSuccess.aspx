<%@Language="C#" MasterPageFile="~/Views/Shared/Site.Master" AutoEventWireup="true" CodeBehind="ChangePasswordSuccess.aspx.cs" Inherits="TrazadorMVC.Views.Account.ChangePasswordSuccess" %>

<asp:Content ID="changePasswordSuccessContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2><%=ViewData["Titulo"]%> %></h2>
    <p>
        <%=ViewData["Mensaje"] %>
    </p>
</asp:Content>
