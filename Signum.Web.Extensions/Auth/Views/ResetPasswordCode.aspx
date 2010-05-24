<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <script type="text/javascript"> $(function() { $("#email").focus(); }); </script>

    <div id="reset-password-container">    
        <h2>Email sent</h2>
        <p>An email has been sent to your account  <%= ViewData["email"] %> with a confirmation code.</p>
        <p>Please, enter the confirmation code.</p>
        
        <%= Html.ValidationSummary() %>
        <% using (Html.BeginForm()) { %>
        <div id="reset-password-code-form">
            <label for="code">Confirmation code to password reset</label>:
            <%= Html.TextBox("code", "", new { size = 30 })%>
            <%= Html.ValidationMessage("code")%>
             <input type="submit" />
        </div>
        <% } %>
    </div>
</asp:Content>