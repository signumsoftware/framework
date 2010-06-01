<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <script type="text/javascript"> $(function() { $("#email").focus(); }); </script>

    <div id="reset-password-container">    
        <h2><%=Html.Encode(Resources.ResetPassword) %></h2>
        <p><%=Html.Encode(Resources.ForgotYourPassword) %></p>
        
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