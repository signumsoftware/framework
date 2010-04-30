<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <script type="text/javascript"> $(function() { $("#email").focus(); }); </script>

    <div id="reset-password-container">    
        <h2>Reset password</h2>
        <p>Forgot your password? Enter your login email below. We will send you an email with a link to reset your password.</p>
        
        <%= Html.ValidationSummary() %>
        <% using (Html.BeginForm()) { %>
        <div id="reset-password-form">
            <label for="email">Email</label>:
            <%= Html.TextBox("email", "", new { size = 30 })%>
            <%= Html.ValidationMessage("email") %>
             <input type="submit" />
        </div>
        <% } %>
    </div>
</asp:Content>