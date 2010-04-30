<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div id="reset-password-container">    
        <h2>Password reset successfully</h2>
        <p>Your password has been changed successfully. Please, <%= Html.ActionLink("login", "auth", "login") %> into your account.</p>    
    </div>
</asp:Content>